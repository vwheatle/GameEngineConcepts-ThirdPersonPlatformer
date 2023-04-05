using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour {
	private CharacterController cc;
	
	[Header("Movement")]
	
	public float movementSpeed = 8f;
	[Tooltip("The ")]
	public float jumpHeight = 10f;
	public float gravity = -12f;
	public float terminalFallVelocity = -12f;
	[Tooltip("The maximum allowed jumps, possibly in mid-air, before the player must land safely to jump again.")]
	public int maximumJumps = 2;
	private int remainingJumps = 0;
	
	private float upward = -4f;
	
	[Header("Juice")]
	
	[Tooltip("The transform to scale during stretch/squish animations. Should be a child of this.")]
	public Transform juiceScalePivot;
	
	[Tooltip("The transform to rotate during leaning animations. Should be a child of the scale pivot.")]
	public Transform juiceRotationPivot;
	
	[Tooltip("The effect prefab to spawn when you jump.")]
	public GameObject juiceJumpEffect;
	
	// Lean into/out of movement.
	private Vector3 leanEulerRotation;
	private Vector3 leanEulerRotationVelocity;
	
	[Tooltip("Positive to lean forward, Negative to lean back.")]
	public float leanMagnitude = -20f;
	
	[Tooltip("Number of seconds needed to lean completely in a direction.")]
	public float leanSpeed = 1/8f;
	
	[Tooltip("Number of seconds needed to return from leaning to standing straight.")]
	public float leanRecoverSpeed = 1/2f;
	
	// Squish while jumping and landing.
	private Vector3 stretchVector = new Vector3(7/8f, 5/4f, 7/8f);
	private float stretchAmount; // <- could replace with Vector3s, but it's more efficient this way...
	private float stretchVelocity;
	
	[Tooltip("Number of seconds needed to return from deformations to normal size.")]
	public float stretchRecoverSpeed = 1/4f;
	
	// Internal flags and state and stuff
	
	private bool prevGrounded, grounded;
	private float stolenSlopeLimit;
	
	void Start() {
		cc = GetComponent<CharacterController>();
		
		// Sorry cc, I handle slopes now.
		// (This lets me stick the player to downward slopes.)
		stolenSlopeLimit = cc.slopeLimit;
		cc.slopeLimit = 0f;
		// adapted this tutorial
		// https://youtu.be/PEHtceu7FBw
		
		leanEulerRotation = Vector3.zero;
		remainingJumps = maximumJumps;
	}

	void Update() {
		bool justLanded     = (!prevGrounded) && ( grounded);
		bool justLeftGround = ( prevGrounded) && (!grounded);
		
		RaycastHit? hit = null;
		{
			RaycastHit shit;
			if (Physics.SphereCast(
				transform.position + cc.center,
				cc.radius,
				Vector3.down, out shit,
				(cc.height / 2f + cc.skinWidth * 2f) - cc.radius,
				// height must be half, and we gotta subtract radius,
				// but doubling skinWidth was arbitrary on my part
				-1,
				QueryTriggerInteraction.Ignore
			)) {
				hit = shit;
			}
			// Option<T> has spoiled me, in terms of interface design.
			// Now I'm chasing its elegant code even when it's missing.
		}
		
		Vector3 normal = hit?.normal ?? Vector3.up;
		
		// some debug visuals
		Debug.DrawRay(transform.position, normal, Color.red, 1f);
		Debug.DrawRay(transform.position + cc.center, Vector3.down * (cc.height / 2f + cc.skinWidth * 2f), Color.blue, 1f);
		
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
		
		bool tryJump = Input.GetButtonDown("Jump");
		
		if (grounded) {
			// Hack to allow easy bunny-hopping.
			if (Input.GetButton("Jump") && upward < 0f) tryJump = true;
			
			upward = 0f;
			
			if (justLanded) {
				// recover jumps we lost in air
				remainingJumps = maximumJumps;
				
				// and recoil a bit from the landing
				// (squish amount can be negative. it's fun)
				stretchAmount = -1/2f;
			}
		} else {
			if (justLeftGround) {
				// lose an implicit jump
				// (this is how  double jump works in most games)
				remainingJumps--;
			}
			
			if (!tryJump) {
				// Otherwise, be affected by gravity.
				upward = Mathf.Max(terminalFallVelocity, upward + Mathf.Max(0f, Time.deltaTime) * gravity);
			}
		}
	
		if (tryJump) {
			if (remainingJumps > 0) {
				// Spawn the "Jump Effect" (ring that shrinks and disappears)
				Instantiate(
					juiceJumpEffect,
					transform.position,
					Quaternion.FromToRotation(Vector3.up, normal)
				);
				
				// Lose a jump
				remainingJumps--;
				
				// Stretch out
				stretchAmount = 1f;
				stretchVelocity = 0f;
				
				// Initial jump velocity
				upward = jumpHeight;
				
				// You're no longer on the ground,
				// so take away ground information.
				grounded = false; hit = null;
			} else {
				// You tried to jump, but it's just
				// "your body stretched in midair" right now...
				// Try again when you land!
				stretchAmount = 1/4f;
			}
		}
		
		leanEulerRotation = Vector3.ClampMagnitude(
			Vector3.SmoothDamp(
				leanEulerRotation,
				new Vector3(wasd.y, 0f, -wasd.x) * leanMagnitude,
				ref leanEulerRotationVelocity,
				moving ? leanSpeed : leanRecoverSpeed
			),
			leanMagnitude
		);
		
		juiceScalePivot.localScale = Vector3.LerpUnclamped(Vector3.one, stretchVector, stretchAmount);
		juiceRotationPivot.localEulerAngles = leanEulerRotation;
		
		Vector3 movement = new Vector3(wasd.x, 0f, wasd.y) * movementSpeed;
		movement = transform.localRotation * movement;
		movement = AdjustVelocityToNormal(movement, normal, stolenSlopeLimit);
		movement.y += upward;
		
		prevGrounded = grounded;
		cc.Move(movement * Time.deltaTime);
		grounded = cc.isGrounded && upward <= Mathf.Epsilon;
		
		// Hack to fix grounded flag jitter.
		// (CharacterController only does some collision stuff for you, it
		//  doesn't know what Y velocity variables you use, so you gotta
		//  give it hints sometimes. It's all good.)
		grounded |= prevGrounded && hit.HasValue;
	}
	
	void OnControllerColliderHit(ControllerColliderHit hit) {
		if (!hit.gameObject.isStatic) {
			// transform.parent = hit.transform;
			
			// UNITY WHY DOESN'T THIS WORK?????
			// It straight up updates its local position in the inspector
			// so i'm mystified how this doesn't work.
			
			// http://answers.unity.com/answers/1903352/view.html
			// this "ghost" concept seems like a good solution tho
		}
	}
	
	static Vector3 AdjustVelocityToNormal(Vector3 velocity, Vector3 normal, float slopeLimit = 90f) {
		Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
		if (Quaternion.Angle(Quaternion.identity, rotation) > slopeLimit) return velocity;
		return rotation * velocity;
	}
}
