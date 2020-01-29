using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class ListItem : ScriptableObject
{
	public string itemName;
	public bool openFolder;
	public int parentItemIndex;
	public int indentLevel;
	public Cutscene cutscene;

	public void OnEnable()
	{
		//hideFlags = HideFlags.HideInHierarchy;
	}
}
