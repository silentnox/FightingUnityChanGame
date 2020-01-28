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


	public float MoveFactor = 1;
	public float RunFactor = 1;
	public float TurnFactor = 1;

	public float VisionRange = 8;
	public float VisionAngle = 60;
	public float AbsVisionRange = 0;

	public float AlertTime = 8;
	public float ChaseTime = 4;

	public float MinFiringRange = 1.5f;
	public float MaxFiringRange = 8;

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

	bool	shouldRun = false;
	//bool	shouldMove = false;
	//bool	shouldTurn = false;
	bool	shouldFire = false;
	//float	signedAngle = 0;

	Vector3 aimTarget;

	NavMeshAgent agent;
	Animator animator;
	UnitHealth unitHealth;

	public Transform lookTarget;
	public Transform offset;
	//public Quaternion offset2;

	void OnGunFire() {
		if (Weapon) {
			Weapon.HitTrigger();
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

	void FireWeapon() {
		shouldFire = true;
	}

	void AimWeapon( Vector3 point ) {

	}

	void StopMoving() {
		navNextPoint = transform.position;
		navRemainDist = 0;

		targetPos = transform.position;
	}

	void StopFiring() {
		shouldFire = false; 
	}

	//float GetTurn() {
	//	float turn = transform.rotation.eulerAngles.y % 360;
	//	if (turn < 0) turn = 360 + turn;
	//	return turn;
	//}

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

		offset = animator.GetBoneTransform(HumanBodyBones.Spine);

		animator.applyRootMotion = true;
    }

	//Vector3 GetNavPoint() {
	//	agent.nextPosition = transform.position;

	//	NavMeshPath path = new NavMeshPath();
	//	bool valid = agent.CalculatePath(targetPos, path);

	//	if (valid) {
	//		Vector3 next = path.corners[1];

	//		Vector3 dir = (next - transform.position).normalized;

	//		//return dir;
	//		return next;
	//	}
	//	else {
	//		return Vector3.zero;
	//	}
	//}

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

		//Vector3 delta = (navNextPoint - transform.position);
		//Vector3 dir = delta.normalized;
		////float dist = navRemainDist;

		//signedAngle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

		//if (Mathf.Abs(signedAngle) < 5) {
		//	transform.Rotate(0, signedAngle, 0);
		//}

		//shouldTurn = Mathf.Abs(signedAngle) > 5;
		//shouldMove = navRemainDist > agent.radius && !shouldTurn;

		//if (!shouldMove) {
		//	if (useTargetDir) {
		//		signedAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
		//		shouldTurn = Mathf.Abs(signedAngle) > 5;
		//	}
		//}
	}

	void UpdateMotion(float deltaTime) {

		Vector3 delta = (navNextPoint - transform.position);
		Vector3 dir = delta.normalized;
		float dist = navRemainDist;

		float signedAngle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

		if (Mathf.Abs(signedAngle) < 5) {
			transform.Rotate(0, signedAngle, 0);
		}

		bool shouldTurn = Mathf.Abs(signedAngle) > 5;
		bool shouldMove = dist > agent.radius && !shouldTurn;

		if (!shouldMove) {
			if (useTargetDir) {
				signedAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
				shouldTurn = Mathf.Abs(signedAngle) > 5;
			}
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

		if(shouldTurn) {
			animator.speed = TurnFactor;
		}
		else {
			if(shouldMove) {
				if (shouldRun) {
					animator.speed = RunFactor;
				}
				else {
					animator.speed = MoveFactor;
				}
			}
			else {
				animator.speed = 1.0f;
			}
		}
	}

	void Update() {
		//Think();
		UpdateNavigation();
		UpdateMotion(Time.deltaTime);

		//animator.SetInteger("Direction", 1);

		//if(shouldFire && Weapon) {
		//	Weapon.HitTrigger();
		//}
	}

	private void LateUpdate() {
		Transform tr = animator.GetBoneTransform(HumanBodyBones.Spine);
		Quaternion q = tr.rotation;
		tr.LookAt(lookTarget, Vector3.up);
		//tr.rotation = offset2 * tr.rotation;
		//tr.rotation = tr.rotation * q * Quaternion.Inverse(transform.rotation);
		//tr.rotation = Quaternion.Inverse(transform.rotation);
		//tr.rotation = transform.rotation * q * tr.rotation;
	}
	private void OnAnimatorMove() {
		Vector3 pos = animator.deltaPosition;
		Debug.Log(pos.magnitude);
		//animator.ApplyBuiltinRootMotion();
		transform.position += pos;
		transform.rotation = animator.deltaRotation * transform.rotation;
	}

	private void OnDrawGizmos() {
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(targetPos, 0.1f);
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(navNextPoint, 0.1f);
	}
}
