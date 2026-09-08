using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class blankScript : MonoBehaviour {

	
	void Start ()
	{
		GetComponent<KMSelectable>().OnInteract += delegate
		{
			print("pressed blank");
			return false;
		};
	}
}
