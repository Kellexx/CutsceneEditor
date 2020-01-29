using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CutsceneData : ScriptableObject 
{
	public Texture mainBackground;
	public Texture listBackground;
	public List<ListItem> listItems;
	public List<int> cutsceneIndexes;
	public int nextCutsceneID;
}
