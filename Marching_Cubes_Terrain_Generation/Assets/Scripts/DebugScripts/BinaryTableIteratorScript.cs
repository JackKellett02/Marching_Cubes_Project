using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BinaryTableIteratorScript : MonoBehaviour {
	#region Variables to assign via the unity inspector (SerializeFields).
	[SerializeField]
	private int startingConfig = 0;

	[SerializeField]
	[Range(0.01f, 2.0f)]
	private float cooldownTime = 0.25f;

	[SerializeField]
	private Text currentValueText = null;

	[SerializeField]
	private Image[] cornerImages = new Image[8];

	[SerializeField]
	private bool genDebugMesh = false;
	#endregion

	#region Private Variables.

	private bool cooldown = false;
	private static int currentValue;
	#endregion

	#region Private Functions.
	// Start is called before the first frame update
	void Start()
	{
		currentValue = startingConfig;
		UpdateIndicators(currentValue);

		//Gen the debug mesh.
		if (genDebugMesh) {
			CubeVisualiser visualiser = GameObject.FindGameObjectsWithTag("MCVisualiser")[0].GetComponent<CubeVisualiser>();
			visualiser.GenerateNextConfig(currentValue);
		}
	}

	void FixedUpdate() {
		if (Input.GetKey(KeyCode.Return) && !cooldown) {
			//Iterate the integer "currentValue".
			currentValue++;
			if (currentValue > 255) {
				currentValue = 0;
			}

			//Update the indicator UI.
			UpdateIndicators(currentValue);

			//Update the current value text.
			currentValueText.text = currentValue.ToString();

			//Gen the debug mesh.
			if (genDebugMesh)
			{
				CubeVisualiser visualiser = GameObject.FindGameObjectsWithTag("MCVisualiser")[0].GetComponent<CubeVisualiser>();
				visualiser.GenerateNextConfig(currentValue);
			}

			//Enable Cooldown.
			StartCoroutine(ButtonCooldown(cooldownTime));
		}
	}

	private IEnumerator ButtonCooldown(float time)
	{
		cooldown = true;
		yield return new WaitForSeconds(time);
		cooldown = false;
	}

	private void UpdateIndicators(int value) {
		if (currentValue == 0) {
			for (int i = 0; i < cornerImages.Length; i++) {
				cornerImages[i].color = Color.black;
			}
		} else {
			//Calculate which indicator lights should be on.
			int originalValue = value;
			int calculationOriginalValue = value;

			//Corner 0.
			if ((calculationOriginalValue - 128) >= 0) {
				calculationOriginalValue -= 128;
				cornerImages[0].color = Color.green;
			} else {
				cornerImages[0].color = Color.black;
			}

			//Corner 1.
			if ((calculationOriginalValue - 64) >= 0) {
				calculationOriginalValue -= 64;
				cornerImages[1].color = Color.green;
			} else {
				cornerImages[1].color = Color.black;
			}

			//Corner 2.
			if ((calculationOriginalValue - 32) >= 0) {
				calculationOriginalValue -= 32;
				cornerImages[2].color = Color.green;
			} else {
				cornerImages[2].color = Color.black;
			}

			//Corner 3.
			if ((calculationOriginalValue - 16) >= 0) {
				calculationOriginalValue -= 16;
				cornerImages[3].color = Color.green;
			} else {
				cornerImages[3].color = Color.black;
			}

			//Corner 4.
			if ((calculationOriginalValue - 8) >= 0) {
				calculationOriginalValue -= 8;
				cornerImages[4].color = Color.green;
			} else {
				cornerImages[4].color = Color.black;
			}

			//Corner 5.
			if ((calculationOriginalValue - 4) >= 0) {
				calculationOriginalValue -= 4;
				cornerImages[5].color = Color.green;
			} else {
				cornerImages[5].color = Color.black;
			}

			//Corner 6.
			if ((calculationOriginalValue - 2) >= 0) {
				calculationOriginalValue -= 2;
				cornerImages[6].color = Color.green;
			} else {
				cornerImages[6].color = Color.black;
			}

			if ((calculationOriginalValue - 1) >= 0) {
				calculationOriginalValue -= 1;
				cornerImages[7].color = Color.green;
			} else {
				cornerImages[7].color = Color.black;
			}

		}
	}
	#endregion

	#region Public Access Functions (Getters and Setters).

	public static int GetCurrentValue()
	{
		return currentValue;
	}
	#endregion
}
