using Unity.Entities;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    public class CameraControlSystem : ComponentSystem
    {
        Camera camera;

        public void Init(Camera _camera)
        {
            camera = _camera;
        }

        protected override void OnUpdate()
        {
            float dt = Time.deltaTime;

            var rotation = camera.transform.eulerAngles;
            rotation.x -= Input.GetAxis("Mouse Y");
            rotation.y += Input.GetAxis("Mouse X");
            camera.transform.eulerAngles = rotation;

            float forward = 0, strafe = 0;
            if (Input.GetKey(KeyCode.W))
                forward += 2;
            if (Input.GetKey(KeyCode.S))
                forward += -2;
            if (Input.GetKey(KeyCode.A))
                strafe += -2;
            if (Input.GetKey(KeyCode.D))
                strafe += 2;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                forward *= 10;
                strafe *= 10;
            }

            var deltaPos = camera.transform.forward * forward * dt * 10;
            deltaPos += camera.transform.right * strafe * dt * 10;
            camera.transform.position += deltaPos;
        }
    }
}
