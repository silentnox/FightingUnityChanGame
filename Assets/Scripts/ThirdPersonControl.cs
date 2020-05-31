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
	bool inputRoll = false;
	bool inputAnimateLocomotion = true;

	// cached component refs
	Animator animator = null;
	Collider collider = null;
	UnitHealth unitHealth = null;

	// animator parameers
	Smooth anSmoothDir = new Smooth();

	AnimatorStateInfo? currentState = null;

	Transform trackingTarget = null;
	float distToTarget = Mathf.Infinity;
	float angleToTarget = 0;

	// number of times attack button was pressed
	int hitQuery = 0;
	int hitCounter = 0;
	//int HitQueryMax = 3;

	bool attackIsHit = false;

	// when processing attacking animation
	bool activePunching = false;
	bool activeHit = false;
	// when processing roll animation
	bool activeRoll = false;
	// when processing locomotion animation
	bool activeLocomotion = false;

	List<Collision> touches = new List<Collision>();
	float distToGround = 0.0f;
	bool isAir = false;

	bool turnAndAttack = false;

	float turnSpeed = 0;

	public bool IsDead() {
		return unitHealth && unitHealth.IsDead();
	}

	void OnPunchActivate(int flag) {
		activeHit = flag > 0;
		Debug.Log("Punch " + flag);

		HitCollider[] hitColliders = GetComponentsInChildren<HitCollider>();

		foreach( HitCollider collider in hitColliders) {
			collider.enabled = activeHit;
		}
	}

	void OnInvulnerable(int flag) {
		if(unitHealth) {
			unitHealth.Invulnerable = flag > 0;
		}
	}

	public void OnAnimatorStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (animator != this.animator) return;

		currentState = stateInfo;

		//if(stateInfo.shortNameHash == Animator.StringToHash("Punching")) {
		if (stateInfo.IsTag("Punching")) {
			Debug.Log("Animator:" + hitQuery);
			if (hitQuery > 0) {
				hitQuery--;
				hitCounter++;
			}
			//if (hitQuery <= 0) hitCounter = 0;
			if (hitCounter > 2) hitCounter = 0;
			//activeHit = true;
			activePunching = true;
			attackIsHit = false;
		}
		if(stateInfo.shortNameHash == Animator.StringToHash("Locomotion")) {
			activeLocomotion = true;
		}
		if(stateInfo.IsTag("Roll")) {
			activeRoll = true;
		}
	}

	public void OnAnimatorStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (animator != this.animator) return;

		currentState = null;

		//if (stateInfo.shortNameHash == Animator.StringToHash("Punching")) {
		if (stateInfo.IsTag("Punching")) {
			activeHit = false;
			activePunching = false;
		}
		if (stateInfo.shortNameHash == Animator.StringToHash("Locomotion")) {
			activeLocomotion = false;
		}
		if (stateInfo.IsTag("Roll")) {
			activeRoll = false;
		}
	}

	void OnAttackHit( HitCollider self, HitCollider other ) {
		if (activeHit) {

			if (attackIsHit) {
				self.enabled = false;
				return;
			}

			UnitHealth owner = other.GetOwner();

			float angle = Mathf.Abs(Vector3.SignedAngle(owner.transform.forward, (transform.position - owner.transform.position).normalized, Vector3.up));

			if (angle > 90) {
				owner.Damage(100);
			}
			else {
				owner.Damage(36);
			}

			if(other.GetOwner().IsDead()) {
				Transform chest = other.GetOwner().GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Chest);
				Vector3 force = ( chest.position-transform.position );
				force.y = 0;
				force = force.normalized;
				//force.y += 1.3f;
				force.y += 0.8f;
				force = force.normalized;
				//force *= 12000;
				//chest.GetComponent<Rigidbody>().AddForce(force);
				force *= 200;
				chest.GetComponent<Rigidbody>().AddForce(force,ForceMode.Impulse);
			}

			attackIsHit = true;

			//self.enabled = false;
			//OnPunchActivate(0);
			//self.enabled = false;
		}
	}

	bool FindClosestTarget() {
		int layer = 1 << LayerMask.NameToLayer("Units");
		Collider[] colliders = Physics.OverlapCapsule(transform.position, transform.position + new Vector3(0, 3, 0), TargetSearchRadius, layer );

		float minDist = Mathf.Infinity;

		trackingTarget = null;

		Vector3 top = transform.position + new Vector3(0, collider.bounds.extents.y, 0 );
		int visLayer = 1 << LayerMask.NameToLayer("Default");

		foreach (Collider c in colliders) {
			if (c.GetComponent<UnitHealth>() == null) continue;
			if (c.gameObject == gameObject) continue;
			if (!Helpers.IsVisible(top, c.bounds.center, visLayer)) continue;

			float dist = (c.transform.position - transform.position).magnitude;

			if (dist < minDist) {
				minDist = dist;
				trackingTarget = c.transform;
			}
		}

		distToTarget = trackingTarget?minDist:Mathf.Infinity;
		angleToTarget = trackingTarget ? Mathf.Abs(Vector3.SignedAngle(transform.forward, (trackingTarget.transform.position - transform.position).normalized, Vector3.up)) : 0;

		return trackingTarget != null;
	}

	void UpdateInput() {

		Vector2 dir = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		inputDir = (dir.magnitude > 1) ? dir.normalized : dir;

		inputMove = false;
		inputPunch = false;
		inputRoll = false;

		inputMove = inputDir.magnitude > 0.01;

		turnSpeed = (inputDir.magnitude > 0.7 ? TurnSpeed : StaticTurnSpeed);

		//// crouching
		//if (Input.GetKey(KeyCode.LeftShift)) {
		//	inputDir.x *= 0.5f;
		//	inputDir.y *= 0.5f;
		//	//inputDir *= 0.5;
		//}

		bool punching = false;
		if (Input.GetKeyDown(KeyCode.Z)) {
			punching = true;
		}

		bool turning = false;

		// punching
		if (punching) {
			if (hitQuery < 3) hitQuery++;
		}

		//Debug.Log(dir + " " + inputDir + " " + dir.magnitude + " " + inputDir.magnitude + " " + hitQuery);

		if (inputDir.magnitude > 0.5 && (hitQuery > 1 || (hitQuery > 0 && activePunching))) hitQuery = 0;

		// turn to closest enemy if not moving
		if (trackingTarget != null) {
			Vector2 dirToTarget = GetDirToPoint(trackingTarget.position);
			float angle = GetSignedAngleTo(dirToTarget);
			float absAngle = Mathf.Abs(angle);

			if(punching && absAngle > 3 && distToTarget < 3) {
				turnAndAttack = true;
			}

			if (absAngle < 3 || distToTarget > 3) turnAndAttack = false;

			if ((absAngle < 180 && !inputMove) || turnAndAttack ) {
				inputMove = false;
				inputDir = dirToTarget;
				turning = true;
			}
		}

		if (turnAndAttack) turnSpeed *= 2;

		if(hitQuery > 0 && !turnAndAttack) {
			inputPunch = true;
			inputMove = false;
			inputDir = Vector2.zero;
		}
		if(activePunching) {
			inputMove = false;
			inputDir = Vector2.zero;
			inputPunch = false;
		}

		if(Input.GetKeyDown(KeyCode.X) && !activeRoll && activeLocomotion) {
			inputRoll = true;
			//activeRoll = true;
			inputMove = false;
			inputDir = Vector2.zero;
			inputPunch = false;
		}
		if (activeRoll) {
			inputMove = false;
			inputDir = Vector2.zero;
			inputPunch = false;
		}
		//Debug.Log(hitQuery);
	}

	// Start is called before the first frame update
	void Start() {
		animator = GetComponent<Animator>();
		collider = GetComponent<Collider>();
		unitHealth = GetComponent<UnitHealth>();

		anSmoothDir.current = 0;

		if(unitHealth) {
			unitHealth.OnInflictHit += OnAttackHit;
		}

		StateMachineControl.onStateEnter += OnAnimatorStateEnter;
		StateMachineControl.onStateExit += OnAnimatorStateExit;

		OnPunchActivate(0);
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

		if (isAir) return;

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
		//animator.SetBool("Punching", inputPunch);
		//if(inputPunch) animator.SetTrigger("Punching");
		if(inputPunch && hitCounter == 0) animator.SetTrigger("Punching");
		if(inputPunch && hitCounter == 1) animator.SetTrigger("Punching1");
		if(inputPunch && hitCounter == 2) animator.SetTrigger("Punching2");

		if(inputRoll) {
			animator.SetTrigger("Roll");
		}
		//else {
		//	animator.ResetTrigger("Roll");
		//}
	}
	
	void UpdateLastPosition() {

	}

	void ApplyMotion( float deltaTime ) {

		if (isAir) return;

		float dashFactor = animator.GetFloat("DashFactor");
		bool longDash = ((!trackingTarget || distToTarget < 1.3 || distToTarget > 3 || angleToTarget > 30));
		transform.Translate(new Vector3(0, 0, dashFactor * (longDash? 10f : 40f) * deltaTime ), Space.Self);

		if (trackingTarget && /*hitQuery > 0 &&*/ activePunching && distToTarget < 1.6 && distToTarget > 1 && angleToTarget < 30) {
			//transform.LookAt(trackingTarget, Vector3.up);
			transform.Translate(new Vector3(0, 0, distToTarget - 1), Space.Self);
		}

		if (inputDir != Vector2.zero) {
			Vector2 dirNorm = inputDir.normalized;
			float speedFactor = inputDir.magnitude;
			float angleDiff = GetSignedAngleTo(dirNorm);

			bool isMoving = speedFactor > 0.05 && inputMove;

			//float turnAmount = (isMoving ? TurnSpeed : StaticTurnSpeed) * Mathf.Sign(angleDiff) * deltaTime;
			float turnAmount = turnSpeed * Mathf.Sign(angleDiff) * deltaTime;

			if (Mathf.Abs(turnAmount) > Mathf.Abs(angleDiff)) {
				turnAmount = angleDiff;
			}

			bool isTurning = Mathf.Abs(turnAmount) > 0.1;

			float speed = MoveSpeed * speedFactor;

			Vector3 velocity = transform.TransformDirection(new Vector3(0, 0, speed));
			Vector3 moveAmount = velocity * deltaTime;

			//Debug.Log(velocity);

			if (speedFactor > 0.01) {
				if (inputMove) {
					transform.localPosition += moveAmount;
				}
				transform.Rotate(0, turnAmount, 0);
			}
		}
	}

	private void Update() {
		if (IsDead()) {
			return;
		}

		FindClosestTarget();

		CheckAir();

		//Debug.Log("HQ:" + hitQuery);

		UpdateInput();
		UpdateAnimatorParameters();
	}

	void FixedUpdate() {
		if(IsDead()) {
			return;
		}

		ApplyMotion( Time.fixedDeltaTime );
	}

	private void OnAnimatorMove() {
		//transform.rotation = animator.deltaRotation * transform.rotation;
		//transform.position += animator.deltaPosition;
		animator.ApplyBuiltinRootMotion();
	}
}
