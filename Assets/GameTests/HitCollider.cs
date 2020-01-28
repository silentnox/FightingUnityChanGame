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

	public UnitHealth GetOwner() {
		return GetComponentInParent<UnitHealth>();
	}

	// Start is called before the first frame update
    void Start() {
		unitHealth = GetComponentInParent<UnitHealth>();
		collider = GetComponent<Collider>();
    }

	private void OnTriggerEnter(Collider other) {
		if (unitHealth == null) return;

		HitCollider otherHitCollider = other.GetComponent<HitCollider>();

		if (otherHitCollider == null) return;
		if (unitHealth == otherHitCollider.unitHealth) return;

		unitHealth.OnHitColliderContact(this, otherHitCollider);
	}

	// Update is called once per frame
	void Update() {
		
    }
}
