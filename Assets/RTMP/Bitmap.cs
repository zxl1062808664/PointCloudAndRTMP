using System;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ZC
{
    public static class Bitmap
    {
        public const int FileHeaderSize = 14;
        public const int ImageHeaderSize = 40;

        // bmp文件头
        [StructLayout(LayoutKind.Explicit)]
        struct BitmapFileHeader
        {
            [FieldOffset(0)] public ushort bfType; // 19778，必须是BM字符串，对应的十六进制为0x4d42,十进制为19778
            [FieldOffset(2)] public uint bfSize; // 文件大小
            [FieldOffset(6)] public ushort bfReserved1; // 0
            [FieldOffset(8)] public ushort bfReserved2; // 0
            [FieldOffset(10)] public uint bfOffBits; // 从文件头到像素数据的偏移，也就是这两个结构体的大小之和
        }

// bmp图像头
        [StructLayout(LayoutKind.Sequential)]
        struct BitmapImageHeader
        {
            public uint biSize; // 此结构体的大小
            public int biWidth; // 图像的宽
            public int biHeight; // 图像的高
            public ushort biPlanes; // 1
            public ushort biBitCount; // 24
            public uint biCompression; // 0
            public uint biSizeImage; // 像素数据所占大小, 这个值应该等于上面文件头结构中bfSize-bfOffBits
            public int biXPelsPerMeter; // 0
            public int biYPelsPerMeter; // 0
            public uint biClrUsed; // 0 
            public uint biClrImportant; // 0
        }

        public static void EncodeToBitmap(NativeArray<byte> srcArray, int offset, int count, int width, int height,
            Allocator allocator, out NativeArray<byte> destArray)
        {
            unsafe
            {
                int dataActualCount = count;
                destArray = new NativeArray<byte>(FileHeaderSize + ImageHeaderSize + dataActualCount, allocator);
                BitmapFileHeader fileHeader = new BitmapFileHeader()
                {
                    bfType = 19778,
                    bfSize = (uint)(FileHeaderSize + ImageHeaderSize + dataActualCount),
                    bfOffBits = (uint)(FileHeaderSize + ImageHeaderSize)
                };
                BitmapImageHeader imageHeader = new BitmapImageHeader()
                {
                    biSize = (uint)ImageHeaderSize,
                    biWidth = width,
                    biHeight = height,
                    biPlanes = 1,
                    biBitCount = 24,
                    biSizeImage = (uint)dataActualCount,
                };
                var dest = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(destArray);
                var src = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(srcArray);

                var fileHeaderPtr = &fileHeader;
                var imageHeaderPtr = &imageHeader;
                UnsafeUtility.MemCpy(dest, fileHeaderPtr, FileHeaderSize);
                UnsafeUtility.MemCpy((void*)((nint)dest + FileHeaderSize), imageHeaderPtr, ImageHeaderSize);
                UnsafeUtility.MemCpy((void*)((nint)dest + FileHeaderSize + ImageHeaderSize), src, dataActualCount);
            }
        }

        public static void EncodeToBitmap(NativeArray<byte> srcArray, byte[] destArray, int offset, int count,
            int width, int height)
        {
            if (!srcArray.IsCreated)
                throw new NullReferenceException("srcArray is null");
            if (destArray == null)
                throw new NullReferenceException("destArray is null");

            unsafe
            {
                int dataActualCount = count;
                BitmapFileHeader fileHeader = new BitmapFileHeader()
                {
                    bfType = 19778,
                    bfSize = (uint)(FileHeaderSize + ImageHeaderSize + dataActualCount),
                    bfOffBits = (uint)(FileHeaderSize + ImageHeaderSize)
                };
                BitmapImageHeader imageHeader = new BitmapImageHeader()
                {
                    biSize = (uint)ImageHeaderSize,
                    biWidth = width,
                    biHeight = height,
                    biPlanes = 1,
                    biBitCount = 24,
                    biSizeImage = (uint)dataActualCount,
                };
                var dest = UnsafeUtility.PinGCArrayAndGetDataAddress(destArray, out var handle);
                try
                { 
                    var src = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(srcArray);
                    var fileHeaderPtr = &fileHeader;
                    var imageHeaderPtr = &imageHeader;
                    UnsafeUtility.MemCpy(dest, fileHeaderPtr, FileHeaderSize);
                    UnsafeUtility.MemCpy((void*)((nint)dest + FileHeaderSize), imageHeaderPtr, ImageHeaderSize);
                    UnsafeUtility.MemCpy((void*)((nint)dest + FileHeaderSize + ImageHeaderSize), src, dataActualCount);
                }
                finally
                {
                    UnsafeUtility.ReleaseGCObject(handle);
                }
               
            }
        }

        public static void SaveToDisk(string filePath, NativeArray<byte> data, int offset, int count, int width,
            int height)
        {
            unsafe
            {
                int dataActualCount = count;
                var fileHeaderSize = 14;
                var imageHeaderSize = 40;
                byte[] bytes = new byte[fileHeaderSize + imageHeaderSize + dataActualCount];
                var span = bytes.AsSpan();
                var dest = UnsafeUtility.AddressOf(ref span.GetPinnableReference());
                BitmapFileHeader fileHeader = new BitmapFileHeader()
                {
                    bfType = 19778,
                    bfSize = (uint)(fileHeaderSize + imageHeaderSize + dataActualCount),
                    bfOffBits = (uint)(fileHeaderSize + imageHeaderSize)
                };
                BitmapImageHeader imageHeader = new BitmapImageHeader()
                {
                    biSize = (uint)imageHeaderSize,
                    biWidth = width,
                    biHeight = height,
                    biPlanes = 1,
                    biBitCount = 24,
                    biSizeImage = (uint)dataActualCount,
                };
                var arrayPtr = (nint)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(data);

                var fileHeaderPtr = &fileHeader;
                var imageHeaderPtr = &imageHeader;
                UnsafeUtility.MemCpy(dest, fileHeaderPtr, fileHeaderSize);
                UnsafeUtility.MemCpy((void*)((nint)dest + fileHeaderSize), imageHeaderPtr, imageHeaderSize);
                UnsafeUtility.MemCpy((void*)((nint)dest + fileHeaderSize + imageHeaderSize), (void*)arrayPtr,
                    dataActualCount);
                File.WriteAllBytes(filePath, bytes);
            }
        }

        public static void LoadFromBmp(string filePath)
        {
            unsafe
            {
                var readAllBytes = File.ReadAllBytes(filePath);
                var src = UnsafeUtility.AddressOf(ref readAllBytes.AsSpan().GetPinnableReference());
                BitmapFileHeader fileHeader = new BitmapFileHeader();
                BitmapImageHeader imageHeader = new BitmapImageHeader();
                UnsafeUtility.MemCpy(&fileHeader, src, 14);
                UnsafeUtility.MemCpy(&imageHeader, (void*)((nint)src + 14), 40);
            }
        }
    }
}