using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseSettings
{
	public int seed = 12312314;
	public Vector2 offset = Vector2.zero;
	public float scale = 1.0f;

	public NoiseSettings()
	{
		seed = 0;
		offset = Vector2.zero;
		scale = 1.0f;
	}

	public NoiseSettings(NoiseSettings copySettings)
	{
		seed = copySettings.seed;
		offset = copySettings.offset;
		scale = copySettings.scale;
	}
}
