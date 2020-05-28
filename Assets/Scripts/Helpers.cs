using System;
using System.Collections.Generic;
using UnityEngine;

class Smooth {
	public float current = 0;
	public float target = 0;
	public float smoothTime = 0;
	public float currentVelocity = 0;
	public float maxSpeed = Mathf.Infinity;

	//public static float deltaTime = 0;

	//public Smooth(float input) {
	//	this.target = input;
	//	this.current = input;
	//}
	public void Eval(float deltaTime) {
		current = Mathf.SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
	}
	public static implicit operator float(Smooth smooth) {
		return smooth.current;
	}
	//public static implicit operator Smooth(float input) {
	//	return new Smooth(input);
	//}
}

class Helpers {
	public static float GetAngle360(Vector2 vec) {
		float angle = Vector2.SignedAngle(Vector2.down, vec);

		if (angle < 0) angle = 360 + angle;

		return angle;
	}

	public static float GetSignedAngleDiff(float from, float to) {
		float angleDiff = to - from;

		if (angleDiff > 180) angleDiff -= 360;
		if (angleDiff < -180) angleDiff += 360;

		return angleDiff;
	}

	public static Vector2 ToVec2( Vector3 inVec ) {
		return new Vector2(inVec.x, inVec.z);
	}

	public static bool IsVisible( Vector3 from, Vector3 to, int layerMask ) {
		RaycastHit hit;
		Physics.Raycast(new Ray(from, from.DirTo(to)), out hit, Mathf.Infinity, layerMask);
		return Vector3.Distance(from, to) < hit.distance;
	}

}

public static class VectorHelpers {
	public static Vector3 SetX(this Vector3 vec, float x) {
		return new Vector3(x, vec.y, vec.z);
	}

	public static Vector3 SetY(this Vector3 vec, float y) {
		return new Vector3(vec.x, y, vec.z);
	}

	public static Vector3 SetZ(this Vector3 vec, float z) {
		return new Vector3(vec.x, vec.y, z);
	}
	public static Vector3 DirTo(this Vector3 vec, Vector3 target) {
		return (target-vec).normalized;
	}
}

