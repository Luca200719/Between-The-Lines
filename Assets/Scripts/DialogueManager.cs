using SocialScenarios;
using System.Collections;
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
        DontDestroyOnLoad(gameObject);
    }

    void Start() {
        List<Dialogue> all = new(FindObjectsByType<Dialogue>(FindObjectsSortMode.None));

        Dialogue picked = all[Random.Range(0, all.Count)];

        // Mark it as used in ScenarioManager so it won't be picked again
        DialogueEntry entry = picked.GetComponent<DialogueEntry>();
        if (entry != null && ScenarioManager.Instance != null)
            ScenarioManager.Instance.usedIds.Add(entry.id);

        // Move camera then play
        if (entry != null && CameraController.Instance != null)
            CameraController.Instance.MoveTo(entry.cameraTarget, () => picked.Play());
        else
            picked.Play();
    }

    /// <summary>Called by ScenarioManager to play a specific dialogue after camera arrives.</summary>
    public void PlayDialogue(Dialogue dialogue) => dialogue.Play();
}