using System;
using System.Collections.Generic;
using UnityEngine;

class Timer {
	float interval;
	bool once;
	Action callable;
	bool active = true;

	float delta = 0;

	public static List<Timer> instances = new List<Timer>();
	//static TimerManager manager = null;

	public Timer(float interval, bool once, Action callable) {
		this.interval = interval;
		this.once = once;
		this.callable = callable;
		instances.Add(this);
	}

	public void Update() {
		delta += Time.deltaTime;
		if (delta > interval) {
			delta = 0;
			callable?.Invoke();

			if(once) {
				RemoveTimer(this);
			}
		}
	}

	static void test() {
		Debug.Log("hellowtf");
	}

	public static Timer StartTimer(float interval, bool once, Action callable) {
		return new Timer(interval, once, callable);
	}
	public static void RemoveTimer( Timer timer ) {
		instances.Remove(timer);
	}

	//static Timer() {
	//	GameObject gameObject = new GameObject();
	//	gameObject.AddComponent<TimerManager>();

	//	Timer t = new Timer(1, false, test);

	//	Debug.Log("wasds");
	//}
}

public class TimerManager : MonoBehaviour {
	private void Update() {
		foreach (Timer t in Timer.instances) {
			t.Update();
		}
	}
	static void test() {
		Debug.Log("hellowtf");
	}
	private void Start() {
		//GameObject gameObject = new GameObject();
		//gameObject.AddComponent<TimerManager>();

		//Timer t = new Timer(1, false, test);
	}

	static TimerManager() {
		//GameObject gameObject = new GameObject();
		//gameObject.AddComponent<TimerManager>();

		Timer t = new Timer(1, false, test);

		Debug.Log("wasds");
	}
}