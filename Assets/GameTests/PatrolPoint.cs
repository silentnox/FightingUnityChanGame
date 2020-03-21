using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolPoint : MonoBehaviour {

	public bool UseDirection = false;
	public PatrolPoint NextPatrolPoint = null;
	public float WaitTime = 0;

	PatrolPoint nextPatrolPoint = null;
	PatrolPoint prevPatrolPoint = null;

	public PatrolPoint GetNextPatrolPoint() {
		//Transform node = transform.parent.GetChild(transform.GetSiblingIndex() + 1);
		//PatrolPoint nextPoint = node?node.GetComponent<PatrolPoint>():null;
		//return NextPatrolPoint?NextPatrolPoint:nextPoint;
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

		if(nextPatrolPoint) {
			nextPatrolPoint.prevPatrolPoint = this;
		}
    }

    // Update is called once per frame
    void Update() {
        
    }

	private void OnDrawGizmos() {
		Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1);
	}
}
