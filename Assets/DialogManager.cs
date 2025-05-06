using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class DialogManager : MonoBehaviour
{
    public string sceneName = "yetimhane";
    public TextMeshProUGUI dialogText;
    public TextMeshProUGUI characterText;
    public Button continueButton;
    public GameObject choicePanel;
    public Button[] choiceButtons;
    public GameObject introCutsceneObject;
    public float introCutsceneDuration = 5f;
    public GameObject firstScreenObject;
    public List<CharacterSceneAvatar> characterAvatars;
    public RawImage avatarSlot; // Inspector’dan RawImage bağlanacak
    public List<CharacterSceneTransform> characterTransforms;
    public Transform cameraTransform;
    public Vector3 zoomOffset = new Vector3(0, 1, -3);
    public float zoomDuration = 0.5f;
    private Vector3 defaultCameraPosition;
    public List<int> sceneTransitionOrders; // Inspector'dan yönetilebilir olsun
    public List<GameObject> objectsToHideDuringCutscene; // Inspector’dan doldur
    public List<int> SpecialOrders;
    public GameObject inputPanel;
    public TMP_Text questionText;
    public TMP_InputField playerInputField;
    public Button submitButton;


    private DialogLine[] dialogLines;
    private int currentIndex = 0;


    public GameObject dialogPanel; // ← Inspector'da tüm panel atanacak

    [System.Serializable]
    public class CutsceneData
    {
        public int order;
        public GameObject cutsceneObject;
        public float duration;

        public GameObject postCutsceneObject; // ← Cutscene bitince aktif olacak nesne
    }
    public List<CutsceneData> cutscenes;

    [System.Serializable]
    public class CharacterSceneAvatar
    {
        public string scene;
        public string character;
        public List<Texture2D> avatarTextures; // 👈 Artık birden fazla avatar var
    }

    [System.Serializable]
    public class CharacterSceneTransform
    {
        public string scene;
        public string character;
        public Transform characterTransform;
    }

    void Start()
    {
        StartCoroutine(GetDialogData());
        HideDialogPanel();
        StartCoroutine(PlayIntroCutscene());
        defaultCameraPosition = cameraTransform.position;
        firstScreenObject.SetActive(false);
        inputPanel.SetActive(false);

    }

    IEnumerator GetDialogData()
    {
        string url = $"https://unity-choice-api-production.up.railway.app/api/dialog/{sceneName}";
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string json = "{\"lines\":" + www.downloadHandler.text + "}";
            DialogLineList data = JsonUtility.FromJson<DialogLineList>(json);
            dialogLines = data.lines;

            ShowCurrentLine();
        }
        else
        {
            Debug.LogError("Diyalog verisi çekilemedi: " + www.error);
        }
    }

    IEnumerator SendInputAnswer(string scene, string input, string character, int order)
    {
        string url = "https://unity-choice-api-production.up.railway.app/api/input"; // kendi endpointinle değiştir
        WWWForm form = new WWWForm();
        form.AddField("scene", scene);
        form.AddField("input", input);
        form.AddField("character", character);
        form.AddField("order", order);

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Input gönderilemedi: " + www.error);
        }
        else
        {
            Debug.Log("Input başarıyla gönderildi.");
        }
    }

    public void OnContinueClicked()
    {
        DialogLine currentLine = dialogLines[currentIndex];
        currentIndex++;

        // Cutscene kontrolü
        CutsceneData cs = cutscenes.Find(c => c.order == currentLine.order);
        if (cs != null && cs.cutsceneObject != null)
        {
            HideDialogPanel();
            StartCoroutine(PlayCutsceneObject(cs));
            return;


        }

        if (currentIndex < dialogLines.Length)
            ShowCurrentLine();
        else
        {
            dialogText.text = "Diyalog bitti.";
            characterText.text = "";
            continueButton.gameObject.SetActive(false);
        }
    }


    void ShowCurrentLine()
    {   
        DialogLine line = dialogLines[currentIndex];
        Debug.Log($"Current: scene={line.scene}, character={line.character}, order={line.order}");
        if (line.type == "input")
        {
            dialogPanel.SetActive(false);
            choicePanel.SetActive(false);
            inputPanel.SetActive(true);

            questionText.text = line.question;
            playerInputField.text = "";

            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(() =>
            {
                string userAnswer = playerInputField.text;

                StartCoroutine(SendInputAnswer(line.scene, userAnswer, line.character, line.order));

                inputPanel.SetActive(false);
                dialogPanel.SetActive(true);
                currentIndex++;
                ShowCurrentLine();
            });

            return;
        }

        // Tüm karakterleri önce görünür yap
        foreach (var ct in characterTransforms)
        {
            if (ct.characterTransform != null)
                ct.characterTransform.gameObject.SetActive(true);
        }

        // Konuşan karakteri sahneden gizle
        if (line.character.ToLower() != "anlatıcı" && !sceneTransitionOrders.Contains(line.order))
        {
            var speakingChar = characterTransforms.Find(c =>
                c.scene == line.scene && c.character == line.character);

            if (speakingChar != null && speakingChar.characterTransform != null)
                speakingChar.characterTransform.gameObject.SetActive(false);
        }

        if (SpecialOrders.Contains(line.order))
        {


            return;
        }

        if (line.character.ToLower() != "anlatıcı" && !sceneTransitionOrders.Contains(line.order))
        {
            var target = characterTransforms.Find(c =>
                c.scene == line.scene && c.character == line.character);

            if (target != null)
                StartCoroutine(ZoomToCharacter(target.characterTransform));
        }

          // Eğer intro cutscene açık ve şu anki order kapanma noktasıysa, kapat
        if (introCutsceneObject != null && introCutsceneObject.activeSelf && sceneTransitionOrders.Contains(line.order))
        {
            introCutsceneObject.SetActive(false);
            firstScreenObject.SetActive(true);
        }

        // Avatar atama 
        
       if (line.character.ToLower() == "anlatıcı" || sceneTransitionOrders.Contains(line.order))
        {
            avatarSlot.texture = null;
            avatarSlot.gameObject.SetActive(false);
            StartCoroutine(ZoomCameraTo(defaultCameraPosition));
        }
        else
        {
            var avatarData = characterAvatars.Find(a =>
                a.scene == line.scene && a.character == line.character);

            if (avatarData != null && avatarData.avatarTextures != null && avatarData.avatarTextures.Count > 0)
            {
                int randomIndex = Random.Range(0, avatarData.avatarTextures.Count);
                avatarSlot.texture = avatarData.avatarTextures[randomIndex];
                avatarSlot.gameObject.SetActive(true);
            }
            else
            {
                avatarSlot.texture = null;
                avatarSlot.gameObject.SetActive(false);
            }
        }

        characterText.text = line.character;

        if (line.type == "line")
        {
            dialogText.text = line.line;
            continueButton.gameObject.SetActive(true);
            choicePanel.SetActive(false);
        }
        else if (line.type == "choice")
        {
            dialogText.text = "";
            continueButton.gameObject.SetActive(false);
            choicePanel.SetActive(true);

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (i < line.choices.Length)
                {
                    choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = line.choices[i];
                    int choiceIndex = i;
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(choiceIndex));
                    choiceButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    void OnChoiceSelected(int index)
    {
        string selectedChoice = dialogLines[currentIndex].choices[index];
        Debug.Log($"Seçilen: {selectedChoice}");

        // Seçimi sunucuya gönder
        FindObjectOfType<ChoiceSender>().SendChoice(sceneName, selectedChoice);


        choicePanel.SetActive(false);
        continueButton.gameObject.SetActive(true);
        OnContinueClicked(); // seçimden sonra otomatik devam
    }

    public void HideDialogPanel()
    {
        dialogPanel.SetActive(false);
    }

    public void ShowDialogPanel()
    {
        dialogPanel.SetActive(true);
        ShowCurrentLine(); // panel açıldığında mevcut satırı da yükle
    }

    IEnumerator PlayCutsceneObject(CutsceneData cs)
    {
        StartCoroutine(ZoomCameraTo(defaultCameraPosition));
        SetActiveOtherObjects(false);// her şeyi gizle
        cs.cutsceneObject.SetActive(true);

        yield return new WaitForSeconds(cs.duration);
        
        cs.cutsceneObject.SetActive(false);
        SetActiveOtherObjects(true);// her şeyi göster

            if (cs.postCutsceneObject != null)
        cs.postCutsceneObject.SetActive(true); // Cutscene sonrası objeyi aktif et

        ShowDialogPanel();
    }

        IEnumerator PlayIntroCutscene()
    {
        introCutsceneObject.SetActive(true);
        yield return new WaitForSeconds(introCutsceneDuration);
        ShowDialogPanel(); // diyalog başlasın
    }

    IEnumerator ZoomToCharacter(Transform target)
    {
        Vector3 startPos = cameraTransform.position;
        Vector3 targetPos = target.position + zoomOffset;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            cameraTransform.position = Vector3.Lerp(startPos, targetPos, elapsed / zoomDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.position = targetPos;
    }

    IEnumerator ZoomCameraTo(Vector3 targetPosition)
    {
        Vector3 startPos = cameraTransform.position;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            cameraTransform.position = Vector3.Lerp(startPos, targetPosition, elapsed / zoomDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.position = targetPosition;
    }

    IEnumerator DelayBeforeNextLine()
    {
        yield return new WaitForSeconds(1.5f);
        currentIndex++;
        ShowCurrentLine();
    }

    void SetActiveOtherObjects(bool isActive)
    {
        foreach (var obj in objectsToHideDuringCutscene)
        {
            if (obj != null)
                obj.SetActive(isActive);
        }
    }


}
