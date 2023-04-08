using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSoul : MonoBehaviour {
	// http://answers.unity.com/answers/1903352/view.html
	// this "ghost" concept works!!
	
	Transform parent { get => transform.parent; set {
		if (transform.parent != value) {
			transform.parent = value;
			transform.position = Vector3.zero;
		}
	} }
	
	void Awake() {
		parent = null;
		transform.position = Vector3.zero;
	}
	
	// Note that this delta position is already scaled to
	// the previous frame's Time.deltaTime.
	// I'm not sure how I feel about this.
	public Vector3 GetDeltaPosition() {
		if (parent == null) return Vector3.zero;
		
		Vector3 delta = transform.position;
		transform.position = Vector3.zero;
		
		return delta;
	}
	
	public void AttachTo(Transform t) { parent = t; }
	public void Detach() { parent = null; }
}
