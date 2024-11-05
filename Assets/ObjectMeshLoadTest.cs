using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets
{
    public class ObjectMeshLoadTest : MonoBehaviour
    {
        public Mesh mesh;
        public GameObject meshPrefab;
        public GameObject meshPrefab1;

        public int count;

        private void Start()
        {

        }

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.T))
            {
                List<GameObject> list = new List<GameObject>();
                for (int i = 0; i < count; i++)
                {
                    var x = Random.Range(-20f, 20f);
                    var y = Random.Range(-20f, 20f);
                    var z = Random.Range(-20f, 20f);
                    GameObject go = GameObject.Instantiate(meshPrefab, new Vector3(x, y, z), Quaternion.identity, transform);
                    go.GetComponent<MeshFilter>().mesh = mesh;
                    list.Add(go);
                }
                StaticBatchingUtility.Combine(gameObject);
                //StaticBatchingUtility.Combine(list.ToArray(), gameObject);
            }
            if (Input.GetKeyUp(KeyCode.R))
            {
                for (int i = 0; i < count; i++)
                {
                    var x = Random.Range(-20f, 20f);
                    var y = Random.Range(-20f, 20f);
                    var z = Random.Range(-20f, 20f);
                    GameObject go = GameObject.Instantiate(meshPrefab1, new Vector3(x, y, z), Quaternion.identity, transform);
                    //go.GetComponent<MeshFilter>().mesh = mesh;
                }
            }

        }
    }
}
