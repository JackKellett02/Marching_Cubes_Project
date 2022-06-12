using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelGeneratorScript))]
public class LevelGeneratorEditor : Editor {
	#region Variables.

	#endregion

	#region Functions.
	public override void OnInspectorGUI() {

		//Get the target.
		LevelGeneratorScript levelGen = (LevelGeneratorScript)target;

		//Draw the normal inspector.
		if (DrawDefaultInspector()) {

		}

		//Button
		if (GUILayout.Button("Generate Level")) {
			//Generate the level mesh.
			if (Thread.CurrentThread.Name != "Main Unity Thread.") {
				Thread.CurrentThread.Name = "Main Unity Thread.";
			}
			Console.Clear();
			levelGen.DestroyAllChunks(30);
			FreeMemory();
			Debug.Log("Starting Thread Name: " + Thread.CurrentThread.Name);
			levelGen.CreateLevel();

		}

		if (GUILayout.Button("Destroy All Chunks.")) {
			//Destroy all the level chunks.
			levelGen.DestroyAllChunks(30);
			FreeMemory();
		}

		if (GUILayout.Button("Run Garbage Collection")) {
			FreeMemory();
		}
	}

	private void FreeMemory() {
		GC.Collect(0, GCCollectionMode.Forced);
	}
	#endregion
}
