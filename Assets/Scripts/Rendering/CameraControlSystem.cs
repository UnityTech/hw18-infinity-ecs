using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    [AlwaysUpdateSystem]
    public class CameraControlSystem : ComponentSystem
    {
        [Inject]
        CameraSystem camera;

        protected override void OnUpdate()
        {
            float dt = Time.deltaTime;

            var cameraShift = EntityManager.GetComponentData<Shift>(camera.main);
            var cameraRotate = EntityManager.GetComponentData<Rotation>(camera.main);

            Quaternion quat = new Quaternion(cameraRotate.rotation.value.x, cameraRotate.rotation.value.y, cameraRotate.rotation.value.z, cameraRotate.rotation.value.w);
            Vector3 rotation = quat.eulerAngles;
            rotation.x -= Input.GetAxis("Mouse Y");
            rotation.y += Input.GetAxis("Mouse X");
            EntityManager.SetComponentData<Rotation>(camera.main, new Rotation(Quaternion.Euler(rotation)));

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

            var right = math.cross(math.forward(quat), math.up(quat));
            var deltaPos = math.forward(quat) * forward * dt * 10;
            deltaPos -= right * strafe * dt * 10;
            EntityManager.SetComponentData<Shift>(camera.main, new Shift(cameraShift.value + deltaPos));
        }
    }
}
