using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneTrigger : MonoBehaviour 
{
	public int cutsceneID;
	private BoxCollider2D mainCollider;
	private GameManager gameManager;

	public string relatedCharacter;
	[SerializeField]
	private int requiredAffection=0;

	[SerializeField]
	private bool activeInSpring = true;
	[SerializeField]
	private bool activeInSummer = true;
	[SerializeField]
	private bool activeInFall = true;
	[SerializeField]
	private bool activeInWinter = true;

	void Start()
	{
		mainCollider = GetComponent<BoxCollider2D>();
		SetTriggerStatus(false);
		gameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();
	}

	public void SetTriggerStatus(bool newStatus)
	{
		mainCollider.enabled = newStatus;
	}

	public void ActivateTrigger()
	{
		string cSeason = gameManager.GetSeason();
		bool seasonCriteriaMet = false;
		bool affectionCriteriaMet = false;

		if(cSeason=="spring" && activeInSpring)
		{
			seasonCriteriaMet = true;
		} else if(cSeason=="summer" && activeInSummer)
		{
			seasonCriteriaMet = true;
		} else if(cSeason=="fall" && activeInFall)
		{
			seasonCriteriaMet = true;
		} else if(cSeason=="winter" && activeInWinter)
		{
			seasonCriteriaMet = true;
		}

		if(seasonCriteriaMet)
		{
			// TODO: Make this work a different way, without depending on the GameManager
			// If there's an affection requirement, check if any current players meet it
			// This get schecked again when players step in the trigger, to be sure they're the right player
			if(relatedCharacter.Length > 0 && requiredAffection > 0)
			{
				List<GameObject> playerObjects = gameManager.playerObjects;

				for(int i=0; i<playerObjects.Count; i++)
				{
					if(CheckAffectionRequirement(playerObjects[i]))
					{
						affectionCriteriaMet = true;
						break;
					}
				}
			} else
			{
				affectionCriteriaMet = true;
			}

			if(affectionCriteriaMet)
			{
				SetTriggerStatus(true);
			}
		}
	}

	void OnTriggerEnter2D(Collider2D collider)
	{
		CutsceneManager cutsceneManager = collider.transform.parent.GetComponent<CutsceneManager>();
		GameObject playerObject = collider.transform.parent.gameObject;

		if(cutsceneManager && playerObject)
		{
			// Since there are multiple players, check that the one in the trigger meets the required affection, if specified
			if(CheckAffectionRequirement(playerObject))
			{
				cutsceneManager.StartCutscene(cutsceneID);
			}
		}
	}

	bool CheckAffectionRequirement(GameObject playerObjectToCheck)
	{
		AffectionTracker tAffectionTracker = playerObjectToCheck.GetComponent<AffectionTracker>();

		if(requiredAffection > 0 && relatedCharacter.Length > 0)
		{
			if(tAffectionTracker)
			{
				return (tAffectionTracker.GetAffectionLevel(relatedCharacter) >= requiredAffection);
			} else
			{
				return false;
			}
		} else
		{
			return true;
		}
	}
}
