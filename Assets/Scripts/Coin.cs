using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour {
	const float REVOLUTIONS_PER_SECOND = 2f;
	const float ROTATE_SPEED = 360f / REVOLUTIONS_PER_SECOND;
	
	const float SHRINK_DURATION = 0.25f;
	
	private Transform graphic;
	
	private float? collectTime = null;
	
	void Awake() {
		ResetLevel();
	}
	
	void ResetLevel() {
		collectTime = null;
		gameObject.SetActive(true);
	}
	
	void Update() {
		transform.localEulerAngles = Vector3.up * ROTATE_SPEED * Time.time;
		
		if (collectTime.HasValue) {
			float startTime = collectTime.Value;
			
			float diffTime = Time.time - startTime;
			float percentDead = diffTime / SHRINK_DURATION;
			
			float scalarScale = Mathf.Max(0f, 1f - percentDead);
			
			// Hard-code coin size, don't care any more...
			transform.localScale = (Vector3.one / 2f) * scalarScale;
			
			if (percentDead >= 1f) gameObject.SetActive(false);
		}
	}
	
	void OnTriggerEnter(Collider other) {
		if (other.CompareTag("Player")) {
			// TODO send message to game state singleton
			collectTime = Time.time;
		}
	}
}
