using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;

public class UnitHealth : MonoBehaviour {

	public enum UnitType {
		Neutral,
		Player,
		Enemy
	}

	public UnitType Type = UnitType.Neutral;
	public float Health = 100;
	public float MaxHealth = 100;
	public float HealthRegen = 0;
	public float DamageRegenDelay = 0;
	public bool Invulnerable = false;
	public bool ReactToDamage = true;
	public bool RagdollOnDeath = false;

	bool ragdollActive = false;

	[HideInInspector]
	public event Action<float> OnDamage;
	[HideInInspector]
	public event Action OnDeath;
	[HideInInspector]
	public event Action AfterRagdoll;
	[HideInInspector]
	public event Action<HitCollider,HitCollider> OnInflictHit;
	[HideInInspector]
	public event Action<HitCollider, HitCollider> OnReceiveHit;

	float regenDelay = 0;
	int prevSecond = 0;

	Vector3 lastDamageDir = Vector3.zero;
	Vector3 lastDamagePos = Vector3.zero;



	public bool IsRagdoll() {
		return ragdollActive;
	}

	public bool IsDead() {
		return Health <= 0;
	}

	public void ActivateRagdoll( bool activate ) {

		if (activate == ragdollActive) return;

		Collider collider = gameObject.GetComponent<Collider>();
		Rigidbody rigidBody = gameObject.GetComponent<Rigidbody>();
		Animator animator = gameObject.GetComponent<Animator>();

		Collider[] childColliders = gameObject.GetComponentsInChildren<Collider>();
		Rigidbody[] childRigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();

		transform.Translate(new Vector3(0, 0.1f, 0));

		foreach (Rigidbody r in childRigidbodies) {
			if (r.gameObject == gameObject) continue;
			if (r.GetComponent<HitCollider>() != null) continue;

			r.detectCollisions = activate;
			r.useGravity = activate;
			r.isKinematic = !activate;
		}

		rigidBody.isKinematic = activate;
		rigidBody.useGravity = !activate;
		rigidBody.detectCollisions = !activate;
		animator.enabled = !activate;

		ragdollActive = activate;

		if(ragdollActive) {
			AfterRagdoll?.Invoke();
		}

		Debug.Log("Ragdoll: " + ragdollActive);
	}

	public void Damage( float amount, GameObject attacker =null, Vector3? from = null ) {
		if (Invulnerable) return;
		if (Health <= 0) return;

		Health -= amount;

		if(DamageRegenDelay > 0) {
			regenDelay = DamageRegenDelay;
		}

		OnDamage?.Invoke(amount);

		if(Health < 0 + Mathf.Epsilon) {
			if (RagdollOnDeath) {
				ActivateRagdoll(true);
			}
			OnDeath?.Invoke();
		}
	}

	public void OnHitColliderContact( HitCollider self, HitCollider other ) {
		if(self.ColliderType != other.ColliderType) {
			if(self.ColliderType == global::HitCollider.Type.Inflict ) {
				//OnInflictHit(self,other);
				OnInflictHit?.Invoke(self, other);
			}
			else if (self.ColliderType == global::HitCollider.Type.Receive) {
				//OnReceiveHit(self,other);
				OnReceiveHit?.Invoke(self, other);
			}
		}
	}

	void Start() {

		Rigidbody[] childRigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();

		foreach( Rigidbody r in childRigidbodies) {
			if (r.gameObject == gameObject) continue;
			if (r.GetComponent<HitCollider>() != null) continue;

			r.detectCollisions = false;
			r.useGravity = false;
			r.isKinematic = true;
			r.gameObject.layer = 10;
		}
	} 

	void Update() {
		if (Input.GetKeyDown(KeyCode.R)) {
			ActivateRagdoll(!ragdollActive);
		}
		if(regenDelay > 0) {
			regenDelay -= Time.deltaTime;
		}

		if(Math.Truncate(Time.time) >  prevSecond ) {
			prevSecond = (int)Math.Truncate(Time.time);

			if (Health > 0 && regenDelay <= 0) {
				Health += HealthRegen;
				if (Health > MaxHealth) Health = MaxHealth;
			}
		}
	}
}
