using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GunType {
	None,
	Pistol,
	Rifle
}


public class GunHelper : MonoBehaviour {
	public Transform Grip1 = null;
	public Transform Grip2 = null;
	public Transform Muzzle = null;
	public GunType Type = GunType.None;

	public int SalvoSize = 4;
	public float Accuracy = 1;
	public float ShotInterval = 0.06f;
	public float SalvoInterval = 1;

	public float ClipSize = 0;
	public float ClipReloadTime = 0;

	public float DamagePerBullet = 6;

	public float FlashStayTime = 0.1f;
	public Color FlashColor = Color.white;

	int salvoCounter = 0;
	float shotDelay = 0.0f;
	float salvoDelay = 0.0f;

	int clipCounter = 0;
	float clipDelay = 0;

	LineRenderer flashLine;
	float flashStay = 0;

	UnitHealth gunOwner = null;

	public int GetSalvoCounter() {
		return salvoCounter;
	}

	public RaycastHit ProbeRay(out UnitHealth hitBody) {
		Ray ray = new Ray(Muzzle.position, Muzzle.forward);
		RaycastHit hit;

		Physics.Raycast(ray, out hit);

		if (hit.collider) {
			hitBody = hit.collider.gameObject.GetComponent<UnitHealth>();
		}
		else {
			hitBody = null;
		}

		return hit;
	}

	void FireBullet() {
		Ray ray = new Ray(Muzzle.position, Muzzle.forward);
		RaycastHit hit;

		Physics.Raycast(ray, out hit);

		flashLine.positionCount = 2;

		flashLine.SetPosition(0, Muzzle.position);
		flashLine.SetPosition(1, hit.point);
		flashStay = FlashStayTime;
		flashLine.enabled = true;

		if (hit.collider) {
			UnitHealth hitBody = hit.collider.gameObject.GetComponent<UnitHealth>();
			HitCollider hitCollider = hit.collider.gameObject.GetComponent<HitCollider>();

			if(!hitBody && hitCollider && hitCollider.ColliderType == HitCollider.Type.Receive) {
				hitBody = hitCollider.GetOwner();
			}

			if(hitBody) {
				hitBody.Damage(DamagePerBullet);
			}
		}
	}

	public bool HitTrigger() {
		if (salvoCounter > 0) {
			if (shotDelay <= 0) {
				salvoCounter--;
				FireBullet();
				shotDelay = ShotInterval;

				if (salvoCounter == 0) {
					salvoDelay = SalvoInterval;
				}

				return true;
			}
		}
		return false;
	}

	public void ReloadGun() {

	}

	private void Start() {
		GameObject flash = new GameObject();
		flashLine = flash.AddComponent<LineRenderer>();
		flashLine.startColor = FlashColor;
		flashLine.endColor = FlashColor;
		flashLine.startWidth = 0.02f;
		flashLine.endWidth = 0.02f;
		flashLine.material = new Material(Shader.Find("Standard"));
		flashLine.material.EnableKeyword("_EMISSION");
		flashLine.material.SetColor("_EmissionColor", Color.white);
		flashLine.allowOcclusionWhenDynamic = false;
		flashLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		flash.transform.SetParent(Muzzle);

		salvoCounter = SalvoSize;
	}

	private void Update() {
		if (flashStay > 0) {
			flashStay -= Time.deltaTime;
		}

		if(flashStay < 0) {
			flashLine.enabled = false;
		}
		//else {
		//	flashLine.enabled = false;
		//}

		if (shotDelay > 0) {
			shotDelay -= Time.deltaTime;
		}

		//if (salvoCounter <= 0) {
		//	salvoDelay = SalvoInterval;
		//}

		if(salvoDelay > 0) {
			salvoDelay -= Time.deltaTime;
		}

		if (salvoDelay <= 0 && salvoCounter == 0) {
			salvoCounter = SalvoSize;
		}
	}
}
