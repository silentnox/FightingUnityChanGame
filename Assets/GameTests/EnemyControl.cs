using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyControl : MonoBehaviour {

	public enum State {
		Idle,
		Turning,
		Walking,
		Running,
		Firing
	}


	//public float MoveFactor = 1;
	//public float RunFactor = 1;
	//public float TurnFactor = 1;

	public float VisionRange = 8;
	public float VisionAngle = 60;
	public float AbsVisionRange = 0;

	public float AlertTime = 8;
	public float ChaseTime = 4;

	public float MinFiringRange = 1.5f;
	public float MaxFiringRange = 8;

	public float AimMaxAngleH = 90;
	public float AimMinAngleV = -180;
	public float AimMaxAngleV = 180;

	public float OptimalFiringRange = 5;

	public float AllyAlertRange = 0;
	public bool ChainAlert = false;

	public bool DropWeaponOnDeath = true;

	public GunHelper Weapon = null;

	UnitHealth targetEnemy = null;
	float alertTimer = 0;

	Vector3 targetPos;
	Vector3	targetDir;
	bool	useTargetDir = false;

	Vector3 navNextPoint;
	float	navRemainDist;

	public bool	shouldRun = false;
	public bool	shouldFire = false;
	public bool shouldAim = false;

	Vector3 aimTarget;

	bool isMoving = false;
	bool isRotating = false;

	NavMeshAgent agent;
	Animator animator;
	UnitHealth unitHealth;

	public Transform lookTarget;

	Smooth lookSmooth = new Smooth();

	bool aimRotate = false;

	void OnDeath() {
		if(Weapon && DropWeaponOnDeath) {
			Weapon.transform.parent = null;
			//Weapon.GetComponent<Collider>().enabled = true;
			Weapon.GetComponent<Rigidbody>().isKinematic = false;
			Weapon.GetComponent<Rigidbody>().useGravity = true;
			Weapon.GetComponent<Rigidbody>().detectCollisions = true;
		}
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
		useTargetDir = false;
	}

	public void SetDestination(Vector3 position, Vector3 dir) {
		targetPos	= position;
		targetDir	= dir;
		useTargetDir = true;
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

		return targetEnemy != null;
	}

	bool IsVisible( Vector3 point ) {
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
    }

	private void OnDestroy() {
		unitHealth.OnDeath -= OnDeath;
	}

	void Think() {
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
			if (useTargetDir) {
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

		//move = 1;

		animator.SetInteger("Move", move);
		animator.SetInteger("Direction", shouldTurn ? (int)Mathf.Sign(signedAngle) : 0);

		//if(shouldTurn) {
		//	animator.speed = TurnFactor;
		//}
		//else {
		//	if(shouldMove) {
		//		if (shouldRun) {
		//			animator.speed = RunFactor;
		//		}
		//		else {
		//			animator.speed = MoveFactor;
		//		}
		//	}
		//	else {
		//		animator.speed = 1.0f;
		//	}
		//}
	}

	void UpdateAim() {
		if (!lookTarget) return;
		if (isMoving || isRotating) return;

		Vector3 dir = (lookTarget.position - transform.position).normalized;
		dir.y = 0;
		float signedAngle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

		if (Mathf.Abs(signedAngle) < 8) aimRotate = false;

		if(shouldAim && (Mathf.Abs(signedAngle) > AimMaxAngleH || aimRotate)) {
			targetDir = dir;
			useTargetDir = true;
			animator.SetInteger("Firing", 0);
			aimRotate = true;
	
			return;
		}
		//Debug.Log(signedAngle);
		useTargetDir = false;

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

		//Think();
		UpdateNavigation();
		UpdateMotion(Time.deltaTime);
		UpdateAim();

		//Debug.Log(animator.GetAnimatorTransitionInfo(0).duration);
	}

	private void LateUpdate() {
		if (unitHealth && unitHealth.IsDead()) {
			return;
		}

		lookSmooth.smoothTime = 0.5f;
		lookSmooth.target = shouldAim && !aimRotate? 1f : 0f;
		if (!lookTarget) lookSmooth.target = 0f;
		lookSmooth.Eval(Time.deltaTime);
		if (!isMoving && !isRotating /*&& shouldAim*/) {
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
