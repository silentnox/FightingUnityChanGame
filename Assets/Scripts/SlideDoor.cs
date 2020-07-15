using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// simple slide door script
// detect when entity enters collision box and then play open animation
public class SlideDoor : MonoBehaviour {

	public bool Open = false;

	public float Speed = 1.0f;

	bool changedState = false;

	int numColliders = 0;

	Animator[] animators;

    // Start is called before the first frame update
    void Start() {
		animators = GetComponentsInChildren<Animator>();
    }

	private void OnTriggerEnter(Collider other) {
		UnitHealth unit = other.GetComponent<UnitHealth>();
		numColliders++;
	}

	private void OnTriggerExit(Collider other) {
		numColliders--;
	}

	// Update is called once per frame
	void Update() {
		foreach (Animator anim in animators) {
			anim.speed = Speed;
			anim.Play(numColliders > 0 || Open? "Open" : "Close");
		}
		changedState = false;
    }
}
