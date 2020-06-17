using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

	public Canvas canvas = null;

	public UnitHealth unityChan = null;

	Text text1 = null;
	Text text2 = null;

	bool levelPassed = false;

	public static GameManager Instance = null;

	public void WinGame() {
		levelPassed = true;
	}
    
	// Start is called before the first frame update
    void Start() {
        if(canvas) {
			canvas.enabled = true;

			Transform tr = canvas.GetComponent<Transform>();
			text1 = tr.Find("Text1").GetComponent<Text>();
			text2 = tr.Find("Text2").GetComponent<Text>();

			text1.text = "";
			text2.text = "";
		}

		GameManager.Instance = this;
    }

    // Update is called once per frame
    void Update() {
        if(Input.GetKeyDown(KeyCode.Escape)) {
			Debug.Log("Exiting game");
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
		if(Input.GetKeyDown(KeyCode.R)) {
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}

		if(unityChan.IsDead()) {
			text1.text = "You died";
			text2.text = "Press R to restart. Press Esc to exit the game.";
		}
		if(levelPassed) {
			Time.timeScale = 0;
			unityChan.Invulnerable = true;
			text1.text = "Victory";
		}
    }
}
