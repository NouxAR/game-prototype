using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ChoiceSender : MonoBehaviour
{
    // Bu method, bir seçim yapıldığında çağrılır
    public void SendChoice(string key, string value)
    {
        StartCoroutine(SendChoiceCoroutine(key, value));
    }

    IEnumerator SendChoiceCoroutine(string key, string value)
    {
        WWWForm form = new WWWForm();
        form.AddField("key", key);
        form.AddField("value", value);

        string url = "https://unity-choice-api-production.up.railway.app/api/save"; // ← Bunu kendi URL’inle değiştir
        UnityWebRequest www = UnityWebRequest.Post(url, form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Seçim başarıyla gönderildi: " + key + " = " + value);
             // Seçim başarıyla gönderildi, şimdi son 3 seçimi güncelle

        }
        else
        {
            Debug.LogError("❌ Hata oluştu: " + www.error);
        }
    }

        public void SendRed()
    {
        SendChoice("renk", "kirmizi");
    }

    public void SendBlue()
    {
        SendChoice("renk", "mavi");
    }

}
