using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BossHitFlash : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Hit Tint Settings")]
    [SerializeField] private float tintDurationSeconds = 0.08f;
    [Range(0f, 1f)]
    [SerializeField] private float whiteTintStrength = 0.35f;
    [SerializeField] private Color tintColor = new Color(1f, 0.25f, 0.25f, 1f);

    private Coroutine tintRoutine;
    private Color baseColor = Color.white;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogWarning("BossHitFlash: No SpriteRenderer found to tint.", this);
            return;
        }

        baseColor = spriteRenderer.color;
    }

    public void Flash()
    {
        // Keep the public API name so BossHealth can call it.
        if (spriteRenderer == null) return;

        if (tintRoutine != null)
            StopCoroutine(tintRoutine);

        tintRoutine = StartCoroutine(TintRoutine());
    }

    private IEnumerator TintRoutine()
    {
        // In case something else modified the sprite color (animations, etc.), treat current as base.
        baseColor = spriteRenderer.color;

        float duration = Mathf.Max(0.01f, tintDurationSeconds);
        float strength = Mathf.Clamp01(whiteTintStrength);

        // Tint towards tintColor (works on visible pixels; transparent pixels remain invisible due to alpha).
        spriteRenderer.color = Color.Lerp(baseColor, tintColor, strength);
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = baseColor;
        tintRoutine = null;
    }

    private void OnDisable()
    {
        if (tintRoutine != null)
        {
            StopCoroutine(tintRoutine);
            tintRoutine = null;
        }
        if (spriteRenderer != null)
            spriteRenderer.color = baseColor;
    }
}

