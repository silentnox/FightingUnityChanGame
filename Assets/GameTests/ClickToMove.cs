// ClickToMove.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class ClickToMove : MonoBehaviour {
	RaycastHit hitInfo = new RaycastHit();
	UnityEngine.AI.NavMeshAgent agent;

	void Start() {
		agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
	}
	void Update() {
		if (Input.GetMouseButtonDown(0)) {
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray.origin, ray.direction, out hitInfo)) {
				//agent.destination = hitInfo.point;
				//GetComponent<EnemyControl>().targetPos = hitInfo.point;
				GetComponent<EnemyControl>().SetDestination( hitInfo.point );
				//GetComponent<NavMeshAgent>().SetDestination( hitInfo.point );
			}
			Debug.Log("CTM");
		}
	}
}
