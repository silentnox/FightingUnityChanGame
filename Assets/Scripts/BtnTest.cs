using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BtnTest : MonoBehaviour
{

	public struct Wtf {
		int a;
		int b;
	}
	Wtf wtf;
	public List<Wtf> helloWtf = new List<Wtf>();

	public int WAT;


	int numPress = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Z)) {
			numPress++;
			Debug.Log(numPress);
		}
    }
}
