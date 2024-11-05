using System;
using ROS2;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace ZC
{
    public class RenderC: MonoBehaviour
    {
        private NativeArray<ZPointInfo> flattenedPointsInfo;
        private NativeArray<int> _hitCountArray;
        private PointsDrawer _pointsDrawer;
        private bool _isShowPointsData;
        [SerializeField] private Bounds _bounds;
        private Camera _camera;
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _mat;
        [SerializeField] int rayCount;

        private ROS2Node ros2UnityNode;
        private ROS2UnityComponent _ros2UnityComponent;
        
        private void Start()
        {
            if (_ros2UnityComponent.Ok())
            {
                
            }
            
            _pointsDrawer = new PointsDrawer();
            _pointsDrawer.Setup(_mesh, rayCount, 1000, _mat);
        }

        private void Update()
        {
            
            
            float3 transformPosition = this._camera.transform.position;
            var hitCount = _hitCountArray[0];
            if (_isShowPointsData)
            {
                _bounds = new Bounds(transformPosition, Vector3.one * (1000 + 1));
                _pointsDrawer.Render(flattenedPointsInfo, hitCount, _bounds, transformPosition);
            }

        }
    }
}