using System;
using UnityEngine;
using TMPro;
using System.Collections;
using Unity.VisualScripting;

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
    AudioSource playerSpeaker;

    public AudioClip[] audioClips;
    public AudioClip audioClipA;
    public AudioClip audioClipB;

    public DialogueLine[] lines;

    public void Start() {
        textBoxA = transform.GetChild(0).GetChild(2).GetComponent<TextMeshProUGUI>();
        textBoxB = transform.GetChild(0).GetChild(3).GetComponent<TextMeshProUGUI>();
        bubbleBoxA = transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
        bubbleBoxB = transform.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>();
        playerSpeaker = GameObject.FindGameObjectWithTag("audio").GetComponent<AudioSource>();
        audioClipA = audioClips[UnityEngine.Random.Range(0,13)];
        audioClipB = audioClips[UnityEngine.Random.Range(0,13)];



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
                playerSpeaker.generator = audioClipA;
            } else {
                textBoxA.text = string.Empty;
                playerSpeaker.generator = audioClipB;
            }

            bubbleBoxA.gameObject.SetActive(isA);
            bubbleBoxB.gameObject.SetActive(!isA);

            TextMeshProUGUI textBoxText = isA ? textBoxA : textBoxB;
            TextMeshProUGUI bubbleBoxText = isA ? bubbleBoxA : bubbleBoxB;

            textBoxText.text = text;
            textBoxText.maxVisibleCharacters = 0;

            bubbleBoxText.text = text;
            bubbleBoxText.maxVisibleCharacters = 0;

            playerSpeaker.Play();

            for (int i = 0; i <= text.Length; i++) {
                textBoxText.maxVisibleCharacters = i;
                bubbleBoxText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(0.05f);
            }

            while (playerSpeaker.isPlaying) {
                yield return null;
            }
        }

        textBoxA.text = string.Empty;
        textBoxB.text = string.Empty;

        bubbleBoxA.gameObject.SetActive(false);
        bubbleBoxB.gameObject.SetActive(false);

        IsPlaying = false;
    }
}