using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkScript : MonoBehaviour {
	#region Static Private Variables.
	private NoiseSettings chunkNoiseSettings = null;
	private float surfaceThreshold = 0.5f;
	private AnimationCurve terrainHeights;
	private float heightMultiplier = 5.0f; //Is limited to half the height of a single chunk.
	private float chunkCubeSize = 1.0f;
	private static Material chunkMaterial = null;

	//[SerializeField]
	//[Range(1, 100)]
	private int chunksPerFrame = 1;
	#endregion

	#region Private Variables.
	private Queue<SubChunkData> chunkQueue = null;
	private List<SubChunkData> chunkList = null;

	private Vector3 chunkSize = Vector3.one;
	private MeshFilter levelMeshFilter = null;
	private MeshCollider levelMeshCollider = null;
	private MeshRenderer levelMeshRenderer = null;
	private Mesh chunkMesh = null;

	private Vector3Int subgridSize = Vector3Int.zero;
	private int subChunksComplete = 0;
	private int subChunksGenerationStarted = 0;
	private int subChunksTotal = int.MaxValue;
	private bool chunkComplete = false;
	#endregion

	#region Private Functions.
	private void Start() {

	}

	private void Update() {
		if (chunkQueue != null && Application.isPlaying && !chunkComplete) {
			if (chunkQueue.Count > 0) {
				for (int i = 0; i < chunksPerFrame; i++) {

					SubChunkData data = chunkQueue.Dequeue();
					TerrainSubChunkScript currentGenerationScript = data.generationScript;
					if (currentGenerationScript != null) {
						currentGenerationScript.StartGeneration(data.subChunkPos, chunkSize, subgridSize,
							chunkCubeSize, surfaceThreshold, chunkNoiseSettings, terrainHeights, heightMultiplier,
							OnMeshDataRecieved);
					}

					subChunksGenerationStarted++;
					chunkList.Add(data);
				}

			}

			if (chunkList.Count > 0) {
				for (int i = 0; i < chunkList.Count; i++) {
					SubChunkData data = chunkList[i];
					data.generationScript.UpdateThreadInfo();
					chunkQueue.Enqueue(data);
				}
			}
		}
	}

	public void OnMeshDataRecieved(MeshData a_meshData) {
		//Debug.Log("Mesh Data Recieved.");
		//Null check for mesh.
		if (chunkMesh == null) {
			Debug.LogError("Invalid mesh in chunk: " + gameObject.name);
			return;
		}
		//Update tris. 
		List<int> triangles = new List<int>();
		for (int i = 0; i < chunkMesh.triangles.Length; i++) {
			triangles.Add(chunkMesh.triangles[i]);
		}
		int vertCount = chunkMesh.vertices.Length;
		for (int i = 0; i < a_meshData.triangles.Count; i++) {
			triangles.Add(a_meshData.triangles[i] + vertCount);
		}

		//Update verts.
		List<Vector3> vertices = new List<Vector3>();
		for (int i = 0; i < chunkMesh.vertices.Length; i++) {
			vertices.Add(chunkMesh.vertices[i]);
		}

		for (int i = 0; i < a_meshData.vertices.Count; i++) {
			vertices.Add(a_meshData.vertices[i]);
		}

		//Update Normals.
		List<Vector3> normals = new List<Vector3>();
		for (int i = 0; i < chunkMesh.normals.Length; i++) {
			normals.Add(chunkMesh.normals[i]);
		}

		for (int i = 0; i < a_meshData.normals.Length; i++) {
			normals.Add(a_meshData.normals[i]);
		}

		//Add them back to the list.
		chunkMesh.vertices = vertices.ToArray();
		chunkMesh.triangles = triangles.ToArray();
		chunkMesh.normals = normals.ToArray();

		#region BENCHMARK.

		subChunksComplete++;
		if (subChunksComplete >= subChunksTotal) {
			chunkComplete = true;
			BenchmarkScript.IncrementCompleteChunks();
		}

		#endregion
	}
	#endregion

	#region Public Access Functions.
	public void StartChunkGeneration(Vector3 a_chunkDimensions, Vector3Int gridSize, float cubeSize, float a_surfaceThreshold, NoiseSettings noiseSettings, AnimationCurve a_terrainHeights, float a_fHeightMultiplier) {
		//Create the mesh and attach it to the mesh filter and collider.
		chunkMesh = new Mesh();
		chunkMesh.name = gameObject.name + "_Mesh";
		if (levelMeshFilter == null) {
			levelMeshFilter = gameObject.AddComponent<MeshFilter>();
		}
		if (levelMeshCollider == null) {
			levelMeshCollider = gameObject.AddComponent<MeshCollider>();
		}

		if (levelMeshRenderer == null) {
			levelMeshRenderer = gameObject.AddComponent<MeshRenderer>();
		}
		levelMeshCollider.sharedMesh = chunkMesh;
		levelMeshFilter.mesh = chunkMesh;
		if (chunkMaterial) {
			levelMeshRenderer.material = chunkMaterial;
		}

		chunkComplete = false;
		chunkQueue = new Queue<SubChunkData>();
		chunkList = new List<SubChunkData>();
		chunkSize = a_chunkDimensions;
		chunkCubeSize = cubeSize;
		surfaceThreshold = a_surfaceThreshold;
		terrainHeights = a_terrainHeights;
		heightMultiplier = a_fHeightMultiplier;
		chunkNoiseSettings = noiseSettings;

		//Create the chunks and position them correctly.
		int startPosX = (0 - (int)(chunkSize.x * 0.5f));
		int startPosZ = (0 - (int)(chunkSize.z * 0.5f));
		int chunkCount = 0;
		for (int z = startPosZ; z < startPosZ + chunkSize.z; z++) {
			for (int x = startPosX; x < startPosX + chunkSize.x; x++) {
				TerrainSubChunkScript script = new TerrainSubChunkScript();

				//Calculate position relative to center of level.
				float localPosX = ((float)x) * chunkCubeSize;
				float localPosZ = ((float)z) * chunkCubeSize;
				Vector3 localPos = new Vector3(localPosX, 0.0f, localPosZ);
				Vector3 newPos = (gameObject.transform.position + localPos);


				//Add it to the chunk map.
				SubChunkData currentChunk = new SubChunkData();
				currentChunk.generationScript = script;
				currentChunk.subChunkPos = newPos;
				chunkQueue.Enqueue(currentChunk);
				chunkCount++;
			}
		}

		//Get number of control nodes.
		int sizeY = CalculateNumberOfControlNodesInGrid(chunkSize.y, chunkCubeSize);
		subgridSize = new Vector3Int(2, sizeY, 2);

		#region Benchmark Stuff.
		subChunksTotal = chunkCount;
		#endregion


		//Generate the chunk meshes here if in editor.
		if (!Application.isPlaying) {
			//Loop through the map and generate the meshes for each chunk.
			for (int i = 0; i < chunkQueue.Count; i++) {
				SubChunkData data = chunkQueue.Dequeue();
				TerrainSubChunkScript currentGenerationScript = data.generationScript;
				currentGenerationScript.StartGeneration(data.subChunkPos, chunkSize, subgridSize, chunkCubeSize, surfaceThreshold,
					chunkNoiseSettings, terrainHeights, heightMultiplier, OnMeshDataRecieved);
			}
		}

	}

	public static int CalculateNumberOfControlNodesInGrid(float gridSizeValue, float cubeSize) {
		int number = (int)(gridSizeValue / cubeSize);
		if (number < 1) {
			number = 1;
		}
		number = number + 1;
		return number;
	}

	public static void SetChunkMaterial(Material a_material) {
		chunkMaterial = a_material;
	}
	#endregion

	#region Unity Events.

	private void OnDrawGizmos() {
		Gizmos.color = Color.white;
		Gizmos.DrawWireCube(transform.position, chunkSize);
	}
	#endregion

	private struct SubChunkData {
		//Variables.
		public TerrainSubChunkScript generationScript;
		public Vector3 subChunkPos;
	}
}