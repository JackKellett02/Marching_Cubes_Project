using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainSubChunkScript {
	#region Private Variables.
	private static Dictionary<Vector2, NoiseUtility.FallOffData> falloffMap;

	protected Vector3 chunkDimensions = Vector3.one;
	protected Action<MeshData> meshCallback = null;
	protected TerrainChunkScript instance = null;
	#endregion



	#region Private Functions.
	private  float[,,] PopulateGridMap(int xSize, int ySize, int zSize, float a_heightMultiplier,
		AnimationCurve a_terrainHeights, ChunkGenerationData chunkData, bool useNormData) {
		//Create the map.
		float[,,] map = new float[xSize, ySize, zSize];

		//Use the correct chunk data.
		ChunkData data = chunkData.chunkData;
		if (useNormData) {
			data = chunkData.normChunkData;
		}


		//Loop through heights and activate up to specified height.
		float lastFalloffValue = 0.0f;
		//Look at the float map and decide whether or not the current point should be active.
		for (int z = 0; z < zSize; z++) {
			for (int x = 0; x < xSize; x++) {
				//Get falloff value for current cell.
				float falloffValue;
				Vector2 samplePoint = NoiseUtility.CalculateWorldSpacePos(x, z, xSize, zSize, chunkData.m_cubeSize, chunkData.m_chunkPos);

				//Calculate position in falloff map of current node.
				lock (falloffMap) {
					//Check the key exists.
					if (falloffMap.ContainsKey(samplePoint)) {
						falloffValue = falloffMap[samplePoint].falloffValue;
						lastFalloffValue = falloffValue;
					} else {
						falloffValue = lastFalloffValue;
					}
				}

				float heightMapValue = Mathf.Clamp01(data.heightMap[x, z] + falloffValue);

				//Casting to int so it can be compared to Y value of the gridMap
				float size = a_terrainHeights.Evaluate(heightMapValue);
				size = size * a_heightMultiplier;
				//size = (size * 2) - 1;
				int currentCellHeight = (ySize / 2) + Mathf.RoundToInt(size);
				currentCellHeight = Mathf.Clamp(currentCellHeight, 0, ySize - 1);


				for (int y = 0; y < ySize; y++) {
					float noiseValue = data.chunkNoise[x, y, z];

					//Add the falloff value to the noise value for the current cell.
					noiseValue = Mathf.Clamp01(noiseValue + falloffValue);

					//Get a value for the current cell that will be passed to the marching cubes function to be used in interpolation.
					float value = (Mathf.InverseLerp((float)0, (float)(ySize - 1), (float)currentCellHeight)) * (1 - noiseValue);


					if (y <= currentCellHeight && noiseValue >= chunkData.m_surfaceThreshold && y < (ySize - 1)) {
						map[x, y, z] = -value;
					} else {
						map[x, y, z] = Mathf.Clamp01(value + 0.01f);

					}
				}
			}
		}

		//Return the completed map.
		return map;
	}

	private void ChunkDataThread(ChunkGenerationData generationData, Action<ChunkGenerationData> a_callback) {
		//Generate Level Noise.
		float[,] heightMap = NoiseGenerator.GenerateNoiseMap(generationData.m_gridSize.x, generationData.m_gridSize.z, generationData.m_cubeSize, generationData.m_chunkWorldPos, generationData.m_noiseSettings);
		float[,,] chunkNoise = NoiseGenerator.Generate3DNoiseMap(generationData.m_gridSize.x, generationData.m_gridSize.y, generationData.m_gridSize.z, generationData.m_cubeSize, generationData.m_chunkWorldPos, generationData.m_noiseSettings);

		float[,] normHeightMap = NoiseGenerator.GenerateNoiseMap(generationData.m_gridSize.x + 2, generationData.m_gridSize.z + 2, generationData.m_cubeSize, generationData.m_chunkWorldPos, generationData.m_noiseSettings);
		float[,,] normChunkNoise = NoiseGenerator.Generate3DNoiseMap(generationData.m_gridSize.x + 2, generationData.m_gridSize.y, generationData.m_gridSize.z + 2, generationData.m_cubeSize, generationData.m_chunkWorldPos, generationData.m_noiseSettings);

		generationData.normChunkData = new ChunkData(normHeightMap, normChunkNoise);
		generationData.chunkData = new ChunkData(heightMap, chunkNoise);

		a_callback(generationData);
		//TerrainChunkScript.EnqueueChunkThreadInfo(new MapThreadInfo<ChunkGenerationData>(a_callback, generationData));

	}


	private void GenerateChunk(ChunkGenerationData generationData) {
		//Generate the grid map.
		float[,,] gridMap = PopulateGridMap(generationData.m_gridSize.x, generationData.m_gridSize.y,
			generationData.m_gridSize.z, generationData.m_heightMultiplier, generationData.m_terrainHeights,
			generationData, false);
		float[,,] normMap = PopulateGridMap(generationData.m_gridSize.x + 2, generationData.m_gridSize.y,
			generationData.m_gridSize.z + 2, generationData.m_heightMultiplier, generationData.m_terrainHeights,
			generationData, true);

		//Pass the grid map and cube size to the marching cubes script to generate the mesh.
		if (meshCallback == null) {
			Debug.LogError("ERROR: Mesh callback function not present in Mesh Generation Script.");
			return;
		}

		RequestMeshData(new MapData(gridMap, normMap, generationData.m_cubeSize, generationData.m_chunkPos), meshCallback);
	}

	private void MeshDataThread(MapData chunkData, Action<MeshData> a_callback) {
		//Get the mesh data and send it back to the main thread.
		MeshData meshData = MarchingCubes.GenerateMesh(chunkData.map, chunkData.normalMap, chunkData.cubeSize, chunkData.pos);

		if (instance == null) {
			//Early out.
			Debug.LogError("ERROR: INSTANCE OF CHUNK SCRIPT IS NULL IN MESH GENERATION SCRIPT");
			return;
		}

		instance.EnqueueMeshThreadInfo(new MapThreadInfo<MeshData>(a_callback, meshData));

	}

	private void RequestMeshData(MapData chunkData, Action<MeshData> a_callback) {
		MeshDataThread(chunkData, a_callback);

		//ThreadStart threadStart = delegate {
		//	MeshDataThread(chunkData, a_callback);
		//};
		//Thread thread = new Thread(threadStart);
		//thread.Name = "Mesh Data Thread.";
		//thread.Start();
		//Debug.Log("Mesh Data Thread Started.");
	}

	private void RequestChunkData(ChunkGenerationData generationData, Action<ChunkGenerationData> a_callback) {
		ChunkDataThread(generationData, a_callback);

		//ThreadStart threadStart = delegate { ChunkDataThread(generationData, a_callback); }
		//Thread thread = new Thread(threadStart);
		//thread.Name = "Chunk Data Thread.";
		//thread.Start();
		//Debug.Log("Chunk Data Thread started.");
	}

	private void OnNoiseDataRecieved(ChunkGenerationData a_chunkData) {
		//Debug.Log("Chunk Data Recieved.");
		GenerateChunk(a_chunkData);
	}
	#endregion

	#region Public Access Functions (Getters and Setters).
	public static void InitialiseFalloffMap(Vector2Int levelSize, Vector2 chunkSize, float cubeSize) {
		falloffMap = new Dictionary<Vector2, NoiseUtility.FallOffData>();
		lock (falloffMap) {
			falloffMap = NoiseUtility.GenerateLevelFalloffMap(levelSize, chunkSize, cubeSize);
		}
	}


	public void StartGeneration(Vector3 a_chunkWorldPos, Vector3 a_chunkPos, Vector3 a_chunkDimensions, Vector3Int gridSize, float cubeSize, float a_surfaceThreshold, NoiseSettings noiseSettings, AnimationCurve a_terrainHeights, float a_fHeightMultiplier, Action<MeshData> a_callback, TerrainChunkScript a_instance) {
		instance = a_instance;
		meshCallback = a_callback;
		chunkDimensions = a_chunkDimensions;
		ChunkGenerationData data = new ChunkGenerationData(
			a_chunkWorldPos, a_chunkPos, a_chunkDimensions, gridSize, cubeSize, a_surfaceThreshold, noiseSettings, a_terrainHeights, a_fHeightMultiplier);
		//Debug.Log("GENERATION STARTED.");
		RequestChunkData(data, OnNoiseDataRecieved);
	}
	#endregion
}