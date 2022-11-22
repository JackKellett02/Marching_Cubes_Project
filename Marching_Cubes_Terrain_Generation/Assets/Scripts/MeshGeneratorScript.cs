using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The base class for any mesh generation using the marching cubes script with threading.
/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
/// DO NOT INSTANTIATE THIS CLASS AS IT IS ONLY THE BASE CLASS
/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
/// </summary>
public class MeshGeneratorScript {
	#region Private Variables.
	protected Vector3 chunkDimensions = Vector3.one;
	protected Action<MeshData> meshCallback = null;
	protected TerrainChunkScript instance = null;
	#endregion

	#region Virtual Functions.

	protected virtual float[,,] PopulateGridMap(int xSize, int ySize, int zSize, float a_heightMultiplier,
		AnimationCurve a_terrainHeights, ChunkGenerationData chunkData, bool useNormData) {
		return new float[0, 0, 0];
	}

	protected virtual void ChunkDataThread(ChunkGenerationData generationData, Action<ChunkGenerationData> a_callback) {

	}

	#endregion

	#region Protected class Functions.
	protected void RequestMeshData(MapData chunkData, Action<MeshData> a_callback) {
		MeshDataThread(chunkData, a_callback);

		//ThreadStart threadStart = delegate {
		//	MeshDataThread(chunkData, a_callback);
		//};
		//Thread thread = new Thread(threadStart);
		//thread.Name = "Mesh Data Thread.";
		//thread.Start();
		//Debug.Log("Mesh Data Thread Started.");
	}

	protected void RequestChunkData(ChunkGenerationData generationData, Action<ChunkGenerationData> a_callback) {
		ChunkDataThread(generationData, a_callback);

		//ThreadStart threadStart = delegate { ChunkDataThread(generationData, a_callback); }
		//Thread thread = new Thread(threadStart);
		//thread.Name = "Chunk Data Thread.";
		//thread.Start();
		//Debug.Log("Chunk Data Thread started.");
	}

	protected void OnNoiseDataRecieved(ChunkGenerationData a_chunkData) {
		//Debug.Log("Chunk Data Recieved.");
		GenerateChunk(a_chunkData);
	}
	#endregion

	#region Private Functions.

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
	#endregion

	#region Public Access Functions (Getters and Setters).

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

public struct ChunkGenerationData {
	public readonly Vector3 m_chunkWorldPos;
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

	public ChunkGenerationData(Vector3 a_chunkWorldPos, Vector3 a_chunkPos, Vector3 a_chunkDimensions, Vector3Int a_gridSize, float a_cubeSize, float a_surfaceThreshold, NoiseSettings a_noiseSettings, AnimationCurve a_terrainHeights, float a_fHeightMultiplier) {
		m_chunkWorldPos = a_chunkWorldPos;
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
