using SocialScenarios;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour {
    public static DialogueManager dialogueManager { get; private set; }

    void Awake() {
        if (dialogueManager != null && dialogueManager != this) {
            Destroy(gameObject);
            return;
        }
        dialogueManager = this;

        GameObject[] personSquares = GameObject.FindGameObjectsWithTag("Person Square");
        foreach (GameObject obj in personSquares) {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend == null) continue;

            Material instanceMat = new Material(rend.sharedMaterial);

            Color originalColor = instanceMat.color;
            float randomHue = Random.Range(0f, 1f);
            Color newColor = Color.HSVToRGB(randomHue, 0.2f, 0.7f);
            newColor.a = originalColor.a;
            instanceMat.color = newColor;
            newColor.a = originalColor.a;
            instanceMat.color = newColor;

            rend.material = instanceMat;
        }
    }

    void Start() {
        ScenarioManager sm = ScenarioManager.Instance;
        if (sm == null) {
            Debug.LogError("DialogueManager: No ScenarioManager instance found.");
            return;
        }

        DialogueEntry startEntry = sm.dialogues.Find(d => d.id == sm.startId);
        if (startEntry == null) {
            Debug.LogError($"DialogueManager: No DialogueEntry found with startId={sm.startId}.");
            return;
        }

        sm.usedIds.Add(startEntry.id);

        Dialogue dialogue = startEntry.GetComponent<Dialogue>();
        if (dialogue == null) {
            Debug.LogError($"DialogueManager: No Dialogue component on startId={sm.startId}.");
            return;
        }

        dialogue.Play();
    }

    public void PlayDialogue(Dialogue dialogue) => dialogue.Play();
}