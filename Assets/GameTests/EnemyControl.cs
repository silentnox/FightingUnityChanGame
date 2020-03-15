using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyControl : MonoBehaviour {

	//public enum State {
	//	Idle,
	//	Turning,
	//	Walking,
	//	Running,
	//	Firing
	//}


	//public float MoveFactor = 1;
	//public float RunFactor = 1;
	//public float TurnFactor = 1;

	public float VisionRange = 8;
	public float VisionAngle = 60;
	public float AbsVisionRange = 0;

	public float AlertTime = 8;
	//public float ChaseTime = 4;

	public float MinFiringRange = 1.5f;
	public float MaxFiringRange = 8;

	public float AimMaxAngleH = 90;
	public float AimMinAngleV = -180;
	public float AimMaxAngleV = 180;

	public float OptimalFiringRange = 5;

	//public float AllyAlertRange = 0;
	//public bool ChainAlert = false;

	public bool DropWeaponOnDeath = true;

	public GunHelper Weapon = null;

	UnitHealth targetEnemy = null;
	float distToEnemy = 0.0f;

	float alertTimer = 0;

	Vector3 lastSeenEnemyPos;

	Vector3 targetPos;
	Vector3	targetDir;
	//bool	useTargetDir = false;

	Vector3 navNextPoint;
	float	navRemainDist;

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

	//public Transform lookTarget;
	Vector3 lookTarget;

	Smooth lookSmooth = new Smooth();

	bool aimRotate = false;

	bool moveIntoRange = false;

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
		if (unitHealth.Health > 0) {
			animator.SetTrigger("Hit");
		}
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

	public bool IsDead() {
		return unitHealth && unitHealth.IsDead();
	}

	public void SetDestination(Vector3 position) {
		targetPos	= position;
		targetDir	= Vector3.zero;
		//useTargetDir = false;
	}

	public void SetDestination(Vector3 position, Vector3 dir) {
		targetPos	= position;
		targetDir	= dir;
		//useTargetDir = true;
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

		return targetEnemy != null;
	}

	bool IsVisible(Vector3 point) {
		//return true;

		Transform head = animator.GetBoneTransform(HumanBodyBones.Head);

		Vector3 diff = point - transform.position;
		float heightDiff = diff.y;
		diff.y = 0;
		Vector2 dir = diff.normalized;
		float angle = Mathf.Abs( Vector3.SignedAngle(dir, transform.forward,Vector3.up) );
		float dist = diff.magnitude;
		float headDist = Vector3.Distance(head.position, point);

		RaycastHit hit;
		int layerMask = ~(1 << LayerMask.NameToLayer("Units"));
		Physics.Raycast(new Ray(head.position,diff.normalized), out hit, Mathf.Infinity, layerMask);

		if(hit.distance < headDist) {
			//if (targetEnemy != null && hit.collider.GetComponentInParent<UnitHealth>() != targetEnemy.GetComponent<UnitHealth>()) {
				return false;
			//}
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
		agent.updateUpAxis		= false;

		navNextPoint = transform.position;
		navRemainDist = 0;

		//offset = animator.GetBoneTransform(HumanBodyBones.Spine);

		animator.applyRootMotion = true;

		unitHealth.OnDeath += OnDeath;
		unitHealth.OnReceiveHit += OnReceiveHit;
    }

	private void OnDestroy() {
		unitHealth.OnDeath -= OnDeath;
		unitHealth.OnReceiveHit -= OnReceiveHit;
	}

	void Think() {
		shouldAim = false;
		shouldFire = false;
		shouldRun = false;
		shouldWalk = false;
		shouldWalkBack = false;
		//useTargetDir = false;

		targetDir = Vector3.zero;
		targetPos = transform.position;

		ScanTargets();

		if (alertTimer > 0) alertTimer -= Time.deltaTime;

		if(!Weapon) {
			return;
		}

		if(alertTimer > 0) {
			shouldRun = true;
		}

		if(!targetEnemy) {

			if(alertTimer > 0) {
				targetPos = lastSeenEnemyPos;
			}

			return;
		}

		alertTimer = AlertTime;

		//lookTarget = null;

		Transform enemyTransform = targetEnemy.transform;

		if(enemyTransform.GetComponent<Animator>() && enemyTransform.GetComponent<Animator>().isHuman) {
			enemyTransform = enemyTransform.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Chest);
		}

		if(distToEnemy < MinFiringRange) {
			shouldWalkBack = true;
		}
		else {
			if(distToEnemy > MaxFiringRange) {
				targetPos = targetEnemy.transform.position;
				moveIntoRange = true;
			}
			else {
				if(distToEnemy > OptimalFiringRange && moveIntoRange) {
					targetPos = targetEnemy.transform.position;
				}
				else {
					//lookTarget = enemyTransform;
					lookTarget = enemyTransform.position;
					shouldAim = true;
					shouldFire = true;
					moveIntoRange = false;
				}
			}
		}

		lastSeenEnemyPos = targetEnemy.transform.position;
	}

	void UpdateNavigation() {
		agent.nextPosition = transform.position;
		agent.SetDestination(targetPos);

		NavMeshPath path = new NavMeshPath();
		bool valid = agent.CalculatePath(targetPos, path);

		//Debug.Log(path.corners.Length);

		if (valid && path.corners.Length > 1) {
			Vector3 next = path.corners[1];

			float dist = (next - transform.position).magnitude;

			if (dist < agent.radius) {
				if (path.corners.Length > 2) {
					next = path.corners[2];
				}
				else {
					next = transform.position;
				}
			}

			navRemainDist = agent.remainingDistance;
			navNextPoint = next;
		}
		else {
			navRemainDist = 0;
			navNextPoint = transform.position;
		}
	}

	void UpdateMotion(float deltaTime) {

		Vector3 delta = (navNextPoint - transform.position);
		Vector3 dir = delta.normalized;
		dir.y = 0;
		float dist = navRemainDist;

		float signedAngle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

		bool shouldTurn = Mathf.Abs(signedAngle) > 5;
		bool shouldMove = dist > agent.radius && !shouldTurn;

		if (!shouldMove) {
			//if (useTargetDir) {
			if (targetDir != Vector3.zero) {
				signedAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
				shouldTurn = Mathf.Abs(signedAngle) > 5;
			}
		}

		if (Mathf.Abs(signedAngle) < 5) {
			transform.Rotate(0, signedAngle, 0);
		}

		//Debug.Log(shouldMove + " " + shouldTurn + " " + signedAngle + " " + dir + " " + GetTurn() + " " + Helpers.GetAngle360(dir));

		//Debug.Log(delta + " " + dir + " " + dist);

		int move = 0;

		if (shouldMove) {
			move = shouldRun ? 2 : 1;
		}

		if(shouldWalkBack) {
			move = -1;
		}

		//move = 1;

		animator.SetInteger("Move", move);
		animator.SetInteger("Direction", shouldTurn ? (int)Mathf.Sign(signedAngle) : 0);
	}

	void UpdateAim() {
		//if (!lookTarget) return;
		if (!shouldAim) return;
		if (isMoving || isRotating) return;

		//Vector3 dir = (lookTarget.position - transform.position).normalized;
		Vector3 dir = (lookTarget - transform.position).normalized;
		dir.y = 0;
		float signedAngle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

		if (Mathf.Abs(signedAngle) < 8) aimRotate = false;

		if(shouldAim && (Mathf.Abs(signedAngle) > AimMaxAngleH || aimRotate)) {
			targetDir = dir;
			//useTargetDir = true;
			animator.SetInteger("Firing", 0);
			aimRotate = true;
	
			return;
		}

		//Debug.Log(signedAngle);
		//useTargetDir = false;
		targetDir = Vector3.zero;

		int firing = shouldAim ? 1 : 0;

		bool animAiming = animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash("Aiming") && animator.GetAnimatorTransitionInfo(0).duration < 0.001;

		if (animAiming && shouldFire && Weapon && Weapon.GetSalvoCounter() > 0 && lookSmooth.current > 0.98) {
			firing = 2;
		}

		animator.SetInteger("Firing", firing);
	}

	void Update() {
		if(unitHealth && unitHealth.IsDead()) {
			return;
		}

		Think();
		UpdateNavigation();
		UpdateAim();
		UpdateMotion(Time.deltaTime);

		Debug.Log(alertTimer + " " + distToEnemy);

		//Debug.Log(animator.GetAnimatorTransitionInfo(0).duration);
	}

	private void LateUpdate() {
		if (unitHealth && unitHealth.IsDead()) {
			return;
		}

		lookSmooth.smoothTime = 0.5f;
		lookSmooth.target = shouldAim && !aimRotate? 1f : 0f;

		//if (!lookTarget) lookSmooth.target = 0f;
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
		//Debug.Log(pos.magnitude);
		//animator.ApplyBuiltinRootMotion();
		transform.position += pos;
		transform.rotation = animator.deltaRotation * transform.rotation;

		isMoving = animator.deltaPosition.magnitude > 0.01;
		isRotating = animator.deltaRotation != new Quaternion(0,0,0,1);

		//Debug.Log("M: R: " + isMoving + " " + isRotating);
		//Debug.Log(animator.deltaRotation);
	}

	private void OnDrawGizmos() {
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(targetPos, 0.1f);
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(navNextPoint, 0.1f);
	}
}
