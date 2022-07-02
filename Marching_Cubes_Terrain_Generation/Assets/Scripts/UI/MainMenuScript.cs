using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuScript : MonoBehaviour {
	// Start is called before the first frame update
	void Start() {

	}

	// Update is called once per frame
	void Update() {

	}

	#region Public Access Functions.
	/// <summary>
	/// Loads whatever scene is second in the build index.
	/// </summary>
	public void Play() {
		SceneManager.LoadScene(1);
	}

	/// <summary>
	/// Quits the game
	/// </summary>
	public void Quit() {
		Application.Quit();
		Debug.Log("Application Quitting");
	}

	#endregion
}
