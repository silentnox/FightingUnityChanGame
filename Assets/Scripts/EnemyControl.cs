using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

	public float AlertTime = 8;
	public float ChaseTime = 4;

	public float MinFiringRange = 1.5f;
	public float MaxFiringRange = 8;
	public float OptimalFiringRange = 5;

	public float AimMaxAngleH = 90;
	public float AimMinAngleV = -180;
	public float AimMaxAngleV = 180;

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

	public bool shouldWalk = false;
	public bool	shouldRun = false;
	public bool	shouldFire = false;
	public bool shouldAim = false;
	public bool shouldWalkBack = false;

	Vector3 aimTarget;

	bool isMoving = false;
	bool isRotating = false;

	NavMeshAgent agent;
	Animator animator;
	UnitHealth unitHealth;

	Vector3 lookTarget;

	Smooth lookSmooth = new Smooth();

	bool aimRotate = false;

	bool moveIntoRange = false;

	float rotateAngleDelta;
	Vector3 rotateDir;

	float turnFactor = 1.0f;

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
		if (Weapon) {
			Weapon.HitTrigger();
			if(Weapon.GetSalvoCounter() == 0) {
				animator.SetInteger("Firing", 1);
			}
		}
	}

	private void OnCollisionEnter(Collision collision) {
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
		Vector3 dir = (point - transform.position);
		dir.y = 0;
		dir = dir.normalized;
		return Vector3.SignedAngle(transform.forward, dir, Vector3.up);
	}

	public void Alert( UnitHealth target = null ) {
		alertTimer = AlertTime;
		targetEnemy = target;
	}

	void AlertAlliesInRadius( float radius ) {
		if (radius <= 0.0f) return;

		Collider[] colliders = Physics.OverlapCapsule(transform.position, transform.position + new Vector3(0, 5, 0), radius);

		foreach (Collider c in colliders) {
			EnemyControl ally = c.GetComponent<EnemyControl>();
			if (ally == null) continue;
			if (c.gameObject == gameObject) continue;

			ally.Alert( targetEnemy );
		}
	}

	bool ScanTargets() {
		Collider[] colliders = Physics.OverlapCapsule(transform.position, transform.position + new Vector3(0, 5, 0), VisionRange);

		float minDist = Mathf.Infinity;

		targetEnemy = null;

		foreach (Collider c in colliders) {
			UnitHealth enemy = c.GetComponent<UnitHealth>();
			if (enemy == null) continue;
			if (c.gameObject == gameObject) continue;
			if (enemy.Type != UnitHealth.UnitType.Player) continue;

			if (!IsVisible(c.transform.position)) continue;

			float dist = (c.transform.position - transform.position).magnitude;

			if (dist < minDist) {
				minDist = dist;
				targetEnemy = enemy;
			}
		}

		distToEnemy = minDist;
		angleToEnemy = targetEnemy ? Mathf.Abs(Vector3.SignedAngle(transform.forward, (targetEnemy.transform.position - transform.position).normalized, Vector3.up)) : 0;

		return targetEnemy != null;
	}

	bool IsVisible(Vector3 point) {
		//return true;

		Transform head = animator.GetBoneTransform(HumanBodyBones.Head);

		Vector3 diff = point - transform.position;
		float heightDiff = Mathf.Abs(diff.y);
		diff.y = 0;
		Vector3 dir = diff.normalized;
		float angle = Mathf.Abs( Vector3.SignedAngle(dir, transform.forward,Vector3.up) );
		float dist = diff.magnitude;
		float headDist = Vector3.Distance(head.position, point);

		RaycastHit hit;
		int layerMask = ~(1 << LayerMask.NameToLayer("Units"));
		Physics.Raycast(new Ray(head.position,diff.normalized), out hit, Mathf.Infinity, layerMask);

		if(hit.distance < headDist) {
			return false;
		}

		if(heightDiff > 5) {
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
		targetPos	= transform.position;
		agent		= GetComponent<NavMeshAgent>();
		animator	= GetComponent<Animator>();
		unitHealth	= GetComponent<UnitHealth>();
		agent.updatePosition	= false;
		agent.updateRotation	= false;
		agent.updateUpAxis		= true;

		navNextPoint = transform.position;
		navRemainDist = 0;

		patrolPos = transform.position;
		patrolPoint = StartPatrolPoint;

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

		if(alertTimer > 0) {
			shouldRun = true;
		}

		if(chaseTimer < 0) {
			chaseMode = false;
		}

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
						}
					}
				}
			}

			if (Vector3.Distance(transform.position, patrolPos) < 0.5) {
				returnToPatrol = false;
			}

		}
		else {
			alertTimer = AlertTime;

			Transform enemyTransform = targetEnemy.transform;

			if (enemyTransform.GetComponent<Animator>() && enemyTransform.GetComponent<Animator>().isHuman) {
				enemyTransform = enemyTransform.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Chest);
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

		//turnFactor = alertTimer > 0 ? 1.5f : 1.0f;

		//if (Vector3.Distance(transform.position, targetPos) > 0.3) {
		//	//shouldWalk = true;
		//	targetDir = (targetPos - transform.position).SetY(0).normalized;
		//}
		//else {
		//	targetDir = Vector3.zero;
		//}
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
		agent.SamplePathPosition(NavMesh.AllAreas, 0.5f, out hit);

		navNextPoint = hit.position;
		//navRemainDist = hit.distance;
		navRemainDist = agent.remainingDistance;

		Vector3 delta = (navNextPoint - transform.position);
		navMoveDir = delta.normalized;
		navMoveDir2 = delta.SetY(0).normalized;
		navMoveDir.y = 0;

	}

	void UpdateMotion(float deltaTime) {

		float dashFactor = animator.GetFloat("DashFactor");
		transform.Translate(new Vector3(0, 0, dashFactor * 0.5f), Space.Self);

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

		if (Mathf.Abs(signedAngle) < 11) {
			transform.Rotate(0, signedAngle, 0);
			rotateAngleDelta = 0;
			rotateDir = Vector3.zero;
			shouldTurn = false;
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

		Think();
		UpdateNavigation();
		UpdateAim();
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

		if (!shouldAim) lookSmooth.target = 0f;

		lookSmooth.Eval(Time.deltaTime);

		if (!isMoving && !isRotating) {
			Transform tr = animator.GetBoneTransform(HumanBodyBones.Spine);
			Quaternion q = tr.rotation;
			tr.LookAt(lookTarget, Vector3.up);
			tr.rotation = Quaternion.Slerp(q, tr.rotation, lookSmooth);
			tr.rotation = tr.rotation * q * Quaternion.Inverse(transform.rotation);
		}
	}

	private void OnAnimatorMove() {
		if (unitHealth && unitHealth.IsDead()) {
			return;
		}

		Vector3 pos = animator.deltaPosition;
		transform.position += pos;
		transform.rotation = animator.deltaRotation * transform.rotation;

		NavMeshHit hit;
		agent.SamplePathPosition(NavMesh.AllAreas, 0, out hit);

		transform.position = transform.position.SetY(hit.position.y);

		isMoving = animator.deltaPosition.magnitude > 0.01;
		isRotating = animator.deltaRotation != new Quaternion(0,0,0,1);

		if (rotateDir != Vector3.zero) {
			float signedAngle = Vector3.SignedAngle(transform.forward.SetY(0), rotateDir, Vector3.up);

			if (Mathf.Sign(signedAngle) != Mathf.Sign(rotateAngleDelta)) {
				transform.Rotate(0, signedAngle, 0);
				animator.SetInteger("Direction", 0);
			}
			Debug.Log(signedAngle + " " + rotateAngleDelta);
		}
	}

	void OnDrawGizmos() {
//#if UNITY_EDITOR
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(targetPos, 0.1f);
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(navNextPoint, 0.1f);
		Gizmos.DrawLine(transform.position, transform.position + targetDir * 1);
		Gizmos.color = Color.blue;
		Gizmos.DrawLine(transform.position, targetPos);
//#endif
	}
}
