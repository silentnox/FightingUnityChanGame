using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RobotController : MonoBehaviour {

	public float WalkSpeed;
	public float RunSpeed;

	NavMeshAgent agent = null;
	Animator animator = null;

	bool shouldRun = false;

	//Smooth currentSpeed = new Smooth();

    // Start is called before the first frame update
    void Start() {
		agent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();
    }

	bool IsMoving() {
		return agent.velocity.magnitude > 0.1;
	}

	void UpdateAnimator() {
		float currentSpeed = agent.velocity.magnitude;
		bool moving = agent.remainingDistance > 0.2;
		animator.SetFloat("Speed", Mathf.Max(0,(currentSpeed-WalkSpeed))/(RunSpeed-WalkSpeed));
		animator.SetBool("Moving", moving);
	}

    // Update is called once per frame
    void Update() {
		float speed = shouldRun ? RunSpeed : WalkSpeed;
		//if (IsMoving()) {
		//	currentSpeed.target = 0;
		//}
		//else {
		//	currentSpeed.target = speed;
		//}
		//currentSpeed.smoothTime = 0.3f;
		//agent.speed = currentSpeed;
		agent.speed = speed;
		UpdateAnimator();
    }
}
