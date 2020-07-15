using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// basic patroling point to be used by EnemyControl script
[ExecuteInEditMode]
public class PatrolPoint : MonoBehaviour {

	public bool UseDirection = false;
	public bool UseRunning = false;
	public PatrolPoint NextPatrolPoint = null;
	public float WaitTime = 0;

	PatrolPoint nextPatrolPoint = null;
	PatrolPoint prevPatrolPoint = null;

	public PatrolPoint GetNextPatrolPoint() {
		return nextPatrolPoint;
	}

	public PatrolPoint GetPrevPatrolPoint() {
		return prevPatrolPoint;
	}

    // Start is called before the first frame update
    void Start() {
		Transform node = transform.GetSiblingIndex()+1 < transform.parent.childCount?transform.parent.GetChild(transform.GetSiblingIndex() + 1):null;
		PatrolPoint nextPoint = node ? node.GetComponent<PatrolPoint>() : null;
		nextPatrolPoint = NextPatrolPoint ? NextPatrolPoint : nextPoint;

		if (nextPatrolPoint && !nextPatrolPoint.enabled) nextPatrolPoint = null;

		if(nextPatrolPoint) {
			nextPatrolPoint.prevPatrolPoint = this;
		}
    }

    // Update is called once per frame
    void Update() {
        
    }

	private void OnDrawGizmos() {
		Gizmos.color = Color.green;
		if (UseDirection) {
			Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1);
		}
		Gizmos.DrawSphere(transform.position, 0.15f);

		if(nextPatrolPoint) {
			Gizmos.DrawLine(transform.position, nextPatrolPoint.transform.position);
		}
	}
}
