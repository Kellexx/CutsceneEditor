using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class CutsceneEditor : EditorWindow 
{
	private CutsceneData cutsceneData;
	private Cutscene currentCutscene;
	private ListItem currentListItem;
	private List<CutsceneAction> cutsceneActions;
	private List<ListItem> listItems;

	private Rect mainRect;
	private Rect listRect;

	private CutsceneAction cutsceneActionBeingEdited;
	private bool connectingActions = false;
	private bool clickHasOccurred = false;
	private int activeWindowId;
	public List<int> cutsceneIndexes;

	private bool dragItemState = false;
	private ListItem itemBeingDragged;
	private int editItemIndex;
	private int editConditionIndex;
	private bool exitEditMode;
	private bool newItemCreated;

	private float PanX = -500000f;
	private float PanY = -500000f;
	private const int DEFAULT_ACTION_WIDTH = 300;
	private const int START_ACTION_WIDTH = 62;
	private const int START_ACTION_HEIGHT = 42;
	private const int DIALOGUE_HEIGHT = 108;
	private const int ANIMATION_HEIGHT = 83;
	private const int ANIMATION_WIDTH = 190;
	private const int VARIABLE_HEIGHT = 84;
	private const int VARIABLE_WIDTH = 250;
	private const int CONDITION_HEIGHT = 85;
	private const int CONDITION_WIDTH = 240;
	private const int CONDITION_LINE_HEIGHT = 22;
	private const int MULTICON_HEIGHT = 88;
	private const int MULTICON_WIDTH = 300;
	private const int MULTICON_LINE_HEIGHT = 20;
	private const int RESPONSE_HEIGHT = 88;
	private const int CAMERA_HEIGHT = 150;
	private const int CAMERA_WIDTH = 235;
	private const int PIVOT_WIDTH = 85;
	private const int PIVOT_HEIGHT = 42;
	private const int PORTRAIT_HEIGHT = 105;
	private const int PORTRAIT_WIDTH = 190;
	private const int PAUSE_HEIGHT = 63;
	private const int PAUSE_WIDTH = 130;
	private int listWidth = 250;
	private int buttonWidth = 20;
	private int indentSpace = 20;
	private GUIStyle folderStyle;
	private GUIStyle buttonStyle;
	private GUIStyle headerStyle;
	private GUIStyle dragHandleStyle;

	[MenuItem("Window/Cutscene Editor")]
	static void ShowEditor()
	{
		CutsceneEditor editor = EditorWindow.GetWindow<CutsceneEditor>();
		editor.Show();
	}

	void Awake()
	{
		cutsceneActions = new List<CutsceneAction>();

		listRect = new Rect(0, 0, listWidth, 1000);
		mainRect = new Rect(0, 0, 1000000, 1000000);

		editItemIndex = -1;

		folderStyle = new GUIStyle();
		headerStyle = new GUIStyle();

		buttonStyle = new GUIStyle();
		buttonStyle.fixedWidth = buttonWidth;

		dragHandleStyle = new GUIStyle();
		dragHandleStyle.fixedWidth = 10;
		dragHandleStyle.normal.textColor = Color.black;
		dragHandleStyle.hover.textColor = Color.white;
		dragHandleStyle.active.textColor = Color.red;

		cutsceneData = (CutsceneData)AssetDatabase.LoadAssetAtPath("Assets/Cutscenes/CutsceneData.asset", typeof(CutsceneData));
		if(!cutsceneData)
		{
			cutsceneData = (CutsceneData)ScriptableObject.CreateInstance(typeof(CutsceneData));
			cutsceneData.listItems = new List<ListItem>();
			cutsceneData.mainBackground = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Cutscenes/DottedBG.png", typeof(Texture2D));
			cutsceneData.listBackground = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Cutscenes/WindowBG_GrayBlue.png", typeof(Texture2D));
			AssetDatabase.CreateAsset(cutsceneData, "Assets/Cutscenes/CutsceneData.asset");
		}

		cutsceneIndexes = cutsceneData.cutsceneIndexes;
		listItems = cutsceneData.listItems;
		EditorUtility.SetDirty(cutsceneData);
	}

	void ResetData()
	{
		/*listItems = new List<ListItem>();
		cutsceneData.listItems = listItems;*/

		cutsceneActions = new List<CutsceneAction>();
		RefreshCutsceneIndexList();
		SaveCutsceneData();
	}

	void OnGUI()
	{
		string currentEventType = Event.current.type.ToString();

		clickHasOccurred = false;

		if(currentEventType == "mouseDown")
		{
			clickHasOccurred = true;
			if(Event.current.button == 1)
			{
				if(editItemIndex < 0)
				{
					editItemIndex = -2;
				}
			}
		}

		//ResetData();
		Repaint();
		DrawMainEditor();
		DrawFolders();

		if(currentEventType == "mouseDown")
		{
			connectingActions = false;
		} else if(currentEventType == "mouseUp")
		{
			if (currentCutscene != null)
			{
				SaveCutsceneData();
			}
		}
	}

	void SaveCutsceneData()
	{
		currentListItem.cutscene.cutsceneActions = cutsceneActions;
		currentListItem.cutscene.PanX = PanX;
		currentListItem.cutscene.PanY = PanY;
		EditorUtility.SetDirty(currentListItem);
	}

	void DrawMainEditor()
	{
		Vector3 lineStart, lineEnd;
		CutsceneAction cutsceneAction;
		bool clickedOnWindow = false;

		GUI.BeginGroup(new Rect(PanX, PanY, 1000000, 1000000));

		GUI.DrawTextureWithTexCoords(mainRect, cutsceneData.mainBackground, new Rect(0, 0, mainRect.width / cutsceneData.mainBackground.width, mainRect.height / cutsceneData.mainBackground.height));

		if(connectingActions)
		{
			if(cutsceneActionBeingEdited.myActionType == CutsceneAction.actionType.Condition || (cutsceneActionBeingEdited.myActionType == CutsceneAction.actionType.MultiCondition && editConditionIndex == 0))
			{
				lineStart = SetConditionalConnectionPoint(cutsceneActionBeingEdited, cutsceneActionBeingEdited, editConditionIndex);
			} else
			{
				lineStart = cutsceneActionBeingEdited.editorWindowRect.position;
				lineStart.x += cutsceneActionBeingEdited.editorWindowRect.width / 2;
				lineStart.y += cutsceneActionBeingEdited.editorWindowRect.height;
			}
			lineEnd = Event.current.mousePosition;
			Handles.DrawSolidDisc(lineStart, Vector3.back, 5f);
			Handles.DrawLine(lineStart, lineEnd);
		}

		BeginWindows();
		for(int i=0; i<cutsceneActions.Count; i++)
		{
			cutsceneAction = cutsceneActions[i];

			if(i == activeWindowId)
			{
				Rect highlightRect = cutsceneAction.editorWindowRect;
				highlightRect.width += 4;
				highlightRect.x -= 2;
				highlightRect.height += 4;
				highlightRect.y -= 2;

				Color highlightColor = Color.cyan;
				highlightColor.a = 0.5f;
				EditorGUI.DrawRect(highlightRect, highlightColor);
			}

			cutsceneAction.editorWindowRect = GUI.Window(i, cutsceneAction.editorWindowRect, DrawCutsceneAction, cutsceneAction.myActionType.ToString());

			if(clickHasOccurred)
			{
				if(cutsceneAction.editorWindowRect.Contains(Event.current.mousePosition) && (Event.current.mousePosition.x + PanX) > listRect.width)
				{
					clickedOnWindow = true;
					activeWindowId = i;
					if(connectingActions && cutsceneAction != cutsceneActionBeingEdited)
					{
						if(cutsceneActionBeingEdited.myActionType == CutsceneAction.actionType.Condition || cutsceneActionBeingEdited.indexesOfNextActions.Contains(i) == false)
						{
							UpdateNextAction(cutsceneActionBeingEdited, cutsceneAction.myActionType, i);
							SaveCutsceneData();
						}
					}
				}
			}

			DrawConnections(cutsceneAction);
		}
		EndWindows();

		if(clickHasOccurred && clickedOnWindow == false)
		{
			activeWindowId = -1;
		}

		GUI.EndGroup();

		DrawNewActionButtons();

		if(Event.current.type == EventType.MouseDrag)
		{
			if(Event.current.mousePosition.x > listWidth)
			{
				PanX += Event.current.delta.x;
				PanY += Event.current.delta.y;
			}
		}
	}

	void DrawConnections(CutsceneAction cutsceneAction)
	{
		Vector3 lineStart, lineEnd;
		Color handleColor = Color.white;

		for(int j=0; j<cutsceneAction.indexesOfNextActions.Count; j++)
		{
			if(cutsceneAction.indexesOfNextActions[j] >= 0)
			{
				if(cutsceneAction.myActionType == CutsceneAction.actionType.Condition || (cutsceneAction.myActionType == CutsceneAction.actionType.MultiCondition && j == 0))
				{
					lineStart = SetConditionalConnectionPoint(cutsceneAction, cutsceneActions[cutsceneAction.indexesOfNextActions[j]], j);
				} else
				{
					lineStart = cutsceneAction.editorWindowRect.position;
					lineStart = SetConnectionPoint(cutsceneAction, cutsceneActions[cutsceneAction.indexesOfNextActions[j]]);
				}

				lineEnd = cutsceneActions[cutsceneAction.indexesOfNextActions[j]].editorWindowRect.position;
				lineEnd = SetConnectionPoint(cutsceneActions[cutsceneAction.indexesOfNextActions[j]], cutsceneAction);

				if(activeWindowId == cutsceneActions.IndexOf(cutsceneAction))
				{
					handleColor.a = 1f;
				} else
				{
					handleColor.a = 0.2f;
				}

				Handles.color = handleColor;
				Handles.DrawLine(lineStart, lineEnd);

				if(activeWindowId == cutsceneActions.IndexOf(cutsceneAction))
				{
					handleColor.a = 0.3f;
				} else
				{
					handleColor.a = 0.1f;
				}
				Handles.color = handleColor;
				lineStart.x += 1;
				lineEnd.x += 1;
				Handles.DrawLine(lineStart, lineEnd);
				lineStart.x -= 2;
				lineEnd.x -= 2;
				Handles.DrawLine(lineStart, lineEnd);
				lineStart.x += 1;
				lineEnd.x += 1;

				handleColor.a = 1f;
				Handles.color = handleColor;
				Handles.DrawSolidDisc(lineEnd, Vector3.back, 8f);
				Handles.DrawSolidDisc(lineStart, Vector3.back, 5f);
			}
		}
	}

	void DrawNewActionButtons()
	{
		Rect addActionButtonRect = new Rect(listWidth + 6, 6, 60, 20);

		if(currentCutscene != null)
		{
			Vector2 newWindowPosition = new Vector2((PanX * -1) + (position.width / 2), (PanY * -1) + (position.height / 2 - 100));
			GUIStyle recordIDStyle = new GUIStyle();
			recordIDStyle.normal.textColor = Color.white;

			GUI.Label(addActionButtonRect, "ID: " + currentCutscene.cutsceneID.ToString(), recordIDStyle);

			// Line 1
			addActionButtonRect.x += addActionButtonRect.width + 6;
			addActionButtonRect.width = 90;
			if(GUI.Button(addActionButtonRect,"+ Dialogue"))
			{
				CreateNewCutsceneAction(newWindowPosition.x, newWindowPosition.y, CutsceneAction.actionType.Dialogue);
			}

			addActionButtonRect.x += addActionButtonRect.width + 6;
			if(GUI.Button(addActionButtonRect,"+ Portrait"))
			{
				CreateNewCutsceneAction(newWindowPosition.x, newWindowPosition.y, CutsceneAction.actionType.Portrait);
			}

			addActionButtonRect.x += addActionButtonRect.width + 6;
			if(GUI.Button(addActionButtonRect,"+ Response"))
			{
				CreateNewCutsceneAction(newWindowPosition.x, newWindowPosition.y, CutsceneAction.actionType.Response);
			}

			addActionButtonRect.x += addActionButtonRect.width + 6;
			if(GUI.Button(addActionButtonRect,"+ Animation"))
			{
				CreateNewCutsceneAction(newWindowPosition.x, newWindowPosition.y, CutsceneAction.actionType.Animation);
			}

			addActionButtonRect.x += addActionButtonRect.width + 6;
			if(GUI.Button(addActionButtonRect,"+ Variable"))
			{
				CreateNewCutsceneAction(newWindowPosition.x, newWindowPosition.y, CutsceneAction.actionType.Variable);
			}

			addActionButtonRect.x += addActionButtonRect.width + 6;
			if(GUI.Button(addActionButtonRect,"+ Condition"))
			{
				CreateNewCutsceneAction(newWindowPosition.x, newWindowPosition.y, CutsceneAction.actionType.Condition);
			}

			addActionButtonRect.x += addActionButtonRect.width + 6;
			if(GUI.Button(addActionButtonRect,"+ Multi-Con"))
			{
				CreateNewCutsceneAction(newWindowPosition.x, newWindowPosition.y, CutsceneAction.actionType.MultiCondition);
			}

			addActionButtonRect.x += addActionButtonRect.width + 6;
			if(GUI.Button(addActionButtonRect,"+ Camera"))
			{
				CreateNewCutsceneAction(newWindowPosition.x, newWindowPosition.y, CutsceneAction.actionType.Camera);
			}

			addActionButtonRect.x += addActionButtonRect.width + 6;
			if(GUI.Button(addActionButtonRect,"+ Pivot"))
			{
				CreateNewCutsceneAction(newWindowPosition.x, newWindowPosition.y, CutsceneAction.actionType.Pivot);
			}


			// Line 2
			addActionButtonRect = new Rect(listWidth + 6, 28, 60, 20);
			addActionButtonRect.x += addActionButtonRect.width + 6;
			addActionButtonRect.width = 90;
			if(GUI.Button(addActionButtonRect,"+ Pause"))
			{
				CreateNewCutsceneAction(newWindowPosition.x, newWindowPosition.y, CutsceneAction.actionType.Pause);
			}
		}
	}

	void DrawFolders()
	{
		exitEditMode = false;
		newItemCreated = false;

		GUIStyle foldoutStyle = GUI.skin.GetStyle("Foldout");
		folderStyle.alignment = foldoutStyle.alignment;
		folderStyle.border = foldoutStyle.border;
		folderStyle.contentOffset = foldoutStyle.contentOffset;
		folderStyle.focused = foldoutStyle.focused;
		folderStyle.font = foldoutStyle.font;
		folderStyle.fontSize = foldoutStyle.fontSize;
		folderStyle.hover = foldoutStyle.hover;
		folderStyle.margin = foldoutStyle.margin;
		folderStyle.normal = foldoutStyle.normal;
		folderStyle.onActive = foldoutStyle.onActive;
		folderStyle.fixedWidth = listWidth - (buttonWidth*3) - 15;
		folderStyle.padding = GUI.skin.button.padding;

		headerStyle.fixedWidth = folderStyle.fixedWidth + 19;

		headerStyle.padding = GUI.skin.button.padding;
		dragHandleStyle.padding.top = folderStyle.padding.top;
		dragHandleStyle.padding.bottom = folderStyle.padding.bottom;
		buttonStyle.padding = GUI.skin.button.padding;

		GUI.BeginGroup(listRect);
		//EditorGUI.DrawRect(new Rect(0, 0, listWidth, mainRect.height), Color.black);
		GUI.DrawTextureWithTexCoords(listRect, cutsceneData.listBackground, new Rect(0, 0, listRect.width / cutsceneData.listBackground.width, listRect.height / cutsceneData.listBackground.height));
		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();

		GUILayout.Label("Cutscenes: " + listItems.Count, headerStyle);

		// CutsceneObject = (GameObject)EditorGUILayout.ObjectField("", CutsceneObject, typeof(GameObject), true);
		// currentCutscene.RefName = EditorGUILayout.TextField(currentCutscene.RefName);

		if(GUILayout.Button("N", buttonStyle))
		{
			CreateNewCutscene(0, -1);
		}

		if(GUILayout.Button("+", buttonStyle))
		{
			CreateNewFolder(0, -1);
		}
		GUILayout.EndHorizontal();

		if(!newItemCreated)
		{
			for(int i=0; i<listItems.Count; i++)
			{
				if(listItems[i].parentItemIndex < 0)
				{
					DrawFolder(i);
					if(newItemCreated)
					{
						i++;
					}
				} else
				{
					if(CheckParentFolderState(listItems[i]) == true)
					{
						DrawFolder(i);
						if(newItemCreated)
						{
							i++;
						}
					}
				}
			}
		}

		GUILayout.EndVertical();
		GUI.EndGroup();

		if(Event.current.type == EventType.MouseUp)
		{
			dragItemState = false;
		}
	}

	void CreateNewFolder(int index, int parentIndex)
	{
		//ListItem newItem = new ListItem();
		ListItem newItem = (ListItem)ScriptableObject.CreateInstance<ListItem>();
		newItem.parentItemIndex = parentIndex;
		newItem.itemName = "New";
		newItem.openFolder = false;

		UpdateParentIndexes(index, 1);
		editItemIndex = index;
		newItemCreated = true;

		if(parentIndex >= 0)
		{
			newItem.indentLevel = listItems[parentIndex].indentLevel + 1;
		} else
		{
			newItem.indentLevel = 0;
		}

		AssetDatabase.AddObjectToAsset(newItem, cutsceneData);
		AssetDatabase.SaveAssets();
		listItems.Insert(index, newItem);
		RefreshCutsceneIndexList();
		cutsceneData.listItems = listItems;
		EditorUtility.SetDirty(cutsceneData);
	}

	void CreateNewCutscene(int index, int parentIndex)
	{
		ListItem newItem = (ListItem)ScriptableObject.CreateInstance<ListItem>();
		newItem.parentItemIndex = parentIndex;
		newItem.itemName = "New";
		newItem.openFolder = false;
		newItem.cutscene = (Cutscene)ScriptableObject.CreateInstance("Cutscene");
		newItem.cutscene.PanX = -500000f;
		newItem.cutscene.PanY = -500000f;
		newItem.cutscene.cutsceneID = cutsceneData.nextCutsceneID;
		cutsceneData.nextCutsceneID++;

		CutsceneAction newCutsceneAction = new CutsceneAction();
		Vector2 newWindowPosition = new Vector2((newItem.cutscene.PanX * -1) + (position.width / 2), (newItem.cutscene.PanX * -1) + (position.height / 2 - 100));
		newCutsceneAction.editorWindowRect = new Rect(newWindowPosition.x, newWindowPosition.y, START_ACTION_WIDTH, START_ACTION_HEIGHT);
		newCutsceneAction.myActionType = CutsceneAction.actionType.Start;
		newItem.cutscene.cutsceneActions.Add(newCutsceneAction);

		UpdateParentIndexes(index, 1);
		editItemIndex = index;
		newItemCreated = true;

		if(parentIndex >= 0)
		{
			newItem.indentLevel = listItems[parentIndex].indentLevel + 1;
		} else
		{
			newItem.indentLevel = 0;
		}

		AssetDatabase.AddObjectToAsset(newItem, cutsceneData);
		AssetDatabase.AddObjectToAsset(newItem.cutscene, newItem);
		AssetDatabase.SaveAssets();
		listItems.Insert(index, newItem);
		RefreshCutsceneIndexList();
		cutsceneData.listItems = listItems;
		EditorUtility.SetDirty(cutsceneData);
	}

	void UpdateNextAction(CutsceneAction cutsceneAction, CutsceneAction.actionType cutsceneActionType, int newActionIndex)
	{
		if(cutsceneAction.indexesOfNextActions.Count > 0)
		{
			if(cutsceneAction.myActionType == CutsceneAction.actionType.Condition)
			{
				if(cutsceneActionType != CutsceneAction.actionType.Response)
				{
					cutsceneAction.indexesOfNextActions[editConditionIndex] = newActionIndex;
				}
			} else if(cutsceneAction.myActionType == CutsceneAction.actionType.MultiCondition)
			{
				if(cutsceneActionType != CutsceneAction.actionType.Response)
				{
					cutsceneAction.indexesOfNextActions[editConditionIndex] = newActionIndex;
				}
			} else if(cutsceneActions[cutsceneAction.indexesOfNextActions[0]].myActionType == CutsceneAction.actionType.Response && cutsceneActionType == CutsceneAction.actionType.Response)
			{
				cutsceneAction.indexesOfNextActions.Add(newActionIndex);
			} else
			{
				cutsceneAction.indexesOfNextActions = new List<int>();
				cutsceneAction.indexesOfNextActions.Add(newActionIndex);
			}
		} else
		{
			cutsceneAction.indexesOfNextActions.Add(newActionIndex);
		}
	}

	void DisconnectActions(CutsceneAction cutsceneAction)
	{
		CutsceneAction.actionType actionType = cutsceneAction.myActionType;

		cutsceneAction.indexesOfNextActions = new List<int>();

		if(actionType == CutsceneAction.actionType.Condition || actionType == CutsceneAction.actionType.MultiCondition)
		{
			cutsceneAction.indexesOfNextActions.Add(-1);
			cutsceneAction.indexesOfNextActions.Add(-1);
		}
	}

	Vector3 SetConnectionPoint(CutsceneAction startAction, CutsceneAction endAction)
	{
		Rect startWindow = startAction.editorWindowRect;
		Rect endWindow = endAction.editorWindowRect;

		Vector3 startPosition = startWindow.position;
		Vector3 endPosition = endWindow.position;
		Vector3 resultPosition;

		if(startAction.myActionType == CutsceneAction.actionType.Condition)
		{
			resultPosition = SetConditionalConnectionPoint(startAction, endAction, 0);
		} else
		{
			startPosition.x += startWindow.width / 2;
			startPosition.y += startWindow.height / 2;
			endPosition.x += endWindow.width / 2;
			endPosition.y += endWindow.height / 2;

			resultPosition = startPosition;

			float offsetX = Mathf.Abs(endPosition.x - startPosition.x);
			float offsetY = Mathf.Abs(endPosition.y - startPosition.y);

			if(offsetX > offsetY)
			{
				// endWindow is further from startWindow horizontally more than vertically
				if(endPosition.x > startPosition.x)
				{
					resultPosition.x += startWindow.width / 2;
				} else
				{
					resultPosition.x -= startWindow.width / 2;
				}
			} else
			{
				// endWindow is further from startWindow vertically more than horizontally
				if(endPosition.y > startPosition.y)
				{
					resultPosition.y += startWindow.height / 2;
				} else
				{
					resultPosition.y -= startWindow.height / 2;
				}
			}
		}

		return resultPosition;
	}

	Vector3 SetConditionalConnectionPoint(CutsceneAction startAction, CutsceneAction endAction, int conditionIndex)
	{
		Rect startWindow = startAction.editorWindowRect;
		Rect endWindow = endAction.editorWindowRect;
		Vector3 resultPosition = startWindow.position;

		if(startAction.myActionType == CutsceneAction.actionType.Condition)
		{
			resultPosition.y += 30 + CONDITION_LINE_HEIGHT * conditionIndex;
		} else if(startAction.myActionType == CutsceneAction.actionType.MultiCondition)
		{
			resultPosition.y += 31;
		}

		if(startWindow.x <= endWindow.x)
		{
			resultPosition.x += startWindow.width;
		}

		return resultPosition;
	}

	void LoadCutscene(ListItem selectedListItem)
	{
		currentCutscene = selectedListItem.cutscene;
		cutsceneActions = new List<CutsceneAction>();
		cutsceneActions = currentCutscene.cutsceneActions;
		currentListItem = selectedListItem;
		PanX = currentCutscene.PanX;
		PanY = currentCutscene.PanY;
	}

	void CreateNewCutsceneAction(float startX, float startY, CutsceneAction.actionType newActionType)
	{
		CutsceneAction newCutsceneAction = new CutsceneAction();

		newCutsceneAction.myActionType = newActionType;
		newCutsceneAction.indexesOfNextActions = new List<int>();

		if(newActionType == CutsceneAction.actionType.Start)
		{
			newCutsceneAction.editorWindowRect = new Rect(startX, startY, START_ACTION_WIDTH, START_ACTION_WIDTH);
		} else if(newActionType == CutsceneAction.actionType.Dialogue)
		{
			newCutsceneAction.editorWindowRect = new Rect(startX, startY, DEFAULT_ACTION_WIDTH, DIALOGUE_HEIGHT);
		} else if(newActionType == CutsceneAction.actionType.Portrait)
		{
			newCutsceneAction.editorWindowRect = new Rect(startX, startY, PORTRAIT_WIDTH, PORTRAIT_HEIGHT);
		} else if(newActionType == CutsceneAction.actionType.Animation)
		{
			newCutsceneAction.editorWindowRect = new Rect(startX, startY, ANIMATION_WIDTH, ANIMATION_HEIGHT);
		} else if(newActionType == CutsceneAction.actionType.Response)
		{
			newCutsceneAction.editorWindowRect = new Rect(startX, startY, DEFAULT_ACTION_WIDTH, RESPONSE_HEIGHT);
		} else if(newActionType == CutsceneAction.actionType.Variable)
		{
			newCutsceneAction.editorWindowRect = new Rect(startX, startY, VARIABLE_WIDTH, VARIABLE_HEIGHT);
		} else if(newActionType == CutsceneAction.actionType.Condition)
		{
			newCutsceneAction.editorWindowRect = new Rect(startX, startY, CONDITION_WIDTH, CONDITION_HEIGHT);
			newCutsceneAction.conditionValues = new List<string>();
			newCutsceneAction.indexesOfNextActions.Add(-1);
			AddConditionToAction(newCutsceneAction);
		} else if(newActionType == CutsceneAction.actionType.MultiCondition)
		{
			newCutsceneAction.editorWindowRect = new Rect(startX, startY, MULTICON_WIDTH, MULTICON_HEIGHT);
			newCutsceneAction.conditionVariables = new List<string>();
			newCutsceneAction.conditionValues = new List<string>();
			newCutsceneAction.titleText = "AND";
			newCutsceneAction.indexesOfNextActions.Add(-1);
			newCutsceneAction.indexesOfNextActions.Add(-1);
			AddConditionToAction(newCutsceneAction);
			AddConditionToAction(newCutsceneAction);
		} else if(newActionType == CutsceneAction.actionType.Camera)
		{
			newCutsceneAction.editorWindowRect = new Rect(startX, startY, CAMERA_WIDTH, CAMERA_HEIGHT);
			newCutsceneAction.conditionValues = new List<string>();
			newCutsceneAction.conditionValues.Add("");		// Camera Event to Occur
			newCutsceneAction.conditionValues.Add("");		// New Follow Target
			newCutsceneAction.conditionValues.Add("");		// X Position/Offset
			newCutsceneAction.conditionValues.Add("");		// Y Position/Offset
			newCutsceneAction.conditionValues.Add("");		// Move Speed
		} else if(newActionType == CutsceneAction.actionType.Pivot)
		{
			newCutsceneAction.editorWindowRect = new Rect(startX, startY, PIVOT_WIDTH, PIVOT_HEIGHT);
		} else if(newActionType == CutsceneAction.actionType.Pause)
		{
			newCutsceneAction.editorWindowRect = new Rect(startX, startY, PAUSE_WIDTH, PAUSE_HEIGHT);
		}

		cutsceneActions.Add(newCutsceneAction);
		SaveCutsceneData();
	}

	void DeleteCutsceneAction(CutsceneAction actionToDelete)
	{
		CutsceneAction tempAction;
		CutsceneAction subAction;
		int deleteIndex = cutsceneActions.IndexOf(actionToDelete);

		for(int i=0; i<cutsceneActions.Count; i++)
		{
			tempAction = cutsceneActions[i];
			for(int j=0; j<cutsceneActions[i].indexesOfNextActions.Count; j++)
			{
				if(cutsceneActions[i].indexesOfNextActions[j] >= 0)
				{
					subAction = cutsceneActions[tempAction.indexesOfNextActions[j]];
					if (subAction == actionToDelete)
					{
						if(tempAction.myActionType == CutsceneAction.actionType.Condition || tempAction.myActionType == CutsceneAction.actionType.MultiCondition)
						{
							tempAction.indexesOfNextActions[j] = -1;
						} else
						{
							tempAction.indexesOfNextActions.RemoveAt(j);
							j--;
						}
					} else if (tempAction.indexesOfNextActions[j] > deleteIndex)
					{
						tempAction.indexesOfNextActions[j]--;
					}
				}
			}
		}

		cutsceneActions.Remove(actionToDelete);
		SaveCutsceneData();
	}

	void AddConditionToAction(CutsceneAction cutsceneAction)
	{
		if(cutsceneAction.myActionType == CutsceneAction.actionType.Condition)
		{
			cutsceneAction.indexesOfNextActions.Add(-1);
			cutsceneAction.conditionValues.Add("");
		} else if(cutsceneAction.myActionType == CutsceneAction.actionType.MultiCondition)
		{
			cutsceneAction.conditionVariables.Add("");
			cutsceneAction.conditionValues.Add("");
		} else if(cutsceneAction.myActionType == CutsceneAction.actionType.Response)
		{
			if(cutsceneAction.conditionValues == null)
			{
				cutsceneAction.conditionVariables = new List<string>();
				cutsceneAction.conditionValues = new List<string>();
			}
			cutsceneAction.conditionVariables.Add("");
			cutsceneAction.conditionValues.Add("");
		}

		SaveCutsceneData();
	}

	void ChangeConditionType(CutsceneAction cutsceneAction, string newType)
	{
		cutsceneAction.titleText = newType;
		SaveCutsceneData();
	}

	void DeleteCondition(CutsceneAction cutsceneAction, int indexBeingRemoved)
	{
		cutsceneAction.conditionValues.RemoveAt(indexBeingRemoved);

		if(cutsceneAction.myActionType == CutsceneAction.actionType.Condition)
		{
			cutsceneAction.indexesOfNextActions.RemoveAt(indexBeingRemoved+1);
		} else if (cutsceneAction.myActionType == CutsceneAction.actionType.MultiCondition || cutsceneAction.myActionType == CutsceneAction.actionType.Response)
		{
			cutsceneAction.conditionVariables.RemoveAt(indexBeingRemoved);
		}

		if(cutsceneAction.conditionValues.Count <= 0 && cutsceneAction.myActionType != CutsceneAction.actionType.Response)
		{
			AddConditionToAction(cutsceneAction);
		}

		SaveCutsceneData();
	}

	bool ClearToDeleteListItem(ListItem listItem)
	{
		int listItemIndex = listItems.IndexOf(listItem);
		ListItem childItem;
		bool clearForDeletion = true;

		if(listItem.cutscene != null)
		{
			if(listItem.cutscene.cutsceneActions.Count > 1)
			{
				clearForDeletion = false;
			}
		} else
		{
			for(int i=listItemIndex+1; i<listItems.Count; i++)
			{
				childItem = listItems[i];
				if(childItem.parentItemIndex == listItem.parentItemIndex)
				{
					break;
				} else
				{
					if(childItem.cutscene != null)
					{
						if(childItem.cutscene.cutsceneActions.Count > 1)
						{
							clearForDeletion = false;
						}
					}
				}
			}
		}

		return clearForDeletion;
	}

	void DeleteFolder(int index)
	{
		ListItem itemBeingDeleted = cutsceneData.listItems[index];

		DeleteSubfolders(index);
		if(itemBeingDeleted.cutscene != null)
		{
			UnityEngine.Object.DestroyImmediate(itemBeingDeleted.cutscene, true);
		}
		UpdateParentIndexes(index + 1, -1);

		listItems.Remove(itemBeingDeleted);
		cutsceneData.listItems.Remove(itemBeingDeleted);

		UnityEngine.Object.DestroyImmediate(itemBeingDeleted, true);
		AssetDatabase.SaveAssets();
		EditorUtility.SetDirty(cutsceneData);
		RefreshCutsceneIndexList();
	}

	void DeleteSubfolders(int index)
	{
		for(int i=index+1; i<listItems.Count; i++)
		{
			if(listItems[i].parentItemIndex == index)
			{
				DeleteFolder(i);
				i--;
			}
		}
	}

	void MoveFolder(int insertIndex, int newParentIndex, int newIndentLevel)
	{
		//Debug.Log("Moving: " + itemBeingDragged.itemName + ", Insert: " + insertIndex + ", New Parent: " + newParentIndex + ", New Indent: " + newIndentLevel);
		List<ListItem> movingList = new List<ListItem>();
		int currentIndex = listItems.IndexOf(itemBeingDragged);

		movingList.Add(itemBeingDragged);

		for(int i=currentIndex+1; i<listItems.Count; i++)
		{
			if(listItems[i].indentLevel > itemBeingDragged.indentLevel)
			{
				listItems[i].parentItemIndex = listItems[i].parentItemIndex - currentIndex;
				listItems[i].indentLevel = newIndentLevel + (listItems[i].indentLevel - itemBeingDragged.indentLevel);
				movingList.Add(listItems[i]);
			} else
			{
				break;
			}
		}

		itemBeingDragged.parentItemIndex = newParentIndex;
		itemBeingDragged.indentLevel = newIndentLevel;

		for(int i=0; i<movingList.Count; i++)
		{
			if(insertIndex > currentIndex)
			{
				// If moving the item down the list, bump the insert index down, since the items being moved are about to be temporarily deleted
				insertIndex--;
			}

			if(newParentIndex > currentIndex)
			{
				// If the new parent is further down the list than where the item was before, bump the parent index down, since the items being moved are about to be temporarily deleted
				itemBeingDragged.parentItemIndex--;
			}

			listItems.Remove(movingList[i]);
		}

		UpdateParentIndexes(currentIndex, 0-movingList.Count);
		UpdateParentIndexes(insertIndex, movingList.Count);
		RefreshCutsceneIndexList();

		for(int i=0; i<movingList.Count; i++)
		{
			if(movingList[i].parentItemIndex >= 0 && i>0)
			{
				movingList[i].parentItemIndex += insertIndex;
			}

			if(insertIndex+i < listItems.Count)
			{
				listItems.Insert(insertIndex+i, movingList[i]);
			} else
			{
				listItems.Add(movingList[i]);
			}
		}
	}

	void UpdateParentIndexes(int startIndex, int indexAdjustment)
	{
		for (int i=startIndex; i<listItems.Count; i++)
		{
			if(listItems[i].parentItemIndex >= 0 && listItems[i].parentItemIndex >= startIndex)
			{
				listItems[i].parentItemIndex += indexAdjustment;
			}
		}
	}

	bool CheckParentFolderState(ListItem currentItem)
	{
		if(currentItem.parentItemIndex < 0)
		{
			return true;
		} else
		{
			if(listItems[currentItem.parentItemIndex].openFolder == false)
			{
				return false;
			} else
			{
				return CheckParentFolderState(listItems[currentItem.parentItemIndex]);
			}
		}
	}

	bool CheckIsSubFolder(ListItem subItem, ListItem parentItem)
	{
		if(subItem.parentItemIndex < 0)
		{
			return false;
		} else if(subItem == parentItem)
		{
			return true;
		} else
		{
			if(subItem.parentItemIndex == listItems.IndexOf(parentItem))
			{
				return true;
			} else
			{
				return CheckIsSubFolder(listItems[subItem.parentItemIndex], parentItem);
			}
		}
	}

	void RefreshCutsceneIndexList()
	{
		ListItem listItem;
		cutsceneIndexes = new List<int>();

		for(int i=0; i<cutsceneData.nextCutsceneID; i++)
		{
			cutsceneIndexes.Add(-1);
		}

		for(int i=0; i<listItems.Count; i++)
		{
			listItem = listItems[i];

			if(listItem.cutscene != null)
			{
				cutsceneIndexes[listItem.cutscene.cutsceneID] = i;
			}
		}

		cutsceneData.cutsceneIndexes = cutsceneIndexes;
	}

	void DrawFolder(int index)
	{
		ListItem currentItem = listItems[index];
		Rect lineRect = new Rect();

		folderStyle.fixedWidth -= indentSpace * currentItem.indentLevel;
		GUILayout.BeginHorizontal();

		if(index == editItemIndex)
		{
			Rect nameEditRect = new Rect();
			string currentEventType = Event.current.type.ToString();

			GUILayout.Label(" ");
			nameEditRect.width = 120;
			nameEditRect.height = GUILayoutUtility.GetLastRect().height;
			nameEditRect.y = GUILayoutUtility.GetLastRect().y;
			nameEditRect.x = 10 + (indentSpace * currentItem.indentLevel);

			EditorGUI.BeginChangeCheck();
			currentItem.itemName = GUI.TextField(nameEditRect, currentItem.itemName);

			if(currentEventType == "mouseDown" || currentEventType == "used" || currentEventType == "Ignore")
			{
				if(nameEditRect.Contains(Event.current.mousePosition))
				{
					// Clicked on label being edited, no action
				} else
				{
					editItemIndex = -1;
					exitEditMode = true;
					if(currentItem.itemName == "")
					{
						currentItem.itemName = "Unnamed";
					}
				}
			} else if(currentEventType == "KeyDown")
			{
				if(Event.current.keyCode == KeyCode.Return)
				{
					editItemIndex = -1;
					exitEditMode = true;
					if(currentItem.itemName == "")
					{
						currentItem.itemName = "Unnamed";
					}
				}
			}

			if(EditorGUI.EndChangeCheck())
			{
				cutsceneData.listItems = listItems;
				EditorUtility.SetDirty(cutsceneData);
			}
		} else
		{
			dragHandleStyle.fixedWidth = 1;
			GUILayout.Label(" ", dragHandleStyle);
			lineRect = GUILayoutUtility.GetLastRect();
			lineRect.width = listWidth-1;
			dragHandleStyle.fixedWidth = 10 + (indentSpace * currentItem.indentLevel);

			if(currentCutscene != null)
			{
				if (currentCutscene == currentItem.cutscene)
				{
					Color selectedItemColor = Color.black;
					selectedItemColor.a = 0.2f;
					EditorGUI.DrawRect(lineRect, selectedItemColor);
				}
			}

			if(lineRect.Contains(Event.current.mousePosition) && dragItemState == false && editItemIndex < 0)
			{
				GUILayout.Label(" ||", dragHandleStyle);
				Rect handleRect = GUILayoutUtility.GetLastRect();
				handleRect.width = dragHandleStyle.fixedWidth;
				handleRect.height = lineRect.height;

				if(handleRect.Contains(Event.current.mousePosition))
				{
					if(Event.current.type == EventType.MouseDown)
					{
						itemBeingDragged = listItems[index];
						dragItemState = true;
					}
				}
			} else
			{
				GUILayout.Space(dragHandleStyle.fixedWidth);
			}

			dragHandleStyle.fixedWidth = 10;

			EditorGUI.BeginChangeCheck();

			if(currentItem.cutscene == null)
			{
				currentItem.openFolder = GUILayout.Toggle(currentItem.openFolder, "  " + currentItem.itemName, folderStyle, GUILayout.ExpandWidth(false));
			} else
			{
				GUIStyle cutsceneStyle = new GUIStyle();
				cutsceneStyle.fixedWidth = folderStyle.fixedWidth + 8;
				cutsceneStyle.padding = folderStyle.padding;

				if(GUILayout.Button(currentItem.itemName, cutsceneStyle))
				{
					if(Event.current.button == 0)
					{
						LoadCutscene(currentItem);
					}
				}
			}

			if(dragItemState == true)
			{
				Rect folderRect = GUILayoutUtility.GetLastRect();
				Vector3 mousePosition = Event.current.mousePosition;

				if (currentItem.cutscene != null)
				{
					folderRect.y += 2;
				}

				if(folderRect.Contains(mousePosition) && itemBeingDragged != currentItem)
				{
					bool validItemForMovement = true;

					if(CheckIsSubFolder(currentItem, itemBeingDragged))
					{
						validItemForMovement = false;
					}

					if(validItemForMovement)
					{
						float rectPortion = folderRect.height / 3;

						if(mousePosition.y < folderRect.y + rectPortion)
						{
							// Top
							Handles.color = Color.black;
							Handles.DrawLine(folderRect.position, new Vector3(folderRect.position.x + folderRect.width, folderRect.y, 0));
							if(Event.current.type == EventType.MouseUp)
							{
								MoveFolder(index, currentItem.parentItemIndex, currentItem.indentLevel);
							}
						} else if(mousePosition.y > folderRect.y + (rectPortion * 2))
						{
							// Bottom
							Handles.color = Color.black;
							Handles.DrawLine(new Vector3(folderRect.position.x, folderRect.position.y + folderRect.height, 0), new Vector3(folderRect.position.x + folderRect.width, folderRect.position.y + folderRect.height, 0));
							if(Event.current.type == EventType.MouseUp)
							{
								int insertIndex = -1;
								for(int i=index + 1; i < listItems.Count; i++)
								{
									if(listItems[i].indentLevel <= currentItem.indentLevel)
									{
										insertIndex = i;
										break;
									}
								}

								if(insertIndex < 0)
								{
									insertIndex = listItems.Count;
								}

								MoveFolder(insertIndex, currentItem.parentItemIndex, currentItem.indentLevel);
							}
						} else
						{
							// Middle
							if(currentItem.cutscene == null)
							{
								Handles.color = Color.black;
								Handles.DrawLine(new Vector3(folderRect.position.x - 20, folderRect.position.y + folderRect.height/2), new Vector3(folderRect.position.x, folderRect.position.y + folderRect.height/2));
								if(Event.current.type == EventType.MouseUp)
								{
									MoveFolder(index + 1, index, currentItem.indentLevel + 1);
								}
							}
						}
					}
				}
			} else
			{
				if(editItemIndex < 0 && exitEditMode == false)
				{
					if(currentItem.cutscene == null)
					{
						if(GUILayout.Button("N", buttonStyle))
						{
							currentItem.openFolder = true;
							CreateNewCutscene(index + 1, index);
						}

						if(GUILayout.Button("+", buttonStyle))
						{
							currentItem.openFolder = true;
							CreateNewFolder(index + 1, index);
						}
					} else
					{
						GUILayout.Space(buttonStyle.fixedWidth*2);
					}

					if(GUILayout.Button("-", buttonStyle))
					{
						if(ClearToDeleteListItem(currentItem))
						{
							DeleteFolder(index);
						} else
						{
							if(EditorUtility.DisplayDialog("Delete Cutscenes?", "This list item contains at least one cutscene with actions, are you sure you want to delete it?", "Yes", "No" ) == true)
							{
								DeleteFolder(index);
							}
						}
					}
				}
			}

			if(EditorGUI.EndChangeCheck())
			{
				if(editItemIndex == -2)
				{
					editItemIndex = index;
					currentItem.openFolder = !currentItem.openFolder;
				}
				cutsceneData.listItems = listItems;
				EditorUtility.SetDirty(cutsceneData);
			}

			folderStyle.fixedWidth += indentSpace * currentItem.indentLevel;
		}
		GUILayout.EndHorizontal();
	}

	void DrawCameraAction(CutsceneAction cutsceneAction)
	{
		float lineYPosition = 20f;
		float labelWidth = 14f;
		float xyWidth = 52f;
		float textWidth = 120f;
		float labelHeight = 16f;
		float buttonWidth = 58f;
		float xBasePosition = 110f;
		string cameraEvent = cutsceneAction.conditionValues[0];
		string followTarget = cutsceneAction.conditionValues[1];
		string xPos = cutsceneAction.conditionValues[2];
		string yPos = cutsceneAction.conditionValues[3];
		string cameraSpeed = cutsceneAction.conditionValues[4];
		bool waitForCompletion = cutsceneAction.rightActor;
		Rect tempRect = new Rect(5, lineYPosition, buttonWidth, labelHeight);

		//Handles.color = Color.black;
		//Handles.DrawLine(new Vector3(0, yPosition + 60, 0), new Vector3(250, yPosition + 60, 0));

		// Event
		tempRect = new Rect(5, lineYPosition, 110, labelHeight);
		EditorGUI.LabelField(tempRect, "Event");
		tempRect = new Rect(xBasePosition, lineYPosition, textWidth, labelHeight);
		cameraEvent = EditorGUI.TextField(tempRect, cameraEvent);

		// Follow Target
		lineYPosition += labelHeight + 3;
		tempRect = new Rect(5, lineYPosition, 110, labelHeight);
		EditorGUI.LabelField(tempRect, "Follow Target");
		tempRect = new Rect(xBasePosition, lineYPosition, textWidth, labelHeight);
		followTarget = EditorGUI.TextField(tempRect, followTarget);

		// Position/offset
		lineYPosition += labelHeight + 3;
		tempRect = new Rect(5, lineYPosition, 110, labelHeight);
		EditorGUI.LabelField(tempRect, "Position/Offset");

		// Position/Offset: X
		tempRect = new Rect(xBasePosition - labelWidth, lineYPosition, labelWidth, labelHeight);
		EditorGUI.LabelField(tempRect, "X");
		tempRect = new Rect(xBasePosition, lineYPosition, xyWidth, labelHeight);
		xPos = EditorGUI.TextField(tempRect, xPos);

		// Position/Offset: Y
		tempRect = new Rect(xBasePosition + 2 + xyWidth, lineYPosition, labelWidth, labelHeight);
		EditorGUI.LabelField(tempRect, "Y");
		tempRect = new Rect(xBasePosition + 2 + labelWidth + xyWidth, lineYPosition, xyWidth, labelHeight);
		yPos = EditorGUI.TextField(tempRect, yPos);

		// Speed
		lineYPosition += labelHeight + 3;
		tempRect = new Rect(5, lineYPosition, 110, labelHeight);
		EditorGUI.LabelField(tempRect, "Speed");
		tempRect = new Rect(xBasePosition, lineYPosition, xyWidth, labelHeight);
		cameraSpeed = EditorGUI.TextField(tempRect, cameraSpeed);

		// Wait
		lineYPosition += labelHeight + 3;
		tempRect = new Rect(5, lineYPosition, 110, labelHeight);
		EditorGUI.LabelField(tempRect, "Wait");
		tempRect = new Rect(xBasePosition, lineYPosition, xyWidth, labelHeight);
		waitForCompletion = EditorGUI.Toggle(tempRect, waitForCompletion);


		cutsceneAction.conditionValues[0] = cameraEvent;
		cutsceneAction.conditionValues[1] = followTarget;
		cutsceneAction.conditionValues[2] = xPos;
		cutsceneAction.conditionValues[3] = yPos;
		cutsceneAction.conditionValues[4] = cameraSpeed;
		cutsceneAction.rightActor = waitForCompletion;

		EditorGUILayout.LabelField(" ");
		EditorGUILayout.LabelField(" ");
		EditorGUILayout.LabelField(" ");
		EditorGUILayout.LabelField(" ");
		EditorGUILayout.LabelField(" ");
		EditorGUILayout.LabelField(" ");
	}

	bool CheckIfClickIsInEditorWindow(float windowX, float mouseX)
	{
		// Only works properly when called from a window within the cutscene editor, since the Event.current is fired local to the window
		if(windowX + PanX + mouseX > listRect.width)
		{
			return true;
		} else
		{
			return false;
		}
	}

	public void DrawCutsceneAction(int id)
	{
		float windowHeight;
		GUIStyle defaultLabelStyle = GUI.skin.label;
		GUIStyle labelStyle = new GUIStyle();
		labelStyle.alignment = defaultLabelStyle.alignment;
		labelStyle.border = defaultLabelStyle.border;
		labelStyle.contentOffset = defaultLabelStyle.contentOffset;
		labelStyle.focused = defaultLabelStyle.focused;
		labelStyle.font = defaultLabelStyle.font;
		labelStyle.fontSize = defaultLabelStyle.fontSize;
		labelStyle.hover = defaultLabelStyle.hover;
		labelStyle.margin = defaultLabelStyle.margin;
		labelStyle.normal = defaultLabelStyle.normal;
		labelStyle.onActive = defaultLabelStyle.onActive;
		labelStyle.fixedWidth = defaultLabelStyle.fixedWidth;

		labelStyle.padding = GUI.skin.button.padding;
		GUIStyle textAreaStyle = GUI.skin.textArea;
		bool showConditions = false;

		if(cutsceneActions[id].myActionType == CutsceneAction.actionType.Start)
		{
			//GUILayout.Label("Start",labelStyle);
		} else if(cutsceneActions[id].myActionType == CutsceneAction.actionType.Pivot)
		{
			// Placeholder for if anything is added later
		} else if(cutsceneActions[id].myActionType == CutsceneAction.actionType.Dialogue)
		{
			labelStyle.fixedWidth = 45;
			EditorGUILayout.BeginHorizontal();
			EditorStyles.textField.wordWrap = false;
			GUILayout.Label("Actor",labelStyle);
			cutsceneActions[id].titleText = EditorGUILayout.TextField(cutsceneActions[id].titleText, GUILayout.ExpandHeight(false), GUILayout.MinWidth(DEFAULT_ACTION_WIDTH - labelStyle.fixedWidth - 100));
			
			//GUILayout.Label("Right Side", null);
			GUILayout.Label("Right Side", labelStyle);

			//cutsceneActions[id].rightActor = EditorGUILayout.Toggle(cutsceneActions[id].rightActor, null);
			cutsceneActions[id].rightActor = EditorGUILayout.Toggle(cutsceneActions[id].rightActor, labelStyle);

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Text",labelStyle);
			EditorStyles.textField.wordWrap = true;
			textAreaStyle.fixedWidth = DEFAULT_ACTION_WIDTH - labelStyle.fixedWidth - 14;
			cutsceneActions[id].textContent = EditorGUILayout.TextArea(cutsceneActions[id].textContent, textAreaStyle, GUILayout.ExpandHeight(true));

			GUIContent textContent = new GUIContent();
			textContent.text = cutsceneActions[id].textContent;

			float textHeight = textAreaStyle.CalcHeight(textContent, textAreaStyle.fixedWidth);
			if(textHeight > (13 * 3) + 5)
			{
				windowHeight = DIALOGUE_HEIGHT + (textHeight - (13 * 3));
			} else
			{
				windowHeight = DIALOGUE_HEIGHT;
			}
			cutsceneActions[id].editorWindowRect.height = windowHeight;
			EditorGUILayout.EndHorizontal();
		} else if(cutsceneActions[id].myActionType == CutsceneAction.actionType.Animation)
		{
			labelStyle.fixedWidth = 70;
			EditorGUILayout.BeginHorizontal();
			EditorStyles.textField.wordWrap = false;
			GUILayout.Label("Actor",labelStyle);
			cutsceneActions[id].titleText = EditorGUILayout.TextField(cutsceneActions[id].titleText, GUILayout.ExpandHeight(false));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Animation",labelStyle);
			cutsceneActions[id].textContent = EditorGUILayout.TextField(cutsceneActions[id].textContent, GUILayout.ExpandHeight(false));
			EditorGUILayout.EndHorizontal();
		} else if(cutsceneActions[id].myActionType == CutsceneAction.actionType.Pause)
		{
			labelStyle.fixedWidth = 55;
			EditorGUILayout.BeginHorizontal();
			EditorStyles.textField.wordWrap = false;
			GUILayout.Label("Seconds",labelStyle);
			cutsceneActions[id].titleText = EditorGUILayout.TextField(cutsceneActions[id].titleText, GUILayout.ExpandHeight(false));
			EditorGUILayout.EndHorizontal();
		} else if(cutsceneActions[id].myActionType == CutsceneAction.actionType.Portrait)
		{
			labelStyle.fixedWidth = 70;
			EditorGUILayout.BeginHorizontal();
			EditorStyles.textField.wordWrap = false;
			GUILayout.Label("Character",labelStyle);
			cutsceneActions[id].titleText = EditorGUILayout.TextField(cutsceneActions[id].titleText, GUILayout.ExpandHeight(false));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Expression",labelStyle);
			cutsceneActions[id].textContent = EditorGUILayout.TextField(cutsceneActions[id].textContent, GUILayout.ExpandHeight(false));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Right Side", labelStyle);

			//cutsceneActions[id].rightActor = EditorGUILayout.Toggle(cutsceneActions[id].rightActor, null);
			cutsceneActions[id].rightActor = EditorGUILayout.Toggle(cutsceneActions[id].rightActor, labelStyle);

			EditorGUILayout.EndHorizontal();
		} else if(cutsceneActions[id].myActionType == CutsceneAction.actionType.Response)
		{
			labelStyle.fixedWidth = 68;
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Response",labelStyle);
			cutsceneActions[id].textContent = EditorGUILayout.TextField(cutsceneActions[id].textContent, GUILayout.ExpandHeight(false));
			EditorGUILayout.EndHorizontal();

			windowHeight = RESPONSE_HEIGHT;
			if(cutsceneActions[id].conditionValues == null)
			{
				if(GUILayout.Button("Add Conditions"))
				{
					if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
					{
						AddConditionToAction(cutsceneActions[id]);
						cutsceneActions[id].titleText = "AND";
					}
				}
			} else
			{
				if(cutsceneActions[id].conditionValues.Count <= 0)
				{
					if(GUILayout.Button("Add Conditions"))
					{
						if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
						{
							AddConditionToAction(cutsceneActions[id]);
							cutsceneActions[id].titleText = "AND";
						}
					}
				} else
				{
					windowHeight += cutsceneActions[id].conditionValues.Count * 20;
					showConditions = true;
				}
			}
			cutsceneActions[id].editorWindowRect.height = windowHeight;
		} else if(cutsceneActions[id].myActionType == CutsceneAction.actionType.Variable)
		{
			labelStyle.fixedWidth = 65;
			EditorGUILayout.BeginHorizontal();
			EditorStyles.textField.wordWrap = false;
			GUILayout.Label("Variable",labelStyle);
			cutsceneActions[id].titleText = EditorGUILayout.TextField(cutsceneActions[id].titleText, GUILayout.ExpandHeight(false));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Value",labelStyle);
			cutsceneActions[id].textContent = EditorGUILayout.TextField(cutsceneActions[id].textContent, GUILayout.ExpandHeight(false));
			EditorGUILayout.EndHorizontal();
		} else if(cutsceneActions[id].myActionType == CutsceneAction.actionType.Condition)
		{
			labelStyle.fixedWidth = 60;
			EditorGUILayout.BeginHorizontal();
			EditorStyles.textField.wordWrap = false;
			GUILayout.Label("Variable",labelStyle);
			cutsceneActions[id].titleText = EditorGUILayout.TextField(cutsceneActions[id].titleText, GUILayout.ExpandHeight(false));

			if(GUILayout.Button("Else"))
			{
				if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
				{
					cutsceneActionBeingEdited = cutsceneActions[id];
					editConditionIndex = 0;
					connectingActions = true;
				}
			}

			EditorGUILayout.EndHorizontal();

			windowHeight = CONDITION_HEIGHT;
			if(cutsceneActions[id].conditionValues.Count > 1)
			{
				windowHeight += ((cutsceneActions[id].conditionValues.Count - 1) * CONDITION_LINE_HEIGHT);
			}
			cutsceneActions[id].editorWindowRect.height = windowHeight;

			for(int i=0; i<cutsceneActions[id].conditionValues.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("  Value",labelStyle);

				EditorGUI.BeginChangeCheck();
				Rect tempRect = new Rect();
				tempRect.x = labelStyle.fixedWidth + 9;
				tempRect.y = 41 + i*CONDITION_LINE_HEIGHT;
				tempRect.height = CONDITION_LINE_HEIGHT - 2;
				tempRect.width = CONDITION_WIDTH - labelStyle.fixedWidth - 60;
				cutsceneActions[id].conditionValues[i] = GUI.TextField(tempRect, cutsceneActions[id].conditionValues[i]);

				GUILayout.Space(tempRect.width + 2);

				if(EditorGUI.EndChangeCheck())
				{
					SaveCutsceneData();
				}

				if(GUILayout.Button("-"))
				{
					if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
					{
						DeleteCondition(cutsceneActions[id], i);
					}
				}

				if(GUILayout.Button("*"))
				{
					if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
					{
						editConditionIndex = i+1;
						cutsceneActionBeingEdited = cutsceneActions[id];
						connectingActions = true;
					}
				}
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(2);
			}
		} else if(cutsceneActions[id].myActionType == CutsceneAction.actionType.MultiCondition)
		{
			showConditions = true;
			windowHeight = MULTICON_HEIGHT;
			if(cutsceneActions[id].conditionValues.Count > 1)
			{
				windowHeight += ((cutsceneActions[id].conditionValues.Count - 1) * MULTICON_LINE_HEIGHT);
			}
			cutsceneActions[id].editorWindowRect.height = windowHeight;
		} else if(cutsceneActions[id].myActionType == CutsceneAction.actionType.Camera)
		{
			DrawCameraAction(cutsceneActions[id]);
		}

		if(showConditions)
		{
			EditorStyles.textField.wordWrap = false;
			EditorGUILayout.BeginHorizontal();
			labelStyle.fixedWidth = 40;
			GUILayout.Label("Type",labelStyle);
			labelStyle.fixedWidth = 45;
			GUILayout.Label(cutsceneActions[id].titleText,labelStyle);

			if(GUILayout.Button("AND"))
			{
				if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
				{
					ChangeConditionType(cutsceneActions[id],"AND");
				}
			}

			if(GUILayout.Button("OR"))
			{
				if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
				{
					ChangeConditionType(cutsceneActions[id],"OR");
				}
			}

			if(GUILayout.Button("NAND"))
			{
				if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
				{
					ChangeConditionType(cutsceneActions[id],"NAND");
				}
			}

			if(GUILayout.Button("XOR"))
			{
				if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
				{
					ChangeConditionType(cutsceneActions[id],"XOR");
				}
			}

			if(cutsceneActions[id].myActionType != CutsceneAction.actionType.Response)
			{
				if(GUILayout.Button("Else"))
				{
					if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
					{
						editConditionIndex = 0;
						cutsceneActionBeingEdited = cutsceneActions[id];
						connectingActions = true;
					}
				}
			}

			EditorGUILayout.EndHorizontal();
			GUILayout.Space(3);
			labelStyle.fixedWidth = 33;

			for(int i=0; i<cutsceneActions[id].conditionValues.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();

				EditorGUI.BeginChangeCheck();
				Rect tempRect = new Rect();

				GUILayout.Label("Var",labelStyle);
				tempRect.x = labelStyle.fixedWidth + 6;

				tempRect.y = 44 + i*20;
				if(cutsceneActions[id].myActionType == CutsceneAction.actionType.Response)
				{
					tempRect.y += 20;
				}

				tempRect.height = MULTICON_LINE_HEIGHT - 2;
				tempRect.width = ((MULTICON_WIDTH - (2*labelStyle.fixedWidth)) / 2) - 20;
				cutsceneActions[id].conditionVariables[i] = GUI.TextField(tempRect, cutsceneActions[id].conditionVariables[i]);
				GUILayout.Space(tempRect.width + 2);

				GUILayout.Label("Val",labelStyle);

				tempRect.x += tempRect.width + labelStyle.fixedWidth + 6;
				cutsceneActions[id].conditionValues[i] = GUI.TextField(tempRect, cutsceneActions[id].conditionValues[i]);
				GUILayout.Space(tempRect.width + 2);

				if(EditorGUI.EndChangeCheck())
				{
					SaveCutsceneData();
				}

				if(GUILayout.Button("-"))
				{
					if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
					{
						DeleteCondition(cutsceneActions[id], i);
					}
				}

				EditorGUILayout.EndHorizontal();
			}

			GUILayout.Space(2);
		}

		EditorGUILayout.BeginHorizontal();
		if(cutsceneActions[id].myActionType == CutsceneAction.actionType.Condition || showConditions)
		{
			if(GUILayout.Button("+"))
			{
				if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
				{
					AddConditionToAction(cutsceneActions[id]);
				}
			}
		}

		if(cutsceneActions[id].myActionType != CutsceneAction.actionType.Condition)
		{
			if(GUILayout.Button("*"))
			{
				if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
				{
					editConditionIndex = 1;
					cutsceneActionBeingEdited = cutsceneActions[id];
					connectingActions = true;
				}
			}

			if(GUILayout.Button("DC"))
			{
				if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
				{
					DisconnectActions(cutsceneActions[id]);
				}
			}
		}

		if(cutsceneActions[id].myActionType != CutsceneAction.actionType.Start)
		{
			if(GUILayout.Button("X"))
			{
				if(CheckIfClickIsInEditorWindow(cutsceneActions[id].editorWindowRect.x, Event.current.mousePosition.x))
				{
					if(activeWindowId == id)
					{
						activeWindowId = -1;
					}

					DeleteCutsceneAction(cutsceneActions[id]);
					return;
				}
			}
		}
		EditorGUILayout.EndHorizontal();

		if(GUIUtility.GUIToScreenPoint(Event.current.mousePosition).x - position.x > listWidth)
		{
			GUI.DragWindow();
		}
	}
}
