using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SillyMode : MonoBehaviour
{
	private Vector3 home;
	
	public float oscillationSpeed = 1/4f;
	public float oscillationMagnitude = 8f;
	
    // Start is called before the first frame update
    void Start()
    {
        home = transform.localPosition;
    }

    // Update is called once per frame
    void Update() {
        transform.localPosition = home + Vector3.up * Mathf.Sin(oscillationSpeed * 2f * Mathf.PI * Time.time) * oscillationMagnitude;
    }
}
