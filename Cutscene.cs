using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class Cutscene : ScriptableObject 
{
	public int cutsceneID;
	//[HideInInspector]
	public List<CutsceneAction> cutsceneActions = new List<CutsceneAction>();
	public float PanX, PanY;
}
