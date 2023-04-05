using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour {
	private CharacterController cc;
	
	[Header("Movement")]
	
	public float movementSpeed = 8f;
	public float jumpHeight = 10f;
	public int maximumJumps = 2;
	private int remainingJumps = 0;
	
	private float upward = -4f;
	
	[Header("Juice")]
	
	public Transform juiceVisuals;
	
	// Lean back while moving forward.
	private Vector3 leanEulerRotation;
	private Vector3 leanEulerRotationVelocity;
	public float leanMagnitude = 30f;
	public float leanSpeed = 1/8f;
	public float leanRecoverSpeed = 1/2f;
	
	// Squish while jumping and landing.
	private Vector3 stretchVector = new Vector3(7/8f, 5/4f, 7/8f);
	private float stretchAmount, stretchVelocity;
	public float stretchRecoverSpeed = 1/4f;
	
	void Start() {
		cc = GetComponent<CharacterController>();
		
		leanEulerRotation = Vector3.zero;
		remainingJumps = maximumJumps;
	}

	void Update() {
		Vector2 wasd = new Vector2(
			Input.GetAxisRaw("Horizontal"),
			Input.GetAxisRaw("Vertical")
		).normalized;
		
		bool grounded = cc.isGrounded;
		
		bool moving = !Mathf.Approximately(wasd.sqrMagnitude, 0f);
		
		bool shouldJump = false;
		
		stretchAmount = Mathf.SmoothDamp(stretchAmount, 0f, ref stretchVelocity, stretchRecoverSpeed);
		
		if (grounded) {
			upward = 0f;
			
			if (remainingJumps != maximumJumps) {
				Debug.Log("recover!!");
				
				// we just landed, so recover jumps
				remainingJumps = maximumJumps;
				
				// and recoil a bit from the landing
				// (squish amount can be negative. it's fun)
				stretchAmount = -0.5f;
			}
			
			if (Input.GetButton("Jump"))
				shouldJump = true;
		} else {
			if (Input.GetButtonDown("Jump"))
				shouldJump = true;
		}
		
		if (shouldJump) {
			Debug.Log("jump!!");
			
			// Try to jump!
			if (remainingJumps > 0) {
				remainingJumps--;
				
				stretchAmount = 1f;
				stretchVelocity = 0f;
				
				upward = jumpHeight;
				grounded = false;
			} else {
				// You tried to jump, but it's just
				// "your body stretched in midair" right now...
				// Try again when you land!
				stretchAmount = 0.25f;
			}
		} else {
			// Otherwise, be affected by gravity.
			upward = Mathf.Max(-8f, upward - Mathf.Abs(Time.deltaTime * 12f));
		}
		
		leanEulerRotation = Vector3.ClampMagnitude(
			Vector3.SmoothDamp(
				leanEulerRotation,
				new Vector3(-wasd.y, 0f, wasd.x) * leanMagnitude,
				ref leanEulerRotationVelocity,
				moving ? leanSpeed : leanRecoverSpeed
			),
			leanMagnitude
		);
		
		juiceVisuals.localScale = Vector3.LerpUnclamped(Vector3.one, stretchVector, stretchAmount);
		juiceVisuals.localEulerAngles = leanEulerRotation;
		
		Vector3 movement = new Vector3(
			wasd.x * movementSpeed,
			upward,
			wasd.y * movementSpeed
		);
		movement = transform.localRotation * movement;
		cc.Move(movement * Time.deltaTime);
	}
}
