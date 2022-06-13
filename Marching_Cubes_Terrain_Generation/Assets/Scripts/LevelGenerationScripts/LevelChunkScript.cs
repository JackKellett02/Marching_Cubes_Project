using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class LevelChunkScript : MonoBehaviour
{
	#region Variables to assign via the unity inspector (SerialiseFields).

	#endregion

	#region Private Variables.
	private MeshFilter levelMeshFilter = null;
	private MeshCollider levelMeshCollider = null;
	private Vector3 chunkDimensions = Vector3.one;

	private Queue<MapThreadInfo<ChunkGenerationData>> chunkThreadInfo;
	#endregion

	#region Private Functions.
	// Start is called before the first frame update
	void Awake() {

	}

	// Update is called once per frame
	void Update() {
		if (chunkThreadInfo != null) {
			if (chunkThreadInfo.Count > 0) {
				if (!Application.isPlaying) {
					for (int i = 0; i < chunkThreadInfo.Count; i++) {
						MapThreadInfo<ChunkGenerationData> threadInfo = chunkThreadInfo.Dequeue();
						threadInfo.callback(threadInfo.parameter);
					}
				} else {
					MapThreadInfo<ChunkGenerationData> threadInfo = chunkThreadInfo.Dequeue();
					threadInfo.callback(threadInfo.parameter);
				}

			}
		}

	}

	private float[,,] PopulateGridMap(int xSize, int ySize, int zSize, float a_heightMultiplier, AnimationCurve a_terrainHeights, ChunkGenerationData chunkData, bool useNormData) {
		//Create the map.
		float[,,] map = new float[xSize, ySize, zSize];

		//Use the correct chunk data.
		ChunkData data = chunkData.chunkData;
		if (useNormData) {
			data = chunkData.normChunkData;
		}

		//Look at the float map and decide whether or not the current point should be active.
		for (int z = 0; z < zSize; z++) {
			for (int x = 0; x < xSize; x++) {
				//Casting to int so it can be compared to Y value of the gridMap
				float size = a_terrainHeights.Evaluate(data.heightMap[x, z]);
				size = size * a_heightMultiplier;
				//size = (size * 2) - 1;
				int currentCellHeight = (ySize / 2) + Mathf.RoundToInt(size);
				currentCellHeight = Mathf.Clamp(currentCellHeight, 0, ySize - 1);

				//Loop through heights and activate up to specified height.
				for (int y = 0; y < ySize; y++) {
					float value = (Mathf.InverseLerp((float)0, (float)(ySize - 1), (float)currentCellHeight)) * (1 - data.chunkNoise[x, y, z]);
					if (y <= currentCellHeight && data.chunkNoise[x, y, z] >= chunkData.m_surfaceThreshold && y < (ySize - 1)) {
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

	private void OnMeshDataRecieved(MeshData a_meshData) {
		//Generate the actual mesh.
		Mesh mesh = new Mesh();
		mesh.name = gameObject.name + "_Mesh";
		Debug.Log("Mesh Data Recieved.");
		levelMeshFilter.mesh = mesh;
		levelMeshCollider.sharedMesh = mesh;
		mesh.vertices = a_meshData.vertices.ToArray();
		mesh.triangles = a_meshData.triangles.ToArray();
		mesh.normals = a_meshData.normals;
	}

	private void GenerateChunk(ChunkGenerationData generationData) {
		//Generate the grid map.
		float[,,] gridMap = PopulateGridMap(generationData.m_gridSize.x, generationData.m_gridSize.y, generationData.m_gridSize.z, generationData.m_heightMultiplier, generationData.m_terrainHeights, generationData, false);
		float[,,] normMap = PopulateGridMap(generationData.m_gridSize.x + 2, generationData.m_gridSize.y, generationData.m_gridSize.z + 2, generationData.m_heightMultiplier, generationData.m_terrainHeights, generationData, true);

		//Pass the grid map and cube size to the marching cubes script to generate the mesh.
		MarchingCubes.RequestMeshData(new MapData(gridMap, normMap, generationData.m_cubeSize), OnMeshDataRecieved);
	}

	private void OnNoiseDataRecieved(ChunkGenerationData a_chunkData) {
		Debug.Log("Chunk Data Recieved.");
		GenerateChunk(a_chunkData);
	}


	private void ChunkDataThread(ChunkGenerationData generationData, Action<ChunkGenerationData> a_callback) {
		//Generate Level Noise.
		float[,] heightMap = LevelNoiseGenerator.GenerateNoiseMap(generationData.m_gridSize.x, generationData.m_gridSize.z, generationData.m_cubeSize, generationData.m_chunkPos, true, generationData.m_noiseSettings);
		float[,,] chunkNoise = LevelNoiseGenerator.Generate3DNoiseMap(generationData.m_gridSize.x, generationData.m_gridSize.y, generationData.m_gridSize.z, generationData.m_cubeSize, generationData.m_chunkPos, true, generationData.m_noiseSettings);

		float[,] normHeightMap = LevelNoiseGenerator.GenerateNoiseMap(generationData.m_gridSize.x + 2, generationData.m_gridSize.z + 2, generationData.m_cubeSize, generationData.m_chunkPos, true, generationData.m_noiseSettings);
		float[,,] normChunkNoise = LevelNoiseGenerator.Generate3DNoiseMap(generationData.m_gridSize.x + 2, generationData.m_gridSize.y, generationData.m_gridSize.z + 2, generationData.m_cubeSize, generationData.m_chunkPos, true, generationData.m_noiseSettings);

		generationData.normChunkData = new ChunkData(normHeightMap, normChunkNoise);
		generationData.chunkData = new ChunkData(heightMap, chunkNoise);

		lock (chunkThreadInfo) {
			chunkThreadInfo.Enqueue(new MapThreadInfo<ChunkGenerationData>(a_callback, generationData));
		}

	}


	private void RequestChunkData(ChunkGenerationData generationData, Action<ChunkGenerationData> a_callback) {
		if (Application.isPlaying) {
			Debug.Log("Starting chunk data generation.");
			ChunkDataThread(generationData, a_callback);
		} else {
			ThreadStart threadStart = delegate { ChunkDataThread(generationData, a_callback); };
			Debug.Log("Chunk Data Thread started.");

			Thread thread = new Thread(threadStart);
			thread.Name = "Chunk Data Thread.";
			thread.Start();
		}
	}
	#endregion

	#region Public Access Functions (Getters and Setters).

	public void StartGeneration(Vector3 a_chunkDimensions, Vector3Int gridSize, float cubeSize, float a_surfaceThreshold, NoiseSettings noiseSettings, AnimationCurve a_terrainHeights, float a_fHeightMultiplier) {
		//Clear the old queue and recreate it.
		if (chunkThreadInfo != null) {
			chunkThreadInfo.Clear();
			chunkThreadInfo = null;
		}
		chunkThreadInfo = new Queue<MapThreadInfo<ChunkGenerationData>>();
		chunkDimensions = a_chunkDimensions;
		ChunkGenerationData data = new ChunkGenerationData(gameObject.transform.position, a_chunkDimensions, gridSize, cubeSize, a_surfaceThreshold, noiseSettings, a_terrainHeights, a_fHeightMultiplier);
		RequestChunkData(data, OnNoiseDataRecieved);
	}

	public void SetChunkMeshFilter(MeshFilter meshFilter) {
		if (meshFilter) {
			levelMeshFilter = meshFilter;
		}
	}

	public void SetChunkMeshCollider(MeshCollider meshCollider) {
		if (meshCollider) {
			levelMeshCollider = meshCollider;
		}
	}
	#endregion


	#region Unity On Functions.
	private void OnDrawGizmos() {
		Gizmos.color = Color.white;
		Gizmos.DrawWireCube(transform.position, chunkDimensions);
	}

	#endregion


	struct ChunkGenerationData {
		public readonly Vector3 m_chunkPos;
		public readonly Vector3 m_chunkDimensions;
		public readonly Vector3Int m_gridSize;
		public readonly float m_cubeSize;
		public readonly float m_surfaceThreshold;
		public readonly NoiseSettings m_noiseSettings;
		public readonly AnimationCurve m_terrainHeights;
		public readonly float m_heightMultiplier;
		public ChunkData chunkData;
		public ChunkData normChunkData;

		public ChunkGenerationData(Vector3 a_chunkPos, Vector3 a_chunkDimensions, Vector3Int a_gridSize, float a_cubeSize, float a_surfaceThreshold, NoiseSettings a_noiseSettings, AnimationCurve a_terrainHeights, float a_fHeightMultiplier) {
			m_chunkPos = a_chunkPos;
			m_chunkDimensions = a_chunkDimensions;
			m_gridSize = a_gridSize;
			m_cubeSize = a_cubeSize;
			m_surfaceThreshold = a_surfaceThreshold;
			m_noiseSettings = new NoiseSettings(a_noiseSettings);
			m_terrainHeights = new AnimationCurve(a_terrainHeights.keys);
			m_heightMultiplier = a_fHeightMultiplier;
			float[,] floats = new float[1, 1];
			float[,,] noise = new float[1, 1, 1];
			chunkData = new ChunkData(floats, noise);
			normChunkData = new ChunkData(floats, noise);
		}
	}
}
