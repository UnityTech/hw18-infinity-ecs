using System;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.InfiniteWorld
{
    public class DebugOutput : ComponentSystem
    {
        Text debugText;

        [Inject]
        CameraSystem camera;

        public void Init()
        {
            debugText = GameObject.Find("Status").GetComponent<Text>();
        }

        protected override void OnUpdate()
        {
            /*
            var count = EntityManager.Debug.EntityCount;
            var transform = EntityManager.GetComponentData<Transform>(camera.main);

            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("ENTITIES {0} \n COORDINATES: {1}:{2}:{3}", count, transform.transform.c3.x, transform.transform.c3.y, transform.transform.c3.z);

            debugText.text = builder.ToString();
            */
        }
    }
}