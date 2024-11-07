using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Assets
{
    public class TestUnsafeUni : MonoBehaviour
    {
        class MyClass
        {
            public int a;
            public int a1;
            public int a2;
            public int a3;
            public int a4;
        }

        class ListView<T>
        {
            public T[] items;
            public int count;
            public int capacity;
            public int version;
        }

        private void Start()
        {
            unsafe
            {
                int a1 = 10;
                int a2 = 0;
                var add1 = UnsafeUtility.AddressOf(ref a1);
                var add2 = UnsafeUtility.AddressOf(ref a2);
                UnsafeUtility.MemMove(add2, add1, sizeof(int));
                Debug.Log($"{a1} , {a2}");
                a1 = 20;
                Debug.Log($"{a1} , {a2}");

                int[] arr1 = new [] { 10,20,30 };
                int[] arr2 = new[] { 0 };
                var addressOf1 = UnsafeUtility.AddressOf(ref arr1[2]);
                var addressOf2 = UnsafeUtility.AddressOf(ref arr2[0]);
                UnsafeUtility.MemCpy(addressOf2,addressOf1,sizeof(int));
                Debug.Log(arr2[0]);
                var l = UnsafeUtility.As<int,long>(ref a1);
                var add3 = UnsafeUtility.AddressOf(ref l);
                Debug.Log($"{(long)add1} , {(long)add3}");
                

                Debug.Log("-------------------------------------");
            }


            unsafe
            {
                List<int> list1 = new List<int>() { 1, 2, 3, 3, 3 };
                var listView = Unsafe.As<ListView<int>>(list1);
                ref int aaaa = ref listView.items[0];
                var asPointer = Unsafe.AsPointer(ref aaaa);
                int* valuePtr = (int*)asPointer;
                *valuePtr = 123;
                Debug.Log(list1[0]);
                int[] arr = new int[list1.Count];
                // var asSpan = CollectionsMarshal.AsSpan(list1);
                // foreach (var i in asSpan)
                // {
                //     Debug.Log(i);
                // }

                Debug.Log("11111111111111111111111");
                // ref var pinnableReference = ref asSpan.GetPinnableReference();
                // UnsafeUtility.MemMove(Unsafe.AsPointer(ref arr[0]), Unsafe.AsPointer(ref pinnableReference),(uint)(list1.Count * sizeof(int)));
                foreach (var i in arr)
                {
                    Debug.Log(i);
                }

                Debug.Log(listView.count);
                MyClass a = new MyClass();
                var ptr1 = UnsafeUtility.PinGCObjectAndGetAddress(a, out var handle2);
                fixed (int* ptr2 = &a.a)
                {
                    Debug.Log((long)ptr1);
                    Debug.Log((long)ptr2);
                }

                UnsafeUtility.ReleaseGCObject(handle2);
                var _array1 = new int[1] { 100 };
                var addressOf = UnsafeUtility.AddressOf(ref _array1[0]);
                var array2 = new int[1];
                var destination = UnsafeUtility.AddressOf(ref array2[0]);
                UnsafeUtility.MemCpy(destination, addressOf, 4);
                Debug.Log(array2[0]);

                List<int> list = new List<int>() { 1, 2, 3 };


                var pinGCObjectAndGetAddress = UnsafeUtility.PinGCArrayAndGetDataAddress(_array1, out var handle);
                var pinGCObjectAndGetAddress1 = UnsafeUtility.PinGCObjectAndGetAddress(_array1, out var handle1);
                try
                {
                    Debug.Assert(addressOf == pinGCObjectAndGetAddress);
                    Debug.Log((long)addressOf);
                    Debug.Log((long)pinGCObjectAndGetAddress1);
                }
                finally
                {
                    UnsafeUtility.ReleaseGCObject(handle);
                    UnsafeUtility.ReleaseGCObject(handle1);
                }
            }

            unsafe
            {
                NativeArray<int> array1 = new NativeArray<int>(2, Allocator.Persistent);


                var _array1 = new int[1] { 100 };
                var addressOf = UnsafeUtility.AddressOf(ref _array1[0]);
                var array2 = new int[1];
                var destination = UnsafeUtility.AddressOf(ref array2[0]);
                UnsafeUtility.MemCpy(destination, addressOf, 4);
                Debug.Log(array2[0]);

                List<int> list = new List<int>() { 1, 2, 3 };


                var pinGCObjectAndGetAddress = UnsafeUtility.PinGCArrayAndGetDataAddress(_array1, out var handle);
                var pinGCObjectAndGetAddress1 = UnsafeUtility.PinGCObjectAndGetAddress(_array1, out var handle1);
                try
                {
                    Debug.Assert(addressOf == pinGCObjectAndGetAddress);
                    Debug.Log((long)addressOf);
                    Debug.Log((long)pinGCObjectAndGetAddress1);
                }
                finally
                {
                    UnsafeUtility.ReleaseGCObject(handle);
                    UnsafeUtility.ReleaseGCObject(handle1);
                }
            }

            unsafe
            {
                
                Debug.Log("==List扩展=============");
                var list = new List<int>() { 1, 23, 4, 4, 4, 4 };
                var span = list.AsSpan();
                Debug.Log("==AsSpan=============");
                foreach (var i in span)
                {
                    Debug.Log(i);
                }
                Debug.Log("==ForceSetCount=============");
                Debug.Log("=============");
                list.ForceSetCount(3);
                foreach (var i in list)
                {
                    Debug.Log(i);
                }
                Debug.Log("=============");
                list.ForceSetCount(10);
                foreach (var i in list)
                {
                    Debug.Log(i);
                }
                Debug.Log("==创建Span的途径=============");
                Span<int> span1 = stackalloc int[5] { 1, 2, 3, 3, 4 };
                Span<int> span2 = new int[5] { 1, 2, 3, 3, 4 };
                using var nativeArray = new NativeArray<int>(5, Allocator.Temp);
                Span<int> span3 = nativeArray;
                Span<int> span4 = new List<int>(){1,2,3,3,4}.AsSpan();
                ReadOnlySpan<char> span5 = "asdfg".AsSpan();
                ReadOnlySpan<char> span6 = "asdfg".AsSpan(1,3);//sdf
            }
        }
    }
}