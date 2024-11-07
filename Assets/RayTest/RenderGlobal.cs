using System.Collections;
using ROS2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZC
{
    [RequireComponent(typeof(ROS2UnityComponent))]
    public class RenderGlobal : MonoBehaviour
    {
        private static ROS2UnityComponent _ros2UnityComponent;
        private static ROS2Node _ros2Node;
        private static bool _isFinish = false;

        public static bool isFinish => _isFinish && _ros2UnityComponent.Ok();
        public static ROS2UnityComponent ros2UnityComponent => _ros2UnityComponent;
        public static ROS2Node ros2Node => _ros2Node;

        private void Awake()
        {
            _ros2UnityComponent = GetComponent<ROS2UnityComponent>();
            
        }

        public void CreatenODE()
        {
            
            _ros2Node = _ros2UnityComponent.CreateNode("Test1_Node" + this.gameObject.scene.name);
        }
        
    }
}