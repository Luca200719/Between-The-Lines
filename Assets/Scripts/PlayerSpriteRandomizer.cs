using System.Collections.Generic;
using UnityEngine;

public class PlayerSpriteRandomizer : MonoBehaviour {
    public Sprite[] sprites;

    void Awake() {
        sprites = Resources.LoadAll<Sprite>("Sprites");

        GameObject[] playerSprites = GameObject.FindGameObjectsWithTag("Person Sprite");

        List<int> indices = new List<int>();
        for (int i = 0; i < sprites.Length; i++) indices.Add(i);
        for (int i = indices.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        for (int i = 0; i < playerSprites.Length && i < indices.Count; i++) {
            SpriteRenderer sr = playerSprites[i].GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            sr.sprite = sprites[indices[i]];
        }
    }
}