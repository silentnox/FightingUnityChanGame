using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// this script monitors Animator state machine
// and emits events when state changes
public class StateMachineControl : StateMachineBehaviour {

	public static event Action<Animator, AnimatorStateInfo, int> onStateEnter;
	public static event Action<Animator, AnimatorStateInfo, int> onStateExit;
	public static event Action<Animator, AnimatorStateInfo, int> onStateUpdate;
	public static event Action<Animator, AnimatorStateInfo, int> onStateMove;
	public static event Action<Animator, AnimatorStateInfo, int> onStateIK;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		onStateEnter?.Invoke(animator, stateInfo, layerIndex);
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		onStateExit?.Invoke(animator, stateInfo, layerIndex);

	}

	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		onStateUpdate?.Invoke(animator, stateInfo, layerIndex);

		//animator.
	}

	override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		onStateMove?.Invoke(animator, stateInfo, layerIndex);

	}

	override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		onStateIK?.Invoke(animator, stateInfo, layerIndex);

	}
}
