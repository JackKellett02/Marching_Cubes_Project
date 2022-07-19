using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BenchmarkScript : MonoBehaviour {
	#region Variables to Assign via the unity inspector (SerializeFields).
	[SerializeField]
	private int testIterations = 10;

	[SerializeField]
	private Text testIterationText = null;

	[SerializeField]
	private Slider iterationSlider = null;

	[SerializeField]
	private LevelGeneratorScript levelGen = null;

	[SerializeField]
	private Text frameRateText = null;

	[SerializeField]
	private GameObject SettingsObjects = null;
	#endregion

	#region Private Variable Declarations.

	private float startTime = 0.0f;
	private int averageFrameRate = 0;
	private int minFrameRate = int.MaxValue;
	private float destructionTime = 0.0f;
	private int frameCounter = 0;
	private bool benchMarkStarted = false;
	private int iterationsComplete = 0;

	private static int totalChunkCount = 0;
	private static int chunksComplete = 0;
	private static int voxelsPerChunk = 0;
	#endregion

	#region Private Functions.
	// Start is called before the first frame update
	void Start() {
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

		if (testIterationText) {
			testIterationText.text = "Number Of Test Iterations: " + testIterations;
		}

		if (iterationSlider) {
			iterationSlider.value = testIterations;
		}
	}

	// Update is called once per frame
	void Update() {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			//Load the main menu if the test is over and the user presses escape.
			SceneManager.LoadScene(0);
		}
	}

	void LateUpdate() {
		//Increment frame counter.
		if (benchMarkStarted) {
			frameCounter++;

			//Update current average frame rate.
			float runTime = Time.time - startTime;
			averageFrameRate = Mathf.RoundToInt(frameCounter / runTime);
			int currentFramerate = Mathf.RoundToInt(1.0f / Time.deltaTime);
			if (currentFramerate <= minFrameRate) {
				minFrameRate = currentFramerate;
			}

			//Update benchmark UI.
			frameRateText.text = "Framerate: " + currentFramerate + " FPS\nAverage Framerate: " + averageFrameRate + " FPS\nMinimum Framerate: " + minFrameRate + " FPS";

			//Check if simulation over
			if (chunksComplete >= totalChunkCount) {
				//Increment Iteration Counter.
				iterationsComplete++;
				if (iterationsComplete >= testIterations) {
					//End benchmark.
					Debug.Log("End Time: " + Time.time);
					int score = CalculateScore();
					UpdateHighScore(score);
					benchMarkStarted = false;
				} else {
					//Start next iteration.
					if (levelGen != null) {
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

	public void StartBenchmark(int setting) {
		//Make sure the terrain settings are set to the correct values for the current test setting..
		switch (setting) {
			case 0: {
					levelGen.SetLevelSettings(0.4f, 64.0f, new Vector2Int(11, 11), new Vector3(32.0f, 128.0f, 32.0f), 8.0f);
					break;
				}
			case 1: {
					levelGen.SetLevelSettings(0.4f, 64.0f, new Vector2Int(21, 21), new Vector3(16.0f, 128.0f, 16.0f), 4.0f);
					break;
				}
			case 2: {
					levelGen.SetLevelSettings(0.4f, 64.0f, new Vector2Int(43, 43), new Vector3(8.0f, 128.0f, 8.0f), 2.0f);
					break;
				}
			case 3: {
					levelGen.SetLevelSettings(0.4f, 64.0f, new Vector2Int(85, 85), new Vector3(4.0f, 128.0f, 4.0f), 1.0f);
					break;
				}
			case 4: {
					levelGen.SetLevelSettings(0.4f, 64.0f, new Vector2Int(171, 171), new Vector3(2.0f, 128.0f, 2.0f), 0.5f);
					break;
				}
			default: {
					//If we get to the default case we want to just get out of the function before we hit the start benchmark and terrain stuff.
					return;
					break;
				}
		}

		//Modify UI.
		if (frameRateText) {
			frameRateText.gameObject.SetActive(true);
		}

		//Tell the user they can return to menu.
		if (testIterationText) {
			testIterationText.gameObject.SetActive(true);
			testIterationText.text = "Press 'Escape' to return to the main menu.";
		}

		if (iterationSlider) {
			iterationSlider.gameObject.SetActive(false);
		}

		if (SettingsObjects) {
			SettingsObjects.SetActive(false);
		}

		//Start benchmark.
		Application.targetFrameRate = 1000;
		startTime = Time.time;
		benchMarkStarted = true;

		//Start generation of the terrain.
		if (levelGen != null) {
			levelGen.CreateLevel();
		}
	}

	public void SetTestIterationCount(float iterationCount) {
		testIterations = (int)iterationCount;
		if (testIterationText) {
			testIterationText.text = "Number Of Test Iterations: " + testIterations;
		}
	}

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