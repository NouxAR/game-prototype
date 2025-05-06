using UnityEngine;
using UnityEngine.UI;

public class ChoiceManager : MonoBehaviour
{
    public Button redButton;
    public Button blueButton;

    void Start()
    {
        redButton.onClick.AddListener(() => OnChoiceMade("red"));
        blueButton.onClick.AddListener(() => OnChoiceMade("blue"));
    }

    void OnChoiceMade(string color)
    {
        Debug.Log("Oyuncu seçimi: " + color);

        // İleride buraya sunucuya gönderme kodu gelecek
        // örnek: SendChoiceToServer(color);
    }
}
