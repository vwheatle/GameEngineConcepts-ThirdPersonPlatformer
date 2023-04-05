using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour {
	private CharacterController cc;
	
	[Header("Movement")]
	
	public float movementSpeed = 8f;
	public float jumpHeight = 10f;
	public int maximumJumps = 2;
	
	[Header("Juice")]
	
	public Transform juiceVisuals;
	
	private Vector3 leanEulerRotation;
	private Vector3 leanEulerRotationVelocity;
	// private float leanXDampen, leanYDampen;
	public float leanMagnitude = 30f;
	public float leanSpeed = 1/8f;
	public float leanRecoverSpeed = 1/2f;
	
	private float squishAmount, squishVelocity;
	public float squishRecoverSpeed = 1/4f;
	
	void Start() {
		cc = GetComponent<CharacterController>();
		
		leanEulerRotation = Vector3.zero;
	}

	void Update() {
		Vector2 movement = new Vector2(
			Input.GetAxisRaw("Horizontal"),
			Input.GetAxisRaw("Vertical")
		).normalized;
		
		bool moving = !Mathf.Approximately(movement.sqrMagnitude, 0f);
		
		if (Input.GetButton("Jump")) {
			squishAmount = 1f;
			squishVelocity = 0f;
		} else {
			squishAmount = Mathf.SmoothDamp(squishAmount, 0f, ref squishVelocity, squishRecoverSpeed);
		}
		
		leanEulerRotation = Vector3.ClampMagnitude(
			Vector3.SmoothDamp(
				leanEulerRotation,
				new Vector3(-movement.y, 0f, movement.x) * leanMagnitude,
				ref leanEulerRotationVelocity,
				moving ? leanSpeed : leanRecoverSpeed
			),
			leanMagnitude
		);
		
		juiceVisuals.localScale = Vector3.LerpUnclamped(Vector3.one, new Vector3(0.875f, 1.25f, 0.875f), squishAmount);
		juiceVisuals.localEulerAngles = leanEulerRotation;
		
		Vector3 movement3 = new Vector3(movement.x, 0f, movement.y);
		
		cc.Move(movement3 * movementSpeed * Time.deltaTime);
	}
}
