using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class LevelGeneratorScript : MonoBehaviour {
	#region Variables to assign via the unity inspector (SerializeFields).
	[SerializeField]
	private NoiseSettings noiseSettings = null;

	[SerializeField]
	private bool randomiseSeed = true;

	[SerializeField]
	[Range(0.0f, 1.0f)]
	private float surfaceThreshold = 0.5f;

	[SerializeField]
	private AnimationCurve terrainHeights;

	[SerializeField]
	private float heightMultiplier = 5.0f; //Is limited to half the height of a single chunk.

	[SerializeField]
	private Vector2Int levelSize = new Vector2Int(5, 5);

	[SerializeField]
	private Vector3 chunkSize = new Vector3(10.0f, 10.0f, 10.0f);

	[SerializeField]
	private float chunkCubeSize = 1.0f;

	[SerializeField]
	private Material chunkMaterial = null;

	//[SerializeField]
	//[Range(1, 100)]
	private int chunksPerFrame = 1;
	#endregion

	#region Private Variables.
	private List<List<GameObject>> levelChunks = null;
	private Queue<GameObject> chunkQueue = null;
	#endregion

	#region Private Functions.
	private void Start() {

	}
	private void Update() {
		if (chunkQueue != null && Application.isPlaying) {
			for (int i = 0; i < chunksPerFrame; i++) {
				if (chunkQueue.Count > 0) {
					GameObject currentChunk = chunkQueue.Dequeue();
					TerrainSubChunkScript currentGenerationScript = currentChunk.GetComponent<TerrainSubChunkScript>();
					if (currentGenerationScript != null) {
						//Get number of control nodes.
						int sizeX = CalculateNumberOfControlNodesInGrid(chunkSize.x, chunkCubeSize);
						int sizeY = CalculateNumberOfControlNodesInGrid(chunkSize.y, chunkCubeSize);
						int sizeZ = CalculateNumberOfControlNodesInGrid(chunkSize.z, chunkCubeSize);
						Vector3Int gridSize = new Vector3Int(sizeX, sizeY, sizeZ);

						currentGenerationScript.StartGeneration(chunkSize, gridSize, chunkCubeSize, surfaceThreshold,
							noiseSettings, terrainHeights, heightMultiplier);
					}
				}
			}
		}

	}

	private GameObject ConstructChunk(Transform parent) {
		//Initialise the new chunk.
		GameObject newChunk = new GameObject();
		newChunk.transform.parent = parent;

		//Add the correct components.
		TerrainSubChunkScript chunkGenerationScript = newChunk.AddComponent<TerrainSubChunkScript>();
		MeshFilter meshFilter = newChunk.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = newChunk.AddComponent<MeshRenderer>();
		if (chunkMaterial) {
			meshRenderer.material = chunkMaterial;
		}
		MeshCollider meshCollider = newChunk.AddComponent<MeshCollider>();
		chunkGenerationScript.SetChunkMeshCollider(meshCollider);
		chunkGenerationScript.SetChunkMeshFilter(meshFilter);

		//Return it.
		return newChunk;
	}
	#endregion

	#region Public Access Functions.

	public void SetLevelSettings(float a_threshhold, float a_heightMultiplier, Vector2Int a_levelSize, Vector3 a_chunkSize, float a_cubeSize)
	{
		surfaceThreshold = a_threshhold;
		heightMultiplier = a_heightMultiplier;
		levelSize = a_levelSize;
		chunkSize = a_chunkSize;
		chunkCubeSize = a_cubeSize;
	}

	public void CreateLevel() {
		if (randomiseSeed)
		{
			RandomiseSeed();
		}
		if (levelChunks != null) {
			levelChunks.Clear();
			levelChunks = null;
		}
		levelChunks = new List<List<GameObject>>();
		chunkQueue = new Queue<GameObject>();

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
			List<GameObject> currentRow = new List<GameObject>();
			for (int x = startPosX; x < startPosX + levelSize.x; x++) {
				GameObject currentChunk;

				//Calculate position relative to center of level.
				float localPosX = ((float)x) * chunkSize.x;
				float localPosZ = ((float)z) * chunkSize.z;
				Vector3 localPos = new Vector3(localPosX, 0.0f, localPosZ);
				Vector3 newPos = (gameObject.transform.position + localPos);

				//Construct the new chunk.
				currentChunk = ConstructChunk(this.transform);
				currentChunk.SetActive(true);
				currentChunk.name = "Chunk X: " + newPos.x + " Z: " + newPos.z;
				currentChunk.gameObject.transform.position = newPos;


				//Add it to the chunk map.
				currentRow.Add(currentChunk);
				chunkQueue.Enqueue(currentChunk);
				chunkCount++;
			}
			levelChunks.Add(currentRow);
		}

		//Get number of control nodes.
		int sizeX = CalculateNumberOfControlNodesInGrid(chunkSize.x, chunkCubeSize);
		int sizeY = CalculateNumberOfControlNodesInGrid(chunkSize.y, chunkCubeSize);
		int sizeZ = CalculateNumberOfControlNodesInGrid(chunkSize.z, chunkCubeSize);
		Vector3Int gridSize = new Vector3Int(sizeX, sizeY, sizeZ);

		//Generate the chunk meshes here if in editor.
		if (!Application.isPlaying) {
			//Clear the chunk queue as it won't be used.
			chunkQueue.Clear();


			//Loop through the map and generate the meshes for each chunk.
			for (int z = 0; z < levelChunks.Count; z++) {
				for (int x = 0; x < levelChunks[z].Count; x++) {
					TerrainSubChunkScript currentGenerationScript = levelChunks[z][x].GetComponent<TerrainSubChunkScript>();
					if (currentGenerationScript != null) {
						currentGenerationScript.StartGeneration(chunkSize, gridSize, chunkCubeSize,
							surfaceThreshold, noiseSettings, terrainHeights, heightMultiplier);
					}
				}
			}
		}

		#region Benchmark Stuff.
		BenchmarkScript.AddTotalChunkCount(chunkCount);
		BenchmarkScript.AddVoxelsPerChunk(sizeX * sizeY * sizeZ);
		#endregion
	}

	public void DestroyAllChunks(int destroyIterations) {
		//Then check if the level generator gameobject has any children.
		List<GameObject> children = new List<GameObject>();
		for (int j = 0; j < destroyIterations; j++) {
			for (int i = 0; i < gameObject.transform.childCount; i++) {
				DestroyImmediate(gameObject.transform.GetChild(0).gameObject);
			}
		}

		//Clear the level chunk list and set it to null.
		if (levelChunks != null) {
			levelChunks.Clear();
		}
		levelChunks = null;
	}

	public void RandomiseSeed()
	{
		noiseSettings.seed = (int)(noiseSettings.seed * Time.time) + (int)(noiseSettings.seed * Time.deltaTime) + (int)(noiseSettings.seed * (1.0f / Time.deltaTime));
		System.Random prng = NoiseUtility.SeedNoise(noiseSettings);
		noiseSettings.seed = prng.Next(-100000, 100000);
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
		Vector3 locLevelSize = chunkSize;
		locLevelSize.x *= levelSize.x;
		locLevelSize.z *= levelSize.y;
		Gizmos.DrawWireCube(gameObject.transform.position, locLevelSize);
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

public struct MapData {
	public readonly float[,,] map;
	public readonly float[,,] normalMap;
	public readonly float cubeSize;

	public MapData(float[,,] gridMap, float[,,] normMap, float size) {
		map = gridMap;
		normalMap = normMap;
		cubeSize = size;
	}
}

public struct ChunkData {
	public readonly float[,] heightMap;
	public readonly float[,,] chunkNoise;

	public ChunkData(float[,] heights, float[,,] noise) {
		heightMap = heights;
		chunkNoise = noise;
	}
}