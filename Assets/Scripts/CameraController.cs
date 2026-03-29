using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour {
    public static CameraController Instance { get; private set; }

    public float moveDuration = 1.5f;

    private Coroutine moveCoroutine;

    void Awake() => Instance = this;

    public void MoveTo(Transform target, System.Action onArrival = null) {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(Lerp(target, onArrival));
    }

    public void SnapTo(Transform target) {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        transform.SetPositionAndRotation(target.position, target.rotation);
    }

    private IEnumerator Lerp(Transform target, System.Action onArrival) {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < moveDuration) {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
            transform.position = Vector3.Lerp(startPos, target.position, t);
            transform.rotation = Quaternion.Slerp(startRot, target.rotation, t);
            yield return null;
        }

        transform.SetPositionAndRotation(target.position, target.rotation);
        moveCoroutine = null;
        onArrival?.Invoke();
    }
}