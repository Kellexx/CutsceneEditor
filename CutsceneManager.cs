using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;

public class CutsceneManager : MonoBehaviourPun
{
	private PlayerController playerController;
	private PlayerManager playerManager;
	private AffectionTracker affectionTracker;
	[SerializeField]
	private int playerNumber;
	private GameManager gameManager;
	private DynamicReference dynamicReference;
	public CameraController cameraController;
	private ObjectDetector objectDetector;
	[SerializeField]
	private DialogWindow dialogWindow;
	//[SerializeField]
	//private CameraScreen cameraScreen;
	[SerializeField]
	private bool cutsceneInProgress;
	public CutsceneData cutsceneData; // Must be serialized to assign the data properly
	private Cutscene currentCutscene;
	private CutsceneAction currentAction;
	private List<CutsceneAction> responseActions;

	private bool waitingOnInput;
	private int selectionIndex;
	private bool readyToContinue;
	private float timeOfLastChange;
	private float shiftDelay;
	private bool monitorScrollState;
	private float timeOfPause;
	private float pauseLength;


	void Start () 
	{
		InitializeProperties();
	}
	
	void Update () 
	{
		if(cutsceneInProgress)
		{
			CheckScrollState();
			if(waitingOnInput)
			{
				UpdateResponseSelection();
			} else if (timeOfPause > 0)
			{
				CheckPause();
			}
		}
	}

	public bool CutsceneIsInProgress()
	{
		return cutsceneInProgress;
	}

	void InitializeProperties()
	{
		playerController = GetComponent<PlayerController>();
		playerManager = GetComponent<PlayerManager>();
		affectionTracker = GetComponent<AffectionTracker>();
		playerNumber = playerManager.GetPlayerNumber();

		gameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();
		dynamicReference = gameManager.GetComponent<DynamicReference>();

		objectDetector = transform.Find("Character").Find("Object Detector").GetComponent<ObjectDetector>();

		AssignDialogWindow();

		cutsceneInProgress = false;
		waitingOnInput = false;
		timeOfPause = 0;
		pauseLength = 0;
		selectionIndex = 0;
		shiftDelay = 0.25f;
		readyToContinue = true;
		monitorScrollState = false;
		responseActions = new List<CutsceneAction>();
	}

	public void AssignDialogWindow()
	{
		// Called during initialization

		if(gameManager.GetNumberOfLocalPlayers() > 1)
		{
			dialogWindow = GameObject.Find("Dialog " + playerNumber).GetComponent<DialogWindow>();
			//cameraScreen = GameObject.Find("Screen " + playerNumber).GetComponent<CameraScreen>();
		} else
		{
			dialogWindow = GameObject.Find("Dialog Full").GetComponent<DialogWindow>();
			//cameraScreen = GameObject.Find("Screen Full").GetComponent<CameraScreen>();
		}

		if(cameraController != null)
		{
			cameraController.SetViewport(playerNumber, gameManager.GetNumberOfLocalPlayers());
		}
	}

	public void InteractPressed()
	{
		// Called from PlayerController when the interact button is pressed

		if(cutsceneInProgress)
		{
			if(waitingOnInput)
			{
				ExecuteResponse();
			} else if(readyToContinue)
			{
				GoToNextAction();
			} else
			{
				SkipAction();
			}
		} else
		{
			StartDetectedCutscene();
		}
	}

	void StartDetectedCutscene()
	{
		GameObject detectedObject = objectDetector.objectOfInterest;

		if(detectedObject)
		{
			CutsceneHolder detectedCutsceneHolder = detectedObject.GetComponent<CutsceneHolder>();
			if(detectedCutsceneHolder)
			{
				StartCutscene(detectedCutsceneHolder.cutsceneID);
			}
		}
	}

	public void StartCutscene(int cutsceneID)
	{
		if(cutsceneInProgress)
		{
			Debug.LogWarning("Cannot start new cutscene while another cutscene is in progress");
		} else
		{
			int cutsceneIndex = GetCutsceneIndex(cutsceneID);

			if(cutsceneIndex >= 0)
			{
				playerManager.CloseAllMenus();
				playerManager.SetHotbarVisibility(false);
				playerController.SetCutsceneState(true);
				cutsceneInProgress = true;
				waitingOnInput = false;
				responseActions = null;
				selectionIndex = 1;
				dialogWindow.SetHighlightRow(selectionIndex);
				dialogWindow.ResetText();
				currentCutscene = cutsceneData.listItems[cutsceneIndex].cutscene;
				currentAction = currentCutscene.cutsceneActions[0];
				InterpretCurrentAction();
			} else
			{
				Debug.LogError("Cutscene ID " + cutsceneID + " not found in list of indexes");
			}
		}
	}

	void EndCutscene()
	{
		dialogWindow.SetVisibility(false);
		dialogWindow.ClearPortrait(true);
		dialogWindow.ClearPortrait(false);
		playerManager.SetHotbarVisibility(true);
		currentCutscene = null;
		currentAction = null;
		cutsceneInProgress = false;
		waitingOnInput = false;
		selectionIndex = 0;
		playerController.SetCutsceneState(false);
		cameraController.ResetView();
		//cameraScreen.ResetValues();
	}

	void GoToNextAction()
	{
		waitingOnInput = false;

		if(currentAction.indexesOfNextActions.Count > 0)
		{
			if(currentCutscene.cutsceneActions[currentAction.indexesOfNextActions[0]].myActionType == CutsceneAction.actionType.Response)
			{
				SetResponses(currentAction);
			} else
			{
				currentAction = currentCutscene.cutsceneActions[currentAction.indexesOfNextActions[0]];
				InterpretCurrentAction();
			}
		} else
		{
			EndCutscene();
		}
	}

	void SkipAction()
	{
		if(currentAction.myActionType == CutsceneAction.actionType.Dialogue)
		{
			dialogWindow.SkipScrollingText();
			readyToContinue = true;
		}
	}

	int GetCutsceneIndex(int cutsceneID)
	{
		if(cutsceneID < cutsceneData.nextCutsceneID)
		{
			return cutsceneData.cutsceneIndexes[cutsceneID];
		} else
		{
			return -1;
		}
	}

	string CheckDynamicNames(string lookupName)
	{
		string newName = lookupName;

		if(lookupName == "Player")
		{
			// TODO: Get player's character name
		} else if(lookupName == "Test")
		{
			newName = "Bob the Test";
		}

		return newName;
	}

	void CheckScrollState()
	{
		if(monitorScrollState)
		{
			if(!dialogWindow.GetScrollState())
			{
				monitorScrollState = false;
				readyToContinue = true;
			}
		}
	}

	void InterpretCurrentAction()
	{
		if(currentAction == null)
		{
			Debug.LogError("Invalid cutscene action called");
		} else
		{
			string actionType = currentAction.myActionType.ToString();

			if(actionType == "Start")
			{
				GoToNextAction();
			} else if(actionType == "Pivot")
			{
				GoToNextAction();
			} else if(actionType == "Dialogue")
			{
				InterpretDialogue(currentAction);
			} else if(actionType == "Portrait")
			{
				InterpretPortrait(currentAction);
			} else if(actionType == "Response")
			{
				// Responses are initiated from GoToNextAction() and updated from Update(), no action needed here
			} else if(actionType == "Animation")
			{
				InterpretAnimation(currentAction);
			} else if(actionType == "Variable")
			{
				InterpretVariable(currentAction);
			} else if(actionType == "Condition")
			{
				InterpretCondition(currentAction);
			} else if(actionType == "MultiCondition")
			{
				InterpretMultiCondition(currentAction);
			} else if(actionType == "Camera")
			{
				InterpretCameraAction(currentAction);
			} else if(actionType == "Pause")
			{
				InterpretPauseAction(currentAction);
			} else
			{
				Debug.LogWarning("Unhandled action of type: " + actionType);
				EndCutscene();
			}
		}
	}

	void InterpretDialogue(CutsceneAction cutsceneAction)
	{
		string newDialogueLines = playerManager.RewriteString(cutsceneAction.textContent);
		string newSpeakerName = playerManager.RewriteString(cutsceneAction.titleText);

		readyToContinue = false;
		monitorScrollState = true;
		timeOfLastChange = Time.time;
		dialogWindow.SetMessage(newSpeakerName, newDialogueLines, cutsceneAction.rightActor);
	}

	bool CheckAllConditions(CutsceneAction cutsceneAction)
	{
		int numberOfConditionsMet = 0;
		bool conditionsMet = false;

		if(cutsceneAction.conditionValues == null)
		{
			conditionsMet = true;
		} else
		{
			if(cutsceneAction.conditionValues.Count == 0)
			{
				// It shouldn't be possible for this to happen - condition values found, but count is 0
				conditionsMet = true;
			} else
			{
				for(int j=0; j<cutsceneAction.conditionValues.Count; j++)
				{
					if(playerManager.CheckDynamicCondition(cutsceneAction.conditionVariables[j], cutsceneAction.conditionValues[j]))
					{
						numberOfConditionsMet++;
					}
				}

				if(cutsceneAction.titleText == "AND" && numberOfConditionsMet == cutsceneAction.conditionValues.Count)
				{
					conditionsMet = true;
				} else if(cutsceneAction.titleText == "OR" && numberOfConditionsMet > 0)
				{
					conditionsMet = true;
				} else if(cutsceneAction.titleText == "NAND" && numberOfConditionsMet == 0)
				{
					conditionsMet = true;
				} else if(cutsceneAction.titleText == "XOR" && numberOfConditionsMet == 1)
				{
					conditionsMet = true;
				}
			}
		}

		return conditionsMet;
	}

	void InterpretCondition(CutsceneAction cutsceneAction)
	{
		string variableName = cutsceneAction.titleText;
		int conditionIndex = 0;

		for(int i=0; i<cutsceneAction.conditionValues.Count; i++)
		{
			if(playerManager.CheckDynamicCondition(variableName, cutsceneAction.conditionValues[i]))
			{
				conditionIndex = i+1;
				break;
			}
		}

		if(conditionIndex < currentCutscene.cutsceneActions.Count && currentAction.indexesOfNextActions[conditionIndex] >= 0)
		{
			currentAction = currentCutscene.cutsceneActions[currentAction.indexesOfNextActions[conditionIndex]];
			InterpretCurrentAction();
		} else
		{
			EndCutscene();
		}
	}

	void InterpretMultiCondition(CutsceneAction cutsceneAction)
	{
		if(CheckAllConditions(cutsceneAction))
		{
			currentAction = currentCutscene.cutsceneActions[cutsceneAction.indexesOfNextActions[1]];
			InterpretCurrentAction();
		} else
		{
			if(cutsceneAction.indexesOfNextActions[0] > 0)
			{
				currentAction = currentCutscene.cutsceneActions[cutsceneAction.indexesOfNextActions[0]];
				InterpretCurrentAction();
			} else
			{
				// No else path available
				EndCutscene();
			}
		}
	}

	void InterpretVariable(CutsceneAction cutsceneAction)
	{
		string variableName = cutsceneAction.titleText;
		string variableValue = cutsceneAction.textContent;
		object parentObject = this;
		bool affectionVariable = false;

		// Special rules using . to indicate that the variable being changed is on something other than the cutscene manager
		if(variableName.Contains("."))
		{
			int indexOfDot = variableName.IndexOf(".");
			string variablePrefix = variableName.Substring(0, indexOfDot).ToLower();
			variableName = variableName.Substring(indexOfDot+1, variableName.Length-indexOfDot-1);

			if (variablePrefix == "aff" || variablePrefix == "affection")
			{
				affectionVariable = true;
			}
		}

		if(affectionVariable)
		{
			// Affection variables have their own special processing
			AffectionTracker affectionTracker = this.gameObject.GetComponent<AffectionTracker>();
			if(affectionTracker)
			{
				int affectionChange;
				bool validIntValue = int.TryParse(variableValue, out affectionChange);

				if(validIntValue)
				{
					affectionTracker.AddAffection(variableName, affectionChange);
				} else
				{
					Debug.LogWarning("Invalid affection change value provided");
				}
			}
		} else 
		{
			int variableCacheIndex = playerManager.GetVariableIndex(variableName);

			if(variableCacheIndex >= 0)
			{
				playerManager.SetVariable(variableCacheIndex, variableValue);
			} else
			{
				Debug.LogWarning("Variable " + variableName + " not found while attempting to change");
			}
		}

		GoToNextAction();
	}

	void SetResponses(CutsceneAction cutsceneAction)
	{
		CutsceneAction tempAction;
		responseActions = new List<CutsceneAction>();
		string newResponseText = "";

		for(int i=0; i<cutsceneAction.indexesOfNextActions.Count; i++)
		{
			tempAction = currentCutscene.cutsceneActions[cutsceneAction.indexesOfNextActions[i]];

			if(CheckAllConditions(tempAction))
			{
				// Only add each action to the list, if its conditions have been met
				responseActions.Add(tempAction);
				newResponseText += tempAction.textContent + "\n";
			}
		}

		if(responseActions.Count>0)
		{
			dialogWindow.SetResponses(newResponseText);
			selectionIndex = 1;
			waitingOnInput = true;
		} else
		{
			// All responses have conditions, which have not been met
			EndCutscene();
		}
	}

	void UpdateResponseSelection()
	{
		Vector2 mousePosition = Input.mousePosition;
		int hoverIndex = dialogWindow.GetResponseHoverIndex(mousePosition);

		if(hoverIndex > responseActions.Count)
		{
			// The dialog window can bring back hover indexes above the number of responses, since it doesn't track how many there are
			hoverIndex = 0;
		}

		if(hoverIndex > 0)
		{
			// Current mouse position is over a response
			selectionIndex = hoverIndex;
			timeOfLastChange = 0;
			dialogWindow.SetHighlightRow(selectionIndex);
		} else
		{
			// Current mouse position is NOT over a response
			float verticalAxis = Input.GetAxis("Vertical" + playerNumber);
			float horizontalAxis = Input.GetAxis("Horizontal" + playerNumber);

			if(Mathf.Abs(verticalAxis) > 0.1f)
			{
				if(verticalAxis > 0)
				{
					// Up
					ShiftResponseSelection(false);
				} else
				{
					// Down
					ShiftResponseSelection(true);
				}
			} else if(Mathf.Abs(horizontalAxis) > 0.1f)
			{
				if(horizontalAxis > 0)
				{
					// Right
					ShiftResponseSelection(true);
				} else
				{
					// Left
					ShiftResponseSelection(false);
				}
			} else
			{
				// The mouse isn't over any responses and no axis is being moved, player might have released their axis
				timeOfLastChange = 0;
			}
		}
	}

	void ShiftResponseSelection(bool shiftDown)
	{
		if(Time.time - timeOfLastChange >= shiftDelay)
		{
			if(shiftDown)
			{
				if(selectionIndex >= responseActions.Count)
				{
					selectionIndex = 1;
				} else
				{
					selectionIndex++;
				}
			} else
			{
				if(selectionIndex <= 1)
				{
					selectionIndex = responseActions.Count;
				} else
				{
					selectionIndex--;
				}
			}

			dialogWindow.SetHighlightRow(selectionIndex);
			timeOfLastChange = Time.time;
		}
	}

	void ExecuteResponse()
	{
		currentAction = responseActions[selectionIndex-1];
		responseActions = null;
		selectionIndex = 1;
		GoToNextAction();
	}

	void InterpretAnimation(CutsceneAction cutsceneAction)
	{
		GameObject actorObject;
		Animator actorAnimator;

		actorObject = GameObject.Find(cutsceneAction.titleText);
		if(actorObject)
		{
			actorAnimator = actorObject.GetComponent<Animator>();
			if(actorAnimator)
			{
				actorAnimator.SetTrigger(cutsceneAction.textContent);
			}
		}

		GoToNextAction();
	}

	void InterpretCameraAction(CutsceneAction cutsceneAction)
	{
		string eventName = cutsceneAction.conditionValues[0].ToLower();
		List<string> eventParameters = new List<string>();
		string newFollowTarget = cutsceneAction.conditionValues[1];
		bool waitForCompletion = cutsceneAction.rightActor;

		string newPositionX_string = cutsceneAction.conditionValues[2];
		string newPositionY_string = cutsceneAction.conditionValues[3];
		string newSpeed_string = cutsceneAction.conditionValues[4];
		float newPositionX;
		float newPositionY;
		float newSpeed;

		if(eventName.Length > 0)
		{
			// Some events have parameters, separated by dots, extract them if found before processing the event
			if(eventName.Contains("."))
			{
				int numberOfParameters = eventName.Length - eventName.Replace(".","").Length + 1;
				string tempString = eventName;
				string nextValue = "";

				for(int i=0; i<numberOfParameters; i++)
				{
					nextValue = "";

					if(tempString.Contains("."))
					{
						nextValue = tempString.Substring(0, tempString.IndexOf("."));
					} else
					{
						nextValue = tempString;
					}

					if(nextValue.Length > 0)
					{
						eventParameters.Add(nextValue);
					} else
					{
						Debug.LogWarning("Invalid event parameter given, canceling all camera actions");
						return;
					}

					if(i<numberOfParameters-1)
					{
						tempString = tempString.Substring(nextValue.Length+1, tempString.Length - nextValue.Length - 1);
					}
				}
				eventName = eventParameters[0];
				eventParameters.RemoveAt(0);

				if(eventName.Length <= 0)
				{
					Debug.LogWarning("Invalid event given");
				}
			}


			// -----BEGIN CAMERA EVENTS-----
			if(eventName == "reset")
			{
				cameraController.ResetView();
			} else if(eventName == "tint")
			{
				if(eventParameters.Count == 4)
				{
					float newColorR = NormalizeColor( StringToFloat(eventParameters[0]) );
					float newColorG = NormalizeColor( StringToFloat(eventParameters[1]) );
					float newColorB = NormalizeColor( StringToFloat(eventParameters[2]) );
					float newColorA = NormalizeColor( StringToFloat(eventParameters[3]) );
					float newTintSpeed = StringToFloat(newSpeed_string);

					if(newSpeed_string.Length == 0)
					{
						//cameraScreen.SetTint(newColorR, newColorG, newColorB, newColorA);
					} else
					{
						//cameraScreen.SetTint(newColorR, newColorG, newColorB, newColorA, newTintSpeed);
					}
				} else
				{
					Debug.LogWarning("Invalid number of camera tint parameters (" + eventParameters.Count + ") provided, no tint applied");
				}
			}

			// -----END CAMERA EVENTS-----
		}

		// New follow target
		if(newFollowTarget.Length > 0)
		{
			GameObject newFollowTargetObject = GameObject.Find(newFollowTarget);
			if(newFollowTargetObject)
			{
				cameraController.FollowNewTarget(newFollowTargetObject.transform);
			} else
			{
				Debug.LogWarning("Unable to find target named: " + newFollowTarget);
			}
		}

		// New position/offset
		if(newPositionX_string.Length > 0 || newPositionY_string.Length > 0)
		{
			newPositionX = StringToFloat(newPositionX_string);
			newPositionY = StringToFloat(newPositionY_string);
			cameraController.SetOffset(newPositionX, newPositionY);
		}

		// New speed - this is also used for some events (tint)
		if(newSpeed_string.Length > 0)
		{
			newSpeed = StringToFloat(newSpeed_string);
			cameraController.SetMoveSpeed(newSpeed);
		}
	}

	float StringToFloat(string stringToConvert)
	{
		// Converts a String to a Float value, with the default of 0 if an invalid or blank string is provided
		// Any special action on null values must be checked before calling this, since 0 and null will both come back as 0
		float floatValue = 0;

		if(stringToConvert.Length > 0)
		{
			if (float.TryParse(stringToConvert, out floatValue))
			{
				floatValue = float.Parse(stringToConvert);
			}
		}

		return floatValue;
	}

	float NormalizeColor(float colorNumber)
	{
		if(colorNumber < 0)
		{
			colorNumber = 0;
		} else if (colorNumber > 255)
		{
			colorNumber = 255;
		}

		colorNumber = colorNumber / 255;
		return colorNumber;
	}

	void InterpretPortrait(CutsceneAction cutsceneAction)
	{
		string characterName = cutsceneAction.titleText;
		string expressionName = cutsceneAction.textContent;

		if(characterName.Length > 0)
		{
			if(characterName.ToLower() == "clear")
			{
				dialogWindow.ClearPortrait(cutsceneAction.rightActor);
			} else
			{
				dialogWindow.SetPortrait(characterName, cutsceneAction.rightActor);
			}
		}

		if(expressionName.Length > 0)
		{
			dialogWindow.SetExpression(expressionName, cutsceneAction.rightActor);
		}

		GoToNextAction();
	}

	void InterpretPauseAction(CutsceneAction cutsceneAction)
	{
		string pauseLength_string = cutsceneAction.titleText;
		float newPauseLength;

		if(float.TryParse(pauseLength_string, out newPauseLength))
		{
			timeOfPause = Time.time;
			pauseLength = newPauseLength;
			dialogWindow.SetVisibility(false);
		} else
		{
			Debug.LogWarning("Invalid pause time provided");
		}
	}

	void CheckPause()
	{
		if(Time.time - timeOfPause >= pauseLength)
		{
			timeOfPause = 0;
			pauseLength = 0;
			dialogWindow.SetVisibility(true);
			GoToNextAction();
		}
	}
}
