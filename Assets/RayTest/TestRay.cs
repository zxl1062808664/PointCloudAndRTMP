using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ROS2;
using sensor_msgs.msg;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using zFramework.Extension;
using Debug = UnityEngine.Debug;

namespace ZC
{
    public class TestRay : MonoBehaviour
    {
        [SerializeField] int frameRate = 5;
        [SerializeField] int _currentFrameCount;
        [SerializeField] int rayCount;
        [SerializeField] [Header("需要检测的层级")] private LayerMask toHitMask;
        [SerializeField] [Header("水平射线数")] private int horizontalRaysCount = 400;
        [SerializeField] [Header("垂直射线数")] private int verticalRaysCount = 375;
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _mat;
        [SerializeField] private Camera _camera;
        private PointsDrawer _pointsDrawer;

        private NativeArray<RaycastHit> results;
        private NativeArray<RaycastCommand> commands;
        private NativeArray<TransData> rayCaster;
        private NativeArray<ZPointInfo> flattenedPointsInfo;
        private NativeArray<PointCvsData> cvsDatas;
        private NativeArray<ZPointFullInfo> flattenedPointsFullInfo;

        private bool _isRunning;

        private IPublisher<PointCloud2> chatter_pub;

        string[] names = new[]
        {
            "x",
            "y",
            "z",
            "s",
        };

        PointCloud2 s;
        [SerializeField] private Bounds _bounds;

        private NativeArray<int> _hitCountArray;

        
        private void Start()
        {
            rayCount = horizontalRaysCount * verticalRaysCount;
            _pointsDrawer = new PointsDrawer();
            _pointsDrawer.Setup(_mesh, rayCount, 1000, _mat);
            results = new NativeArray<RaycastHit>(rayCount, Allocator.Persistent);
            commands = new NativeArray<RaycastCommand>(rayCount, Allocator.Persistent);
            rayCaster = new NativeArray<TransData>(rayCount, Allocator.Persistent);
            _hitCountArray = new NativeArray<int>(1, Allocator.Persistent);
            flattenedPointsInfo = new NativeArray<ZPointInfo>(rayCount, Allocator.Persistent); // 初始化存储点信息的列表，预先分配空间
            cvsDatas = new NativeArray<PointCvsData>(rayCount, Allocator.Persistent); // 初始化存储点信息的列表，预先分配空间
            flattenedPointsFullInfo =
                new NativeArray<ZPointFullInfo>(rayCount, Allocator.Persistent); // 初始化存储点信息的列表，预先分配空间
            StartCoroutine(InitRays());
        }

        private void OnDestroy()
        {
            _pointsDrawer.OnDispose();
            results.Dispose();
            commands.Dispose();
            rayCaster.Dispose();
            flattenedPointsInfo.Dispose();
            cvsDatas.Dispose();
            _hitCountArray.Dispose();
            flattenedPointsFullInfo.Dispose();
            StopAllCoroutines();
            chatter_pub.Dispose();
            
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.M))
            {
                _isSaveData = !_isSaveData;
            }

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad0))
            {
                _isShowPointsData = !_isShowPointsData;
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                this._isRunning = true;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                this._isRunning = false;
            }

            if (this._isRunning)
            {
                RayTest();
            }
        }

        /// <summary>
        /// 将垂直FOV转成水平FOV
        /// </summary>
        /// <param name="verFOV">垂直FOV值</param>
        /// <param name="aspect">屏幕分辨率比例</param>
        /// <returns></returns>
        public static float ConvertVerticalFOVToHorizontally(float verFOV, float aspect)
        {
            // 垂直fov角度转成弧度
            float verFovRadian = verFOV * Mathf.Deg2Rad;
            // 算出视野高度的一半
            float camHalfHeight = Mathf.Tan(verFovRadian / 2);
            // 算出水平视野的弧度
            float horFOVRadian = Mathf.Atan(camHalfHeight * aspect) * 2;
            // 将水平视野弧度转成角度
            float horFOV = horFOVRadian * Mathf.Rad2Deg;

            return horFOV;
        }

        IEnumerator InitRays()
        {
            var horizontalFov = ConvertVerticalFOVToHorizontally(this._camera.fieldOfView, this._camera.aspect);
            var verticalFov = this._camera.fieldOfView;

            float halfXAngle = horizontalFov / 2.0f;
            float halfYAngle = verticalFov / 2.0f;


            int index = 0;
            for (int i = 0; i < this.horizontalRaysCount; i++)
            {
                float angleDeltaX = Mathf.Lerp(-halfXAngle, halfXAngle, i * 1f / horizontalRaysCount);
                for (int j = 0; j < this.verticalRaysCount; j++)
                {
                    float angleDeltaY = Mathf.Lerp(-halfYAngle, halfYAngle, j * 1f / verticalRaysCount);

                    rayCaster[index] = new TransData()
                    {
                        localPosition = Vector3.zero,
                        localRotation = Quaternion.Euler(-angleDeltaY, angleDeltaX, 0),
                    };
                    index++;
                }
            }

            yield return null;
        }

        void RayTest()
        {
            float3 transformPosition = this._camera.transform.position;
            quaternion transformRotation = this._camera.transform.rotation;
            BuildRaycastCommandJob rJob = new BuildRaycastCommandJob
            {
                rayCaster = this.rayCaster,
                commands = this.commands,
                queryParameters = new QueryParameters(this.toHitMask, false, QueryTriggerInteraction.Ignore, false),
                parentPos = transformPosition,
                parentRot = transformRotation
            };
            var handle1 = rJob.Schedule(this.rayCount, 1);
            JobHandle handle2 =
                RaycastCommand.ScheduleBatch(rJob.commands, results, 100, dependsOn: handle1); //调度批量射线投射命令
            _hitCountArray[0] = 0;
            HandleHitResultJob jJob = new HandleHitResultJob
            {
                rayCaster = this.rayCaster,
                hitResults = this.results,
                parentRot = transformRotation,
                parentPos = transformPosition,
                frameFullInfo = flattenedPointsFullInfo
            };
            var handle3 = jJob.Schedule(results.Length, 1, dependsOn: handle2);
            RemoveInvalidResultJob removeInvalidResultJob = new RemoveInvalidResultJob()
            {
                frameInfo = this.flattenedPointsInfo,
                cvsDatas = this.cvsDatas,
                hitCount = _hitCountArray,
                frameFullInfo = flattenedPointsFullInfo
            };

            var handle4 = removeInvalidResultJob.Schedule(dependsOn: handle3);
            handle4.Complete();
            var hitCount = _hitCountArray[0];
            if (_isShowPointsData)
            {
                _bounds = new Bounds(transformPosition, Vector3.one * (1000 + 1));
                _pointsDrawer.Render(flattenedPointsInfo, hitCount, _bounds, transformPosition,_camera.transform.rotation);
            }

            if (_isSaveData)
                SaveData();

            PublishData(flattenedPointsInfo, hitCount);
        }

        private void PublishData(NativeArray<ZPointInfo> frameInfos, int hitCount)
        {
            unsafe
            {
                s = new PointCloud2();

                s.Width = (uint)hitCount;
                s.Height = 1;
                if (s.Fields == null)
                {
                    s.Fields = new PointField[]
                    {
                        new PointField
                        {
                            Name = names[0],
                            Offset = 0,
                            Datatype = PointField.FLOAT32,
                            Count = 1
                        },
                        new PointField
                        {
                            Name = names[1],
                            Offset = 4,
                            Datatype = PointField.FLOAT32,
                            Count = 1
                        },
                        new PointField
                        {
                            Name = names[2],
                            Offset = 8,
                            Datatype = PointField.FLOAT32,
                            Count = 1
                        },
                        new PointField
                        {
                            Name = names[3],
                            Offset = 12,
                            Datatype = PointField.FLOAT32,
                            Count = 1
                        }
                    };
                }

                s.Is_dense = false;
                s.Point_step = 16;
                s.Row_step = s.Width * s.Point_step;

                byte[] buffer = ArrayPool<byte>.Shared.Rent((int)s.Row_step);
                s.Data = buffer;

                var dest = UnsafeUtility.PinGCArrayAndGetDataAddress(buffer, out var handle);
                try
                {
                    var src = frameInfos.GetUnsafePtr();
                    UnsafeUtility.MemCpy(dest, src, sizeof(float) * 4 * hitCount);
                }
                finally
                {
                    UnsafeUtility.ReleaseGCObject(handle);
                }

                chatter_pub.Publish(s);
                ArrayPool<byte>.Shared.Return(buffer);
                s.Dispose();
            }
        }

        private string testXyzPath = Application.streamingAssetsPath + "/point.xyz";

        [SerializeField] private bool _isSaveData;
        [SerializeField] private bool _isShowPointsData;

        void SaveData()
        {
            _isSaveData = false;
            var list = cvsDatas.ToList();

            // string testCsvPath = $"{Application.streamingAssetsPath}/point.csv";
            // CsvUtility.Write(list, testCsvPath);

            StringBuilder sb = new StringBuilder();
            foreach (var cvsData in list)
            {
                sb.AppendLine($"{cvsData.x} {cvsData.y} {cvsData.z}");
            }

            File.WriteAllText(testXyzPath, sb.ToString());
            sb = null;
        }
    }

    [BurstCompile]
    struct BuildRaycastCommandJob : IJobParallelFor
    {
        [ReadOnly] public QueryParameters queryParameters;
        [ReadOnly] public NativeArray<TransData> rayCaster;
        [ReadOnly] public float3 parentPos;
        [ReadOnly] public quaternion parentRot;

        public NativeArray<RaycastCommand> commands;

        public void Execute(int index)
        {
            var transData = rayCaster[index];
            //        var position = math.rotate(parentRot, transData.localPosition) + parentPos;
            var position = this.parentPos;
            var rotation = math.mul(parentRot, transData.localRotation);
            var forward = math.mul(rotation, math.forward());
            commands[index] = new RaycastCommand(position, forward, this.queryParameters, 1000); // 准备 RaycastCommand
        }
    }

    public struct TransData
    {
        public float3 localPosition;
        public quaternion localRotation;
    }

    [BurstCompile]
    struct HandleHitResultJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<RaycastHit> hitResults;
        [ReadOnly] public NativeArray<TransData> rayCaster;
        [ReadOnly] public float3 parentPos;
        [ReadOnly] public quaternion parentRot;

        public NativeArray<ZPointFullInfo> frameFullInfo;

        public void Execute(int i)
        {
            RaycastHit hit = this.hitResults[i];
            var isHit = hit.colliderInstanceID != 0;
            if (!isHit)
            {
                this.frameFullInfo[i] = new ZPointFullInfo(default, default, false);
                return; // 若未命中碰撞体，则跳过
            }

            var transData = this.rayCaster[i];
            var quaternion = math.mul(parentRot, transData.localRotation);
            var forward = math.mul(quaternion, math.forward());
            var dir = math.abs(math.dot(hit.normal, forward));
            this.frameFullInfo[i] = new ZPointFullInfo(hit.point, dir, true);
            //                UnsafeUtility.MemCpy(UnsafeUtility.AddressOf(ref frameSpan[i]), UnsafeUtility.AddressOf(ref hit), sizeof(Vector3));
        }
    }


    [BurstCompile]
    struct RemoveInvalidResultJob : IJob
    {
        [ReadOnly] public NativeArray<ZPointFullInfo> frameFullInfo;
        public NativeArray<ZPointInfo> frameInfo;
        public NativeArray<PointCvsData> cvsDatas;

        public NativeArray<int> hitCount;
        public float3 transformPosition;

        public void Execute()
        {
            for (var i = 0; i < frameFullInfo.Length; i++)
            {
                ZPointFullInfo info = this.frameFullInfo[i];
                // info.point -= transformPosition;
                if (info.isvaild)
                {
                    cvsDatas[hitCount[0]] = new PointCvsData(info.point.x, info.point.y, info.point.z, info.dir);
                    frameInfo[hitCount[0]++] = new ZPointInfo(info.point, info.dir);
                }
            }
        }
    }


    #region mD

    public struct ZPointFullInfo
    {
        private float3 _point;
        private float _dir;
        private bool _isvaild;

        public ZPointFullInfo(float3 point, float dir, bool isvaild)
        {
            _point = point;
            _dir = dir;
            this._isvaild = isvaild;
        }

        public float3 point
        {
            get => this._point;
            set => this._point = value;
        }


        public float dir => _dir;

        public bool isvaild => _isvaild;
    }

    [System.Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct ZPointInfo
    {
        [FieldOffset(0)] public float3 _point;
        [FieldOffset(12)] public float _dir;

        public ZPointInfo(float3 point, float dir)
        {
            _point = point;
            _dir = dir;
        }

        public float3 point
        {
            get => this._point;
            set => this._point = value;
        }


        public float dir => _dir;
    }

    #endregion

    struct PointCvsData
    {
        public float x;
        public float y;
        public float z;
        public float dir;

        public PointCvsData(float x, float y, float z, float dir)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.dir = dir;
        }
    }
}