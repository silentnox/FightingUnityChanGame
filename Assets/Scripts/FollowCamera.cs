using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour {

	public GameObject FollowTarget = null;

	private Camera mCam = null;

	public float AttackAngle = 45;
	public float Distance = 10;
	public float Inertia = 1;

	private Vector3 Pos;

	// Start is called before the first frame update
    void Start() {
		if (mCam == null) {
			//mCam = new Camera();
			mCam = GetComponent<Camera>();
		}
		if (FollowTarget) {
			Pos = FollowTarget.transform.position;
		}
		Update();
    }

    // Update is called once per frame
    void Update() {
		if(FollowTarget == null) {
			return;
		}
		Vector3 camPos,camOffset;
		Quaternion camRot;

		Vector3 camVelocity = new Vector3();

		//Pos = FollowTarget.transform.position;
		Pos = Vector3.SmoothDamp(Pos, FollowTarget.transform.position, ref camVelocity, Inertia, 100, Time.deltaTime);

		camOffset = Vector3.up * Distance;
		camPos = Pos + Quaternion.Euler(90 - AttackAngle, 0, 0) * camOffset;
		camRot = Quaternion.LookRotation((Pos - camPos).normalized);

		mCam.transform.position = camPos;
		mCam.transform.rotation = camRot;
    }
}
