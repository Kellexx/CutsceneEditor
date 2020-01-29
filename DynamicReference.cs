using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicReference : MonoBehaviour 
{
	private Dictionary<string, string> keywordDictionary;

	void Start()
	{
		keywordDictionary = new Dictionary<string, string>();
		keywordDictionary.Add("peg", "Peg-Leg Greg");
		keywordDictionary.Add("gabe", "Gabe");
	}

	public string RewriteWord(string baseString)
	{
		string tString = baseString.ToLower();

		if(keywordDictionary.ContainsKey(baseString.ToLower()))
		{
			tString = keywordDictionary[baseString.ToLower()];
		}

		return tString;
	}

	private string RewriteString(string baseString)
	{
		string tString = baseString;
		
		while(tString.Contains("%%"))
		{
			tString = tString.Replace("%%","%");
		}

		string dynamicReferenceLookup = "";
		string dynamicReferenceValue = "";
		string newString = "";
		int cStartIndex = 0;
		int cEndIndex = 0;
		int dynamicReferenceCount = tString.Length - tString.Replace("%","").Length;

		for(int i=0; i<dynamicReferenceCount; i++)
		{
			cStartIndex = tString.IndexOf("%") + 1;
			cEndIndex = tString.IndexOf(" ", cStartIndex);

			if(cEndIndex < 0)
			{
				cEndIndex = tString.Length;
			}

			newString += tString.Substring(0, cStartIndex-1);
			dynamicReferenceLookup = tString.Substring(cStartIndex, cEndIndex-cStartIndex);

			if(keywordDictionary.ContainsKey(dynamicReferenceLookup.ToUpper()))
			{
				dynamicReferenceValue = keywordDictionary[dynamicReferenceLookup.ToUpper()];
			} else
			{
				dynamicReferenceValue = dynamicReferenceLookup;
			}

			newString += dynamicReferenceValue;

			if(cEndIndex+1 < tString.Length)
			{
				tString = tString.Substring(cEndIndex, tString.Length - cEndIndex);
			} else
			{
				tString = "";
			}
		}

		if(tString.Length > 0)
		{
			newString += tString.Substring(0, tString.Length);
		}

		while(newString.Contains("  "))
		{
			newString = newString.Replace("  "," ");
		}

		return newString.Trim();
	}
}
