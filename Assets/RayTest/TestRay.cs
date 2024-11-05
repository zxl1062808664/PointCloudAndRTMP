using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
        public ParticleSystem _particleSystem;
        private PointsDrawer _pointsDrawer;

        private NativeArray<RaycastHit> results;
        private NativeArray<ParticleSystem.Particle> particles;
        private NativeArray<RaycastCommand> commands;
        private NativeArray<TransData> rayCaster;
        private NativeArray<ZPointInfo> flattenedPointsInfo;
        private NativeArray<CvsData> cvsDatas;

        private bool _isRunning;

        private List<Renderer> _renderers = new List<Renderer>(150000);

        public void AddRenderer(Renderer renderer)
        {
            this._renderers.Add(renderer);
        }

        private void Start()
        {
            //        var o = new GameObject();
            //        for (var i = 0; i < 10000; i++)
            //        {
            //            var insideUnitSphere = UnityEngine.Random.insideUnitSphere * 100;
            //            var primitive = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Cube);
            //            primitive.transform.position = insideUnitSphere;
            //            primitive.transform.parent = o.transform;
            //            _renderers.Add(primitive.GetComponent<Renderer>());
            //        }


            rayCount = horizontalRaysCount * verticalRaysCount;
            _pointsDrawer = new PointsDrawer();
            _pointsDrawer.Setup(_mesh, rayCount, 1000, new Bounds(default, Vector3.one * (1000 + 1)), _mat);
            results = new NativeArray<RaycastHit>(rayCount, Allocator.Persistent);
            commands = new NativeArray<RaycastCommand>(rayCount, Allocator.Persistent);
            rayCaster = new NativeArray<TransData>(rayCount, Allocator.Persistent);
            particles = new NativeArray<ParticleSystem.Particle>(rayCount, Allocator.Persistent);
            flattenedPointsInfo = new NativeArray<ZPointInfo>(rayCount, Allocator.Persistent); // 初始化存储点信息的列表，预先分配空间
            cvsDatas = new NativeArray<CvsData>(rayCount, Allocator.Persistent); // 初始化存储点信息的列表，预先分配空间
            StartCoroutine(InitRays());
        }

        private void OnDestroy()
        {
            _pointsDrawer.OnDispose();
            results.Dispose();
            commands.Dispose();
            rayCaster.Dispose();
            particles.Dispose();
            flattenedPointsInfo.Dispose();
            cvsDatas.Dispose();
            StopAllCoroutines();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log("Start");
                this._isRunning = true;
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                Debug.Log("Stop");
                this._isRunning = false;
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                Debug.Log("Stop");
                this.isSave = !isSave;
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                foreach (var renderer1 in this._renderers)
                {
                    renderer1.enabled = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                foreach (var renderer1 in this._renderers)
                {
                    renderer1.enabled = true;
                }
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
            Profiler.BeginSample("ExecuteJobs");
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


            HandleHitResultJob jJob = new HandleHitResultJob
            {
                rayCaster = this.rayCaster,
                hitResults = this.results,
                frameInfo = this.flattenedPointsInfo,
                cvsDatas = this.cvsDatas,
                parentRot = transformRotation,
                parentPos = transformPosition,
            };

            var handle3 = jJob.Schedule(this.rayCount, 1, dependsOn: handle2);
            handle3.Complete();
            Profiler.EndSample();
            Profiler.BeginSample("RenderPoints");
            // _pointsDrawer.Render(flattenedPointsInfo);
            SaveData();
            Profiler.EndSample();
        }

        private bool isSave;
        void SaveData()
        {
            isSave = false;
            var list = cvsDatas.ToList();
            
            // string testCsvPath = $"{Application.streamingAssetsPath}/point.csv";
            // CsvUtility.Write(list, testCsvPath);
            
            string testXyzPath = $"{Application.streamingAssetsPath}/point.xyz";
            StringBuilder sb = new StringBuilder();
            foreach (var cvsData in list)
            {
                sb.AppendLine($"{cvsData.x} {cvsData.y} {cvsData.z}");
            }
            File.WriteAllText(testXyzPath,sb.ToString());
            sb = null;
        }

        #region 粒子

        public void RenderPointsCloud(NativeArray<ZPointInfo> frame, int renderCount)
        {
            unsafe
            {
                Profiler.BeginSample("RenderPointsCloud.Emit");
                this._particleSystem.Emit(renderCount); // 发射pointCount个粒子
                Profiler.EndSample();
                Profiler.BeginSample("RenderPointsCloud.GetParticles");
                this._particleSystem.GetParticles(this.particles); // 获取当前还存活的粒子数
                Profiler.EndSample();

                Profiler.BeginSample("RenderPointsCloud.Clear");
                var asSpan = this.particles.AsSpan();
                UnsafeUtility.MemClear(
                    (void*)((nint)UnsafeUtility.AddressOf(ref asSpan[0]) +
                            renderCount * UnsafeUtility.SizeOf<ParticleSystem.Particle>()),
                    UnsafeUtility.SizeOf<ParticleSystem.Particle>() * (asSpan.Length - renderCount)
                );
                Profiler.EndSample();

                Profiler.BeginSample("RenderPointsCloud.Copy");
                for (int i = 0; i < renderCount; i++)
                {
                    var zPointInfo = frame[i];
                    ref var particle = ref this.particles.AsSpan()[i];
                    particle.position = zPointInfo.point;
                    particle.startSize = 0.05f;
                }

                Profiler.EndSample();

                Profiler.BeginSample("RenderPointsCloud.Apply");
                this._particleSystem.SetParticles(this.particles, renderCount); // 将点云载入粒子系统
                Profiler.EndSample();
            }
        }

        #endregion
    }

    struct CvsData
    {
        public float x;
        public float y;
        public float z;
        public float dir;

        public CvsData(float x, float y, float z, float dir)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.dir = dir;
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

        public NativeArray<ZPointInfo> frameInfo;
        public NativeArray<CvsData> cvsDatas;

        public void Execute(int i)
        {
            RaycastHit hit = this.hitResults[i];
            var isHit = hit.colliderInstanceID != 0;
            if (!isHit)
            {
                this.frameInfo[i] = new ZPointInfo(default, 0, false);
                cvsDatas[i] = new CvsData(0, 0, 0, 0);
                return; // 若未命中碰撞体，则跳过
            }

            var transData = this.rayCaster[i];
            var quaternion = math.mul(parentRot, transData.localRotation);
            var forward = math.mul(quaternion, math.forward());
            var dir = math.abs(math.dot(hit.normal, forward));
            this.frameInfo[i] = new ZPointInfo(hit.point, dir, true);
            this.cvsDatas[i] = new CvsData(hit.point.x, hit.point.y, hit.point.z, dir);
            //                UnsafeUtility.MemCpy(UnsafeUtility.AddressOf(ref frameSpan[i]), UnsafeUtility.AddressOf(ref hit), sizeof(Vector3));
        }
    }

    #region mD

    [StructLayout(LayoutKind.Sequential)]
    public struct ZPointInfo
    {
        private float3 _point;
        private float _dir;
        private bool _isDraw;

        public ZPointInfo(float3 point, float dir, bool isDraw)
        {
            _point = point;
            _dir = dir;
            _isDraw = isDraw;
        }

        public float3 point
        {
            get => this._point;
            set => this._point = value;
        }

        public bool isDraw
        {
            get => this._isDraw;
            set => this._isDraw = value;
        }

        public float dir => _dir;
    }

    #endregion
}