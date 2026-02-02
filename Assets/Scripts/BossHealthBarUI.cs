using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple boss health bar UI anchored at the bottom-center of the screen.
/// Listens to BossHealth.HealthChanged and updates a filled Image.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [Header("Scene References (optional)")]
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private Canvas canvas;

    [Header("Layout")]
    [Tooltip("Optional: if assigned, UI is created under this transform instead of auto-creating one.")]
    [SerializeField] private RectTransform barRoot;
    [SerializeField] private Vector2 barSize = new Vector2(520f, 24f);
    [SerializeField] private float yOffset = 28f;
    [SerializeField] private float innerPadding = 2f;

    [Header("Style")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color fillColor = new Color(0.9f, 0.1f, 0.1f, 0.95f);

    [Header("Visibility")]
    [Tooltip("If true, the bar stays hidden until a BossHealth exists.")]
    [SerializeField] private bool hideWhenNoBossFound = true;

    private Image backgroundImage;
    private Image fillImage;
    private Sprite defaultUiSprite;
    private bool createdRuntimeSprite;

    private void Awake()
    {
        EnsureReferences();
        EnsureUiBuilt();

        // Initialize (in case we missed the boss's Awake event).
        if (bossHealth != null)
        {
            OnHealthChanged(bossHealth.CurrentHealth, bossHealth.MaxHealth);
        }
        else if (hideWhenNoBossFound)
        {
            SetVisible(false);
        }
    }

    private void OnEnable()
    {
        BossHealth.HealthChanged += OnHealthChanged;
        BossHealth.BossDied += OnBossDied;
    }

    private void OnDisable()
    {
        BossHealth.HealthChanged -= OnHealthChanged;
        BossHealth.BossDied -= OnBossDied;
    }

    private void EnsureReferences()
    {
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        if (bossHealth == null)
        {
            bossHealth = FindObjectOfType<BossHealth>();
        }
    }

    private void EnsureUiBuilt()
    {
        if (defaultUiSprite == null)
        {
            // Avoid relying on Unity's built-in UI sprite paths (they vary by version).
            // A simple 1x1 white sprite works for solid-color bars.
            Texture2D tex = Texture2D.whiteTexture;
            if (tex != null)
            {
                defaultUiSprite = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                defaultUiSprite.name = "RuntimeWhiteSprite";
                createdRuntimeSprite = true;
            }
        }

        if (barRoot == null)
        {
            if (canvas == null)
            {
                Debug.LogError("BossHealthBarUI: No Canvas found in scene. Cannot create boss health bar.");
                return;
            }

            GameObject root = new GameObject("Boss Health Bar");
            root.layer = canvas.gameObject.layer;
            root.transform.SetParent(canvas.transform, false);

            barRoot = root.AddComponent<RectTransform>();
            barRoot.anchorMin = new Vector2(0.5f, 0f);
            barRoot.anchorMax = new Vector2(0.5f, 0f);
            barRoot.pivot = new Vector2(0.5f, 0f);
            barRoot.anchoredPosition = new Vector2(0f, yOffset);
            barRoot.sizeDelta = barSize;
        }

        if (backgroundImage == null)
        {
            backgroundImage = barRoot.GetComponent<Image>();
            if (backgroundImage == null) backgroundImage = barRoot.gameObject.AddComponent<Image>();
            backgroundImage.sprite = defaultUiSprite;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = backgroundColor;
            backgroundImage.raycastTarget = false;
        }

        if (fillImage == null)
        {
            Transform existingFill = barRoot.Find("Fill");
            GameObject fillObj;
            if (existingFill != null)
            {
                fillObj = existingFill.gameObject;
            }
            else
            {
                fillObj = new GameObject("Fill");
                fillObj.layer = barRoot.gameObject.layer;
                fillObj.transform.SetParent(barRoot, false);
            }

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            if (fillRect == null) fillRect = fillObj.AddComponent<RectTransform>();

            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fillRect.offsetMin = new Vector2(innerPadding, innerPadding);
            fillRect.offsetMax = new Vector2(-innerPadding, -innerPadding);

            fillImage = fillObj.GetComponent<Image>();
            if (fillImage == null) fillImage = fillObj.AddComponent<Image>();
            fillImage.sprite = defaultUiSprite;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0; // left
            fillImage.fillAmount = 1f;
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
        }
    }

    private void OnDestroy()
    {
        // Clean up runtime sprite to avoid leaking editor objects across play sessions.
        if (createdRuntimeSprite && defaultUiSprite != null)
        {
            Destroy(defaultUiSprite);
            defaultUiSprite = null;
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        if (fillImage == null || barRoot == null)
        {
            EnsureReferences();
            EnsureUiBuilt();
            if (fillImage == null || barRoot == null) return;
        }

        float safeMax = Mathf.Max(1f, max);
        float pct = Mathf.Clamp01(current / safeMax);
        fillImage.fillAmount = pct;

        // If we found a boss via the event (static), keep a reference for initial state checks.
        if (bossHealth == null) bossHealth = FindObjectOfType<BossHealth>();

        SetVisible(!(hideWhenNoBossFound && bossHealth == null) && pct > 0f);
    }

    private void OnBossDied()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (barRoot != null)
        {
            barRoot.gameObject.SetActive(visible);
        }
    }
}

