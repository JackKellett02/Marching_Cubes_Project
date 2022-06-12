using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class boopScript : MonoBehaviour {
	public float speed = 10.0f;
	private Rigidbody rb;
	private void Start() {
		rb = gameObject.GetComponent<Rigidbody>();
	}

	private void Update() {
		if (Input.GetMouseButtonDown(0)) {
			rb.velocity += new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(0.25f, 1.0f), Random.Range(-1.0f, 1.0f)) * speed;
		}
	}
}
