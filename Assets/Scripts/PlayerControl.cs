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
	
	public Transform juiceScalePivot;
	public Transform juiceRotationPivot;
	
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
	
	private bool prevGrounded, grounded;
	
	void Start() {
		cc = GetComponent<CharacterController>();
		
		leanEulerRotation = Vector3.zero;
		remainingJumps = maximumJumps;
	}

	void Update() {
		bool justLanded     = (!prevGrounded) && ( grounded);
		bool justLeftGround = ( prevGrounded) && (!grounded);
		
		Vector2 wasd = new Vector2(
			Input.GetAxisRaw("Horizontal"),
			Input.GetAxisRaw("Vertical")
		).normalized;
		
		bool moving = !Mathf.Approximately(wasd.sqrMagnitude, 0f);
		
		stretchAmount = Mathf.SmoothDamp(
			stretchAmount, 0f,
			ref stretchVelocity,
			stretchRecoverSpeed
		);
		
		bool tryJump = false;
		
		if (grounded) {
			upward = 0f;
			
			if (justLanded) {
				Debug.Log("recover!");
				
				// recover jumps we lost in air
				remainingJumps = maximumJumps;
				
				// and recoil a bit from the landing
				// (squish amount can be negative. it's fun)
				stretchAmount = -0.5f;
			}
			
			if (Input.GetButton("Jump")) tryJump = true;
		} else {
			if (justLeftGround) {
				Debug.Log("peace!");
				
				// lose an implicit jump
				// (this is how  double jump works in most games)
				// remainingJumps--;
			}
			
			if (Input.GetButtonDown("Jump")) tryJump = true;
			
			if (!tryJump) {
				// Otherwise, be affected by gravity.
				upward = Mathf.Max(-8f, upward - Mathf.Abs(Time.deltaTime * 12f));
			}
		}
	
		if (tryJump) {
			if (remainingJumps > 0) {
				// Lose a jump
				remainingJumps--;
				
				// Stretch out
				stretchAmount = 1f;
				stretchVelocity = 0f;
				
				// Initial jump velocity
				upward = jumpHeight;
				
				// You're no longer on the ground
				grounded = false;
			} else {
				// You tried to jump, but it's just
				// "your body stretched in midair" right now...
				// Try again when you land!
				stretchAmount = 0.25f;
			}
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
		
		juiceScalePivot.localScale = Vector3.LerpUnclamped(Vector3.one, stretchVector, stretchAmount);
		juiceRotationPivot.localEulerAngles = leanEulerRotation;
		
		Vector3 movement = new Vector3(
			wasd.x * movementSpeed,
			upward,
			wasd.y * movementSpeed
		);
		movement = transform.localRotation * movement;
		
		prevGrounded = grounded;
		cc.Move(movement * Time.deltaTime);
		grounded = cc.isGrounded;
		
		// Hack to fix stupid grounded flag jitter.
		grounded |= prevGrounded && Mathf.Approximately(upward, 0f);
	}
}
