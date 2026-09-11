using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class blankScript : MonoBehaviour {

	public flumptyModuleScript parent;
	void Start ()
	{
		GetComponent<KMSelectable>().OnInteract += delegate
		{
			print("pressed blank");
			parent.onPressHidden(GetComponent<GameObject>().transform);
			return false;
		};
	}
}
