using System;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace ZC
{
    public static class ZPointInfoSerializer
    {
        private static readonly int offset1 = Unsafe.SizeOf<Vector3>();
        private static readonly int offset2 = Unsafe.SizeOf<Quaternion>();
        private static readonly int offset3 = Unsafe.SizeOf<int>();

        public static void Serialize(NativeArray<ZPointInfo> pointInfos, int length, Vector3 position,
            Quaternion quaternion, out byte[] bytes)
        {
            unsafe
            {
                bytes = new byte[length * Unsafe.SizeOf<ZPointInfo>() + sizeof(int) + Unsafe.SizeOf<Vector3>() +
                                 Unsafe.SizeOf<Quaternion>()];
                var dest = Unsafe.AsPointer(ref bytes.AsSpan().GetPinnableReference());
                Unsafe.CopyBlockUnaligned(dest, Unsafe.AsPointer(ref position), (uint)Unsafe.SizeOf<Vector3>());
                dest = (void*)((nint)dest + offset1);
                Unsafe.CopyBlockUnaligned(dest, Unsafe.AsPointer(ref quaternion), (uint)Unsafe.SizeOf<Quaternion>());
                dest = (void*)((nint)dest + offset2);
                Unsafe.CopyBlockUnaligned(dest, Unsafe.AsPointer(ref length), (uint)Unsafe.SizeOf<int>());
                dest = (void*)((nint)dest + offset3);
                Unsafe.CopyBlockUnaligned(dest,
                    NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(pointInfos),
                    (uint)(length * Unsafe.SizeOf<ZPointInfo>()));
            }
        }

        public static void Deserialize(ReadOnlySpan<byte> bytes, Allocator allocator,
            out NativeArray<ZPointInfo> pointInfos, out Vector3 position, out Quaternion quaternion)
        {
            unsafe
            {
                var dest = Unsafe.AsPointer(ref Unsafe.AsRef(in bytes.GetPinnableReference()));
                Debug.Assert(dest!=null,"1");
                Unsafe.SkipInit(out position);
                Unsafe.SkipInit(out quaternion);
                Unsafe.SkipInit(out int length);
                Unsafe.CopyBlockUnaligned(Unsafe.AsPointer(ref position), dest, (uint)Unsafe.SizeOf<Vector3>());
                dest = (void*)((nint)dest + offset1);
                Debug.Assert(dest!=null,"2");
                Unsafe.CopyBlockUnaligned(Unsafe.AsPointer(ref quaternion), dest, (uint)Unsafe.SizeOf<Quaternion>());
                dest = (void*)((nint)dest + offset2);
                Debug.Assert(dest!=null,"3");
                Unsafe.CopyBlockUnaligned(Unsafe.AsPointer(ref length), dest, (uint)Unsafe.SizeOf<int>());
                dest = (void*)((nint)dest + offset3);
                Debug.Assert(dest!=null,"4");
                pointInfos = new NativeArray<ZPointInfo>(length, allocator);
                Unsafe.CopyBlockUnaligned(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(pointInfos),
                    dest, (uint)(length * Unsafe.SizeOf<ZPointInfo>()));
            }
        }
    }
}