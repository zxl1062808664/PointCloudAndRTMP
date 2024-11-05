using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ZC
{
    public class PointsDrawer
    {
        private static readonly int id_properties = Shader.PropertyToID("_Properties");
        private int population;
        private float range;
        private Mesh mesh;
        private Material material;

        private ComputeBuffer meshPropertiesBuffer;
        private ComputeBuffer argsBuffer;
        private NativeArray<MeshProperties> _properties;
        private Unity.Mathematics.Random _random;

        [StructLayout(LayoutKind.Explicit, Size = 80)]
        private struct MeshProperties
        {
            [FieldOffset(0)] public float4x4 mat;
            [FieldOffset(64)] public float4 color;

            public static int Size()
            {
                return sizeof(float) * 4 * 4 + sizeof(float) * 4;
            }
        }

        public void Setup(Mesh mesh, int population, float range ,Material material)
        {
            this.mesh = mesh;
            this.population = population;
            this.range = range;
            this.material = material;

            InitializeBuffers();
        }

        private void InitializeBuffers()
        {
            // Argument buffer used by DrawMeshInstancedIndirect.
            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            // Arguments for drawing mesh.
            // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
            args[0] = (uint)mesh.GetIndexCount(0);
            args[1] = (uint)population;
            args[2] = (uint)mesh.GetIndexStart(0);
            args[3] = (uint)mesh.GetBaseVertex(0);
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            this._properties = new NativeArray<MeshProperties>(this.population, Allocator.Persistent);
            this._random = Unity.Mathematics.Random.CreateFromIndex(12345);
            //        for (int i = 0; i < population; i++)
            //        {
            //            MeshProperties props = new MeshProperties();
            //            Vector3 position = new Vector3(this._random.NextFloat(-range, range), this._random.NextFloat(-range, range), this._random.NextFloat(-range, range));
            //            Quaternion rotation = Quaternion.Euler(this._random.NextFloat(-180, 180), this._random.NextFloat(-180, 180), this._random.NextFloat(-180, 180));
            //            Vector3 scale = Vector3.one;
            //
            //            props.mat = Matrix4x4.TRS(position, rotation, scale);
            //            props.color = Color.Lerp(Color.red, Color.blue, Random.value);
            //
            //            this._properties[i] = props;
            //        }

            meshPropertiesBuffer = new ComputeBuffer(population, MeshProperties.Size());
            //        meshPropertiesBuffer.SetData(this._properties);
            //        material.SetBuffer(id_properties, meshPropertiesBuffer);
        }

        public void OnDispose()
        {
            if (meshPropertiesBuffer != null)
            {
                meshPropertiesBuffer.Release();
            }

            meshPropertiesBuffer = null;

            if (argsBuffer != null)
            {
                argsBuffer.Release();
            }

            argsBuffer = null;
            this._properties.Dispose();
        }
        public void Render(NativeArray<ZPointInfo> frames, int hitCount,Bounds bounds, float3 position)
        {
            TransformJob job = new TransformJob()
            {
                properties = this._properties,
                frames = frames,
                count = hitCount,
                position=position,
            };
            job.Schedule(_properties.Length, 1).Complete();

            meshPropertiesBuffer.SetData(this._properties);

            material.SetBuffer(id_properties, meshPropertiesBuffer);

            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, bufferWithArgs: argsBuffer, receiveShadows: false, castShadows: ShadowCastingMode.Off);
        }

        [BurstCompile]
        struct TransformJob : IJobParallelFor
        {
            public NativeArray<MeshProperties> properties;
            public NativeArray<ZPointInfo> frames;
            public int count;
            public float3 position;

            public void Execute(int i)
            {
                MeshProperties props = new MeshProperties();
                if (i >= count)
                {
                    // props.color = math.float4(0, 0, 0, 0);
                    props.color = math.float4(1, 0, 0, 1);
                    properties[i] = props;
                    return;
                }
                var frame = this.frames[i];
                float3 localPosition =frame.point -position;
                const float scale = 0.02f;
                props.mat = math.float4x4(math.float3x3(
                    scale, 0, 0,
                    0, scale, 0,
                    0, 0, scale
                ), localPosition);
                props.color = math.float4(1, 1, 1, 1);

                properties[i] = props;
            }
        }
        [BurstCompile]
        struct RandomTransformJob : IJobParallelFor
        {
            public NativeArray<MeshProperties> properties;
            public float range;
            public Unity.Mathematics.Random random;
            public bool draw;

            public void Execute(int i)
            {
                MeshProperties props = new MeshProperties();
                float3 position = new float3(random.NextFloat(-range, range), random.NextFloat(-range, range), random.NextFloat(-range, range));
                quaternion rotation = quaternion.Euler(random.NextFloat(-180, 180), random.NextFloat(-180, 180), random.NextFloat(-180, 180));
                float3 scale = new float3(1, 1, 1);

                props.mat = Matrix4x4.TRS(position, rotation, scale);
                props.color = this.draw ? (Vector4)Color.Lerp(Color.red, Color.blue, random.NextFloat()) : (Vector4)new Color(0, 0, 0, 0);

                properties[i] = props;
            }
        }
    }
}