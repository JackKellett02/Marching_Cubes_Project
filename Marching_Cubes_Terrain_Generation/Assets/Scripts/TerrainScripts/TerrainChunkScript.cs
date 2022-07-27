using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkScript : MonoBehaviour {
	#region Variables to assign via the unity inspector (SerializeFields).
	private NoiseSettings chunkNoiseSettings = null;
	private float surfaceThreshold = 0.5f;
	private AnimationCurve terrainHeights;
	private float heightMultiplier = 5.0f; //Is limited to half the height of a single chunk.
	private Vector2Int levelSize = new Vector2Int(5, 5);
	private float chunkCubeSize = 1.0f;
	private Material chunkMaterial = null;

	//[SerializeField]
	//[Range(1, 100)]
	private int chunksPerFrame = 1;
	#endregion

	#region Private Variables.
	private Queue<TerrainSubChunkScript> chunkQueue = null;

	private Vector3 chunkSize = Vector3.one;
	private MeshFilter levelMeshFilter = null;
	private MeshCollider levelMeshCollider = null;
	private MeshRenderer levelMeshRenderer = null;
	private Mesh chunkMesh = null;

	private Vector3Int subgridSize = Vector3Int.zero;
	private int subChunksComplete = 0;
	private int subChunksTotal = int.MaxValue;
	#endregion

	#region Private Functions.
	private void Start() {

	}
	private void Update() {
		if (chunkQueue != null && Application.isPlaying) {
			for (int i = 0; i < chunksPerFrame; i++) {
				if (chunkQueue.Count > 0) {
					TerrainSubChunkScript currentGenerationScript = chunkQueue.Dequeue();
					if (currentGenerationScript != null) {
						currentGenerationScript.StartGeneration(gameObject.transform.position, chunkSize, subgridSize,
							chunkCubeSize, surfaceThreshold, chunkNoiseSettings, terrainHeights, heightMultiplier, OnMeshDataRecieved);
					}
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

		//Add them back to the list.
		chunkMesh.vertices = a_meshData.vertices.ToArray();
		chunkMesh.triangles = triangles.ToArray();
		chunkMesh.normals = a_meshData.normals;

		#region BENCHMARK.

		subChunksComplete++;
		if (subChunksComplete >= subChunksTotal) {
			BenchmarkScript.IncrementCompleteChunks();
		}

		#endregion
	}
	#endregion

	#region Public Access Functions.

	public void SetChunkSettings(float a_threshhold, float a_heightMultiplier, Vector2Int a_levelSize, Vector3 a_chunkSize, float a_cubeSize) {
		surfaceThreshold = a_threshhold;
		heightMultiplier = a_heightMultiplier;
		levelSize = a_levelSize;
		chunkSize = a_chunkSize;
		chunkCubeSize = a_cubeSize;
	}

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

		chunkQueue = new Queue<TerrainSubChunkScript>();
		chunkSize = a_chunkDimensions;
		chunkCubeSize = cubeSize;
		surfaceThreshold = a_surfaceThreshold;
		terrainHeights = a_terrainHeights;
		heightMultiplier = a_fHeightMultiplier;

		//Make sure the chunk is valid before generation occurs.
		ValidateChunk();

		//Make sure the level falloff map is initialised.
		Vector2Int test = new Vector2Int(levelSize.x - 2, levelSize.y - 2);
		TerrainSubChunkScript.InitialiseFalloffMap(test, new Vector2(chunkSize.x, chunkSize.z), chunkCubeSize);

		//Create the chunks and position them correctly.
		int startPosX = (0 - (int)(levelSize.x * 0.5f));
		int startPosZ = (0 - (int)(levelSize.y * 0.5f));
		int chunkCount = 0;
		for (int z = startPosZ; z < startPosZ + levelSize.y; z++) {
			for (int x = startPosX; x < startPosX + levelSize.x; x++) {
				TerrainSubChunkScript currentChunk = new TerrainSubChunkScript();

				//Calculate position relative to center of level.
				float localPosX = ((float)x) * chunkSize.x;
				float localPosZ = ((float)z) * chunkSize.z;
				Vector3 localPos = new Vector3(localPosX, 0.0f, localPosZ);
				Vector3 newPos = (gameObject.transform.position + localPos);


				//Add it to the chunk map.
				chunkQueue.Enqueue(currentChunk);
				chunkCount++;
			}
		}

		//Get number of control nodes.
		int sizeX = CalculateNumberOfControlNodesInGrid(chunkSize.x, chunkCubeSize);
		int sizeY = CalculateNumberOfControlNodesInGrid(chunkSize.y, chunkCubeSize);
		int sizeZ = CalculateNumberOfControlNodesInGrid(chunkSize.z, chunkCubeSize);
		subgridSize = new Vector3Int(2, sizeY, 2);

		#region Benchmark Stuff.
		subChunksTotal = chunkCount;
		#endregion


		//Generate the chunk meshes here if in editor.
		if (!Application.isPlaying) {
			//Loop through the map and generate the meshes for each chunk.
			for (int i = 0; i < chunkQueue.Count; i++) {
				TerrainSubChunkScript currentGenerationScript = chunkQueue.Dequeue();
				currentGenerationScript.StartGeneration(gameObject.transform.position, chunkSize, subgridSize, chunkCubeSize, surfaceThreshold,
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
	#endregion

	#region Unity Events.

	private void ValidateChunk() {
		//Make sure there aren't too many verts in a single chunk mesh.
		bool chunkValid = CheckNotTooManyVerts() && CubesFitInChunk();
		bool notTooManyVerts = false;
		bool cubesFit = false;
		int numIters = 0;
		while (!chunkValid) {
			if (numIters > 100000) {
				break;
			}
			numIters++;

			//Calculate num verts in a single chunk.
			if (!notTooManyVerts) {
				int numCubesInChunk = (int)(chunkSize.x / chunkCubeSize) * (int)(chunkSize.y / chunkCubeSize) * (int)(chunkSize.z / chunkCubeSize);
				int numVerts = numCubesInChunk * 12;
				notTooManyVerts = numVerts <= 65535;
				if (!notTooManyVerts) {
					chunkCubeSize += 0.5f;
				}
			}




			//Check that cubes neatly fit into chunk.
			if (notTooManyVerts) {
				//Check if the cubes fit in the chunk neatly.
				if (!cubesFit) {
					//Calculate num of cubes in each axis. Then calculate the distance that covers.
					//If the distance is less than the length of the length of the chunk in that axis then the cubes don't fit.
					int numX = (int)(chunkSize.x / chunkCubeSize);
					int numY = (int)(chunkSize.y / chunkCubeSize);
					int numZ = (int)(chunkSize.z / chunkCubeSize);
					Vector3 axisLengths = new Vector3(numX * chunkCubeSize, numY * chunkCubeSize, numZ * chunkCubeSize);
					cubesFit = axisLengths.x >= chunkSize.x && axisLengths.y >= chunkSize.y && axisLengths.z >= chunkSize.z;
					if (!cubesFit) {
						chunkCubeSize += 0.5f;
					}
				} else {
					break;
				}
			}

		}

		//Make sure the grid is not smaller than the cube in any axis.
		Vector3 chunkSizeNew = chunkSize;
		if (chunkSize.x < chunkCubeSize) {
			chunkSizeNew.x = chunkCubeSize;
		}

		if (chunkSize.y < chunkCubeSize) {
			chunkSizeNew.y = chunkCubeSize;
		}

		if (chunkSize.z < chunkCubeSize) {
			chunkSizeNew.z = chunkCubeSize;
		}

		//Reset the new grid size.
		chunkSize = chunkSizeNew;
	}

	private bool CheckNotTooManyVerts() {
		//Check that the grid isn't too big.
		int sizeX = CalculateNumberOfControlNodesInGrid(chunkSize.x, chunkCubeSize) - 1;
		int sizeY = CalculateNumberOfControlNodesInGrid(chunkSize.y, chunkCubeSize) - 1;
		int sizeZ = CalculateNumberOfControlNodesInGrid(chunkSize.z, chunkCubeSize) - 1;
		//Number of verts.
		int numCubes = (sizeX * sizeY * sizeZ);
		int numVerts = numCubes * 12;
		if (numVerts > 60000) {
			return false;
		} else {
			return true;
		}

	}

	private bool CubesFitInChunk() {
		if (chunkSize.x % chunkCubeSize != 0 || chunkSize.y % chunkCubeSize != 0 || chunkSize.z % chunkCubeSize != 0) {
			return false;
		} else {
			return true;
		}
	}

	private void OnDrawGizmos() {
		Gizmos.color = Color.white;
		Gizmos.DrawWireCube(transform.position, chunkSize);
	}

	private void OnValidate() {
		//Ensure cube size stays in increments of 0.5
		float newCubeSize = Mathf.Round(chunkCubeSize);
		float checkSize = newCubeSize;
		if (newCubeSize < chunkCubeSize) {
			checkSize += 0.5f;
		} else if (newCubeSize > chunkCubeSize) {
			checkSize -= 0.5f;
		}

		float lowerBound = checkSize - 0.25f;
		float upperBound = checkSize + 0.25f;
		if (lowerBound < checkSize && upperBound > checkSize) {
			chunkCubeSize = checkSize;
		} else {
			chunkCubeSize = newCubeSize;
		}

		if (chunkCubeSize < 0.5f) {
			chunkCubeSize = 0.5f;
		}

		//Make sure the grid is not smaller than the cube in any axis.
		Vector3 chunkSizeNew = chunkSize;
		if (chunkSize.x < chunkCubeSize) {
			chunkSizeNew.x = chunkCubeSize;
		}

		if (chunkSize.y < chunkCubeSize) {
			chunkSizeNew.y = chunkCubeSize;
		}

		if (chunkSize.z < chunkCubeSize) {
			chunkSizeNew.z = chunkCubeSize;
		}

		//Reset the new grid size.
		chunkSize = chunkSizeNew;

		//Ensure level will always equal atleast one chunk big.
		if (levelSize.x <= 0) {
			levelSize.x = 1;
		}

		if (levelSize.y <= 0) {
			levelSize.y = 1;
		}

		if (levelSize.x % 2 == 0) {
			levelSize.x += 1;
		}

		if (levelSize.y % 2 == 0) {
			levelSize.y += 1;
		}

		//Make sure height multiplier is not too big and not too small.
		if (heightMultiplier > chunkSize.y * 0.5f) {
			heightMultiplier = chunkSize.y * 0.5f;
		}
		if (heightMultiplier < 1.0f) {
			heightMultiplier = 1.0f;
		}
	}
	#endregion
}