using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;

public class EnemyControl : MonoBehaviour {

	public enum State {
		Idle,
		Turning,
		Moving,
		Aiming,
		Firing
	}

	// public vars
	public float VisionRange = 8;
	public float VisionAngle = 60;
	public float AbsVisionRange = 0;

	public float TurnVisionAngle = -1;

	public float AlertTime = 8;
	public float ChaseTime = 4;

	public float ReactionTime = 0;

	public float MinFiringRange = 1.5f;
	public float MaxFiringRange = 8;
	public float OptimalFiringRange = 5;

	public float AimMaxAngleH = 90;
	//public float AimMinAngleV = -180;
	//public float AimMaxAngleV = 180;

	public float AllyAlertRange = 5;
	//public bool ChainAlert = false;

	public bool DropWeaponOnDeath = true;

	public GunHelper Weapon = null;

	public PatrolPoint StartPatrolPoint = null;

	public bool DoThink = true;

	// current state
	State state = State.Idle;

	UnitHealth targetEnemy = null;
	UnitHealth lastSeenEnemy = null;
	float distToEnemy = 0.0f;
	float angleToEnemy = 0.0f;

	float alertTimer = 0;
	float chaseTimer = 0;

	float reactionTimer = 0;

	bool chaseMode = false;

	Vector3 lastSeenEnemyPos;

	float lastDamageAngle = 0;

	Vector3 targetPos;
	Vector3	targetDir;

	Vector3 navNextPoint;
	float	navRemainDist;
	Vector3 navMoveDir,navMoveDir2;

	// patroling related variables
	PatrolPoint patrolPoint = null;
	Vector3 patrolPos;
	bool returnToPatrol = false;
	float patrolTimer = 0;
	bool patrolMovingToNext = false;
	bool patrolMovingBack = false;

	bool shouldWalk = false;
	bool shouldRun = false;
	bool shouldFire = false;
	bool shouldAim = false;
	bool shouldWalkBack = false;

	Vector3 aimTarget;

	bool isMoving = false;
	bool isRotating = false;

	NavMeshAgent agent;
	Animator animator;
	UnitHealth unitHealth;
	Collider collider;
	CapsuleCollider capsule;

	Vector3 lookTarget;

	Smooth lookSmooth = new Smooth();

	bool aimRotate = false;

	bool moveIntoRange = false;

	float rotateAngleDelta;
	Vector3 rotateDir;

	float turnFactor = 1.0f;

	Vector3 lastPosOnNavMesh;

	Quaternion torsoQuat;

	bool isVisibleToCamera = false;

	int robotId = 0;
	static int numRobots = 0;

	void OnDeath() {
		if(Weapon && DropWeaponOnDeath) {
			Weapon.transform.parent = null;
			//Weapon.GetComponent<Collider>().enabled = true;
			Weapon.GetComponent<Rigidbody>().isKinematic = false;
			Weapon.GetComponent<Rigidbody>().useGravity = true;
			Weapon.GetComponent<Rigidbody>().detectCollisions = true;
		}

		HitCollider[] colliders = GetComponentsInChildren<HitCollider>();

		foreach(HitCollider c in colliders) {
			c.enabled = false;
		}
	}

	void OnReceiveHit(HitCollider self, HitCollider other) {
		UnitHealth attacker = other.GetOwner();
		float angle = Mathf.Abs(Vector3.SignedAngle(transform.forward, (attacker.transform.position - transform.position).normalized, Vector3.up));

		lastDamageAngle = angle;

		if(angle < 90) {
			transform.LookAt(attacker.transform, Vector3.up);
		}

		if (unitHealth.Health > 0) {
			animator.SetTrigger("Hit");
		}
		alertTimer = AlertTime;

		AlertAlliesInRadius(AllyAlertRange);
	}

	void OnDamage(float amount) {
	}

	void OnGunFire() {
		if (Weapon && !animator.IsInTransition(0)) {
			Weapon.HitTrigger();
			if(Weapon.GetSalvoCounter() == 0) {
				animator.SetInteger("Firing", 1);
			}
		}
	}

	private void OnBecameVisible() {
		isVisibleToCamera = true;
	}

	private void OnBecameInvisible() {
		isVisibleToCamera = false;
	}

	private void OnCollisionEnter(Collision collision) {
		Rigidbody rb = GetComponent<Rigidbody>();
		rb.velocity = Vector3.ProjectOnPlane(rb.velocity, collision.contacts[0].normal);
		if (collision.rigidbody) {
			collision.rigidbody.velocity = Vector3.ProjectOnPlane(collision.rigidbody.velocity, -collision.contacts[0].normal);
		}
		//rb.AddForce(-collision.impulse * collision.contactCount, ForceMode.Impulse);
		Debug.Log(collision.impulse + " " + collision.relativeVelocity);
		//GetComponent<Rigidbody>().isKinematic = false;
		//if (collision.gameObject.GetComponent<UnitHealth>()) {
		//	GetComponent<Rigidbody>().isKinematic = true;
		//}
		//foreach(ContactPoint cp in collision.contacts) {
		//	//collision.
		//}
	}

	public bool IsDead() {
		return unitHealth && unitHealth.IsDead();
	}

	public float GetAlertTimer() {
		return alertTimer;
	}

	public void SetDestination(Vector3 position) {
		targetPos	= position;
		targetDir	= Vector3.zero;
	}

	public void SetDestination(Vector3 position, Vector3 dir) {
		targetPos	= position;
		targetDir	= dir;
	}

	float GetAngleTo( Vector3 point ) {
		Vector3 dir = (point - transform.position).SetY(0).normalized;
		return Vector3.SignedAngle(transform.forward, dir, Vector3.up);
	}

	bool IsNearPosition( Vector3 position ) {
		Vector3 diff = position - transform.position;

		if (Mathf.Abs(diff.y) > capsule.height * 1.2f) return false;

		return diff.SetY(0).magnitude < capsule.radius * 0.5f;
	}

	public void Alert( UnitHealth target = null ) {
		alertTimer = AlertTime;
		if (target) {
			targetEnemy = target;
			lastSeenEnemy = target;
			lastSeenEnemyPos = target.transform.position;
		}
	}

	void AlertAlliesInRadius( float radius, bool onlyIfVisible = true ) {
		if (radius <= 0.0f) return;

		Collider[] colliders = Physics.OverlapCapsule(transform.position, transform.position + new Vector3(0, 5, 0), radius);

		foreach (Collider c in colliders) {
			EnemyControl ally = c.GetComponent<EnemyControl>();
			if (ally == null) continue;
			if (c.gameObject == gameObject) continue;

			if(onlyIfVisible) {
				if (!IsVisible(c.bounds.center)) continue;
			}
			
			ally.Alert( targetEnemy );
		}
	}

	bool ScanTargets() {

		Profiler.BeginSample("ScanTargets");

		int layer = 1 << LayerMask.NameToLayer("Units");
		Collider[] colliders = Physics.OverlapCapsule(transform.position - new Vector3(0, 8, 0), transform.position + new Vector3(0, 8, 0), VisionRange, layer);

		float minDist = Mathf.Infinity;

		targetEnemy = null;

		foreach (Collider c in colliders) {
			UnitHealth enemy = c.GetComponent<UnitHealth>();
			if (enemy == null) continue;
			if (c.gameObject == gameObject) continue;
			if (enemy.Type != UnitHealth.UnitType.Player) continue;

			Collider col = enemy.GetComponent<Collider>();

			Vector3 pos = col ? col.bounds.center : transform.position;

			if (!IsVisible(pos)) continue;

			float dist = (c.transform.position.SetY(0) - transform.position.SetY(0)).magnitude;

			if (dist < minDist) {
				minDist = dist;
				targetEnemy = enemy;
			}
		}

		distToEnemy = minDist;
		angleToEnemy = targetEnemy ? Mathf.Abs(Vector3.SignedAngle(transform.forward, (targetEnemy.transform.position - transform.position).normalized, Vector3.up)) : 0;

		Profiler.EndSample();

		return targetEnemy != null;
	}

	bool IsVisible(Vector3 point) {
		//return true;

		//Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
		//Vector3 headPos = head.position;

		Vector3 headPos = transform.position + new Vector3(0, capsule.height, 0);

		Vector3 diff = point - headPos;
		Vector3 dir = diff.normalized;
		float angle = Mathf.Abs( Vector3.SignedAngle(dir.SetY(0).normalized, transform.forward, Vector3.up) );
		float dist = diff.SetY(0).magnitude;
		float headDist = diff.magnitude;

		double visAngle = !isRotating ? VisionAngle : TurnVisionAngle;

		RaycastHit hit;
		//int layerMask = ~(1 << LayerMask.NameToLayer("Units") | 1 << LayerMask.NameToLayer("Ragdoll"));
		int layerMask = 1 << LayerMask.NameToLayer("Default");
		Physics.Raycast(new Ray(headPos,dir), out hit, Mathf.Infinity, layerMask);

		if(hit.distance < headDist) {
			return false;
		}

		if(Mathf.Abs(diff.y) > 5) {
			return false;
		}

		if(dist < AbsVisionRange) {
			return true;
		}

		if (alertTimer > 0) {
			if(dist > VisionRange) {
				return false;
			}
		}
		else {
			if(dist > VisionRange || angle > VisionAngle) {
				return false;
			}
		}

		return true;
	}

	void StopMoving() {
		navNextPoint = transform.position;
		navRemainDist = 0;

		targetPos = transform.position;
	}

	void Start() {
		agent		= GetComponent<NavMeshAgent>();
		animator	= GetComponent<Animator>();
		unitHealth	= GetComponent<UnitHealth>();
		collider	= GetComponent<Collider>();
		capsule		= GetComponent<CapsuleCollider>();

		if(!agent) {
			agent = gameObject.AddComponent<NavMeshAgent>();
			agent.radius = 0.5f;
		}

		agent.updatePosition = false;
		agent.updateRotation = false;
		agent.updateUpAxis = true;

		navNextPoint = transform.position;
		navRemainDist = 0;

		patrolPos = transform.position;
		patrolPoint = StartPatrolPoint;

		if(patrolPoint) {
			patrolMovingToNext = true;
		}

		lastPosOnNavMesh = transform.position;

		targetPos = transform.position;

		animator.applyRootMotion = true;

		unitHealth.OnDeath += OnDeath;
		unitHealth.OnReceiveHit += OnReceiveHit;
		unitHealth.OnDamage += OnDamage;

		robotId = EnemyControl.numRobots;
		EnemyControl.numRobots++;
    }

	private void OnDestroy() {
		unitHealth.OnDeath -= OnDeath;
		unitHealth.OnReceiveHit -= OnReceiveHit;
		unitHealth.OnDamage -= OnDamage;
	}

	void ThinkPatroling() {

	}

	void Think() {
		if (!DoThink) return;

		shouldAim = false;
		shouldFire = false;
		shouldRun = false;
		shouldWalk = false;
		shouldWalkBack = false;

		targetDir = Vector3.zero;
		targetPos = transform.position;

		ScanTargets();

		if (targetEnemy) {
			lastSeenEnemy = targetEnemy;
		}

		bool alertExpired = false;

		if (alertTimer > 0) {
			alertTimer -= Time.deltaTime;

			if(alertTimer < 0) {
				alertExpired = true;
			}
		}
		if (patrolTimer > 0) {
			patrolTimer -= Time.deltaTime;
		}
		if(chaseTimer > 0) {
			chaseTimer -= Time.deltaTime;
		}
		if(reactionTimer > 0) {
			reactionTimer -= Time.deltaTime;
		}

		if(alertTimer > 0) {
			shouldRun = true;
		}

		if(chaseTimer < 0) {
			chaseMode = false;
		}

		//if (targetEnemy) {
		//	if (reactionTimer > 0) {
		//		//if (!isRotating) {

		//		//}
		//		//else {
		//		//	targetEnemy = null;
		//		//}
		//		targetEnemy = null;
		//	}
		//	else {
		//		if (alertTimer < 0) {
		//			reactionTimer = ReactionTime;
		//		}
		//	}
		//}

		if (!targetEnemy) {

			if (alertTimer > 0) {
				shouldWalk = true;

				targetPos = lastSeenEnemyPos;

				if (Vector3.Distance(transform.position.SetY(0), lastSeenEnemyPos.SetY(0)) < 1 && !chaseMode) {
					chaseMode = true;
					chaseTimer = ChaseTime;
				}

				if (chaseMode) {
					targetPos = lastSeenEnemy.transform.position;
				}
			}
			else {
				shouldWalk = true;

				if (alertExpired) {
					returnToPatrol = true;
				}

				if (returnToPatrol) {
					targetPos = patrolPos;
				}
				else {
					patrolPos = transform.position;

					if (patrolPoint) {
						if (Vector3.Distance(transform.position, patrolPoint.transform.position) < 0.2 && patrolMovingToNext) {
							patrolTimer = patrolPoint.WaitTime;
							patrolMovingToNext = false;
						}
						else {
							patrolTimer -= Time.deltaTime;
						}

						if (patrolTimer < 0 && !patrolMovingToNext) {
							PatrolPoint nextPoint = null;

							if (!patrolMovingBack) {
								nextPoint = patrolPoint.GetNextPatrolPoint();
								if (!nextPoint) {
									nextPoint = patrolPoint.GetPrevPatrolPoint();
									patrolMovingBack = !patrolMovingBack;
								}
							}
							else {
								nextPoint = patrolPoint.GetPrevPatrolPoint();
								if (!nextPoint) {
									nextPoint = patrolPoint.GetNextPatrolPoint();
									patrolMovingBack = !patrolMovingBack;
								}
							}

							patrolPoint = nextPoint;

							patrolMovingToNext = true;
						}

						if (patrolPoint) {
							targetPos = patrolPoint.transform.position;

							if (patrolPoint.UseDirection) {
								targetDir = patrolPoint.transform.forward;
							}

							if (patrolPoint.UseRunning) shouldRun = true;
						}
					}
				}
			}

			if (Vector3.Distance(transform.position, patrolPos) < 0.5) {
				returnToPatrol = false;
			}

		}
		else {

			if(alertTimer <= 0) {
				AlertAlliesInRadius(AllyAlertRange);
			}

			alertTimer = AlertTime;

			Transform enemyTransform = targetEnemy.transform;

			Animator enemyAnimator = enemyTransform.GetComponent<Animator>();
			if (enemyAnimator && enemyAnimator.isHuman) {
				enemyTransform = enemyAnimator.GetBoneTransform(HumanBodyBones.Chest);
			}

			if (distToEnemy < MinFiringRange) {
				if (angleToEnemy < 20) {
					//shouldWalk = true;
					shouldWalkBack = true;
				}
				else {
					targetDir = transform.position.DirTo(targetEnemy.transform.position);
					turnFactor = 1.5f;
				}
			}
			else {
				if (distToEnemy > MaxFiringRange) {
					targetPos = targetEnemy.transform.position;
					moveIntoRange = true;
				}
				else {
					if (distToEnemy > OptimalFiringRange && moveIntoRange) {
						targetPos = targetEnemy.transform.position;
					}
					else {
						lookTarget = enemyTransform.position;
						shouldAim = true;
						shouldFire = true;
						moveIntoRange = false;
						targetPos = targetEnemy.transform.position;
					}
				}

				if (moveIntoRange) shouldWalk = true;
			}

			lastSeenEnemyPos = targetEnemy.transform.position;
		}

		if (!shouldWalk) shouldRun = false;

		turnFactor = alertTimer > 0 ? 1.5f : 1.0f;
	}

	void UpdateNavigation() {
		agent.nextPosition = transform.position;
		agent.SetDestination(targetPos);

		//NavMeshPath path = new NavMeshPath();
		//bool valid = agent.CalculatePath(targetPos, path);

		////Debug.Log(path.corners.Length);

		//if (valid && path.corners.Length > 1) {
		//	Vector3 next = path.corners[1];

		//	float dist = (next - transform.position).magnitude;

		//	if (dist < agent.radius) {
		//		if (path.corners.Length > 2) {
		//			next = path.corners[2];
		//		}
		//		else {
		//			next = transform.position;
		//		}
		//	}

		//	navRemainDist = agent.remainingDistance;
		//	navNextPoint = next;
		//}
		//else {
		//	navRemainDist = 0;
		//	navNextPoint = transform.position;
		//}

		//Vector3 delta = (navNextPoint - transform.position);
		//navMoveDir = delta.normalized;
		//navMoveDir2 = delta.SetY(0).normalized;
		//navMoveDir.y = 0;

		NavMeshHit hit;
		agent.SamplePathPosition(NavMesh.AllAreas, 0.3f, out hit);

		navNextPoint = hit.position;
		//navRemainDist = hit.distance;
		navRemainDist = agent.remainingDistance;

		if(Vector3.Distance(transform.position,targetPos) < 1e-3f) {
			navNextPoint = transform.position;
			navRemainDist = 0;
		}

		Vector3 delta = (navNextPoint - transform.position);
		navMoveDir = delta.normalized;
		navMoveDir2 = delta.SetY(0).normalized;
		navMoveDir.y = 0;

		//if(agent.isOnNavMesh) {
		//	lastPosOnNavMesh = transform.position;
		//}
	}

	void UpdateMotion(float deltaTime) {

		float dashFactor = animator.GetFloat("DashFactor");
		transform.Translate(new Vector3(0, 0, dashFactor * deltaTime * 30f), Space.Self);

		float signedAngle = Vector3.SignedAngle(transform.forward.SetY(0), navMoveDir2, Vector3.up);
		float absAngle = Mathf.Abs(signedAngle);

		bool shouldTurn = Mathf.Abs(signedAngle) > 5;
		//bool reachedDest = navRemainDist < agent.radius;
		bool reachedDest = navRemainDist < 0.1;
		bool shouldMove = !reachedDest && !shouldTurn && this.shouldWalk;

		rotateDir = navMoveDir2;

		if (reachedDest) {
			if (targetDir != Vector3.zero) {
				signedAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
				shouldTurn = Mathf.Abs(signedAngle) > 5;
				rotateDir = targetDir;
			}
		}

		rotateAngleDelta = signedAngle;

		//if (Mathf.Abs(signedAngle) < 5) {
		if (!shouldTurn && shouldMove) {
			transform.Rotate(0, signedAngle, 0);
			//rotateAngleDelta = 0;
			//rotateDir = Vector3.zero;
			//shouldTurn = false;
		}

		//Debug.Log(Time.frameCount + " " + signedAngle + " " + targetDir);

		int move = 0;

		if (shouldMove) {
			move = shouldRun ? 2 : 1;
		}

		if (shouldWalkBack) {
			move = -1;
		}

		animator.SetInteger("Move", move);
		animator.SetInteger("Direction", shouldTurn ? (int)Mathf.Sign(signedAngle) : 0);

		animator.SetFloat("TurnFactor", turnFactor);

		if (shouldTurn) {
			state = State.Turning;
		}
		else {
			if (shouldMove || shouldWalkBack) {
				state = State.Moving;
			}
			else {
				state = State.Idle;
			}
		}
	}

	void UpdateAim() {
		Vector3 dir = (lookTarget - transform.position).SetY(0).normalized;
		float signedAngle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

		if (Mathf.Abs(signedAngle) < 8) aimRotate = false;

		if(shouldAim && (Mathf.Abs(signedAngle) > AimMaxAngleH || aimRotate)) {
			targetPos = transform.position;
			targetDir = dir;
			animator.SetInteger("Firing", 0);
			aimRotate = true;
	
			return;
		}

		int firing = 0;

		if (shouldAim) firing = 1;
		if (shouldFire && Weapon && Weapon.GetSalvoCounter() > 0) firing = 2;

		animator.SetInteger("Firing", firing);

		if (firing == 1) state = State.Aiming;
		else if (firing == 2) state = State.Firing;
	}

	void Update() {
		if(unitHealth && unitHealth.IsDead()) {
			return;
		}

		if (TurnVisionAngle < 0) {
			TurnVisionAngle = VisionAngle;
		}

		Think();
		UpdateAim();
		UpdateNavigation();
		UpdateMotion(Time.deltaTime);

		//Debug.Log(targetDir);

		//Debug.Log(alertTimer + " " + distToEnemy);

		//Debug.Log(animator.GetAnimatorTransitionInfo(0).duration);
	}

	private void FixedUpdate() {
		//UpdateMotion(Time.fixedDeltaTime);
	}

	private void LateUpdate() {
		if (unitHealth && unitHealth.IsDead()) {
			return;
		}

		lookSmooth.smoothTime = 0.2f;
		lookSmooth.target = shouldAim && !aimRotate? 1f : 0f;

		lookSmooth.Eval(Time.deltaTime);

		if (!isMoving && !isRotating) {
			Transform tr = animator.GetBoneTransform(HumanBodyBones.Spine);
			//Quaternion q = Quaternion.LookRotation(tr.position.DirTo(lookTarget), Vector3.up);
			Quaternion q = Quaternion.FromToRotation(transform.forward, tr.position.DirTo(lookTarget));
			//q = q * Quaternion.Inverse(tr.rotation);
			//Quaternion q2 = Quaternion.Slerp(Quaternion.identity, q * Quaternion.Inverse(transform.rotation), lookSmooth);
			//tr.rotation = tr.rotation * q2/* * Quaternion.Inverse(transform.rotation)*/;
			tr.rotation = Quaternion.Slerp(tr.rotation, q * tr.rotation/* * Quaternion.Inverse(transform.rotation)*/, lookSmooth);
		}

		NavMeshHit hit;
		NavMesh.SamplePosition(transform.position, out hit, agent.radius, NavMesh.AllAreas);

		if(Vector3.Distance(transform.position,hit.position) < 0.001) {
			lastPosOnNavMesh = transform.position;
		}
		else {
			if (hit.hit) {
				transform.position = hit.position;
			}
		}

		//NavMesh.FindClosestEdge(transform.position, out hit, NavMesh.AllAreas);

		//if (hit.distance < capsule.radius) {
		//	transform.position -= (hit.position - transform.position) * (capsule.radius - hit.distance) / capsule.radius;
		//}
	}

	private void OnAnimatorMove() {
		if (unitHealth && unitHealth.IsDead()) {
			return;
		}

		torsoQuat = animator.GetBoneTransform(HumanBodyBones.Spine).rotation;

		int stateName = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
		bool animTurn = stateName == Animator.StringToHash("LeftTurn") || stateName == Animator.StringToHash("RightTurn");
		bool animMove = stateName == Animator.StringToHash("Walking") || stateName == Animator.StringToHash("Running") || stateName == Animator.StringToHash("WalkingBack");

		Vector3 pos = animator.deltaPosition;

		if(rotateDir != Vector3.zero) {
			pos = Vector3.Project(pos, rotateDir);
		}

		if (animMove) {
			transform.position += pos;
		}
		if (!animator.IsInTransition(0)) {
			transform.rotation = animator.deltaRotation * transform.rotation;
		}

		NavMeshHit hit;
		agent.SamplePathPosition(NavMesh.AllAreas, 0, out hit);

		transform.position = transform.position.SetY(hit.position.y);

		isMoving = animator.deltaPosition.magnitude > 0.01;
		isRotating = animator.deltaRotation != new Quaternion(0,0,0,1);

		if (rotateDir != Vector3.zero) {
			float signedAngle = Vector3.SignedAngle(transform.forward.SetY(0), rotateDir, Vector3.up);

			if (Mathf.Sign(signedAngle) != Mathf.Sign(rotateAngleDelta) && Mathf.Abs(signedAngle) < 90) {
				Debug.Log("Snapping");
				transform.Rotate(0, signedAngle, 0);
				animator.SetInteger("Direction", 0);
			}
			//Debug.Log(signedAngle + " " + rotateAngleDelta);
		}
	}

	void OnDrawGizmosSelected() {
		//#if UNITY_EDITOR
		if (Application.isPlaying) {
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(targetPos, 0.1f);
			Gizmos.color = Color.green;
			Gizmos.DrawSphere(navNextPoint, 0.1f);
			Gizmos.DrawLine(transform.position, transform.position + rotateDir * 1);
			Gizmos.color = Color.blue;
			Gizmos.DrawLine(transform.position, targetPos);
			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(lastPosOnNavMesh, 0.1f);
			Transform tr = animator.GetBoneTransform(HumanBodyBones.Spine);
			Quaternion q = Quaternion.identity;
			q.SetLookRotation(tr.position.DirTo(lookTarget), Vector3.up);
			Gizmos.color = Color.red;
			Gizmos.DrawLine(tr.position, tr.position + q * Vector3.forward * 2);
			//#endif
		}
	}
}
