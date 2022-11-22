using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class TerrainChunkScript : MonoBehaviour {
	#region Variables to assign via the unity inspector (SerilialiseField).
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

	#region Static Variables.

	public static bool showGizmos = false;
	#endregion

	#region Private Variables.
	private List<SubChunkData> chunkList = null;
	private static Queue<MapThreadInfo<MeshData>> meshDataInfoQueue = null;

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
	private MarchingCubes.CubeGrid cubeGrid = null;
	private int lastChunkIndex = 0;
	#endregion

	#region Private Functions.
	private void Start() {

	}

	private void Update() {
		if (chunkList == null || chunkList.Count <= 0 || chunkComplete) {
			//Early out.
			return;
		}

		if (lastChunkIndex < chunkList.Count && subChunksGenerationStarted < subChunksTotal) {
			for (int i = lastChunkIndex; i < lastChunkIndex + chunksPerFrame; i++) {
				SubChunkData data = chunkList[i];
				TerrainSubChunkScript currentGenerationScript = data.generationScript;
				if (currentGenerationScript != null) {
					ThreadStart threadStart = delegate {
						currentGenerationScript.StartGeneration(
							data.worldPos, data.subChunkPos, chunkSize, subgridSize, chunkCubeSize,
							surfaceThreshold, chunkNoiseSettings, terrainHeights, heightMultiplier, OnMeshDataRecieved, this);
					};
					Thread thread = new Thread(threadStart);
					thread.Name = "Subchunk_Thread";
					thread.Start();
				}
				subChunksGenerationStarted++;
			}

			lastChunkIndex += chunksPerFrame;
			if (subChunksGenerationStarted >= subChunksTotal) {
				lastChunkIndex = 0;
			}

		}

		//Check for any thread info to process.
		if (meshDataInfoQueue == null) {
			//Early out.
			return;
		}

		if (meshDataInfoQueue.Count > 0) {
			MapThreadInfo<MeshData> meshThreadInfo = meshDataInfoQueue.Dequeue();
			meshThreadInfo.callback(meshThreadInfo.parameter);
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
		lastChunkIndex = 0;
		chunkList = new List<SubChunkData>();
		chunkSize = a_chunkDimensions;
		chunkCubeSize = cubeSize;
		surfaceThreshold = a_surfaceThreshold;
		terrainHeights = a_terrainHeights;
		heightMultiplier = a_fHeightMultiplier;
		chunkNoiseSettings = noiseSettings;

		//Create the chunks and position them correctly.
		int chunkCount = 0;
		for (int z = 1; z < gridSize.z; z++) {
			for (int x = 1; x < gridSize.x; x++) {
				TerrainSubChunkScript script = new TerrainSubChunkScript();

				//Calculate position relative to center of level.
				float localPosX = (((float)x) * chunkCubeSize) - (gridSize.x * 0.5f);
				float localPosZ = (((float)z) * chunkCubeSize) - (gridSize.z * 0.5f);
				Vector3 localPos = new Vector3(localPosX, 0.0f, localPosZ);
				Vector3 newPos = localPos + gameObject.transform.position;

				//Add it to the chunk map.
				SubChunkData currentChunk = new SubChunkData();
				currentChunk.generationScript = script;
				currentChunk.worldPos = newPos;
				currentChunk.subChunkPos = localPos;
				chunkList.Add(currentChunk);
				chunkCount++;
			}
		}

		//Get number of control nodes.
		int sizeY = CalculateNumberOfControlNodesInGrid(chunkSize.y, chunkCubeSize);
		subgridSize = new Vector3Int(2, sizeY, 2);

		#region Benchmark Stuff.

		subChunksComplete = 0;
		subChunksTotal = chunkCount;
		#endregion
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

	public static void ClearMeshThreadInfo() {
		if (meshDataInfoQueue == null) {
			meshDataInfoQueue = new Queue<MapThreadInfo<MeshData>>();
		}
		meshDataInfoQueue.Clear();
	}

	public void EnqueueMeshThreadInfo(MapThreadInfo<MeshData> a_meshThreadInfo) {
		if (meshDataInfoQueue == null) {
			meshDataInfoQueue = new Queue<MapThreadInfo<MeshData>>();
		}

		lock (meshDataInfoQueue) {
			meshDataInfoQueue.Enqueue(a_meshThreadInfo);
		}
	}
	#endregion

	#region Unity Events.

	private void OnDrawGizmos() {
		Gizmos.color = Color.white;
		Gizmos.DrawWireCube(transform.position, chunkSize);
		if (showGizmos) {
			if (cubeGrid != null && showGizmos) {
				for (int x = 0; x < cubeGrid.cubes.GetLength(0); x++) {
					for (int y = 0; y < cubeGrid.cubes.GetLength(1); y++) {
						for (int z = 0; z < cubeGrid.cubes.GetLength(2); z++) {

							Gizmos.color = (cubeGrid.cubes[x, y, z].bottomFrontLeft.value <= 0) ? Color.black : Color.white;
							Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].bottomFrontLeft.position + gameObject.transform.position, Vector3.one * 0.4f);

							Gizmos.color = (cubeGrid.cubes[x, y, z].bottomBackRight.value <= 0) ? Color.black : Color.white;
							Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].bottomBackRight.position + gameObject.transform.position, Vector3.one * 0.4f);

							Gizmos.color = (cubeGrid.cubes[x, y, z].bottomBackLeft.value <= 0) ? Color.black : Color.white;
							Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].bottomBackLeft.position + gameObject.transform.position, Vector3.one * 0.4f);

							Gizmos.color = (cubeGrid.cubes[x, y, z].bottomFrontRight.value <= 0) ? Color.black : Color.white;
							Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].bottomFrontRight.position + gameObject.transform.position, Vector3.one * 0.4f);

							Gizmos.color = (cubeGrid.cubes[x, y, z].topFrontLeft.value <= 0) ? Color.black : Color.white;
							Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].topFrontLeft.position + gameObject.transform.position, Vector3.one * 0.4f);

							Gizmos.color = (cubeGrid.cubes[x, y, z].topFrontRight.value <= 0) ? Color.black : Color.white;
							Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].topFrontRight.position + gameObject.transform.position, Vector3.one * 0.4f);

							Gizmos.color = (cubeGrid.cubes[x, y, z].topBackLeft.value <= 0) ? Color.black : Color.white;
							Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].topBackLeft.position + gameObject.transform.position, Vector3.one * 0.4f);

							Gizmos.color = (cubeGrid.cubes[x, y, z].topBackRight.value <= 0) ? Color.black : Color.white;
							Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].topBackRight.position + gameObject.transform.position, Vector3.one * 0.4f);

							Gizmos.color = Color.black;
							Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].Position + gameObject.transform.position, Vector3.one * cubeGrid.cubes[x, y, z].size);

							Gizmos.color = Color.grey;
							Gizmos.DrawCube(cubeGrid.cubes[x, y, z].bottomLeft.position, Vector3.one * 0.15f);
							Gizmos.DrawCube(cubeGrid.cubes[x, y, z].bottomFront.position, Vector3.one * 0.15f);
							Gizmos.DrawCube(cubeGrid.cubes[x, y, z].bottomRight.position, Vector3.one * 0.15f);
							Gizmos.DrawCube(cubeGrid.cubes[x, y, z].bottomBack.position, Vector3.one * 0.15f);
							Gizmos.DrawCube(cubeGrid.cubes[x, y, z].topLeft.position, Vector3.one * 0.15f);
							Gizmos.DrawCube(cubeGrid.cubes[x, y, z].topFront.position, Vector3.one * 0.15f);
							Gizmos.DrawCube(cubeGrid.cubes[x, y, z].topRight.position, Vector3.one * 0.15f);
							Gizmos.DrawCube(cubeGrid.cubes[x, y, z].topBack.position, Vector3.one * 0.15f);
							Gizmos.DrawCube(cubeGrid.cubes[x, y, z].midBackLeft.position, Vector3.one * 0.15f);
							Gizmos.DrawCube(cubeGrid.cubes[x, y, z].midBackRight.position, Vector3.one * 0.15f);
							Gizmos.DrawCube(cubeGrid.cubes[x, y, z].midFrontLeft.position, Vector3.one * 0.15f);
							Gizmos.DrawCube(cubeGrid.cubes[x, y, z].midFrontRight.position, Vector3.one * 0.15f);
						}
					}
				}
			}
		}
	}
	#endregion

	private class SubChunkData {
		//Variables.
		public TerrainSubChunkScript generationScript;
		public Vector3 worldPos;
		public Vector3 subChunkPos;

		public SubChunkData() {

		}
	}
}