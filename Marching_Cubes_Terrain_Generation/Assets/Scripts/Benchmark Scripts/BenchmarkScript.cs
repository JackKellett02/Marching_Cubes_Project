using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BenchmarkScript : MonoBehaviour {
	#region Variables to Assign via the unity inspector (SerializeFields).
	[SerializeField]
	private int testIterations = 10;

	[SerializeField]
	private LevelGeneratorScript levelGen = null;

	[SerializeField]
	private Text frameRateText = null;
	#endregion

	#region Private Variable Declarations.

	private float startTime = 0.0f;
	private float averageFrameRate = 0.0f;
	private float minFrameRate = float.MaxValue;
	private float destructionTime = 0.0f;
	private int frameCounter = 0;
	private bool canStart = true;
	private bool benchMarkStarted = false;
	private int iterationsComplete = 0;

	private static int totalChunkCount = 0;
	private static int chunksComplete = 0;
	private static int voxelsPerChunk = 0;
	#endregion

	#region Private Functions.
	// Start is called before the first frame update
	void Start()
	{
		destructionTime = 0.0f;
		iterationsComplete = 0;
		voxelsPerChunk = 0;
		totalChunkCount = 0;
		chunksComplete = 0;
		frameCounter = 0;
		benchMarkStarted = false;
		if (frameRateText) {
			frameRateText.gameObject.SetActive(false);
		}
	}

	// Update is called once per frame
	void Update() {
		if (canStart && Input.GetKeyDown(KeyCode.Space)) {
			//Start benchmark.
			canStart = false;
			startTime = Time.time;
			benchMarkStarted = true;
			if (frameRateText) {
				frameRateText.gameObject.SetActive(true);
			}

			//Start generation of the terrain.
			if (levelGen != null) {
				levelGen.CreateLevel();
			}
		}
	}

	void LateUpdate() {
		//Increment frame counter.
		if (benchMarkStarted) {
			frameCounter++;

			//Update current average frame rate.
			float runTime = Time.time - startTime;
			averageFrameRate = frameCounter / runTime;
			float currentFramerate = 1.0f / Time.deltaTime;
			if (currentFramerate <= minFrameRate) {
				minFrameRate = currentFramerate;
			}

			//Update benchmark UI.
			frameRateText.text = "Current Framerate: " + currentFramerate + " FPS\nAverage Framerate: " + averageFrameRate + " FPS\nMinimum Framerate: " + minFrameRate + " FPS";

			//Check if simulation over
			if (chunksComplete >= totalChunkCount) {
				//Increment Iteration Counter.
				iterationsComplete++;
				if (iterationsComplete >= testIterations) {
					int score = CalculateScore();
					UpdateHighScore(score);
					benchMarkStarted = false;
					Debug.Log("End Time: " + Time.time);
				} else {
					//Start next iteration.
					if (levelGen != null)
					{
						float testTime = Time.time;
						levelGen.DestroyAllChunks(30);
						testTime = Time.time - testTime;
						destructionTime += testTime;
						levelGen.CreateLevel();
					}
				}
			}
		}

	}

	private int CalculateScore() {
		float totalRunTime = Time.time - startTime - destructionTime;
		int voxelsProcessed = totalChunkCount * voxelsPerChunk;
		int score = (int)(((float)voxelsProcessed / totalRunTime) / testIterations);
		return score;
	}
	private void UpdateHighScore(int score) {
		frameRateText.text = "Score: " + score;
	}
	#endregion

	#region Public Access Functions (Getters and Setters).
	public static void SetTotalChunkCount(int chunkCount) {
		totalChunkCount = chunkCount;
	}

	public static void AddTotalChunkCount(int chunkCount) {
		totalChunkCount += chunkCount;
	}

	public static void IncrementCompleteChunks() {
		chunksComplete++;
	}

	public static void SetVoxelsPerChunk(int value) {
		voxelsPerChunk = value;
	}

	public static void AddVoxelsPerChunk(int value) {
		voxelsPerChunk += value;
	}
	#endregion
}
