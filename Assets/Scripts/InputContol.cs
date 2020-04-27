using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputContol : MonoBehaviour {
    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
		MovementController controller = GetComponent<MovementController>();

		if (controller == null) return;

		controller.turnFactor = Input.GetAxis("Horizontal");
		controller.moveFactor = Input.GetAxis("Vertical");

		Debug.Log(Input.GetAxis("Horizontal") + " " + Input.GetAxis("Vertical"));
    }
}
