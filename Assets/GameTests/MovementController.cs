using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour {

	public float turnSpeed = 180;
	public float moveSpeed = 5;

	[HideInInspector]
	public float turnFactor = 0;
	[HideInInspector]
	public float moveFactor = 0;
    
	// Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
		Animator animator = GetComponent<Animator>();
    }

	void FixedUpdate() {
		Animator animator = GetComponent<Animator>();

		Vector3 velocity = new Vector3(0, 0, moveSpeed * moveFactor);
		velocity = transform.TransformDirection(velocity);

		transform.localPosition += velocity * Time.fixedDeltaTime;
		transform.Rotate(0, turnFactor * turnSpeed * Time.fixedDeltaTime, 0);

		animator.SetFloat("Speed", moveFactor);
		animator.SetFloat("Direction", turnFactor);
	}
}
