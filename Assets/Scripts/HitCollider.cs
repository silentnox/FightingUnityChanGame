using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitCollider : MonoBehaviour {

	public enum Type {
		Inflict,
		Receive
	}

	public Type ColliderType = Type.Inflict;

	UnitHealth unitHealth = null;
	Collider collider = null;

	List<Collider> contacts = new List<Collider>();

	public UnitHealth GetOwner() {
		return GetComponentInParent<UnitHealth>();
	}

	// Start is called before the first frame update
    void Start() {
		unitHealth = GetComponentInParent<UnitHealth>();
		collider = GetComponent<Collider>();
    }

	void OnHitContact( Collider other ) {
		if (unitHealth == null) return;
		if (!this.enabled) return;

		HitCollider otherHitCollider = other.GetComponent<HitCollider>();

		if (otherHitCollider == null || !otherHitCollider.enabled) return;
		if (unitHealth == otherHitCollider.unitHealth) return;

		unitHealth.OnHitColliderContact(this, otherHitCollider);
	}

	private void OnTriggerEnter(Collider other) {
		//if (unitHealth == null) return;

		//HitCollider otherHitCollider = other.GetComponent<HitCollider>();

		//if (otherHitCollider == null) return;
		//if (unitHealth == otherHitCollider.unitHealth) return;

		//unitHealth.OnHitColliderContact(this, otherHitCollider);

		if (!contacts.Contains(other)) {
			contacts.Add(other);
			OnHitContact(other);
		}
	}

	private void OnTriggerExit(Collider other) {
		contacts.Remove(other);
	}

	private void OnTriggerStay(Collider other) {
		if (!contacts.Contains(other)) {
			contacts.Add(other);
			OnHitContact(other);
		}
	}

	// Update is called once per frame
	void Update() {
		
    }
}
