using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour {
	private static LevelManager me;
	public static LevelManager the { get => me; }
	
	private GameObject player;
	
	public enum State { Intro, Playing, Dead }
	
	private State currentState = State.Playing;
	public State state { get => currentState; }
	
	void Awake() {
		me = this;
		player = GameObject.FindGameObjectWithTag("Player");
	}
	
	void Start() {
		
	}
	
	void Update() {
		
	}
}
