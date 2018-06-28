using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.InfiniteWorld
{
    // Use a ComponentSystem only to be able to inject it and to properly initialize && dispose it
    public class TerrainChunkAssetDataSystem : ComponentSystem
    {
        struct AssetData
        {
            public NativeArray<float> Heightmap;

            public Texture2D HeightmapTex;
            public Texture2D NormalmapTex;
            public Texture2D SplatmapTex;
        }

        Dictionary<int2, AssetData> sectorData;

        public bool IsHeightmapReady(int2 sector)
        {
            return sectorData.ContainsKey(sector);
        }
        public bool IsHeightmapReady(Sector sector)
        {
            return IsHeightmapReady(sector.value);
        }

        public bool GetHeightmap(Sector sector, out NativeArray<float> heightmap)
        {
            AssetData asset;
            if (!sectorData.TryGetValue(sector.value, out asset))
            {
                heightmap = asset.Heightmap;
                return false;
            }

            heightmap = asset.Heightmap;
            return true;
        }

        public Texture2D GetHeightmapTex(Sector sector)
        {
            AssetData asset;
            if (!sectorData.TryGetValue(sector.value, out asset))
                return null;

            return asset.HeightmapTex;
        }

        public Texture2D GetNormalmapTex(Sector sector)
        {
            AssetData asset;
            if (!sectorData.TryGetValue(sector.value, out asset))
                return null;

            return asset.NormalmapTex;
        }
        public void SetNormalmapTex(Sector sector, Texture2D normalmap)
        {
            AssetData asset;
            if (!sectorData.TryGetValue(sector.value, out asset))
            {
                Debug.Log("ERROR: Setting splat map for unregistered sector");
                return;
            }

            asset.NormalmapTex = normalmap;
            sectorData[sector.value] = asset;
        }

        public Texture2D GetSplatmapTex(Sector sector)
        {
            AssetData asset;
            if (!sectorData.TryGetValue(sector.value, out asset))
                return null;

            return asset.SplatmapTex;
        }
        public void SetSplatmapTex(Sector sector, Texture2D splatmap)
        {
            AssetData asset;
            if (!sectorData.TryGetValue(sector.value, out asset))
            {
                Debug.Log("ERROR: Setting splat map for unregistered sector");
                return;
            }

            asset.SplatmapTex = splatmap;
            sectorData[sector.value] = asset;
        }

        public void AddSector(Sector sector, NativeArray<float> heightmap, Texture2D heightmapTex)
        {
            AssetData asset;
            asset.Heightmap = heightmap;
            asset.HeightmapTex = heightmapTex;
            asset.NormalmapTex = null;
            asset.SplatmapTex = null;
            sectorData.Add(sector.value, asset);
        }

        //NormalMap
        public void DisposeSector(Sector sector)
        {
            AssetData asset;
            if (!sectorData.TryGetValue(sector.value, out asset))
                return;

            asset.Heightmap.Dispose();
            UnityEngine.Object.Destroy(asset.HeightmapTex);
            if (asset.NormalmapTex)
                UnityEngine.Object.Destroy(asset.NormalmapTex);
            if (asset.SplatmapTex)
                UnityEngine.Object.Destroy(asset.SplatmapTex);
            sectorData.Remove(sector.value);
        }

        protected override void OnCreateManager(int capacity)
        {
            sectorData = new Dictionary<int2, AssetData>();
        }

        protected override void OnDestroyManager()
        {
            var keys = sectorData.Keys;
            foreach (var key in keys)
                DisposeSector(new Sector(key));
            sectorData.Clear();
            sectorData = null;
        }

        protected override void OnUpdate()
        {
        }
    }
}
