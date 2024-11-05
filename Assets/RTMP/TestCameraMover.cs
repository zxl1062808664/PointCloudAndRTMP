using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZC
{
    public class TestCameraMover : MonoBehaviour
    {
        [SerializeField]
        private Camera _camare;

        [SerializeField]
        private float _moveSpeed = 1;
        [SerializeField]
        private float _rotateSpeed = 1;

        [SerializeField]
        private Rect _regions;


        private Quaternion _destination;
        private bool _startRotating;



        // Start is called before the first frame update
        void Start()
        {
            _camare.transform.position = new Vector3(0.945037186f, 9.69658566f, -23.7757969f);
            _camare.transform.rotation = Quaternion.Euler(55, 0, 0);
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 vector3 = Vector3.ProjectOnPlane(_camare.transform.forward, Vector3.up);
            _camare.transform.position += vector3 * _moveSpeed * Time.deltaTime;

            if (!_regions.Contains(_camare.transform.position))
            {
                vector3 *= -1;
                _camare.transform.position += 2 * vector3 * _moveSpeed * Time.deltaTime;
                RotateTo(vector3);
            }
            else
            {
                if (!_startRotating && UnityEngine.Random.value > 0.8f)
                {
                    _startRotating = true;
                    _destination =
                       this._camare.transform.rotation
                       * Quaternion.Euler(0, UnityEngine.Random.Range(-180f, 180), 0);
                }
            }
            if (_startRotating)
            {
                if (Quaternion.Angle(_camare.transform.rotation, _destination) < 0.5f)
                {
                    _startRotating = false;
                }
                else
                {
                    Quaternion quaternion = Quaternion.RotateTowards(
                        _camare.transform.rotation, _destination, _rotateSpeed * Time.deltaTime);
                    _camare.transform.rotation = quaternion;
                }

            }

        }


        void RotateTo(Vector3 dir)
        {
            Quaternion quaternion = Quaternion.LookRotation(dir.normalized);
            float y = quaternion.eulerAngles.y;
            Vector3 eulerAngles = _camare.transform.rotation.eulerAngles;
            eulerAngles.y = y;
            _camare.transform.rotation = Quaternion.Euler(eulerAngles);
        }
    }


}