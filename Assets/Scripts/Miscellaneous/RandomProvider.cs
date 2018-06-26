namespace InfiniteWorld
{
    public struct RandomProvider
    {
        uint seed;

        public RandomProvider(uint initialSeed)
        {
            seed = initialSeed > 0u ? initialSeed : 1u;
        }

        public uint Next()
        {
            seed *= 3039177861u;
            return seed;
        }

        public float Uniform()
        {
            return (float)(Next()) / (float)0xFFFFFFFFu;
        }

        public float Uniform(float range)
        {
            return range * Uniform();
        }

        public uint Uniform(uint start, uint end)
        {
            float s = start;
            float e = end;
            float value = Uniform();
            float result = s * (1.0f - value) + e * value;
            return (uint)(result + 0.5f);
        }
    }
}

