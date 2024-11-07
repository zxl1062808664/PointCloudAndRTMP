using System.Buffers;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using ROS2;
using sensor_msgs.msg;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace ZC
{
    public class RenderS : MonoBehaviour
    {
        [SerializeField] int frameRate = 5;
        [SerializeField] int _currentFrameCount;
        [SerializeField] int rayCount;
        [SerializeField] [Header("需要检测的层级")] private LayerMask toHitMask;
        [SerializeField] [Header("水平射线数")] private int horizontalRaysCount = 400;

        [SerializeField] [Header("垂直射线数")] private int verticalRaysCount = 375;

        // [SerializeField] private Mesh _mesh;
        // [SerializeField] private Material _mat;
        [SerializeField] private Camera _camera;
        // private PointsDrawer _pointsDrawer;

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
        // [SerializeField] private Bounds _bounds;

        private NativeArray<int> _hitCountArray;

        private void Start()
        {
            ros2Unity = GetComponent<ROS2UnityComponent>();

            rayCount = horizontalRaysCount * verticalRaysCount;
            // _pointsDrawer = new PointsDrawer();
            // _pointsDrawer.Setup(_mesh, rayCount, 1000, _mat);
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
            // _pointsDrawer.OnDispose();
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

        ROS2UnityComponent ros2Unity;
        ROS2Node ros2Node;

        private void Update()
        {
            if (ros2Unity.Ok())
            {
                if (ros2Node == null)
                {
                    ros2Node = ros2Unity.CreateNode("ROS2UnityTalkerNode");
                    chatter_pub = ros2Node.CreatePublisher<PointCloud2>("chatter");
                }
            }

            RayTest();
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
                frameFullInfo = flattenedPointsFullInfo,
                transformPosition = transformPosition
            };

            var handle4 = removeInvalidResultJob.Schedule(dependsOn: handle3);
            handle4.Complete();
            var hitCount = _hitCountArray[0];

            if (_isSaveData)
                SaveData();

            PublishData(flattenedPointsInfo, hitCount);
        }

        private void PublishData(NativeArray<ZPointInfo> frameInfos, int hitCount)
        {
            Debug.Assert(chatter_pub != null, nameof(chatter_pub) + " != null");
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

                ZPointInfoSerializer.Serialize(frameInfos,hitCount,_camera.transform.position,_camera.transform.rotation,out var buffer);
                // byte[] buffer = ArrayPool<byte>.Shared.Rent(sizeof(float) * 4 * hitCount);
                s.Data = buffer;

                // var dest = UnsafeUtility.PinGCArrayAndGetDataAddress(buffer, out var handle);
                // try
                // {
                //     var src = frameInfos.GetUnsafePtr();
                //     UnsafeUtility.MemCpy(dest, src, sizeof(float) * 4 * hitCount);
                // }
                // finally
                // {
                //     UnsafeUtility.ReleaseGCObject(handle);
                // }

                chatter_pub.Publish(s);
                // ArrayPool<byte>.Shared.Return(buffer);
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

    public class PointData : Message
    {
        public NativeArray<ZPointInfo> flattenedPointsInfo;
        public int count;

        public void Dispose()
        {
            flattenedPointsInfo.Dispose();
        }

        public bool IsDisposed { get; }
    }
}