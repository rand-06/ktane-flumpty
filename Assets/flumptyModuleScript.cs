using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KModkit;
using UnityEngine;

public class flumptyModuleScript : MonoBehaviour
{
	private class ModuleComparer<T>: IComparer<T>
		where T: KMBombModule
	{
		private readonly int sortParam;
		private readonly List<KMBombModule> solvedQueue;
		public ModuleComparer(int sortParam, List<KMBombModule> solvedQueue){
			this.sortParam = sortParam;
			this.solvedQueue = solvedQueue;
		}
		private int CompareInit(T x, T y){ // if x > y: return +; x < y: return -;
			switch(sortParam){
				case 0: return solvedQueue.IndexOf(x) - solvedQueue.IndexOf(y);
				case 1: return string.Compare(
				moduleNameToCompatible(x.ModuleDisplayName),
				moduleNameToCompatible(y.ModuleDisplayName), StringComparison.InvariantCulture);
				case 2: return string.Compare(
					moduleInfos.First(z => z.id == x.ModuleType).date.ToString(),
					moduleInfos.First(z => z.id == y.ModuleType).date.ToString(), StringComparison.InvariantCulture);
				case 3: return moduleNameToCompatible(moduleInfos.First(z => z.id == x.ModuleType).name).Length -
				               moduleNameToCompatible(moduleInfos.First(z => z.id == y.ModuleType).name).Length;
				case 4: return moduleInfos.First(z => z.id == x.ModuleType).tpScore -
							   moduleInfos.First(z => z.id == y.ModuleType).tpScore;
				case 5: return string.Compare(
				moduleNameToCompatible(x.ModuleDisplayName).ToCharArray().Reverse().Select(x0=>x.ToString()).Aggregate("",(a,b)=>a+b),
				moduleNameToCompatible(y.ModuleDisplayName).ToCharArray().Reverse().Select(x0=>x.ToString()).Aggregate("",(a,b)=>a+b), StringComparison.InvariantCulture);
				case 6: return string.Compare(
					moduleNameToCompatible(moduleInfos.First(z => z.id == x.ModuleType).ptSymbol),
					moduleNameToCompatible(moduleInfos.First(z => z.id == y.ModuleType).ptSymbol), StringComparison.InvariantCulture);
				default: return 0;
			}
		}

		public int Compare(T x, T y){
			int ans = CompareInit(x,y);
			return ans==0?new ModuleComparer<T>((sortParam+1)%7, solvedQueue).Compare(x,y):ans;
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

	private List<KMBombModule> allSolved = new List<KMBombModule>();
	private static readonly string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

	private bool mustInvert;
	private int pointer = 0;
	private List<KMBombModule> currentActive;
	private KMBombModule currentPosition;
	public KMBombInfo BombInfo;

	private int stageNumber;
	private bool underAttack;
	
	void log(string msg){print($"[Flumpty #{ModuleID}] {msg}");}
	
	void Awake()
	{
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
		mustInvert = BombInfo.GetSerialNumberNumbers().LastOrDefault() % 2 == 1;
		currentActive = new List<KMBombModule>{GetComponent<KMBombModule>()};
		currentPosition = GetComponent<KMBombModule>();
		moduleInfos = flumptyServiceScript.getModuleInfo();
		centerText.text = moduleInfos == null ? "NULL" : moduleInfos.PickRandom().name;
		IdNumberText.text = InternalID.ToString();
		//remainingSolvables = Enumerable.Range(0, transform.parent.childCount)
		//	.Select(x => transform.parent.GetChild(x).gameObject.GetComponent<KMBombModule>()).ToList();
		StartCoroutine(prepareHiddenMods());
	}

	IEnumerator flashModule(KMBombModule module)
	{
		while (true)
		{
			module.gameObject.transform.localScale = new Vector3(0, 0, 0);
			transformDictionary[module].localScale = new Vector3(1, 1, 1);
			yield return new WaitForSeconds(0.5f);
			module.gameObject.transform.localScale = new Vector3(1, 1, 1);
			transformDictionary[module].localScale = new Vector3(0, 0, 0);
			yield return new WaitForSeconds(0.5f);
		}
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

	public void onPressHidden(Transform blankTransform){
		KMBombModule pressed = transformDictionary.FirstOrDefault(x => x.Value == blankTransform).Key;
		if (pressed == currentPosition){
			GetComponent<KMBombModule>().HandlePass();
			foreach (KMBombModule module in currentActive) showModule(module);
		}
		else {
			GetComponent<KMBombModule>().HandleStrike();
			enterRecoveryMode();
		}
	}

	void enterRecoveryMode(){}

	string getSequenceSnippet(int amount){
		string ans = getSequenceSnippet(currentActive.Select(x => x.ModuleDisplayName).ToList(), pointer, amount);
		pointer += amount;
		return ans;
	}

	void attack()
	{
		underAttack = true;
	}
	void move(int sortParam, bool reverseOrder, int moveAmount){
		List<KMBombModule> sorted = currentActive.OrderBy(x => x, new ModuleComparer<KMBombModule>(sortParam, allSolved)).ToList();
		if (reverseOrder) sorted = sorted.AsEnumerable().Reverse().ToList();
		int currentIndex = sorted.IndexOf(currentPosition);
		currentIndex = (currentIndex + moveAmount)%(sorted.Count);
		currentPosition = sorted[currentIndex];
	}

	void startStage(KMBombModule solvedModule)
	{
		stageNumber++;
		allSolved.Add(solvedModule);
		hideModule(solvedModule);
		if (stageNumber < 5) return;
		string first3 = getSequenceSnippet(3);
		if (first3 == "111"){
			bool invert = getSequenceSnippet(1)=="0";
			if (invert) mustInvert = !mustInvert;
			else attack();
		}
		else {
			bool reverseOrder = getSequenceSnippet(1)=="1";
			int moveAmount = convertFromBinary(getSequenceSnippet(3)) + 1;
			move(convertFromBinary(first3), reverseOrder, moveAmount);
		}
	}

	int convertFromBinary(string bin) => bin.ToCharArray().Reverse().Select((x,i)=>x=='1'?1<<i:0).Sum();
	static string moduleNameToCompatible(string name) {
		string ans = name.ToUpperInvariant().Where(c => base36.Contains(c)).Aggregate("", (a, b) => a + b);
		return ans == ""?"0":ans;
		}
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
		currentActive.Add(moduleToHide);
	}
	
	void showModule(KMBombModule moduleToHide)
	{
		moduleToHide.gameObject.transform.localScale = new Vector3(1, 1, 1);
		transformDictionary[moduleToHide].localScale = new Vector3(0, 0, 0);
		currentActive.Remove(moduleToHide);
	}

	void addCandidate(KMBombModule module)
	{
		allSolved.Add(module);
		hideModule(module);
	}

	IEnumerator die()
	{
		while (true)
		{
			GetComponent<KMBombModule>().HandleStrike();
			yield return new WaitForSeconds(1f);
		}
	}
	
	public static bool IsSolved(KMBombModule module)
	{
		try
		{
			Type type = Type.GetType("ModBombComponent, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
			return (bool) type
				.GetField("IsSolved", BindingFlags.Instance | BindingFlags.Public)
				.GetValue(module.GetComponent("ModBombComponent"));
		}
		catch(Exception e) { print(e); return false; }
	}

	void Update()
	{
		if (bombInfo.GetSolvedModuleIDs().Count == stageNumber) return;
		if (underAttack) StartCoroutine(die());
		List<KMBombModule> solvedModules = Enumerable.Range(0, transform.parent.childCount)
			.Select(x => transform.parent.GetChild(x).gameObject.GetComponent<KMBombModule>())
			.Where(IsSolved).ToList();
		print(solvedModules.Count);
		KMBombModule solvedModule = solvedModules.First(x => !allSolved.Contains(x));
		
		startStage(solvedModule);
	}
	
}
