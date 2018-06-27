namespace Unity.InfiniteWorld
{
    public static class WorldChunkConstants
    {
        public const int ChunkSize = 256;
        public const int ChunkCapacity = 256;

        public const int ObjectsVisibleDistance = 2;
        public const int ObjectsUnloadDistance = ObjectsVisibleDistance + 1;

        public const float TerrainHeightScale = 50.0f;
        public const int TerrainOctaves = 4;
        public const float TerrainOctaveMultiplier = 0.8f;
        public const float TerrainOctavePersistence = 0.5f;
    }
}
