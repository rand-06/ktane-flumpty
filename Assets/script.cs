using UnityEngine;

public class script : MonoBehaviour
{

	public KMSelectable button;
	void Start ()
	{
		button.OnInteract += delegate { GetComponent<KMBombModule>().HandlePass();
			return false;
		};
	}
}
