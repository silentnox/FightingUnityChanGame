//
// Controller with rigidbody when Mecanim animation data does not move at the origin
// sample
// 2014/03/13 N.Kobyasahi
//
using UnityEngine;
using System.Collections;

namespace UnityChan {
	// List of required components
	[RequireComponent(typeof(Animator))]
	[RequireComponent(typeof(CapsuleCollider))]
	[RequireComponent(typeof(Rigidbody))]

	public class UnityChanControlScriptWithRigidbody_en : MonoBehaviour {

		public float animSpeed = 1.5f;              // Animation playback speed setting
		public float lookSmoother = 3.0f;           // a smoothing setting for camera motion
		public bool useCurves = true;               // Set whether to use curve adjustment in Mecanim
													// If this switch is not turned on, the curve is not used
		public float useCurvesHeight = 0.5f;        // Effective height of curve correction (increase when it is easy to slip through the ground)

		// Character controller parameters below
		// Forward speed
		public float forwardSpeed = 7.0f;
		// reverse speed
		public float backwardSpeed = 2.0f;
		// turning speed
		public float rotateSpeed = 2.0f;
		// Jump power
		public float jumpPower = 3.0f;
		// Refer to the character controller (capsule collider)
		private CapsuleCollider col;
		private Rigidbody rb;
		// Move amount of character controller (capsule collider)
		private Vector3 velocity;
		// Variable to store the initial value of collider Heiht and Center set in CapsuleCollider
		private float orgColHight;
		private Vector3 orgVectColCenter;
		private Animator anim;                          // reference to the animator attached to the character
		private AnimatorStateInfo currentBaseState;         // Reference the current state of the animator used in the base layer

		private GameObject cameraObject;    // Reference to main camera

		// reference to each animator state
		static int idleState = Animator.StringToHash("Base Layer.Idle");
		static int locoState = Animator.StringToHash("Base Layer.Locomotion");
		static int jumpState = Animator.StringToHash("Base Layer.Jump");
		static int restState = Animator.StringToHash("Base Layer.Rest");

		// Initialization
		void Start() {
			// Get the Animator component
			anim = GetComponent<Animator>();
			// Get CapsuleCollider component (capsule type collision)
			col = GetComponent<CapsuleCollider>();
			rb = GetComponent<Rigidbody>();
			// Get the main camera
			cameraObject = GameObject.FindWithTag("MainCamera");
			// Save initial values of Height and Center of CapsuleCollider component
			orgColHight = col.height;
			orgVectColCenter = col.center;
		}


		// The following is the main process, which involves the rigid body.
		void FixedUpdate() {
			float h = Input.GetAxis("Horizontal");              // Define the horizontal axis of the input device as h
			float v = Input.GetAxis("Vertical");                // Define the vertical axis of the input device with v
			anim.SetFloat("Speed", v);                          // pass v to the "Speed" parameter set on the Animator side
			anim.SetFloat("Direction", h);                      // pass h to the "Direction" parameter set on the Animator side
			anim.speed = animSpeed;                             // Set animSpeed to Animator's motion playback speed
			currentBaseState = anim.GetCurrentAnimatorStateInfo(0); // Set the current state of Base Layer (0) to the reference state variable
			rb.useGravity = true;								//Since it cuts gravity while jumping, it should be affected by gravity otherwise.



			// Below, the character movement process
			velocity = new Vector3(0, 0, v);        // Get the amount of movement in the Z-axis direction from the up and down keystrokes												
			velocity = transform.TransformDirection(velocity); // Convert to character's local space direction

			//The following v thresholds are adjusted with the Mecanim side transitions
			if (v > 0.1) {
				velocity *= forwardSpeed;       // Multiply moving speed
			}
			else if (v < -0.1) {
				velocity *= backwardSpeed;  // Multiply moving speed
			}

			if (Input.GetButtonDown("Jump")) {  // After entering the space key

				// Jump only when the animation state is Locomotion
				if (currentBaseState.fullPathHash == locoState) {
					// Jump if not in state transition
					if (!anim.IsInTransition(0)) {
						rb.AddForce(Vector3.up * jumpPower, ForceMode.VelocityChange);
						anim.SetBool("Jump", true);     // Send a flag to jump to Animator
					}
				}
			}


			// Move the character with the up and down keystrokes
			transform.localPosition += velocity * Time.fixedDeltaTime;

			// Rotate the character on the Y axis with left and right keystrokes
			transform.Rotate(0, h * rotateSpeed, 0);


			// The following processing in each state of Animator
			// During Locomotion
			// When the current base layer is locoState
			if (currentBaseState.fullPathHash == locoState) {
				// When adjusting the collider with a curve, reset it just in case
				if (useCurves) {
					resetCollider();
				}
			}
			// Processing during JUMP
			// When the current base layer is jumpState
			else if (currentBaseState.fullPathHash == jumpState) {
				cameraObject.SendMessage("setCameraPositionJumpView");  // Change to jumping camera

				// If the state is not in transition
				if (!anim.IsInTransition(0)) {

					// Below, the process for curve adjustment
					if (useCurves) {
						// The following curves JumpHeight and GravityControl attached to the JUMP00 animation
						// JumpHeight: Jump height at JUMP00 (0 to 1)
						// GravityControl: 1⇒ Jumping (gravity disabled), 0⇒ gravity enabled
						float jumpHeight = anim.GetFloat("JumpHeight");
						float gravityControl = anim.GetFloat("GravityControl");
						if (gravityControl > 0)
							rb.useGravity = false;  // Cut the influence of gravity while jumping

						// Drop the Raycast from the character center
						Ray ray = new Ray(transform.position + Vector3.up, -Vector3.up);
						RaycastHit hitInfo = new RaycastHit();
						// Only when the height is more than useCurvesHeight, adjust the height and center of the collider with the curve attached to the JUMP00 animation.
						if (Physics.Raycast(ray, out hitInfo)) {
							if (hitInfo.distance > useCurvesHeight) {
								col.height = orgColHight - jumpHeight;          // Adjusted collider height
								float adjCenterY = orgVectColCenter.y + jumpHeight;
								col.center = new Vector3(0, adjCenterY, 0); // Adjusted collider center
							}
							else {
								// If the value is lower than the threshold, return to the initial value (just in case)					
								resetCollider();
							}
						}
					}
					// Reset Jump bool value (Do not loop)			
					anim.SetBool("Jump", false);
				}
			}
			// Processing during IDLE
			// When the current base layer is idleState
			else if (currentBaseState.fullPathHash == idleState) {
				// When adjusting the collider with a curve, reset it just in case.
				if (useCurves) {
					resetCollider();
				}
				// Enter the Rest state after entering the space key
				if (Input.GetButtonDown("Jump")) {
					anim.SetBool("Rest", true);
				}
			}
			// Processing during REST
			// When the current base layer is restState
			else if (currentBaseState.fullPathHash == restState) {
				//cameraObject.SendMessage("setCameraPositionFrontView");		// Switch the camera to the front
				// If the state is not transitioning, reset the Rest bool value (do not loop)
				if (!anim.IsInTransition(0)) {
					anim.SetBool("Rest", false);
				}
			}
		}

		void OnGUI() {
			GUI.Box(new Rect(Screen.width - 260, 10, 250, 150), "Interaction");
			GUI.Label(new Rect(Screen.width - 245, 30, 250, 30), "Up/Down Arrow : Go Forwald/Go Back");
			GUI.Label(new Rect(Screen.width - 245, 50, 250, 30), "Left/Right Arrow : Turn Left/Turn Right");
			GUI.Label(new Rect(Screen.width - 245, 70, 250, 30), "Hit Space key while Running : Jump");
			GUI.Label(new Rect(Screen.width - 245, 90, 250, 30), "Hit Spase key while Stopping : Rest");
			GUI.Label(new Rect(Screen.width - 245, 110, 250, 30), "Left Control : Front Camera");
			GUI.Label(new Rect(Screen.width - 245, 130, 250, 30), "Alt : LookAt Camera");
		}


		// Character collider size reset function
		void resetCollider() {
			// Return the initial values of the component's Height and Center
			col.height = orgColHight;
			col.center = orgVectColCenter;
		}
	}
}