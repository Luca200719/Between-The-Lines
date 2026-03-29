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
        // Removed DontDestroyOnLoad — not needed for a single-scene setup
        // and caused FindObjectsByType to break across scene loads
    }

    void Start() {
        // Defer to ScenarioManager for the starting dialogue
        // so startId is respected and camera snap is consistent
        ScenarioManager sm = ScenarioManager.Instance;
        if (sm == null) {
            Debug.LogError("DialogueManager: No ScenarioManager instance found.");
            return;
        }

        //List<DialogueEntry> available = sm.dialogues.FindAll(d => d != null);
        //if (available.Count == 0) {
        //    Debug.LogError("DialogueManager: No valid DialogueEntries found.");
        //    return;
        //}

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

        // Camera already snapped in ScenarioManager.Awake, so just play
        dialogue.Play();
    }

    /// <summary>Called by ScenarioManager to play a specific dialogue after camera arrives.</summary>
    public void PlayDialogue(Dialogue dialogue) => dialogue.Play();
}