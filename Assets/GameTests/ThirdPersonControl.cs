using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonControl : MonoBehaviour {

	//				Public Parameters
	public float MoveSpeed = 5;
	public float TurnSpeed = 360;
	public float StaticTurnSpeed = 720;
	public float TurnSpeed180 = 720;

	public bool PreventFalling = false;
	public float MaxFallHeight = 3;

	public float TargetSearchRadius = 3;


	//				Private Parameters

	// input parameters
	Vector2 inputDir = Vector2.zero;
	bool inputMove = false;
	bool inputPunch = false;
	bool inputAnimateLocomotion = true;

	// last turn values cache
	//Vector3 lastPosition = Vector3.zero;
	//float lastRotation = 0;

	//Vector3 lastVelocity = Vector3.zero;
	//float lastSpeed = 0;
	//float lastTurn = 0;

	// cached component refs
	Animator animator = null;
	UnitHealth unitHealth = null;

	// animator parameers
	Smooth anSmoothDir = new Smooth();

	AnimatorStateInfo? currentState = null;

	Transform trackingTarget = null;
	float distToTarget = Mathf.Infinity;

	// number of times attack button was pressed
	int hitQuery = 0;
	int HitQueryMax = 3;

	// when processing attacking animation
	bool activeHit = false;
	// when processing roll animation
	bool activeRoll = false;
	// when processing locomotion animation
	bool activeLocomotion = false;

	List<Collision> touches = new List<Collision>();
	float distToGround = 0.0f;
	bool isAir = false;

	public bool IsDead() {
		return unitHealth && unitHealth.IsDead();
	}

	void OnPunchActivate(int flag) {
		activeHit = flag > 0;
		Debug.Log("Punch " + flag);
	}

	public void OnAnimatorStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (animator != this.animator) return;

		currentState = stateInfo;

		if(stateInfo.shortNameHash == Animator.StringToHash("Punching")) {
			Debug.Log("Animator:" + hitQuery);
			if (hitQuery > 0) hitQuery--;
			//activeHit = true;
		}
		if(stateInfo.shortNameHash == Animator.StringToHash("Locomotion")) {
			activeLocomotion = true;
		}
	}

	public void OnAnimatorStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (animator != this.animator) return;

		currentState = null;

		if (stateInfo.shortNameHash == Animator.StringToHash("Punching")) {
			activeHit = false;
			//if(hitQuery > 0) hitQuery--;
		}
		if (stateInfo.shortNameHash == Animator.StringToHash("Locomotion")) {
			activeLocomotion = false;
		}
	}

	void OnAttackHit( HitCollider self, HitCollider other ) {
		if (activeHit) {
			other.GetOwner().Damage(36);
		}
	}

	bool FindClosestTarget() {
		Collider[] colliders = Physics.OverlapCapsule(transform.position, transform.position + new Vector3(0, 3, 0), TargetSearchRadius );

		float minDist = Mathf.Infinity;

		trackingTarget = null;

		foreach(Collider c in colliders) {
			if (c.GetComponent<UnitHealth>() == null) continue;
			if (c.gameObject == gameObject) continue;

			float dist = (c.transform.position - transform.position).magnitude;

			if (dist < minDist) {
				minDist = dist;
				trackingTarget = c.transform;
			}
		}

		return trackingTarget != null;
	}

	void UpdateInput(/* out Vector2 dirOne,  out bool move, out bool punching */) {

		Vector2 dir = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		inputDir = (dir.magnitude > 1) ? dir.normalized : dir;

		inputMove = true;
		inputPunch = false;

		// crouching
		if (Input.GetKeyDown(KeyCode.LeftShift)) {
			inputDir.x *= 0.5f;
			inputDir.y *= 0.5f;
		}
		FindClosestTarget();

		// turn to closest enemy if not moving
		if(trackingTarget != null) {
			Vector2 dirToTarget = GetDirToPoint(trackingTarget.position);

			if(Mathf.Abs( GetSignedAngleTo(dirToTarget )) < 180 ) {
				if(inputDir.magnitude < 0.1) {
					inputMove = false;
					inputDir = dirToTarget;
				}
			}
		}

		// punching
		if (Input.GetKeyDown(KeyCode.Z)) {
			if (hitQuery < HitQueryMax) hitQuery++;
			//inputPunch = true;
			//inputMove = false;
		}
		if(hitQuery > 0) {
			inputPunch = true;
			inputMove = false;
			inputDir = Vector2.zero;
		}
		if(activeHit) {
			inputMove = false;
			inputDir = Vector2.zero;
		}
		Debug.Log(hitQuery);
	}

	// Start is called before the first frame update
	void Start() {
		animator = GetComponent<Animator>();
		unitHealth = GetComponent<UnitHealth>();

		anSmoothDir.current = 0;

		//lastPosition = transform.position;
		//lastRotation = GetTurn();

		if(unitHealth) {
			unitHealth.OnInflictHit += OnAttackHit;
		}

		StateMachineControl.onStateEnter += OnAnimatorStateEnter;
		StateMachineControl.onStateExit += OnAnimatorStateExit;
	}

	private void OnDestroy() {
		if (unitHealth) {
			unitHealth.OnInflictHit -= OnAttackHit;
		}

		StateMachineControl.onStateEnter -= OnAnimatorStateEnter;
		StateMachineControl.onStateExit -= OnAnimatorStateExit;
	}

	Vector2 GetDirToPoint(Vector3 point) {
		Vector3 dir = (point - transform.position);
		Vector2 dir2 = new Vector2(dir.x, dir.z).normalized;
		dir2.y = -dir2.y;

		return dir2;
	}

	float GetDistToPoint2D( Vector3 point ) {
		Vector3 dir = (point - transform.position);
		Vector2 dir2 = new Vector2(dir.x, dir.z);
		return dir2.magnitude;
	}

	float GetTurn() {
		float turn = transform.rotation.eulerAngles.y % 360;
		if (turn < 0) turn = 360 + turn;
		return turn;
	}

	Vector2 GetDir() {
		Vector3 vec = transform.forward;
		return new Vector2(vec.x, vec.z);
	}

	float GetSignedAngleTo( Vector2 dir ) {
		return Helpers.GetSignedAngleDiff(GetTurn(), Helpers.GetAngle360(dir));
	}

	private void OnCollisionStay(Collision collision) {
		
	}

	private void OnCollisionEnter(Collision collision) {
		touches.Add(collision);
		
	}

	private void OnCollisionExit(Collision collision) {
		touches.Remove(collision);
	}

	void CheckAir() {
		Ray ray = new Ray(transform.position,Vector3.down);
		RaycastHit hit;
		Physics.Raycast(ray, out hit);

		isAir = false;
		distToGround = 0;

		//Collider collider = GetComponent<Collider>();

		if (hit.distance > 0.3 && touches.Count == 0) {
			distToGround = hit.distance;
			isAir = true;
		}

		//Debug.Log(isAir + " " + touches.Count);
	}

	void UpdateAnimatorParameters() {
		float speedFactor = inputDir.magnitude;
		bool isMoving = speedFactor > 0.1 && inputMove;
		float angleDiff = GetSignedAngleTo(inputDir);

		if (Mathf.Abs(angleDiff) > 2 && isMoving) {
			anSmoothDir.smoothTime = 0.5f;
			anSmoothDir.target = Mathf.Sign(angleDiff);
		}
		else {
			anSmoothDir.smoothTime = 0.2f;
			anSmoothDir.target = 0;
		}

		anSmoothDir.Eval(Time.deltaTime);
		speedFactor = inputMove ? speedFactor : 0;

		animator.SetFloat("Speed", inputAnimateLocomotion?speedFactor:0);
		animator.SetFloat("Direction", inputAnimateLocomotion?anSmoothDir.current:0);
		animator.SetBool("Moving", inputAnimateLocomotion?isMoving:false);
		animator.SetBool("Turning", false);
		animator.SetBool("Punching", inputPunch);
	}
	
	void UpdateLastPosition() {

	}

	void ApplyMotion( float deltaTime ) {
		if (inputDir == Vector2.zero) return;

		Vector2 dirNorm = inputDir.normalized;
		float speedFactor = inputDir.magnitude;
		float angleDiff = GetSignedAngleTo(dirNorm);

		bool isMoving = speedFactor > 0.05 && inputMove;

		float turnAmount = (isMoving ? TurnSpeed : StaticTurnSpeed) * Mathf.Sign(angleDiff) * deltaTime;

		if (Mathf.Abs(turnAmount) > Mathf.Abs(angleDiff)) {
			turnAmount = angleDiff;
		}

		bool isTurning = Mathf.Abs(turnAmount) > 0.1;

		float speed = MoveSpeed * speedFactor;

		Vector3 velocity = transform.TransformDirection(new Vector3(0, 0, speed));
		Vector3 moveAmount = velocity * deltaTime;

		Debug.Log(velocity);

		if (speedFactor > 0.01) {
			if (inputMove) {
				transform.localPosition += moveAmount;
			}
			transform.Rotate(0, turnAmount, 0);
		}
	}

	private void Update() {
		if (IsDead()) {
			return;
		}

		CheckAir();

		if (isAir) return;

		UpdateInput();
		UpdateAnimatorParameters();
	}

	void FixedUpdate() {
		if(IsDead()) {
			return;
		}

		ApplyMotion( Time.fixedDeltaTime );

		////(Vector2 dirOne, bool move) = GetInput();
		//Vector2 dirOne = Vector2.zero;
		//bool move = false;
		//bool punching = false;

		//GetInput(out dirOne, out move, out punching);

		//Vector2 dirNorm = dirOne.normalized;

		//float deltaTime = Time.fixedDeltaTime;

		//float angleDiff = GetSignedAngleTo(dirNorm);

		//bool isMoving = dirOne.magnitude > 0.05 && move;
		//bool isTurning = Mathf.Abs(lastTurn) > 0.1;

		//float turnAmount = (isMoving?TurnSpeed:StaticTurnSpeed) * Mathf.Sign(angleDiff) * deltaTime;

		//if (Mathf.Abs(turnAmount) > Mathf.Abs(angleDiff)) {
		//	turnAmount = angleDiff;
		//}

		//lastTurn = turnAmount;

		//float speedFactor = dirOne.magnitude;
		//float speed = MoveSpeed * speedFactor;

		//Vector3 velocity = transform.TransformDirection(new Vector3(0, 0, speed));
		//Vector3 moveAmount = velocity * deltaTime;

		//if (speedFactor > 0.01) {
		//	if (move) {
		//		transform.localPosition += moveAmount;
		//	}
		//	transform.Rotate(0, turnAmount, 0);
		//}

		////Debug.Log(isMoving + " " + isTurning);

		//lastPosition = transform.position;

		//// update and set animator variables

		//if (Mathf.Abs(turnAmount) > 2 && isMoving) {
		//	anSmoothDir.smoothTime = 0.5f;
		//	anSmoothDir.target = Mathf.Sign(angleDiff);
		//}
		//else {
		//	anSmoothDir.smoothTime = 0.2f;
		//	anSmoothDir.target = 0;
		//}

		//anSmoothDir.Eval(deltaTime);

		//if (move == false) speedFactor = 0;

		//animator.SetFloat("Speed", speedFactor);
		//animator.SetFloat("Direction", anSmoothDir );
		//animator.SetBool("Moving", isMoving);
		//animator.SetBool("Turning", isTurning);
		//animator.SetBool("Punching", punching);

	}

	private void OnAnimatorMove() {
		//transform.rotation = animator.deltaRotation * transform.rotation;
		//transform.position += animator.deltaPosition;
		animator.ApplyBuiltinRootMotion();
	}
}
