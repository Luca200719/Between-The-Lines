using System;
using UnityEngine;
using TMPro;
using System.Collections;

[Serializable]
public struct DialogueLine {
    public enum Speaker { A, B }
    public Speaker speaker;
    [TextArea(2, 4)] public string text;
}

public class Dialogue : MonoBehaviour {

    TextMeshProUGUI textBoxA;
    TextMeshProUGUI textBoxB;
    TextMeshProUGUI bubbleBoxA;
    TextMeshProUGUI bubbleBoxB;

    public DialogueLine[] lines;

    public void Start() {
        textBoxA = transform.GetChild(0).GetChild(2).GetComponent<TextMeshProUGUI>();
        textBoxB = transform.GetChild(0).GetChild(3).GetComponent<TextMeshProUGUI>();
        bubbleBoxA = transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
        bubbleBoxB = transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>();

        bubbleBoxA.gameObject.SetActive(false);
        bubbleBoxB.gameObject.SetActive(false);

        DialogueManager.dialogueManager.Enqueue(this);
    }

    public bool IsPlaying { get; private set; }

    public void Play() => StartCoroutine(RunDialogue());

    IEnumerator RunDialogue() {
        IsPlaying = true;

        foreach (var line in lines) {
            bool isA = line.speaker == DialogueLine.Speaker.A;
            string text = line.text;

            if (isA) {
                textBoxB.text = string.Empty;
            } else {
                textBoxA.text = string.Empty;
            }

            bubbleBoxA.gameObject.SetActive(isA);
            bubbleBoxB.gameObject.SetActive(!isA);

            TextMeshProUGUI textBoxText = isA ? textBoxA : textBoxB;
            TextMeshProUGUI bubbleBoxText = isA ? bubbleBoxA : bubbleBoxB;

            textBoxText.text = text;
            textBoxText.maxVisibleCharacters = 0;

            bubbleBoxText.text = text;
            bubbleBoxText.maxVisibleCharacters = 0;

            for (int i = 0; i <= text.Length; i++) {
                textBoxText.maxVisibleCharacters = i;
                bubbleBoxText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(0.05f);
            }

            yield return new WaitForSeconds(1f);
        }

        textBoxA.text = string.Empty;
        textBoxB.text = string.Empty;

        bubbleBoxA.gameObject.SetActive(false);
        bubbleBoxB.gameObject.SetActive(false);

        IsPlaying = false;
    }
}