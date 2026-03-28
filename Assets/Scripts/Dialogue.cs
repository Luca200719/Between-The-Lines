using System;
using UnityEngine;
using TMPro;
using System.Collections;

[Serializable]
public struct DialogueLine {
    public enum Speaker { A, B }
    public Speaker speaker;
    [TextArea(2, 4)] public string text;
    public float duration;
}

public class Dialogue : MonoBehaviour {
    TextMeshPro textBoxA;
    TextMeshPro textBoxB;
    //Animator animatorA;
    //Animator animatorB;
    string talkingParam = "IsTalking";

    public DialogueLine[] lines;

    public void Start() {
        textBoxA = transform.GetChild(0).GetComponent<TextMeshPro>();
        textBoxB = transform.GetChild(1).GetComponent<TextMeshPro>();
        //animatorA = transform.GetChild(2).GetComponent<Animator>();
        //animatorB = transform.GetChild(3).GetComponent<Animator>();

        DialogueManager.dialogueManager.Enqueue(this);
    }

    public bool IsPlaying { get; private set; }

    public void Play() => StartCoroutine(RunDialogue());

    IEnumerator RunDialogue() {
        IsPlaying = true;

        foreach (var line in lines) {
            bool isA = line.speaker == DialogueLine.Speaker.A;

            textBoxA.text = isA ? line.text : string.Empty;
            textBoxB.text = isA ? string.Empty : line.text;

            //animatorA.SetBool(talkingParam, isA);
            //animatorB.SetBool(talkingParam, !isA);

            yield return new WaitForSeconds(line.duration);
        }

        textBoxA.text = string.Empty;
        textBoxB.text = string.Empty;
        //animatorA.SetBool(talkingParam, false);
        //animatorB.SetBool(talkingParam, false);

        IsPlaying = false;
    }
}