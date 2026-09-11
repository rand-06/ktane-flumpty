using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class flumptyModuleScript : MonoBehaviour
{
	public class ModuleComparer<T>: IComparer<T>
		where T: KMBombModule
	{
		int sortParam;
		ModuleComparer<T>(int sortParam){
			this.sortParam = sortParam;
		}
		public int Compare(T x, T y){ // if x > y: return +; x < y: return -;

		}
	} 

	public KMBombInfo bombInfo;
	public TextMesh centerText, IdText, IdNumberText;
	public GameObject blankPrefab;
	
	private static int ModuleIDCounter, InternalIDCounter;
	private int ModuleID, InternalID;
	private static KMBombInfo previousBombInfo;
	private static List<flumptyModuleInfo> moduleInfos;

	private Dictionary<KMBombModule, Transform> transformDictionary = new Dictionary<KMBombModule, Transform>();

	private List<KMBombModule> allSolved = new List<KMBombModule>(), remainingSolvables = new List<KMBombModule>();
	private static readonly string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

	private bool mustInvert;
	private int pointer = 0;
	private List<KMBombModule> currentActive = new List<KMBombModule>{GetComponent<KMBombModule>()};
	private KMBombModule currentPosition = GetComponent<KMBombModule>();
	
	void Awake()
	{
		mustInvert = GetComponent<KMBombInfo>().GetSerialNumberNumbers().LastOrDefault() % 2 == 1;
		ModuleID = ++ModuleIDCounter;
		if (bombInfo != previousBombInfo)
		{
			previousBombInfo = bombInfo;
			InternalIDCounter = 0;
		}
		InternalID = ++InternalIDCounter;
	}
	
	void Start ()
	{
		moduleInfos = flumptyServiceScript.getModuleInfo();
		centerText.text = moduleInfos == null ? "NULL" : moduleInfos.PickRandom().name;
		IdNumberText.text = InternalID.ToString();
		//remainingSolvables = Enumerable.Range(0, transform.parent.childCount)
		//	.Select(x => transform.parent.GetChild(x).gameObject.GetComponent<KMBombModule>()).ToList();
		StartCoroutine(prepareHiddenMods());
	}

	IEnumerator prepareHiddenMods()
	{
		yield return null;
		
		for (int i = 0; i < transform.parent.childCount; i++)
		{
			KMBombModule currentModule = transform.parent.GetChild(i).gameObject.GetComponent<KMBombModule>();
			Transform componentTransform = transform.parent.GetChild(i);
			if (componentTransform.GetComponent<KMBombModule>() != null && 
			     componentTransform.GetComponent<KMBombModule>().ModuleDisplayName != "Flumpty")
			{
				GameObject blank = Instantiate(blankPrefab, componentTransform.parent);
				Transform highlightTransform = componentTransform.GetComponent<KMSelectable>()
					.GetComponentInChildren<KMHighlightable>().transform; 
				blank.GetComponent<KMSelectable>().Highlight = Instantiate(
					componentTransform.GetComponent<KMSelectable>().GetComponentInChildren<KMHighlightable>(), 
					blank.transform);
				
			//List<KMSelectable> children = componentTransform.parent.parent.gameObject.GetComponent<KMSelectable>().Children.ToList();
			//children.Add(blank.GetComponent<KMSelectable>());
			//componentTransform.parent.parent.gameObject.GetComponent<KMSelectable>().Children = children.ToArray();
			//		
			//	componentTransform.parent.parent.gameObject.GetComponent<KMSelectable>().UpdateChildrenProperly();
				blank.transform.localPosition = componentTransform.localPosition;
				blank.transform.localEulerAngles = componentTransform.localEulerAngles;
				blank.transform.localScale = new Vector3(0, 0, 0);
				currentModule.OnPass += delegate
				{
					addCandidate(currentModule);
					return false;
				};
				transformDictionary.Add(currentModule, blank.transform);
			}
		}
	}

	string getSequenceSnippet(int amount){
		string ans = getSequenceSnippet(currentActive, pointer, amount);
		pointer += amount;
		return ans;
	}

	void attack(){}
	void move(int sortParam, bool reverseOrder, int moveAmount){

	}

	void startStage(){
		string first3 = getSequenceSnippet(3);
		if (first3 = "111"){
			bool invert = getSequenceSnippet(1)=="0";
			if (invert) mustInvert = !mustInvert;
			else attack();
		}
		else {
			bool reverseOrder = getSequenceSnippet(1)=="1";
			int moveAmount = convertFromBinary(getSequenceSnippet(3));
			move(convertFromBinary(first3), reverseOrder, moveAmount);
		}
	}

	int convertFromBinary(string bin) => bin.ToCharArray().Reverse().Select((x,i)=>x=='1'?1<<i:0).Sum();
	string moduleNameToCompatible(string name) => name.ToUpperInvariant().Where(c => base36.Contains(c)).Aggregate("", (a, b) => a + b);
	bool xorChars(List<char> list) => list.Count(x => x == '1') % 2 == 1;
	
	string getSequenceSnippet(List<string> moduleNames, int startIndex, int count)
	{
		List<string> moduleSequences = moduleNames.Select(moduleNameToCompatible).Select(x => x.Select(c => base36.IndexOf(c)%2==0?"0":"1").Aggregate("", (a, b) => a + b)).ToList();
		return Enumerable.Range(startIndex, count).Select(i => xorChars(moduleSequences.Select(x => x[i%x.Length]).ToList())^mustInvert?"1":"0").Aggregate("", (a, b) => a + b);
	}

	void hideModule(KMBombModule moduleToHide)
	{
		moduleToHide.gameObject.transform.localScale = new Vector3(0, 0, 0);
		transformDictionary[moduleToHide].localScale = new Vector3(1, 1, 1);
	}
	
	void showModule(KMBombModule moduleToHide)
	{
		moduleToHide.gameObject.transform.localScale = new Vector3(1, 1, 1);
		transformDictionary[moduleToHide].localScale = new Vector3(0, 0, 0);
	}

	void addCandidate(KMBombModule module)
	{
		allSolved.Add(module);
		hideModule(module);
	}

	void Update()
	{
		if (allSolved.Count == bombInfo.GetSolvedModuleIDs().Count) return;
		
		KMBombModule solvedModule = Enumerable.Range(0, transform.parent.childCount)
			.Select(x => transform.parent.GetChild(x).gameObject.GetComponent<KMBombModule>())
			.Where(x => !allSolved.Contains(x) ).ToList()[0];
		
		allSolved.Add(solvedModule);
		hideModule(solvedModule);
	}
	
}
