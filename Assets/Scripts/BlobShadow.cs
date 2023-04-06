using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobShadow : MonoBehaviour {
	private CharacterController cc;
	private Renderer render;
	
	private float offset;
	
	void Start() {
		render = GetComponent<Renderer>();
		cc = GetComponentInParent<CharacterController>();
		
		offset = transform.localPosition.y;
	}

	bool among() {
		Debug.Log("bweh");
		return false;
	}
	
	void LateUpdate() {
		Transform parent = transform.parent.parent;
		
		// tired and need to lay down but
		// so like cast first with line and then
		// go back over it with sphere??
		return;
		// cuz line has more accuracy for flat surfaces
		// and sphere has more accuracy for where you'll rest upon
		// but i did have the problem of the ..
		// um the sphere hitting the side of a surface before it found the front of the surface and
		// that resulting in a weird blob shadow..
		
		RaycastHit sphereHit, rayHit;
		bool foundSomething = Physics.SphereCast(
			parent.position, cc.radius, Vector3.down,
			out sphereHit, Mathf.Infinity,
			-1, QueryTriggerInteraction.Ignore
		) | Physics.Raycast(
			parent.position, Vector3.down,
			out rayHit, Mathf.Infinity,
			-1, QueryTriggerInteraction.Ignore
		);
		bool shouldBeVisible = foundSomething;
		
		if (foundSomething) {
			// If the ray hit another thing, it's probably not what the player
			// will land on, so fall back to the sphere's hit info.
			RaycastHit hit = (sphereHit.transform != rayHit.transform) ? sphereHit : rayHit;
			Debug.Log(hit);
			Debug.Log(sphereHit.transform);
			Debug.Log(rayHit.transform);
			Debug.Log(hit.transform);
			
			transform.localRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
			if (Mathf.Abs(Quaternion.Angle(hit.transform.rotation, transform.rotation)) > 40f) {
				shouldBeVisible = false;
			}
			transform.position = hit.point + Vector3.up * offset;
		}
		
		render.enabled = shouldBeVisible;
	}
}
