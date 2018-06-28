#include "noiseSimplex.cginc"

float fbm(
	float2 sector,
	float2 xy,
	int octaves,
	float multiplier,
	float sectorScale,
	float persistence
)
{
	float value = 0.0f;
	for (int j = 0; j < octaves; ++j)
	{
		value += snoise((xy + sector) * sectorScale) * multiplier;

		sectorScale *= 2.0f;
		multiplier *= persistence;
	}

	return value;
}

float turb(
	float2 sector,
	float2 xy,
	int octaves,
	float multiplier,
	float sectorScale,
	float persistence
)
{
	float value = 0.0f;
	for (int j = 0; j < octaves; ++j)
	{
		value += abs(snoise((xy + sector) * sectorScale)) * multiplier;

		sectorScale *= 2.0f;
		multiplier *= persistence;
	}

	return value;
}

float ridge(
	float2 sector,
	float2 xy,
	int octaves,
	float multiplier,
	float sectorScale,
	float persistence,
	float offset
)
{
	float value = 0.0f;
	for (int j = 0; j < octaves; ++j)
	{
		float n = abs(snoise((xy + sector) * sectorScale)) * multiplier;
		n = offset - n;
		n *= n;

		value += n;

		sectorScale *= 2.0f;
		multiplier *= persistence;
	}

	return value;
}

float clip01(float value, float threshold)
{
	return clamp(value - threshold, 0.0, 1.0) / (1.0 - threshold);
}

float terrainHeight(float2 position)
{
	float height = 0.3;

	// Base height
	float baseHeight = fbm(floor(position), frac(position), 4, 0.8, 0.3, 0.4) * 0.2 + 0.5;

	// Mountains
	//float mountainWeight = snoise(position * 0.3) * 0.5 + 0.5;
	//mountainWeight = clip01(mountainWeight, 0.5);
	float mountainSectorScale = 0.3;
	float mountainBase = ridge(floor(position), frac(position), 3, 0.7, mountainSectorScale, 0.2, 0.56);
	float mountainBaseWeight = snoise(position * 0.2) * 0.5 + 0.5;
	float mountainBaseWeighted = mountainBase * mountainBaseWeight;
	float mountainWeight = clip01(mountainBaseWeighted, 0.2);
	mountainWeight = clip01(mountainWeight, -0.6);
	mountainWeight *= mountainWeight * mountainWeight;
	mountainWeight *= 4.0;
	mountainWeight = clamp(mountainWeight, 0.0, 1.0);

	float mountainDetails = ridge(floor(position), frac(position), 5, 0.6, mountainSectorScale, 0.5, 0.45);

	height = mountainDetails * mountainWeight;

	return height;
}

float3 terrainBaseColor(float2 position, float height)
{
	float3 color = float3(height, height, height);

	float3 grassColor = float3(80.0 / 255.0, 117.0 / 255.0, 56.0 / 255.0);
	float3 snowColor = float3(1.0, 1.0, 1.0);
	float3 rockColor = float3(165.0 / 255.0, 99.0 / 255.0, 38.0 / 255.0);

	float3 weights = float3(
		clamp(-height + 0.3, 0.0, 1.0),
		clamp(clip01(height, 0.7), 0.0, 1.0),
		min(clamp(height - 0.1, 0.0, 1.0), clamp(1.0 - height * 0.7, 0.0, 1.0))
		);

	weights = normalize(max(weights, float3(0.0, 0.0, 0.0)));

	color = grassColor * weights.x
		+ snowColor * weights.y
		+ rockColor * weights.z;

	return color;
}