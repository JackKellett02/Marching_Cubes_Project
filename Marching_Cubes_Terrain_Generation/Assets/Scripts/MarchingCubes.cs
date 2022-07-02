using System;
using System.Collections.Generic;
using UnityEngine;

public static class MarchingCubes {
	#region Private Functions.
	private static MeshData GetMeshData(CubeGrid cubeGrid, bool calcNormals) {
		MeshData meshData = new MeshData();
		List<int> tris = new List<int>();
		List<Vector3> verts = new List<Vector3>();
		Dictionary<Vector3, int> globalVertDictionary = new Dictionary<Vector3, int>();

		//Loop through the cube grid.
		if (cubeGrid != null) {
			for (int x = 0; x < cubeGrid.cubes.GetLength(0); x++) {
				for (int y = 0; y < cubeGrid.cubes.GetLength(1); y++) {
					for (int z = 0; z < cubeGrid.cubes.GetLength(2); z++) {
						if (cubeGrid.cubes[x, y, z].configuration > 0 || cubeGrid.cubes[x, y, z].configuration < 255) {
							//Get the triangle indices and vertices for the input cube.
							List<int> triangles = Cube.triangleTable.GetTriangles(cubeGrid.cubes[x, y, z].configuration);
							List<Vector3> vertices = cubeGrid.cubes[x, y, z].vertices;

							//Create a local dictionary to store the indices for all the verts.
							Dictionary<int, int> localVertDictionary = new Dictionary<int, int>();

							//Loop through all the verts figure out whether or not they need to be added.
							for (int i = 0; i < vertices.Count; i++) {
								Vector3 currentVert = vertices[i];

								int currentVertIndex;
								if (globalVertDictionary.ContainsKey(currentVert)) {
									//If the vert already exists get the index.
									currentVertIndex = globalVertDictionary[currentVert];
								} else {
									//Figure out the correct index.
									verts.Add(currentVert);
									currentVertIndex = verts.IndexOf(currentVert);

									//Add the vert to the global dictionary.
									globalVertDictionary.Add(currentVert, currentVertIndex);
								}

								//Add the verts new index to the local dictionary.
								localVertDictionary.Add(i, currentVertIndex);
							}

							//Loop through all the triangles and ensure the indices are correct.
							for (int i = 0; i < triangles.Count; i++) {
								int processedIndex = localVertDictionary[triangles[i]];
								tris.Add(processedIndex);
							}
						}
					}
				}
			}

			meshData.vertices = verts;
			meshData.triangles = tris;
			if (calcNormals) {
				meshData.CalculateNormals();
			}
		}

		return meshData;
	}
	#endregion

	#region Public Access Functions.
	public static MeshData GenerateMesh(float[,,] map, float[,,] normalsMap, float cubeSize) {
		//Create the cube grid and the vert and tri lists.
		CubeGrid cubeGrid = new CubeGrid(map, cubeSize);
		MeshData chunkMeshData = GetMeshData(cubeGrid, false);

		//Create the cube grid used to calculate the normals.
		CubeGrid normalGrid = new CubeGrid(normalsMap, cubeSize);
		MeshData normalMeshData = GetMeshData(normalGrid, true);

		chunkMeshData.normals = normalMeshData.GetNormals(chunkMeshData.vertices);
		return chunkMeshData;
	}

	public static Mesh DebugGenMesh(int a_config, float[,,] map) {
		//Create the cube grid and the vert and tri lists.
		CubeGrid cubeGrid = new CubeGrid(map, 1.0f);
		List<Vector3> meshVertices = new List<Vector3>();
		List<int> meshTriangles = new List<int>();
		Mesh mesh = new Mesh();

		//Loop through the cube grid.
		if (cubeGrid != null) {
			for (int x = 0; x < cubeGrid.cubes.GetLength(0); x++) {
				for (int y = 0; y < cubeGrid.cubes.GetLength(1); y++) {
					for (int z = 0; z < cubeGrid.cubes.GetLength(2); z++) {
						if (cubeGrid.cubes[x, y, z].configuration > 0 || cubeGrid.cubes[x, y, z].configuration < 255) {
							//Get the triangles for the current cube.
							List<int> localTris = new List<int>();
							List<int> tempTris = Cube.triangleTable.GetTriangles(a_config);
							for (int i = 0; i < tempTris.Count; i++) {
								localTris.Add(tempTris[i]);
							}

							//Modify the tri indexes based on how many items there are current in the mesh vertices list.
							for (int i = 0; i < localTris.Count; i++) {
								//Figure out next index.
								int newIndex = localTris[i] + meshVertices.Count;
								localTris[i] = newIndex;

								//Add it to the mesh triangles list.
								meshTriangles.Add(localTris[i]);
							}

							//Get the vertices list for the current cube.
							List<Vector3> localVerts = cubeGrid.cubes[x, y, z].vertices;

							//Add all the mesh vertex points to the array.
							for (int i = 0; i < localVerts.Count; i++) {
								meshVertices.Add(localVerts[i]);
							}
						}
					}
				}
			}

			//Generate the actual mesh.
			mesh.vertices = meshVertices.ToArray();
			mesh.triangles = meshTriangles.ToArray();
			mesh.RecalculateNormals();
			
		}
		return mesh;
	}

	#endregion

	#region Marchine Cubes Classes.

	public class CubeGrid {
		public Cube[,,] cubes;

		public CubeGrid(float[,,] map, float cubeSize) {
			int nodeCountX = map.GetLength(0);
			int nodeCountY = map.GetLength(1);
			int nodeCountZ = map.GetLength(2);
			float mapWidth = nodeCountX * cubeSize;
			float mapHeight = nodeCountY * cubeSize;
			float mapLength = nodeCountZ * cubeSize;

			ControlNode[,,] controlNodes = new ControlNode[nodeCountX, nodeCountY, nodeCountZ];

			for (int x = 0; x < nodeCountX; x++) {
				for (int y = 0; y < nodeCountY; y++) {
					for (int z = 0; z < nodeCountZ; z++) {
						//Calculate pos for control node.
						Vector3 position = new Vector3((-mapWidth / 2) + x * cubeSize + (cubeSize / 2),
													   (-mapHeight / 2) + y * cubeSize + (cubeSize / 2),
													   (-mapLength / 2) + z * cubeSize + (cubeSize / 2));

						//Create the control node.
						//Debug.Log("Node Value: " + map[x, y, z]);
						controlNodes[x, y, z] = new ControlNode(position, map[x, y, z], cubeSize);
					}
				}
			}

			for (int x = 0; x < nodeCountX; x++) {
				for (int y = 0; y < nodeCountY; y++) {
					for (int z = 0; z < nodeCountZ; z++) {
						//Interpolate the current control node vertices based on the surrounding nodes.
						ControlNode above = null;
						ControlNode right = null;
						ControlNode forward = null;
						if (x < nodeCountX - 1) {
							right = controlNodes[x + 1, y, z];
						}
						if (y < nodeCountY - 1) {
							above = controlNodes[x, y + 1, z];
						}
						if (z < nodeCountZ - 1) {
							forward = controlNodes[x, y, z + 1];
						}
						controlNodes[x, y, z].InterpolateVertexPositions(above, right, forward);
					}
				}
			}

			//Create cube grid.
			cubes = new Cube[nodeCountX - 1, nodeCountY - 1, nodeCountZ - 1];
			for (int x = 0; x < nodeCountX - 1; x++) {
				for (int y = 0; y < nodeCountY - 1; y++) {
					for (int z = 0; z < nodeCountZ - 1; z++) {
						cubes[x, y, z] = new Cube(controlNodes[x, y, z + 1], controlNodes[x + 1, y, z + 1],
												   controlNodes[x, y, z], controlNodes[x + 1, y, z],
												   controlNodes[x, y + 1, z + 1], controlNodes[x + 1, y + 1, z + 1],
												   controlNodes[x, y + 1, z], controlNodes[x + 1, y + 1, z], cubeSize);
					}
				}
			}
		}
	}

	public class Cube {
		public Vector3 Position {
			get {
				Vector3 pos = bottomFront.position;
				pos.z -= (size / 2);
				pos.y += (size / 2);
				return pos;
			}
		}

		public ControlNode bottomFrontLeft, bottomFrontRight, bottomBackLeft, bottomBackRight, topFrontLeft, topFrontRight, topBackLeft, topBackRight;
		public Node bottomLeft, bottomFront, bottomRight, bottomBack, topLeft, topFront, topRight, topBack, midFrontLeft, midFrontRight, midBackRight, midBackLeft;
		public List<Vector3> vertices = new List<Vector3>();
		public static TriangleTable triangleTable = new TriangleTable();
		public int configuration = 0;
		public float size;

		public Cube(ControlNode _bottomFrontLeft, ControlNode _bottomFrontRight, ControlNode _bottomBackLeft,
			ControlNode _bottomBackRight, ControlNode _topFrontLeft, ControlNode _topFrontRight,
			ControlNode _topBackLeft, ControlNode _topBackRight, float cubeSize) {
			size = cubeSize;

			//Assign control nodes.
			bottomFrontLeft = _bottomFrontLeft;
			bottomFrontRight = _bottomFrontRight;
			bottomBackLeft = _bottomBackLeft;
			bottomBackRight = _bottomBackRight;
			topFrontLeft = _topFrontLeft;
			topFrontRight = _topFrontRight;
			topBackLeft = _topBackLeft;
			topBackRight = _topBackRight;

			//Assign center nodes.
			bottomLeft = bottomBackLeft.forward;
			bottomFront = bottomFrontLeft.right;
			bottomRight = bottomBackRight.forward;
			bottomBack = bottomBackLeft.right;
			topLeft = topBackLeft.forward;
			topFront = topFrontLeft.right;
			topRight = topBackRight.forward;
			topBack = topBackLeft.right;
			midFrontLeft = bottomFrontLeft.above;
			midFrontRight = bottomFrontRight.above;
			midBackRight = bottomBackRight.above;
			midBackLeft = bottomBackLeft.above;

			//Get cube configuration.
			if (bottomFrontLeft.value <= 0) {
				configuration += 128;
			}
			if (bottomFrontRight.value <= 0) {
				configuration += 64;
			}
			if (bottomBackRight.value <= 0) {
				configuration += 32;
			}
			if (bottomBackLeft.value <= 0) {
				configuration += 16;
			}
			if (topFrontLeft.value <= 0) {
				configuration += 8;
			}
			if (topFrontRight.value <= 0) {
				configuration += 4;
			}
			if (topBackRight.value <= 0) {
				configuration += 2;
			}
			if (topBackLeft.value <= 0) {
				configuration += 1;
			}

			//Create the vertices list.
			vertices = new List<Vector3>();
			vertices.Add(bottomFront.position);
			vertices.Add(bottomRight.position);
			vertices.Add(bottomBack.position);
			vertices.Add(bottomLeft.position);
			vertices.Add(topFront.position);
			vertices.Add(topRight.position);
			vertices.Add(topBack.position);
			vertices.Add(topLeft.position);
			vertices.Add(midFrontLeft.position);
			vertices.Add(midFrontRight.position);
			vertices.Add(midBackRight.position);
			vertices.Add(midBackLeft.position);
		}
	}

	public class Node {
		public Vector3 position;
		public int vertexIndex = -1;

		public Node(Vector3 _pos) {
			position = _pos;
		}
	}

	public class ControlNode : Node {
		public float value;
		public Node above, right, forward;
		private float size;

		public ControlNode(Vector3 _pos, float _value, float cubeSize) : base(_pos) {
			value = _value;
			size = cubeSize;
			//above = new Node(position + Vector3.up * (cubeSize / 2.0f));
			//right = new Node(position + Vector3.right * (cubeSize / 2.0f));
			//forward = new Node(position + Vector3.forward * (cubeSize / 2.0f));
		}

		public void InterpolateVertexPositions(ControlNode _above, ControlNode _right, ControlNode _forward) {
			if (_above != null) {
				above = new Node(CalculateVertexPosition(this, _above));
			} else {
				above = new Node(Vector3.zero);
			}

			if (_right != null) {
				right = new Node(CalculateVertexPosition(this, _right));
			} else {
				right = new Node(Vector3.zero);
			}

			if (_forward != null) {
				forward = new Node(CalculateVertexPosition(this, _forward));
			} else {
				forward = new Node(Vector3.zero);
			}
		}

		private Vector3 CalculateVertexPosition(ControlNode nodeA, ControlNode nodeB) {
			//Get the positions of the control nodes.
			Vector3 posA = new Vector3(nodeA.position.x, nodeA.position.y, nodeA.position.z);
			Vector3 posB = new Vector3(nodeB.position.x, nodeB.position.y, nodeB.position.z);

			//Interpolate between the two corner points based on their values to estimate the point along the edge at which the value would be zero.
			float t = Mathf.InverseLerp(nodeA.value, nodeB.value, 0);
			Vector3 vertexPos = Vector3.Lerp(posA, posB, t);

			//Return the vertex position.
			return vertexPos;
		}
	}

	public class TriangleTable {
		//Variables.
		private Dictionary<int, List<int>> triangleTable;

		//Functions.
		private List<int> AddTriangles(params int[] indexArray) {
			List<int> triangles = new List<int>();
			for (int i = 0; i < indexArray.Length; i++) {


				if (indexArray[i] > 11) {
					//Early out and log error.
					Debug.Log("Index Array length: " + indexArray[i]);
					Debug.LogError("ERROR: Invalid index passed into 'AddTriangles' function in 'MarchingCubes.cs'");
				}
				triangles.Add(indexArray[i]);
			}

			if (triangles.Count <= 0 || (triangles.Count % 3) != 0) {
				Debug.Log(triangles.Count);
				Debug.LogError(
					"Invalid triangle list has been created in the function 'AddTriangles' in 'MarchingCubes.cs'");
			}
			return triangles;
		}

		public List<int> GetTriangles(int a_configuration) {
			List<int> triangleList = new List<int>();
			if (triangleTable.Count > 0 && a_configuration > 0 && a_configuration < 255) {
				triangleList = triangleTable[a_configuration];
			}

			return triangleList;
		}

		public TriangleTable() {
			//triangles = AddTriangles();
			//triangleTable.Add(, triangles);
			//Create the triangle table.
			triangleTable = new Dictionary<int, List<int>>();

			//Populate it.
			//Configuration 1.
			List<int> triangles = new List<int>();
			triangles.Add(6);
			triangles.Add(7);
			triangles.Add(11);
			triangleTable.Add(1, triangles);

			//Configuration 2.
			triangles = new List<int>();
			triangles.Add(5);
			triangles.Add(6);
			triangles.Add(10);
			triangleTable.Add(2, triangles);

			//Configuration 3.
			triangles = new List<int>();
			triangles.Add(5);
			triangles.Add(7);
			triangles.Add(10);
			triangles.Add(10);
			triangles.Add(7);
			triangles.Add(11);
			triangleTable.Add(3, triangles);

			//Configuration 4.
			triangles = new List<int>();
			triangles.Add(4);
			triangles.Add(5);
			triangles.Add(9);
			triangleTable.Add(4, triangles);

			//Configuration 5.
			triangles = new List<int>();
			triangles.Add(6);
			triangles.Add(7);
			triangles.Add(11);
			triangles.Add(4);
			triangles.Add(5);
			triangles.Add(9);
			triangleTable.Add(5, triangles);

			//Configuration 6.
			triangles = new List<int>();
			triangles.Add(4);
			triangles.Add(6);
			triangles.Add(9);
			triangles.Add(9);
			triangles.Add(6);
			triangles.Add(10);
			triangleTable.Add(6, triangles);

			//Configuration 7.
			triangles = new List<int>();
			triangles.Add(9);
			triangles.Add(4);
			triangles.Add(10);
			triangles.Add(4);
			triangles.Add(7);
			triangles.Add(10);
			triangles.Add(10);
			triangles.Add(7);
			triangles.Add(11);
			triangleTable.Add(7, triangles);

			//Configuration 8.
			triangles = new List<int>();
			triangles.Add(7);
			triangles.Add(4);
			triangles.Add(8);
			triangleTable.Add(8, triangles);

			//Configuration 9.
			triangles = new List<int>();
			triangles.Add(6);
			triangles.Add(4);
			triangles.Add(11);
			triangles.Add(11);
			triangles.Add(4);
			triangles.Add(8);
			triangleTable.Add(9, triangles);

			//Configuration 10.
			triangles = new List<int>();
			triangles.Add(7);
			triangles.Add(4);
			triangles.Add(8);
			triangles.Add(5);
			triangles.Add(6);
			triangles.Add(10);
			triangleTable.Add(10, triangles);

			//Configuration 11.
			triangles = new List<int>();
			triangles.Add(10);
			triangles.Add(5);
			triangles.Add(11);
			triangles.Add(5);
			triangles.Add(4);
			triangles.Add(11);
			triangles.Add(11);
			triangles.Add(4);
			triangles.Add(8);
			triangleTable.Add(11, triangles);

			//Configuration 12.
			triangles = new List<int>();
			triangles.Add(7);
			triangles.Add(5);
			triangles.Add(8);
			triangles.Add(8);
			triangles.Add(5);
			triangles.Add(9);
			triangleTable.Add(12, triangles);

			//Configuration 13.
			triangles = new List<int>();
			triangles.Add(11);
			triangles.Add(6);
			triangles.Add(8);
			triangles.Add(6);
			triangles.Add(5);
			triangles.Add(8);
			triangles.Add(8);
			triangles.Add(5);
			triangles.Add(9);
			triangleTable.Add(13, triangles);

			//Configuration 14.
			triangles = new List<int>();
			triangles.Add(8);
			triangles.Add(7);
			triangles.Add(9);
			triangles.Add(7);
			triangles.Add(6);
			triangles.Add(9);
			triangles.Add(9);
			triangles.Add(6);
			triangles.Add(10);
			triangleTable.Add(14, triangles);

			//Configuration 15.
			triangles = new List<int>();
			triangles.Add(8);
			triangles.Add(11);
			triangles.Add(10);
			triangles.Add(10);
			triangles.Add(9);
			triangles.Add(8);
			triangleTable.Add(15, triangles);

			//Configuration 16.
			triangles = new List<int>();
			triangles.Add(2);
			triangles.Add(11);
			triangles.Add(3);
			triangleTable.Add(16, triangles);

			//Configuration 17.
			triangles = new List<int>();
			triangles.Add(2);
			triangles.Add(6);
			triangles.Add(7);
			triangles.Add(2);
			triangles.Add(7);
			triangles.Add(3);
			triangleTable.Add(17, triangles);

			//Configuration 18.
			triangles = new List<int>();
			triangles.Add(2);
			triangles.Add(11);
			triangles.Add(3);
			triangles.Add(5);
			triangles.Add(6);
			triangles.Add(10);
			triangleTable.Add(18, triangles);

			//Configuration 19.
			triangles = new List<int>();
			triangles.Add(10);
			triangles.Add(5);
			triangles.Add(7);
			triangles.Add(10);
			triangles.Add(7);
			triangles.Add(2);
			triangles.Add(2);
			triangles.Add(7);
			triangles.Add(3);
			triangleTable.Add(19, triangles);

			//Configuration 20.
			triangles = new List<int>();
			triangles.Add(2);
			triangles.Add(11);
			triangles.Add(3);
			triangles.Add(4);
			triangles.Add(5);
			triangles.Add(9);
			triangleTable.Add(20, triangles);

			//Configuration 21.
			triangles = new List<int>();
			triangles.Add(2);
			triangles.Add(6);
			triangles.Add(7);
			triangles.Add(2);
			triangles.Add(7);
			triangles.Add(3);
			triangles.Add(4);
			triangles.Add(5);
			triangles.Add(9);
			triangleTable.Add(21, triangles);

			//Configuration 22.
			triangles = new List<int>();
			triangles.Add(9);
			triangles.Add(4);
			triangles.Add(6);
			triangles.Add(9);
			triangles.Add(6);
			triangles.Add(10);
			triangles.Add(2);
			triangles.Add(11);
			triangles.Add(3);
			triangleTable.Add(22, triangles);

			//Configuration 23.
			triangles = new List<int>();
			triangles.Add(3);
			triangles.Add(2);
			triangles.Add(7);
			triangles.Add(2);
			triangles.Add(10);
			triangles.Add(7);
			triangles.Add(10);
			triangles.Add(9);
			triangles.Add(7);
			triangles.Add(9);
			triangles.Add(4);
			triangles.Add(7);
			triangleTable.Add(23, triangles);

			//Configuration 24.
			triangles = new List<int>();
			triangles.Add(2);
			triangles.Add(11);
			triangles.Add(3);
			triangles.Add(7);
			triangles.Add(4);
			triangles.Add(8);
			triangleTable.Add(24, triangles);

			//Configuration 25.
			triangles = new List<int>();
			triangles.Add(3);
			triangles.Add(2);
			triangles.Add(6);
			triangles.Add(3);
			triangles.Add(6);
			triangles.Add(8);
			triangles.Add(8);
			triangles.Add(6);
			triangles.Add(4);
			triangleTable.Add(25, triangles);

			//Configuration 26.
			triangles = new List<int>();
			triangles.Add(5);
			triangles.Add(6);
			triangles.Add(10);
			triangles.Add(2);
			triangles.Add(11);
			triangles.Add(3);
			triangles.Add(7);
			triangles.Add(4);
			triangles.Add(8);
			triangleTable.Add(26, triangles);

			//Configuration 27.
			triangles = new List<int>();
			triangles.Add(10);
			triangles.Add(5);
			triangles.Add(2);
			triangles.Add(2);
			triangles.Add(5);
			triangles.Add(4);
			triangles.Add(2);
			triangles.Add(4);
			triangles.Add(3);
			triangles.Add(3);
			triangles.Add(4);
			triangles.Add(8);
			triangleTable.Add(27, triangles);

			//Configuration 28.
			triangles = new List<int>();
			triangles.Add(2);
			triangles.Add(11);
			triangles.Add(3);
			triangles.Add(7);
			triangles.Add(5);
			triangles.Add(8);
			triangles.Add(8);
			triangles.Add(5);
			triangles.Add(9);
			triangleTable.Add(28, triangles);

			//Configuration 29.
			triangles = new List<int>();
			triangles.Add(2);
			triangles.Add(6);
			triangles.Add(3);
			triangles.Add(3);
			triangles.Add(6);
			triangles.Add(5);
			triangles.Add(3);
			triangles.Add(5);
			triangles.Add(8);
			triangles.Add(8);
			triangles.Add(5);
			triangles.Add(9);
			triangleTable.Add(29, triangles);

			//Configuration 30.
			triangles = new List<int>();
			triangles.Add(2);
			triangles.Add(11);
			triangles.Add(3);
			triangles.Add(6);
			triangles.Add(10);
			triangles.Add(9);
			triangles.Add(6);
			triangles.Add(9);
			triangles.Add(8);
			triangles.Add(7);
			triangles.Add(9);
			triangles.Add(8);
			triangleTable.Add(30, triangles);

			//Configuration 31.
			triangles = new List<int>();
			triangles.Add(2);
			triangles.Add(10);
			triangles.Add(9);
			triangles.Add(2);
			triangles.Add(9);
			triangles.Add(3);
			triangles.Add(3);
			triangles.Add(9);
			triangles.Add(8);
			triangleTable.Add(31, triangles);

			//Configuration 32.
			triangles = new List<int>();
			triangles.Add(1);
			triangles.Add(10);
			triangles.Add(2);
			triangleTable.Add(32, triangles);

			//Configuration 33.
			triangles = new List<int>();
			triangles.Add(1);
			triangles.Add(10);
			triangles.Add(2);
			triangles.Add(6);
			triangles.Add(7);
			triangles.Add(11);
			triangleTable.Add(33, triangles);

			//Configuration 34.
			triangles = new List<int>();
			triangles.Add(5);
			triangles.Add(6);
			triangles.Add(1);
			triangles.Add(1);
			triangles.Add(6);
			triangles.Add(2);
			triangleTable.Add(34, triangles);

			//Configuration 35.
			triangles = new List<int>();
			triangles.Add(1);
			triangles.Add(5);
			triangles.Add(2);
			triangles.Add(2);
			triangles.Add(5);
			triangles.Add(11);
			triangles.Add(11);
			triangles.Add(5);
			triangles.Add(7);
			triangleTable.Add(35, triangles);

			//Configuration 36.
			triangles = new List<int>();
			triangles.Add(1);
			triangles.Add(10);
			triangles.Add(2);
			triangles.Add(9);
			triangles.Add(4);
			triangles.Add(5);
			triangleTable.Add(36, triangles);

			//Configuration 37.
			triangles = new List<int>();
			triangles.Add(4);
			triangles.Add(5);
			triangles.Add(9);
			triangles.Add(1);
			triangles.Add(10);
			triangles.Add(2);
			triangles.Add(6);
			triangles.Add(7);
			triangles.Add(11);
			triangleTable.Add(37, triangles);

			//Configuration 38.
			triangles = new List<int>();
			triangles.Add(9);
			triangles.Add(4);
			triangles.Add(6);
			triangles.Add(9);
			triangles.Add(6);
			triangles.Add(1);
			triangles.Add(1);
			triangles.Add(6);
			triangles.Add(2);
			triangleTable.Add(38, triangles);

			//Configuration 39.
			triangles = AddTriangles(7, 11, 2, 7, 2, 4, 4, 2, 1, 4, 1, 9);
			triangleTable.Add(39, triangles);

			//Configuration 40.
			triangles = AddTriangles(1, 10, 2, 7, 4, 8);
			triangleTable.Add(40, triangles);

			//Configuration 41.
			triangles = AddTriangles(11, 6, 4, 11, 4, 8, 1, 10, 2);
			triangleTable.Add(41, triangles);

			//Configuration 42.
			triangles = AddTriangles(1, 5, 6, 1, 6, 2, 7, 4, 8);
			triangleTable.Add(42, triangles);

			//Configuration 43.
			triangles = AddTriangles(2, 1, 5, 4, 2, 5, 4, 11, 2, 4, 8, 11);
			triangleTable.Add(43, triangles);

			//Configuration 44.
			triangles = AddTriangles(1, 10, 2, 8, 7, 5, 8, 5, 9);
			triangleTable.Add(44, triangles);

			//Configuration 45.
			triangles = AddTriangles(2, 1, 10, 5, 9, 8, 5, 8, 6, 6, 8, 11);
			triangleTable.Add(45, triangles);

			//Configuration 46.
			triangles = AddTriangles(2, 1, 6, 6, 1, 7, 7, 1, 9, 7, 9, 8);
			triangleTable.Add(46, triangles);

			//Configuration 47.
			triangles = AddTriangles(11, 2, 8, 8, 2, 1, 8, 1, 9);
			triangleTable.Add(47, triangles);

			//Configuration 48.
			triangles = AddTriangles(1, 10, 3, 3, 10, 11);
			triangleTable.Add(48, triangles);

			//Configuration 49.
			triangles = AddTriangles(6, 7, 3, 6, 3, 10, 10, 3, 1);
			triangleTable.Add(49, triangles);

			//Configuration 50.
			triangles = AddTriangles(11, 3, 1, 11, 1, 6, 6, 1, 5);
			triangleTable.Add(50, triangles);

			//Configuration 51.
			triangles = AddTriangles(3, 1, 7, 7, 1, 5);
			triangleTable.Add(51, triangles);

			//Configuration 52.
			triangles = AddTriangles(1, 10, 11, 1, 11, 3, 4, 5, 9);
			triangleTable.Add(52, triangles);

			//Configuration 53.
			triangles = AddTriangles(6, 7, 3, 6, 3, 10, 10, 3, 1, 9, 4, 5);
			triangleTable.Add(53, triangles);

			//Configuration 54.
			triangles = AddTriangles(11, 3, 1, 11, 1, 6, 6, 1, 4, 1, 9, 4);
			triangleTable.Add(54, triangles);

			//Configuration 55.
			triangles = AddTriangles(3, 1, 9, 3, 9, 4, 3, 4, 7);
			triangleTable.Add(55, triangles);

			//Configuration 56.
			triangles = AddTriangles(1, 10, 11, 1, 11, 3, 8, 7, 4);
			triangleTable.Add(56, triangles);

			//Configuration 57.
			triangles = AddTriangles(3, 1, 10, 3, 10, 8, 8, 10, 4, 4, 10, 6);
			triangleTable.Add(57, triangles);

			//Configuration 58.
			triangles = AddTriangles(11, 3, 1, 11, 1, 6, 6, 1, 5, 4, 8, 7);
			triangleTable.Add(58, triangles);

			//Configuration 59.
			triangles = AddTriangles(5, 4, 1, 1, 4, 8, 1, 8, 3);
			triangleTable.Add(59, triangles);

			//Configuration 60.
			triangles = AddTriangles(1, 10, 11, 1, 11, 3, 8, 7, 5, 8, 5, 9);
			triangleTable.Add(60, triangles);

			//Configuration 61.
			triangles = AddTriangles(3, 1, 10, 8, 3, 10, 6, 8, 10, 5, 8, 6, 5, 9, 8);
			triangleTable.Add(61, triangles);

			//Configuration 62.
			triangles = AddTriangles(8, 3, 1, 8, 1, 9, 6, 11, 7);
			triangleTable.Add(62, triangles);

			//Configuration 63.
			triangles = AddTriangles(8, 3, 1, 8, 1, 9);
			triangleTable.Add(63, triangles);

			//Configuration 64.
			triangles = AddTriangles(0, 9, 1);
			triangleTable.Add(64, triangles);

			//Configuration 65.
			triangles = AddTriangles(0, 9, 1, 6, 7, 11);
			triangleTable.Add(65, triangles);

			//Configuration 66.
			triangles = AddTriangles(0, 9, 1, 5, 6, 10);
			triangleTable.Add(66, triangles);

			//Configuration 67.
			triangles = AddTriangles(0, 9, 1, 10, 5, 7, 10, 7, 11);
			triangleTable.Add(67, triangles);

			//Configuration 68.
			triangles = AddTriangles(0, 4, 5, 0, 5, 1);
			triangleTable.Add(68, triangles);

			//Configuration 69.
			triangles = AddTriangles(0, 4, 5, 0, 5, 1, 6, 7, 11);
			triangleTable.Add(69, triangles);

			//Configuration 70.
			triangles = AddTriangles(1, 0, 4, 1, 4, 10, 10, 4, 6);
			triangleTable.Add(70, triangles);

			//Configuration 71.
			triangles = AddTriangles(10, 1, 0, 10, 0, 4, 7, 10, 4, 7, 11, 10);
			triangleTable.Add(71, triangles);

			//Configuration 72.
			triangles = AddTriangles(0, 9, 1, 7, 4, 8);
			triangleTable.Add(72, triangles);

			//Configuration 73.
			triangles = AddTriangles(0, 9, 1, 11, 6, 4, 11, 4, 8);
			triangleTable.Add(73, triangles);

			//Configuration 74.
			triangles = AddTriangles(0, 9, 1, 7, 4, 8, 5, 6, 10);
			triangleTable.Add(74, triangles);

			//Configuration 75.
			triangles = AddTriangles(4, 8, 11, 4, 11, 5, 5, 11, 10, 1, 0, 9);
			triangleTable.Add(75, triangles);

			//Configuration 76.
			triangles = AddTriangles(8, 7, 5, 8, 5, 0, 0, 5, 1);
			triangleTable.Add(76, triangles);

			//Configuration 77.
			triangles = AddTriangles(8, 11, 6, 8, 6, 0, 0, 6, 5, 0, 5, 1);
			triangleTable.Add(77, triangles);

			//Configuration 78.
			triangles = AddTriangles(6, 10, 1, 6, 1, 7, 7, 1, 0, 7, 0, 8);
			triangleTable.Add(78, triangles);

			//Configuration 79.
			triangles = AddTriangles(10, 1, 11, 11, 1, 0, 11, 0, 8);
			triangleTable.Add(79, triangles);

			//Configuration 80.
			triangles = AddTriangles(0, 9, 1, 2, 11, 3);
			triangleTable.Add(80, triangles);

			//Configuration 81.
			triangles = AddTriangles(2, 6, 7, 2, 7, 3, 0, 9, 1);
			triangleTable.Add(81, triangles);

			//Configuration 82.
			triangles = AddTriangles(0, 9, 1, 2, 11, 3, 5, 6, 10);
			triangleTable.Add(82, triangles);

			//Configuration 83.
			triangles = AddTriangles(10, 5, 7, 10, 7, 2, 2, 7, 3, 0, 9, 1);
			triangleTable.Add(83, triangles);

			//Configuration 84.
			triangles = AddTriangles(0, 4, 5, 0, 5, 1, 2, 11, 3);
			triangleTable.Add(84, triangles);

			//Configuration 85.
			triangles = AddTriangles(0, 4, 5, 0, 5, 1, 2, 6, 7, 2, 7, 3);
			triangleTable.Add(85, triangles);

			//Configuration 86.
			triangles = AddTriangles(1, 0, 4, 1, 4, 10, 10, 4, 6, 11, 3, 2);
			triangleTable.Add(86, triangles);

			//Configuration 87.
			triangles = AddTriangles(7, 3, 2, 7, 2, 10, 7, 10, 4, 4, 10, 1, 4, 1, 0);
			triangleTable.Add(87, triangles);

			//Configuration 88.
			triangles = AddTriangles(2, 11, 3, 0, 9, 1, 7, 4, 8);
			triangleTable.Add(88, triangles);

			//Configuration 89.
			triangles = AddTriangles(3, 2, 6, 3, 6, 8, 8, 6, 4, 0, 9, 1);
			triangleTable.Add(89, triangles);

			//Configuration 90.
			triangles = AddTriangles(5, 6, 10, 7, 4, 8, 2, 11, 3, 0, 9, 1);
			triangleTable.Add(90, triangles);

			//Configuration 91.
			triangles = AddTriangles(4, 8, 3, 4, 3, 2, 4, 2, 5, 5, 2, 10, 1, 0, 9);
			triangleTable.Add(91, triangles);

			//Configuration 92.
			triangles = AddTriangles(8, 7, 5, 8, 5, 0, 0, 5, 1, 2, 11, 3);
			triangleTable.Add(92, triangles);

			//Configuration 93.
			triangles = AddTriangles(5, 1, 0, 5, 0, 8, 5, 8, 6, 6, 8, 3, 6, 3, 2);
			triangleTable.Add(93, triangles);

			//Configuration 94.
			triangles = AddTriangles(6, 10, 1, 6, 1, 0, 6, 0, 7, 7, 0, 8, 3, 2, 11);
			triangleTable.Add(94, triangles);

			//Configuration 95.
			triangles = AddTriangles(10, 1, 0, 10, 0, 8, 8, 3, 2, 8, 2, 10);
			triangleTable.Add(95, triangles);

			//Configuration 96.
			triangles = AddTriangles(0, 9, 10, 0, 10, 2);
			triangleTable.Add(96, triangles);

			//Configuration 97.
			triangles = AddTriangles(0, 9, 10, 0, 10, 2, 6, 7, 11);
			triangleTable.Add(97, triangles);

			//Configuration 98.
			triangles = AddTriangles(5, 6, 2, 5, 2, 9, 9, 2, 0);
			triangleTable.Add(98, triangles);

			//Configuration 99.
			triangles = AddTriangles(2, 0, 9, 2, 9, 5, 2, 5, 11, 11, 5, 7);
			triangleTable.Add(99, triangles);

			//Configuration 100.
			triangles = AddTriangles(10, 2, 0, 10, 0, 5, 5, 0, 4);
			triangleTable.Add(100, triangles);

			//Configuration 101.
			triangles = AddTriangles(10, 2, 0, 10, 0, 5, 5, 0, 4, 7, 11, 6);
			triangleTable.Add(101, triangles);

			//Configuration 102.
			triangles = AddTriangles(4, 6, 2, 4, 2, 0);
			triangleTable.Add(102, triangles);

			//Configuration 103.
			triangles = AddTriangles(4, 7, 0, 0, 7, 11, 0, 11, 2);
			triangleTable.Add(103, triangles);

			//Configuration 104.
			triangles = AddTriangles(0, 9, 10, 0, 10, 2, 7, 4, 8);
			triangleTable.Add(104, triangles);

			//Configuration 105.
			triangles = AddTriangles(0, 9, 10, 0, 10, 2, 11, 6, 4, 11, 4, 8);
			triangleTable.Add(105, triangles);

			//Configuration 106.
			triangles = AddTriangles(5, 6, 2, 5, 2, 9, 9, 2, 0, 8, 7, 4);
			triangleTable.Add(106, triangles);

			//Configuration 107.
			triangles = AddTriangles(2, 0, 9, 2, 9, 5, 2, 5, 11, 11, 5, 4, 11, 4, 8);
			triangleTable.Add(107, triangles);

			//Configuration 108.
			triangles = AddTriangles(10, 2, 0, 10, 0, 5, 5, 0, 8, 5, 8, 7);
			triangleTable.Add(108, triangles);

			//Configuration 109.
			triangles = AddTriangles(10, 2, 0, 10, 0, 5, 5, 0, 8, 5, 8, 6, 6, 8, 11);
			triangleTable.Add(109, triangles);

			//Configuration 110.
			triangles = AddTriangles(2, 0, 8, 2, 8, 7, 2, 7, 6);
			triangleTable.Add(110, triangles);

			//Configuration 111.
			triangles = AddTriangles(11, 2, 0, 11, 0, 8);
			triangleTable.Add(111, triangles);

			//Configuration 112.
			triangles = AddTriangles(0, 9, 10, 0, 10, 3, 3, 10, 11);
			triangleTable.Add(112, triangles);

			//Configuration 113.
			triangles = AddTriangles(0, 9, 10, 0, 10, 3, 3, 10, 6, 3, 6, 7);
			triangleTable.Add(113, triangles);

			//Configuration 114.
			triangles = AddTriangles(0, 9, 5, 0, 5, 6, 0, 6, 3, 3, 6, 11);
			triangleTable.Add(114, triangles);

			//Configuration 115.
			triangles = AddTriangles(7, 3, 0, 7, 0, 9, 7, 9, 5);
			triangleTable.Add(115, triangles);

			//Configuration 116.
			triangles = AddTriangles(0, 4, 5, 0, 5, 10, 0, 10, 3, 3, 10, 11);
			triangleTable.Add(116, triangles);

			//Configuration 117.
			triangles = AddTriangles(0, 4, 5, 0, 5, 10, 0, 10, 3, 3, 10, 6, 3, 6, 7);
			triangleTable.Add(117, triangles);

			//Configuration 118.
			triangles = AddTriangles(4, 6, 11, 4, 11, 3, 4, 3, 0);
			triangleTable.Add(118, triangles);

			//Configuration 119.
			triangles = AddTriangles(4, 7, 3, 4, 3, 0);
			triangleTable.Add(119, triangles);

			//Configuration 120.
			triangles = AddTriangles(0, 9, 10, 0, 10, 3, 3, 10, 11, 7, 4, 8);
			triangleTable.Add(120, triangles);

			//Configuration 121.
			triangles = AddTriangles(0, 9, 10, 0, 10, 3, 3, 10, 6, 3, 6, 8, 8, 6, 4);
			triangleTable.Add(121, triangles);

			//Configuration 122.
			triangles = AddTriangles(0, 9, 5, 0, 5, 6, 0, 6, 3, 3, 6, 11, 7, 4, 8);
			triangleTable.Add(122, triangles);

			//Configuration 123.
			triangles = AddTriangles(5, 4, 9, 3, 0, 8);
			triangleTable.Add(123, triangles);

			//Configuration 124.
			triangles = AddTriangles(8, 7, 5, 8, 5, 0, 0, 5, 10, 0, 10, 11, 0, 11, 3);
			triangleTable.Add(124, triangles);

			//Configuration 125.
			triangles = AddTriangles(0, 8, 3, 10, 6, 5);
			triangleTable.Add(125, triangles);

			//Configuration 126.
			triangles = AddTriangles(0, 8, 3, 6, 11, 7);
			triangleTable.Add(126, triangles);

			//Configuration 127.
			triangles = AddTriangles(3, 0, 8);
			triangleTable.Add(127, triangles);

			//Configuration 128.
			triangles = AddTriangles(0, 3, 8);
			triangleTable.Add(128, triangles);

			//Configuration 129.
			triangles = AddTriangles(3, 8, 0, 6, 7, 11);
			triangleTable.Add(129, triangles);

			//Configuration 130.
			triangles = AddTriangles(0, 3, 8, 5, 6, 10);
			triangleTable.Add(130, triangles);

			//Configuration 131.
			triangles = AddTriangles(10, 5, 7, 10, 7, 11, 0, 3, 8);
			triangleTable.Add(131, triangles);

			//Configuration 132.
			triangles = AddTriangles(0, 3, 8, 4, 5, 9);
			triangleTable.Add(132, triangles);

			//Configuration 133.
			triangles = AddTriangles(0, 3, 8, 4, 5, 9, 6, 7, 11);
			triangleTable.Add(133, triangles);

			//Configuration 134.
			triangles = AddTriangles(0, 3, 8, 9, 4, 6, 9, 6, 10);
			triangleTable.Add(134, triangles);

			//Configuration 135.
			triangles = AddTriangles(7, 11, 10, 7, 10, 4, 4, 10, 9, 0, 3, 8);
			triangleTable.Add(135, triangles);

			//Configuration 136.
			triangles = AddTriangles(0, 3, 4, 4, 3, 7);
			triangleTable.Add(136, triangles);

			//Configuration 137.
			triangles = AddTriangles(11, 6, 4, 11, 4, 3, 3, 4, 0);
			triangleTable.Add(137, triangles);

			//Configuration 138.
			triangles = AddTriangles(7, 4, 0, 7, 0, 3, 10, 5, 6);
			triangleTable.Add(138, triangles);

			//Configuration 139.
			triangles = AddTriangles(11, 10, 5, 11, 5, 4, 11, 4, 3, 3, 4, 0);
			triangleTable.Add(139, triangles);

			//Configuration 140.
			triangles = AddTriangles(0, 3, 7, 0, 7, 9, 9, 7, 5);
			triangleTable.Add(140, triangles);

			//Configuration 141.
			triangles = AddTriangles(5, 9, 0, 5, 0, 3, 5, 3, 6, 6, 3, 11);
			triangleTable.Add(141, triangles);

			//Configuration 142.
			triangles = AddTriangles(6, 10, 9, 6, 9, 7, 7, 9, 0, 7, 0, 3);
			triangleTable.Add(142, triangles);

			//Configuration 143.
			triangles = AddTriangles(10, 9, 0, 10, 0, 3, 10, 3, 11);
			triangleTable.Add(143, triangles);

			//Configuration 144.
			triangles = AddTriangles(2, 11, 8, 2, 8, 0);
			triangleTable.Add(144, triangles);

			//Configuration 145.
			triangles = AddTriangles(8, 0, 2, 8, 2, 7, 7, 2, 6);
			triangleTable.Add(145, triangles);

			//Configuration 146.
			triangles = AddTriangles(8, 0, 2, 8, 2, 11, 10, 5, 6);
			triangleTable.Add(146, triangles);

			//Configuration 147.
			triangles = AddTriangles(10, 5, 7, 10, 7, 2, 2, 7, 8, 2, 8, 10);
			triangleTable.Add(147, triangles);

			//Configuration 148.
			triangles = AddTriangles(0, 2, 11, 0, 11, 8, 4, 5, 9);
			triangleTable.Add(148, triangles);

			//Configuration 149.
			triangles = AddTriangles(8, 0, 2, 8, 2, 7, 7, 2, 6, 5, 9, 4);
			triangleTable.Add(149, triangles);

			//Configuration 150.
			triangles = AddTriangles(2, 11, 8, 2, 8, 0, 9, 4, 6, 9, 6, 10);
			triangleTable.Add(150, triangles);

			//Configuration 151.
			triangles = AddTriangles(10, 9, 4, 10, 4, 7, 10, 7, 2, 2, 7, 8, 2, 8, 0);
			triangleTable.Add(151, triangles);

			//Configuration 152.
			triangles = AddTriangles(7, 4, 0, 7, 0, 11, 11, 0, 2);
			triangleTable.Add(152, triangles);

			//Configuration 153.
			triangles = AddTriangles(2, 6, 4, 2, 4, 0);
			triangleTable.Add(153, triangles);

			//Configuration 154.
			triangles = AddTriangles(7, 4, 0, 7, 0, 11, 11, 0, 2, 10, 5, 6);
			triangleTable.Add(154, triangles);

			//Configuration 155.
			triangles = AddTriangles(0, 2, 10, 0, 10, 5, 0, 5, 4);
			triangleTable.Add(155, triangles);

			//Configuration 156.
			triangles = AddTriangles(7, 5, 9, 7, 9, 0, 7, 0, 11, 11, 0, 2);
			triangleTable.Add(156, triangles);

			//Configuration 157.
			triangles = AddTriangles(2, 6, 5, 2, 5, 9, 2, 9, 0);
			triangleTable.Add(157, triangles);

			//Configuration 158.
			triangles = AddTriangles(0, 2, 11, 0, 11, 7, 0, 7, 9, 9, 7, 6, 9, 6, 10);
			triangleTable.Add(158, triangles);

			//Configuration 159.
			triangles = AddTriangles(9, 0, 2, 9, 2, 10);
			triangleTable.Add(159, triangles);

			//Configuration 160.
			triangles = AddTriangles(1, 10, 2, 3, 8, 0);
			triangleTable.Add(160, triangles);

			//Configuration 161.
			triangles = AddTriangles(0, 3, 8, 1, 10, 2, 6, 7, 11);
			triangleTable.Add(161, triangles);

			//Configuration 162.
			triangles = AddTriangles(0, 3, 8, 1, 5, 6, 1, 6, 2);
			triangleTable.Add(162, triangles);

			//Configuration 163.
			triangles = AddTriangles(2, 1, 5, 2, 5, 11, 11, 5, 7, 8, 0, 3);
			triangleTable.Add(163, triangles);

			//Configuration 164.
			triangles = AddTriangles(3, 8, 0, 1, 10, 2, 4, 5, 9);
			triangleTable.Add(164, triangles);

			//Configuration 165.
			triangles = AddTriangles(4, 5, 9, 6, 7, 11, 1, 10, 2, 3, 8, 0);
			triangleTable.Add(165, triangles);

			//Configuration 166.
			triangles = AddTriangles(9, 4, 6, 9, 6, 1, 1, 6, 2, 3, 8, 0);
			triangleTable.Add(166, triangles);

			//Configuration 167.
			triangles = AddTriangles(7, 11, 2, 7, 2, 1, 7, 1, 4, 4, 1, 9);
			triangleTable.Add(167, triangles);

			//Configuration 168.
			triangles = AddTriangles(1, 10, 2, 3, 7, 4, 3, 4, 0);
			triangleTable.Add(168, triangles);

			//Configuration 169.
			triangles = AddTriangles(11, 6, 4, 11, 4, 3, 3, 4, 0, 1, 10, 2);
			triangleTable.Add(169, triangles);

			//Configuration 170.
			triangles = AddTriangles(1, 5, 6, 1, 6, 2, 3, 7, 4, 3, 4, 0);
			triangleTable.Add(170, triangles);

			//Configuration 171.
			triangles = AddTriangles(4, 0, 3, 4, 3, 11, 4, 11, 5, 5, 11, 2, 5, 2, 1);
			triangleTable.Add(171, triangles);

			//Configuration 172.
			triangles = AddTriangles(0, 3, 7, 0, 7, 9, 9, 7, 5, 10, 2, 1);
			triangleTable.Add(172, triangles);

			//Configuration 173.
			triangles = AddTriangles(5, 9, 0, 5, 0, 3, 5, 3, 6, 6, 3, 11, 2, 1, 10);
			triangleTable.Add(173, triangles);

			//Configuration 174.
			triangles = AddTriangles(6, 2, 1, 6, 1, 9, 6, 9, 7, 7, 9, 0, 7, 0, 3);
			triangleTable.Add(174, triangles);

			//Configuration 175.
			triangles = AddTriangles(2, 3, 11, 0, 1, 9);
			triangleTable.Add(175, triangles);

			//Configuration 176.
			triangles = AddTriangles(1, 10, 11, 1, 11, 0, 0, 11, 8);
			triangleTable.Add(176, triangles);

			//Configuration 177.
			triangles = AddTriangles(1, 10, 6, 1, 6, 7, 1, 7, 0, 0, 7, 8);
			triangleTable.Add(177, triangles);

			//Configuration 178.
			triangles = AddTriangles(1, 5, 6, 1, 6, 11, 1, 11, 0, 0, 11, 8);
			triangleTable.Add(178, triangles);

			//Configuration 179.
			triangles = AddTriangles(7, 8, 5, 5, 8, 0, 5, 0, 1);
			triangleTable.Add(179, triangles);

			//Configuration 180.
			triangles = AddTriangles(1, 10, 11, 1, 11, 0, 0, 11, 8, 4, 5, 9);
			triangleTable.Add(180, triangles);

			//Configuration 181.
			triangles = AddTriangles(1, 10, 6, 1, 6, 7, 1, 7, 0, 0, 7, 8, 4, 5, 9);
			triangleTable.Add(181, triangles);

			//Configuration 182.
			triangles = AddTriangles(9, 4, 6, 9, 6, 1, 1, 6, 11, 1, 11, 3);
			triangleTable.Add(182, triangles);

			//Configuration 183.
			triangles = AddTriangles(8, 4, 7, 9, 0, 1);
			triangleTable.Add(183, triangles);

			//Configuration 184.
			triangles = AddTriangles(1, 10, 11, 1, 11, 0, 0, 11, 7, 0, 7, 4);
			triangleTable.Add(184, triangles);

			//Configuration 185.
			triangles = AddTriangles(4, 0, 1, 4, 1, 10, 4, 10, 6);
			triangleTable.Add(185, triangles);

			//Configuration 186.
			triangles = AddTriangles(1, 5, 6, 1, 6, 11, 1, 11, 0, 0, 11, 7, 0, 7, 4);
			triangleTable.Add(186, triangles);

			//Configuration 187.
			triangles = AddTriangles(0, 1, 5, 0, 5, 4);
			triangleTable.Add(187, triangles);

			//Configuration 188.
			triangles = AddTriangles(7, 5, 9, 7, 9, 0, 7, 0, 11, 11, 0, 1, 11, 1, 10);
			triangleTable.Add(188, triangles);

			//Configuration 189.
			triangles = AddTriangles(1, 9, 0, 10, 6, 5);
			triangleTable.Add(189, triangles);

			//Configuration 190.
			triangles = AddTriangles(6, 11, 7, 0, 1, 9);
			triangleTable.Add(190, triangles);

			//Configuration 191.
			triangles = AddTriangles(9, 0, 1);
			triangleTable.Add(191, triangles);

			//Configuration 192.
			triangles = AddTriangles(3, 8, 9, 3, 9, 1);
			triangleTable.Add(192, triangles);

			//Configuration 193.
			triangles = AddTriangles(3, 8, 9, 3, 9, 1, 6, 7, 11);
			triangleTable.Add(193, triangles);

			//Configuration 194.
			triangles = AddTriangles(3, 8, 9, 3, 9, 1, 5, 6, 10);
			triangleTable.Add(194, triangles);

			//Configuration 195.
			triangles = AddTriangles(3, 8, 9, 3, 9, 1, 10, 5, 7, 10, 7, 11);
			triangleTable.Add(195, triangles);

			//Configuration 196.
			triangles = AddTriangles(4, 5, 1, 4, 1, 8, 8, 1, 3);
			triangleTable.Add(196, triangles);

			//Configuration 197.
			triangles = AddTriangles(4, 5, 1, 4, 1, 8, 8, 1, 3, 11, 6, 7);
			triangleTable.Add(197, triangles);

			//Configuration 198.
			triangles = AddTriangles(4, 6, 10, 4, 10, 1, 4, 1, 8, 8, 1, 3);
			triangleTable.Add(198, triangles);

			//Configuration 199.
			triangles = AddTriangles(1, 3, 8, 1, 8, 4, 1, 3, 10, 10, 4, 7, 10, 7, 11);
			triangleTable.Add(199, triangles);

			//Configuration 200.
			triangles = AddTriangles(9, 1, 3, 9, 3, 4, 4, 3, 7);
			triangleTable.Add(200, triangles);

			//Configuration 201.
			triangles = AddTriangles(11, 6, 4, 11, 4, 3, 3, 4, 9, 3, 9, 1);
			triangleTable.Add(201, triangles);

			//Configuration 202.
			triangles = AddTriangles(9, 1, 3, 9, 3, 4, 4, 3, 7, 6, 10, 5);
			triangleTable.Add(202, triangles);

			//Configuration 203.
			triangles = AddTriangles(11, 10, 5, 11, 5, 4, 11, 4, 3, 3, 4, 9, 3, 9, 1);
			triangleTable.Add(203, triangles);

			//Configuration 204.
			triangles = AddTriangles(7, 5, 1, 7, 1, 3);
			triangleTable.Add(204, triangles);

			//Configuration 205.
			triangles = AddTriangles(1, 3, 11, 1, 11, 6, 1, 6, 5);
			triangleTable.Add(205, triangles);

			//Configuration 206.
			triangles = AddTriangles(3, 7, 6, 3, 6, 10, 3, 10, 1);
			triangleTable.Add(206, triangles);

			//Configuration 207.
			triangles = AddTriangles(10, 1, 3, 10, 3, 11);
			triangleTable.Add(207, triangles);

			//Configuration 208.
			triangles = AddTriangles(2, 11, 8, 2, 8, 1, 1, 8, 9);
			triangleTable.Add(208, triangles);

			//Configuration 209.
			triangles = AddTriangles(8, 9, 1, 8, 1, 2, 8, 2, 7, 7, 2, 6);
			triangleTable.Add(209, triangles);

			//Configuration 210.
			triangles = AddTriangles(2, 11, 8, 2, 8, 1, 1, 8, 9, 5, 6, 10);
			triangleTable.Add(210, triangles);

			//Configuration 211.
			triangles = AddTriangles(8, 9, 1, 8, 1, 2, 8, 2, 7, 7, 2, 10, 7, 10, 5);
			triangleTable.Add(211, triangles);

			//Configuration 212.
			triangles = AddTriangles(2, 11, 8, 2, 8, 1, 1, 8, 4, 1, 4, 5);
			triangleTable.Add(212, triangles);

			//Configuration 213.
			triangles = AddTriangles(2, 6, 7, 2, 7, 8, 2, 8, 1, 1, 8, 4, 1, 4, 5);
			triangleTable.Add(213, triangles);

			//Configuration 214.
			triangles = AddTriangles(2, 11, 7, 2, 7, 4, 2, 4, 1, 1, 4, 9, 5, 6, 10);
			triangleTable.Add(214, triangles);

			//Configuration 215.
			triangles = AddTriangles(7, 8, 4, 1, 2, 10);
			triangleTable.Add(215, triangles);

			//Configuration 216.
			triangles = AddTriangles(2, 11, 7, 2, 7, 4, 2, 4, 1, 1, 4, 9);
			triangleTable.Add(216, triangles);

			//Configuration 217.
			triangles = AddTriangles(4, 9, 6, 6, 9, 1, 6, 1, 2);
			triangleTable.Add(217, triangles);

			//Configuration 218.
			triangles = AddTriangles(2, 11, 7, 2, 7, 4, 2, 4, 1, 1, 4, 9, 5, 6, 10);
			triangleTable.Add(218, triangles);

			//Configuration 219.
			triangles = AddTriangles(10, 1, 2, 9, 5, 4);
			triangleTable.Add(219, triangles);

			//Configuration 220.
			triangles = AddTriangles(1, 2, 5, 5, 2, 11, 5, 11, 7);
			triangleTable.Add(220, triangles);

			//Configuration 221.
			triangles = AddTriangles(1, 2, 6, 1, 6, 5);
			triangleTable.Add(221, triangles);

			//Configuration 222.
			triangles = AddTriangles(6, 11, 7, 1, 2, 10);
			triangleTable.Add(222, triangles);

			//Configuration 223.
			triangles = AddTriangles(1, 2, 10);
			triangleTable.Add(223, triangles);

			//Configuration 224.
			triangles = AddTriangles(3, 8, 9, 3, 9, 2, 2, 9, 10);
			triangleTable.Add(224, triangles);

			//Configuration 225.
			triangles = AddTriangles(3, 8, 9, 3, 9, 2, 2, 9, 10);
			triangleTable.Add(225, triangles);

			//Configuration 226.
			triangles = AddTriangles(3, 8, 9, 3, 9, 2, 2, 9, 5, 2, 5, 6);
			triangleTable.Add(226, triangles);

			//Configuration 227.
			triangles = AddTriangles(5, 7, 11, 5, 11, 2, 5, 2, 9, 9, 2, 3, 9, 3, 8);
			triangleTable.Add(227, triangles);

			//Configuration 228.
			triangles = AddTriangles(3, 8, 4, 3, 4, 5, 3, 5, 2, 2, 5, 10);
			triangleTable.Add(228, triangles);

			//Configuration 229.
			triangles = AddTriangles(3, 8, 4, 3, 4, 5, 3, 5, 2, 2, 5, 10, 6, 7, 11);
			triangleTable.Add(229, triangles);

			//Configuration 230.
			triangles = AddTriangles(2, 3, 6, 6, 3, 8, 6, 8, 4);
			triangleTable.Add(230, triangles);

			//Configuration 231.
			triangles = AddTriangles(3, 11, 2, 7, 8, 4);
			triangleTable.Add(231, triangles);

			//Configuration 232.
			triangles = AddTriangles(3, 7, 4, 3, 4, 9, 3, 9, 2, 2, 9, 10);
			triangleTable.Add(232, triangles);

			//Configuration 233.
			triangles = AddTriangles(9, 10, 2, 9, 2, 3, 9, 3, 4, 4, 3, 11, 4, 11, 6);
			triangleTable.Add(233, triangles);

			//Configuration 234.
			triangles = AddTriangles(3, 7, 4, 3, 4, 9, 3, 9, 2, 2, 9, 5, 2, 5, 6);
			triangleTable.Add(234, triangles);

			//Configuration 235.
			triangles = AddTriangles(3, 11, 2, 5, 4, 9);
			triangleTable.Add(235, triangles);

			//Configuration 236.
			triangles = AddTriangles(7, 5, 10, 7, 10, 2, 7, 2, 3);
			triangleTable.Add(236, triangles);

			//Configuration 237.
			triangles = AddTriangles(11, 2, 3, 10, 6, 5);
			triangleTable.Add(237, triangles);

			//Configuration 238.
			triangles = AddTriangles(7, 6, 3, 3, 6, 2);
			triangleTable.Add(238, triangles);

			//Configuration 239.
			triangles = AddTriangles(2, 3, 11);
			triangleTable.Add(239, triangles);

			//Configuration 240.
			triangles = AddTriangles(8, 9, 10, 8, 10, 11);
			triangleTable.Add(240, triangles);

			//Configuration 241.
			triangles = AddTriangles(9, 10, 6, 9, 6, 7, 9, 7, 8);
			triangleTable.Add(241, triangles);

			//Configuration 242.
			triangles = AddTriangles(8, 9, 5, 8, 5, 6, 8, 6, 11);
			triangleTable.Add(242, triangles);

			//Configuration 243.
			triangles = AddTriangles(9, 5, 7, 9, 7, 8);
			triangleTable.Add(243, triangles);

			//Configuration 244.
			triangles = AddTriangles(11, 8, 4, 11, 4, 5, 11, 5, 10);
			triangleTable.Add(244, triangles);

			//Configuration 245.
			triangles = AddTriangles(4, 7, 8, 6, 5, 10, 4, 5, 6, 4, 6, 7);
			triangleTable.Add(245, triangles);

			//Configuration 246.
			triangles = AddTriangles(8, 4, 6, 8, 6, 11);
			triangleTable.Add(246, triangles);

			//Configuration 247.
			triangles = AddTriangles(4, 7, 8);
			triangleTable.Add(247, triangles);

			//Configuration 248.
			triangles = AddTriangles(10, 11, 7, 10, 7, 4, 10, 4, 9);
			triangleTable.Add(248, triangles);

			//Configuration 249.
			triangles = AddTriangles(10, 6, 4, 10, 4, 9);
			triangleTable.Add(249, triangles);

			//Configuration 250.
			triangles = AddTriangles(5, 4, 9, 7, 6, 11, 4, 5, 6, 4, 6, 7);
			triangleTable.Add(250, triangles);

			//Configuration 251.
			triangles = AddTriangles(5, 4, 9);
			triangleTable.Add(251, triangles);

			//Configuration 252.
			triangles = AddTriangles(7, 5, 11, 11, 5, 10);
			triangleTable.Add(252, triangles);

			//Configuration 253.
			triangles = AddTriangles(6, 5, 10);
			triangleTable.Add(253, triangles);

			//Configuration 254.
			triangles = AddTriangles(7, 6, 11);
			triangleTable.Add(254, triangles);
		}
	}
	#endregion

	#region Gizmos.
	//private void OnDrawGizmos() {
	//	if (cubeGrid != null && showGizmos) {
	//		for (int x = 0; x < cubeGrid.cubes.GetLength(0); x++) {
	//			for (int y = 0; y < cubeGrid.cubes.GetLength(1); y++) {
	//				for (int z = 0; z < cubeGrid.cubes.GetLength(2); z++) {

	//					Gizmos.color = (cubeGrid.cubes[x, y, z].bottomFrontLeft.active) ? Color.black : Color.white;
	//					Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].bottomFrontLeft.position + gameObject.transform.position, Vector3.one * 0.4f);

	//					Gizmos.color = (cubeGrid.cubes[x, y, z].bottomBackRight.active) ? Color.black : Color.white;
	//					Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].bottomBackRight.position + gameObject.transform.position, Vector3.one * 0.4f);

	//					Gizmos.color = (cubeGrid.cubes[x, y, z].bottomBackLeft.active) ? Color.black : Color.white;
	//					Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].bottomBackLeft.position + gameObject.transform.position, Vector3.one * 0.4f);

	//					Gizmos.color = (cubeGrid.cubes[x, y, z].bottomFrontRight.active) ? Color.black : Color.white;
	//					Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].bottomFrontRight.position + gameObject.transform.position, Vector3.one * 0.4f);

	//					Gizmos.color = (cubeGrid.cubes[x, y, z].topFrontLeft.active) ? Color.black : Color.white;
	//					Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].topFrontLeft.position + gameObject.transform.position, Vector3.one * 0.4f);

	//					Gizmos.color = (cubeGrid.cubes[x, y, z].topFrontRight.active) ? Color.black : Color.white;
	//					Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].topFrontRight.position + gameObject.transform.position, Vector3.one * 0.4f);

	//					Gizmos.color = (cubeGrid.cubes[x, y, z].topBackLeft.active) ? Color.black : Color.white;
	//					Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].topBackLeft.position + gameObject.transform.position, Vector3.one * 0.4f);

	//					Gizmos.color = (cubeGrid.cubes[x, y, z].topBackRight.active) ? Color.black : Color.white;
	//					Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].topBackRight.position + gameObject.transform.position, Vector3.one * 0.4f);

	//					Gizmos.color = Color.black;
	//					Gizmos.DrawWireCube(cubeGrid.cubes[x, y, z].Position + gameObject.transform.position, Vector3.one * cubeGrid.cubes[x, y, z].size);

	//					//	Gizmos.color = Color.grey;
	//					//	Gizmos.DrawCube(cubeGrid.cubes[x, y, z].bottomLeft.position, Vector3.one * 0.15f);
	//					//	Gizmos.DrawCube(cubeGrid.cubes[x, y, z].bottomFront.position, Vector3.one * 0.15f);
	//					//	Gizmos.DrawCube(cubeGrid.cubes[x, y, z].bottomRight.position, Vector3.one * 0.15f);
	//					//	Gizmos.DrawCube(cubeGrid.cubes[x, y, z].bottomBack.position, Vector3.one * 0.15f);
	//					//	Gizmos.DrawCube(cubeGrid.cubes[x, y, z].topLeft.position, Vector3.one * 0.15f);
	//					//	Gizmos.DrawCube(cubeGrid.cubes[x, y, z].topFront.position, Vector3.one * 0.15f);
	//					//	Gizmos.DrawCube(cubeGrid.cubes[x, y, z].topRight.position, Vector3.one * 0.15f);
	//					//	Gizmos.DrawCube(cubeGrid.cubes[x, y, z].topBack.position, Vector3.one * 0.15f);
	//					//	Gizmos.DrawCube(cubeGrid.cubes[x, y, z].midBackLeft.position, Vector3.one * 0.15f);
	//					//	Gizmos.DrawCube(cubeGrid.cubes[x, y, z].midBackRight.position, Vector3.one * 0.15f);
	//					//	Gizmos.DrawCube(cubeGrid.cubes[x, y, z].midFrontLeft.position, Vector3.one * 0.15f);
	//					//	Gizmos.DrawCube(cubeGrid.cubes[x, y, z].midFrontRight.position, Vector3.one * 0.15f);
	//				}
	//			}
	//		}
	//	}
	//}
	#endregion
}

public class MeshData {
	//Variables.
	public List<Vector3> vertices;
	public List<int> triangles;
	public Vector3[] normals;
	private Dictionary<Vector3, Vector3> normalDictionary;

	//Constructor.
	public MeshData() {
		vertices = new List<Vector3>();
		triangles = new List<int>();
		normalDictionary = new Dictionary<Vector3, Vector3>();
	}

	//Functions.
	public Vector3[] GetNormals(List<Vector3> verts) {
		Vector3[] norms = new Vector3[verts.Count];

		for (int i = 0; i < verts.Count; i++) {
			norms[i] = normalDictionary[verts[i]];
		}

		return norms;
	}

	public Vector3[] CalculateNormals() {
		Vector3[] vertexNormals = new Vector3[vertices.Count];
		int triangleCount = triangles.Count / 3;
		for (int i = 0; i < triangleCount; i++) {
			int normalTriangleIndex = i * 3;
			int vertexIndexA = triangles[normalTriangleIndex];
			int vertexIndexB = triangles[normalTriangleIndex + 1];
			int vertexIndexC = triangles[normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
			vertexNormals[vertexIndexA] += triangleNormal;
			vertexNormals[vertexIndexB] += triangleNormal;
			vertexNormals[vertexIndexC] += triangleNormal;
		}

		for (int i = 0; i < vertexNormals.Length; i++) {
			vertexNormals[i].Normalize();
			normalDictionary.Add(vertices[i], vertexNormals[i]);
		}

		return vertexNormals;
	}

	private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
		Vector3 pointA = vertices[indexA];
		Vector3 pointB = vertices[indexB];
		Vector3 pointC = vertices[indexC];


		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;
		return Vector3.Cross(sideAB, sideAC).normalized;
	}
}

public struct MapThreadInfo<T> {
	public readonly Action<T> callback;
	public readonly T parameter;

	public MapThreadInfo(Action<T> a_callback, T a_parameter) {
		callback = a_callback;
		parameter = a_parameter;
	}
}