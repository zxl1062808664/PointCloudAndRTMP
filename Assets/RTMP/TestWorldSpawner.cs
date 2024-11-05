using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZC
{
    public class TestWorldSpawner : MonoBehaviour
    {
        [SerializeField]
        private Material _groundMat;
        [SerializeField]
        private Material _cubeMat;

        // Start is called before the first frame update
        void Start()
        {
            GameObject gameObject1 = GameObject.CreatePrimitive(PrimitiveType.Plane);
            gameObject1.transform.localScale = Vector3.one * 100;
            gameObject1.GetComponent<Renderer>().material = _groundMat;
            for (int i = 0; i < 10000; i++)
            {
                Vector2 vector2 = UnityEngine.Random.insideUnitCircle * 1000 / 2;
                GameObject gameObject2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(gameObject2.GetComponent<Collider>());
                gameObject2.GetComponent<Renderer>().material = _cubeMat;
                gameObject2.transform.position = new Vector3(vector2.x, 0.5f, vector2.y);
                gameObject2.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(-180, 180), 0);
            }
        }
    }

}
