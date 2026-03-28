using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour {
    public static DialogueManager dialogueManager { get; private set; }

    void Awake() {
        if (dialogueManager != null && dialogueManager != this) {
            Destroy(this.gameObject);
        }
        else {
            dialogueManager = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }


    Queue<Dialogue> _queue = new();
    bool _isPlaying;

    public void Enqueue(Dialogue dialogue) {
        if (dialogue == null) return;
        _queue.Enqueue(dialogue);
        if (!_isPlaying) StartCoroutine(PlayQueue());
    }

    IEnumerator PlayQueue() {
        _isPlaying = true;

        while (_queue.Count > 0) {
            Dialogue next = _queue.Dequeue();
            next.Play();
            yield return new WaitUntil(() => !next.IsPlaying);
        }

        _isPlaying = false;
    }
}