using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System;

public class LastThreeChoicesDisplay : MonoBehaviour
{
    public TextMeshProUGUI line1Text;
    public TextMeshProUGUI line2Text;
    public TextMeshProUGUI line3Text;

    [Serializable]
    public class Choice
    {
        public string key;
        public string value;
        public string createdAt;
    }

    [Serializable]
    public class ChoiceList
    {
        public Choice[] choices;
    }

    public void RefreshLastChoices()
    {
        StartCoroutine(GetLastThreeChoices());
    }

    IEnumerator GetLastThreeChoices()
    {
        UnityWebRequest www = UnityWebRequest.Get("https://unity-choice-api-production.up.railway.app/api/last3");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string json = "{\"choices\":" + www.downloadHandler.text + "}";
            ChoiceList choiceList = JsonUtility.FromJson<ChoiceList>(json);

            // Listeyi güncelle
            line1Text.text = choiceList.choices.Length > 0 ? $"{choiceList.choices[0].key} → {choiceList.choices[0].value}" : "";
            line2Text.text = choiceList.choices.Length > 1 ? $"{choiceList.choices[1].key} → {choiceList.choices[1].value}" : "";
            line3Text.text = choiceList.choices.Length > 2 ? $"{choiceList.choices[2].key} → {choiceList.choices[2].value}" : "";
        }
        else
        {
            Debug.LogError("❌ Veri çekme hatası: " + www.error);
        }
    }
}

