using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour {
	private CharacterController cc;
	
	void Start() {
		cc = GetComponent<CharacterController>();
	}

	void Update() {
		Vector2 movement = new Vector2(
			Input.GetAxis("Horizontal"),
			Input.GetAxis("Vertical")
		);
		
		Vector3 movement3 = new Vector3(movement.x, 0f, movement.y);
		
		cc.Move(movement3 * Time.deltaTime * 8f);
	}
}
