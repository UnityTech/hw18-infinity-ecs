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
            var count = EntityManager.Debug.EntityCount;
            var shift = EntityManager.GetComponentData<Shift>(camera.main);
            var sector = EntityManager.GetComponentData<Sector>(camera.main);
            float3 truePos = new float3(sector.value.x * WorldChunkConstants.ChunkSize, 0, sector.value.y * WorldChunkConstants.ChunkSize) + shift.value;

            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("ENTITIES {0:D6} \n COORDINATES: X:{1:F1} Y:{2:F1} Z:{3:F1}", count, truePos.x, truePos.y, truePos.z);

            debugText.text = builder.ToString();
        }
    }
}