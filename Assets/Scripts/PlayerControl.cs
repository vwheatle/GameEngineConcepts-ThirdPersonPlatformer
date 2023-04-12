using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour {
	private CharacterController cc;
	
	private AudioSource jumpSound;
	
	[Header("Movement")]
	
	[Tooltip("The movement speed, in units per second probably.")]
	public float movementSpeed = 8f;
	
	[Tooltip("The jump height, in units per second -- but no promises made that the player jumps to this height, as gravity is a force that exists. Treat it as a vague \"jump power\" slider maybe?")]
	public float jumpHeight = 10f;
	
	[Tooltip("The maximum velocity allowed for falling, in units per second.")]
	public float terminalFallVelocity = -12f;
	
	[Tooltip("The maximum allowed jumps, possibly in mid-air, before the player must land safely to jump again.")]
	public int maximumJumps = 2;
	
	private int remainingJumps = 0;
	
	private float upward = -4f;
	
	[Header("Juice")]
	
	[Tooltip("The transform to scale during stretch/squish animations. Should be a descendant of this game object.")]
	public Transform scalePivot;
	
	[Tooltip("The transform to rotate during leaning animations. Should be a descendant of the scale pivot.")]
	public Transform rotationPivot;
	
	[Tooltip("The effect prefab to spawn when you jump.")]
	public GameObject jumpEffect;
	
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
	private Transform lastFloor;
	
	private float stolenSlopeLimit;
	
	private PlayerSoul soul;
	// more like "sole" cuz the player's pivot position is their feet lmao!!!
	
	void Start() {
		cc = GetComponent<CharacterController>();
		
		jumpSound = GetComponent<AudioSource>();
		
		// Sorry cc, I handle slopes now.
		// (This helps the player stick to downward slopes.)
		stolenSlopeLimit = cc.slopeLimit;
		cc.slopeLimit = 0f;
		// adapted this tutorial
		// https://youtu.be/PEHtceu7FBw
		
		leanEulerRotation = Vector3.zero;
		remainingJumps = maximumJumps;
		
		// Create "soul" that possesses moving objects
		GameObject soulGo = new GameObject("Soul");
		soul = soulGo.AddComponent<PlayerSoul>();
	}

	void Update() {
		bool justLanded     = (!prevGrounded) && ( grounded);
		bool justLeftGround = ( prevGrounded) && (!grounded);
		
		// Height is halved as we're starting the cast in the capsule's center.
		// Radius is subtracted because of a fun interaction:
		// To start, the capsule's `.height` is not its total height,
		// but rather the height of the cylinder portion of the capsule.
		// That may leave you asking "well then why do you subtract it?
		//  if it's lacking from the length of the cast." Well, it's because
		// I'm already doing a sphere cast, and the "maximum cast length" that
		// I'm specifying will limit how far the *center* of the sphere goes
		// when casting, not how far the sphere will touch. The sphere has a
		// radius, and it kinda "overshoots" the maximum length to this radius.
		// (Unity defines a capsule as a cylinder (of height h and radius r)
		//  with two halves of a sphere (with radius r) glued to the flat ends.)
		// All that detail and then I double skinWidth, which was an arbitrary
		// decision on my part, just to give some wiggle room for the cast.
		float castLength = (cc.height / 2f + cc.skinWidth * 2f) - cc.radius;
		if (upward <= Mathf.Epsilon) {
			// Lengthen the cast's length while still or falling
			// to make sticking to moving ground easier.
			castLength += 1/4f;
			// This does have a downside of sticking
			// to the ground too soon in a few cases.
		}
		
		// // Visualization of the *actual* extent of the sphere cast,
		// // with the radius of the sphere included in the length:
		// Debug.DrawRay(transform.position + cc.center, Vector3.down * (castLength + cc.radius));
		
		RaycastHit? hit = null; {
			// Option<T> has spoiled me, in terms of interface design.
			// Now I'm chasing its elegant code even when it's missing.
			RaycastHit shit;
			if (Physics.SphereCast(
				transform.position + cc.center,
				cc.radius,
				Vector3.down, out shit,
				castLength,
				-1,
				QueryTriggerInteraction.Ignore
			)) {
				hit = shit;
				if (lastFloor != shit.transform) {
					lastFloor = shit.transform;
					soul.AttachTo(lastFloor);
				}
			}
			// (^This code packs the boolean of "if the cast hit or not" with
			//  the cast's hit data both into one nullable variable, which
			//  allows me to use C#'s fancy ?? null-coalescing operator and
			//  ?. null-conditional operator.)
		}
		
		Vector3 normal = hit?.normal ?? Vector3.up;
		Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, normal);
		float normalAngle = Quaternion.Angle(Quaternion.identity, normalRotation);
		
		// if (Mathf.Abs(normalAngle) > stolenSlopeLimit) {
		// 	// You can't stand here. Sorry.
		// 	// TODO: slipping??
		// 	grounded = false;
		// 	justLanded     = (!prevGrounded) && ( grounded);
		// 	justLeftGround = ( prevGrounded) && (!grounded);
		// 	hit = null;
		// }
		
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
			if (Input.GetButton("Jump") && upward < Mathf.Epsilon) tryJump = true;
			
			if (justLanded) {
				// recover jumps we lost in air
				remainingJumps = maximumJumps;
				
				// and recoil a bit from the landing
				// (squish amount can be negative. it's fun)
				if (upward < 1f) stretchAmount = -1/2f;
			}
			
			upward = 0f;
		} else {
			if (justLeftGround) {
				// lose an implicit jump
				// (this is how  double jump works in most games)
				remainingJumps--;
				
				// Detach the soul from the platform we left.
				soul.Detach();
			}
			
			if (!tryJump) {
				// Otherwise, be affected by gravity.
				upward = Mathf.Max(
					terminalFallVelocity,
					upward + Mathf.Max(0f, Time.deltaTime) * Physics.gravity.y
				);
			}
		}
		
		if (LevelManager.the.state != LevelManager.State.Playing) {
			wasd = Vector2.zero;
			moving = false;
			tryJump = false;
		}
	
		if (tryJump) {
			if (remainingJumps > 0) {
				// Spawn the "Jump Effect" (ring that shrinks and disappears)
				Instantiate(jumpEffect, transform.position, normalRotation);
				
				jumpSound.pitch = Mathf.Lerp(1.3f, 1f, remainingJumps / (float)maximumJumps);
				jumpSound.volume = 1f;
				jumpSound.Play();
				
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
				
				// and retreive your soul.
				soul.Detach();
			} else {
				// You tried to jump, but it's just
				// "your body stretched in midair" right now...
				// Try again when you land!
				stretchAmount = 1/4f;
				
				jumpSound.pitch = 1.75f;
				jumpSound.volume = 0.25f;
				jumpSound.Play();
			}
		}
		
		
		leanEulerRotation = Vector3.ClampMagnitude(
			Vector3.SmoothDamp(
				leanEulerRotation,
				new Vector3(wasd.y, 0f, -wasd.x) * leanMagnitude,
				ref leanEulerRotationVelocity,
				moving ? leanSpeed : leanRecoverSpeed
			),
			Mathf.Abs(leanMagnitude)
		);
		
		scalePivot.localScale = Vector3.LerpUnclamped(Vector3.one, stretchVector, stretchAmount);
		rotationPivot.localEulerAngles = leanEulerRotation;
		
		
		Vector3 movement = new Vector3(wasd.x, 0f, wasd.y) * movementSpeed;
		movement = transform.localRotation * movement;
		if (grounded)
			movement = AdjustVelocityToNormal(movement, normal, stolenSlopeLimit);
		movement.y += upward;
		
		// TODO: maybe if the `soul.GetDeltaPosition()` is not equal to the last
		// `soul.GetDeltaPosition()`, then it'll snap the player's position
		// to the position of the cast's hit?
		prevGrounded = grounded;
		cc.Move((movement * Time.deltaTime) + soul.GetDeltaPosition());
		grounded = cc.isGrounded && upward <= Mathf.Epsilon;
		
		// Hack to fix grounded flag jitter.
		// (CharacterController only does some collision stuff for you, it
		//  doesn't know what Y velocity variables you use, so you gotta
		//  give it hints sometimes. It's all good.)
		grounded |= prevGrounded && hit.HasValue;
	}
	
	void OnControllerColliderHit(ControllerColliderHit hit) {
		soul.AttachTo(hit.transform);
	}
	
	static Vector3 AdjustVelocityToNormal(Vector3 velocity, Vector3 normal, float slopeLimit = 90f) {
		Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
		if (Quaternion.Angle(Quaternion.identity, rotation) > slopeLimit) return velocity;
		return rotation * velocity;
	}
}
