﻿using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using ROS2;
using sensor_msgs.msg;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace ZC
{
    public class PointsRender : MonoBehaviour
    {
        // private NativeArray<ZPointInfo> flattenedPointsInfo;
        private PointsDrawer _pointsDrawer;
        [SerializeField] private bool _isShowPointsData;
        [SerializeField] private Bounds _bounds;
        [SerializeField] private Camera _camera;
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _mat;
        [SerializeField] int rayCount;

        Subscription<PointCloud2> subscription;
        private PointCloud2 _pointCloud;


        private void Start()
        {
            // if (_ros2UnityComponent.Ok())
            // {
            // }
            _ros2Unity = GetComponent<ROS2UnityComponent>();

            _pointsDrawer = new PointsDrawer();
            _pointsDrawer.Setup(_mesh, rayCount, 1000, _mat);
            
            _points = new NativeArray<ZPointInfo>(rayCount, Allocator.Persistent);
            // StartCoroutine(InitNode());

        }

        private void OnDestroy()
        {
            _points.Dispose();
            _pointsDrawer.OnDispose();
        }

        private ROS2Node _ros2Node;
        ROS2UnityComponent _ros2Unity;

        private NativeArray<ZPointInfo> _points;

        // IEnumerator InitNode()
        // {
        //     while (true)
        //     {
        //         if (RenderGlobal.ros2Node != null)
        //         {
        //             break;
        //         }
        //
        //         yield return null;
        //     }
        //     subscription = RenderGlobal.ros2Node.CreateSubscription<PointCloud2>("chatter", Callback);
        // }

        private void Callback(PointCloud2 obj)
        {
            this._pointCloud = obj;
        }

        private void Render(PointCloud2 obj1)
        {
            unsafe
            {
                var bytes = obj1.Data;
                var src = UnsafeUtility.PinGCArrayAndGetDataAddress(bytes, out var handle);
                var dest = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(_points);
                try
                {
                    UnsafeUtility.MemCpy(dest, src, bytes.Length);
                    _pointsDrawer.Render(_points, (int)obj1.Width, new Bounds(default, Vector3.one * 1000), default);
                }
                finally
                {
                    UnsafeUtility.ReleaseGCObject(handle);
                }
            }
        }

        private void Update()
        {
            if (_ros2Node == null && _ros2Unity.Ok())
            {
                _ros2Node = _ros2Unity.CreateNode("ROS2UnityListenerNode");
                subscription = _ros2Node.CreateSubscription<PointCloud2>("chatter", Callback);
            }

            if(_pointCloud!=null)
            Render(_pointCloud);
        }
    }
}