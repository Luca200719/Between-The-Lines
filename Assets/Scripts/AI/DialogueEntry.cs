using System;
using UnityEngine;

namespace SocialScenarios {
    public class DialogueEntry : MonoBehaviour {
        [Header("Identity")]
        public int id;
        public string title;

        [TextArea(4, 10)]
        [Tooltip("The full conversation text that plays out in front of this cube.")]
        public string conversationText;

        [Header("Trait Profile")]
        [Tooltip("Pre-author the trait vector this dialogue is 'about'. 0.0 - 1.0 each.")]
        [Range(0f, 1f)] public float assertiveness;
        [Range(0f, 1f)] public float empathy;
        [Range(0f, 1f)] public float emotionalRegulation;
        [Range(0f, 1f)] public float socialConfidence;
        [Range(0f, 1f)] public float prosocialIntent;

        [Header("Camera Target")]
        [Tooltip("Where the camera moves to when this dialogue is selected.")]
        public Transform cameraTarget;

        public float[] TraitVector => new float[]
        {
            assertiveness, empathy, emotionalRegulation, socialConfidence, prosocialIntent
        };
    }
}