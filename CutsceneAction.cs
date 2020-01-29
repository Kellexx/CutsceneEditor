using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CutsceneAction 
{
	public enum actionType {Dialogue, Response, Animation, Variable, Condition, MultiCondition, Start, Camera, Pivot, Portrait, Pause};
	public actionType myActionType;
	public Rect editorWindowRect;
	public string titleText;
	public string textContent;
	public List<int> indexesOfNextActions = new List<int>();
	public List<string> conditionVariables;
	public List<string> conditionValues;
	public bool rightActor;

	/* Possible Additions:
	 - Whether or not to wait for an animation to end before continuing
	 - Position to force an object to
	 - Location to have the target object move towards 
	 - Whether or not to wait for the targe tobject to reach that position before continuing
	 - Sound to play at start of line */
}
