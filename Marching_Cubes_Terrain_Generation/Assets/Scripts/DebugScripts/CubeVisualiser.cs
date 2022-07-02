using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class CubeVisualiser : MonoBehaviour
{
    FreeMovementCamera mainCamera;
    public MeshFilter levelMeshFilter = null;

    public void GenerateNextConfig(int config)
    {
	    float[,,] map = new float[2, 2, 2];
	    for (int z = 0; z < 2; z++)
	    {
		    for (int y = 0; y < 2; y++)
		    {
			    for (int x = 0; x < 2; x++)
			    {
				    map[x, y, z] = 0.0f;
			    }
		    }
	    }

        //Generate the next config.
        levelMeshFilter.mesh = MarchingCubes.DebugGenMesh(config, map);
    }

    private void Start()
    {
        mainCamera = Camera.main.GetComponent<FreeMovementCamera>();
    }

    //private void OnDrawGizmos()
    //{
    //    //Axis.
    //    Vector3 localUp = mainCamera.GetLocalUpVector();

    //    //Main Cube.
    //    Gizmos.color = Color.black;
    //    Gizmos.DrawWireCube(transform.position, Vector3.one);

    //    //Corners.
    //    //Bottom Front Left.
    //    Gizmos.color = Color.white;
    //    Handles.color = Color.white;
    //    Vector3 cornerPos = transform.localPosition;
    //    Vector3 cornerSize = (Vector3.one / 8.0f);
    //    cornerPos.x -= 0.5f;
    //    cornerPos.y -= 0.5f;
    //    cornerPos.z += 0.5f;
    //    Vector3 textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.15f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Bottom Front Left (C0)");

    //    //Bottom Front Right.
    //    cornerPos = transform.localPosition;
    //    cornerPos.x += 0.5f;
    //    cornerPos.y -= 0.5f;
    //    cornerPos.z += 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.15f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Bottom Front Right (C1)");

    //    //Bottom Back Right.
    //    cornerPos = transform.localPosition;
    //    cornerPos.x += 0.5f;
    //    cornerPos.y -= 0.5f;
    //    cornerPos.z -= 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.15f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Bottom Back Right (C2)");

    //    //Bottom Back Left.
    //    cornerPos = transform.localPosition;
    //    cornerPos.x -= 0.5f;
    //    cornerPos.y -= 0.5f;
    //    cornerPos.z -= 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.15f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Bottom Back Left (C3)");

    //    //Top Front Left.
    //    cornerPos = transform.localPosition;
    //    cornerPos.x -= 0.5f;
    //    cornerPos.y += 0.5f;
    //    cornerPos.z += 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.15f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Top Front Left (C4)");

    //    //Top Front Right.
    //    cornerPos = transform.localPosition;
    //    cornerPos.x += 0.5f;
    //    cornerPos.y += 0.5f;
    //    cornerPos.z += 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.15f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Top Front Right (C5)");

    //    //Top Back Right.
    //    cornerPos = transform.localPosition;
    //    cornerPos.x += 0.5f;
    //    cornerPos.y += 0.5f;
    //    cornerPos.z -= 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.15f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Top Back Right (C6)");

    //    //Top Back Left.
    //    cornerPos = transform.localPosition;
    //    cornerPos.x -= 0.5f;
    //    cornerPos.y += 0.5f;
    //    cornerPos.z -= 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.15f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Top Back Left (C7)");


    //    //Edges.
    //    //Bottom Front.
    //    Gizmos.color = Color.black;
    //    cornerSize = (Vector3.one / 16.0f);
    //    cornerPos = transform.localPosition;
    //    cornerPos.y -= 0.5f;
    //    cornerPos.z += 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.1f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Edge 0");

    //    //Bottom Right.
    //    cornerPos = transform.localPosition;
    //    cornerPos.y -= 0.5f;
    //    cornerPos.x += 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.1f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Edge 1");

    //    //Bottom Back.
    //    cornerPos = transform.localPosition;
    //    cornerPos.y -= 0.5f;
    //    cornerPos.z -= 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.1f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Edge 2");

    //    //Bottom Left.
    //    cornerPos = transform.localPosition;
    //    cornerPos.y -= 0.5f;
    //    cornerPos.x -= 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.1f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Edge 3");

    //    //Top Front.
    //    cornerPos = transform.localPosition;
    //    cornerPos.y += 0.5f;
    //    cornerPos.z += 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.1f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Edge 4");

    //    //Top Right.
    //    cornerPos = transform.localPosition;
    //    cornerPos.y += 0.5f;
    //    cornerPos.x += 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.1f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Edge 5");

    //    //Top Back.
    //    cornerPos = transform.localPosition;
    //    cornerPos.y += 0.5f;
    //    cornerPos.z -= 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.1f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Edge 6");

    //    //Top Left.
    //    cornerPos = transform.localPosition;
    //    cornerPos.y += 0.5f;
    //    cornerPos.x -= 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.1f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Edge 7");

    //    //Mid Front Left.
    //    cornerPos = transform.localPosition;
    //    cornerPos.x -= 0.5f;
    //    cornerPos.z += 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.1f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Edge 8");

    //    //Mid Front Right.
    //    cornerPos = transform.localPosition;
    //    cornerPos.x += 0.5f;
    //    cornerPos.z += 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.1f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Edge 9");

    //    //Mid Back Right.
    //    cornerPos = transform.localPosition;
    //    cornerPos.x += 0.5f;
    //    cornerPos.z -= 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.1f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Edge 10");

    //    //Mid Back Left.
    //    cornerPos = transform.localPosition;
    //    cornerPos.x -= 0.5f;
    //    cornerPos.z -= 0.5f;
    //    textPos = new Vector3(cornerPos.x, cornerPos.y, cornerPos.z);
    //    textPos += localUp * 0.1f;
    //    Gizmos.DrawCube(cornerPos, cornerSize);
    //    Handles.Label(textPos, "Edge 11");

    //}
}