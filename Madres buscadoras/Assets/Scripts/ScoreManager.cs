using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestor de puntuación y popups flotantes en pantalla.
/// Otorga puntos al saltar obstáculos, destruir frases y golpear a la Presidenta.
/// Muestra un contador HUD elegante en la esquina superior derecha y números flotantes en el mundo.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public static int CurrentScore { get; private set; } = 0;
    public static event Action<int> OnScoreChanged;

    // Referencias HUD
    private Canvas scoreCanvas;
    private Text scoreText;
    private RectTransform panelRect;
    private int displayedScore = 0;
    private Coroutine punchCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("ScoreManager");
            Instance = go.AddComponent<ScoreManager>();
            DontDestroyOnLoad(go);
        }
        Instance.EnsureHUD();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureHUD();
    }

    public static void ResetScore()
    {
        CurrentScore = 0;
        OnScoreChanged?.Invoke(0);
        if (Instance != null) Instance.displayedScore = 0;
    }

    public static void AddScore(int amount, Vector3? worldPos = null, string popupText = null)
    {
        CurrentScore += amount;
        OnScoreChanged?.Invoke(CurrentScore);

        if (worldPos.HasValue && !string.IsNullOrEmpty(popupText))
        {
            SpawnFloatingPopup(worldPos.Value, popupText, new Color(1f, 0.88f, 0.2f, 1f));
        }

        if (Instance != null)
        {
            Instance.PunchScoreHUD();
        }
    }

    /// <summary>
    /// Genera un texto flotante en las coordenadas del mundo que asciende y se desvanece.
    /// Ideal para "+10 HP", "+100 PTS", etc.
    /// </summary>
    public static void SpawnFloatingPopup(Vector3 worldPos, string text, Color color)
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.FloatingPopupRoutine(worldPos, text, color));
    }

    private IEnumerator FloatingPopupRoutine(Vector3 startPos, string text, Color color)
    {
        GameObject popupGO = new GameObject("ScorePopup_" + text);
        popupGO.transform.position = startPos + new Vector3(0, 0.3f, 0);

        // Sombra negra
        GameObject shadowGO = new GameObject("Shadow");
        shadowGO.transform.SetParent(popupGO.transform, false);
        shadowGO.transform.localPosition = new Vector3(0.04f, -0.04f, 0);
        TextMesh shadowTm = shadowGO.AddComponent<TextMesh>();
        shadowTm.text = text;
        shadowTm.fontSize = 38;
        shadowTm.characterSize = 0.045f;
        shadowTm.alignment = TextAlignment.Center;
        shadowTm.anchor = TextAnchor.MiddleCenter;
        shadowTm.color = new Color(0, 0, 0, 0.9f);
        MeshRenderer smr = shadowGO.GetComponent<MeshRenderer>();
        if (smr != null) smr.sortingOrder = 49;

        // Texto principal
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(popupGO.transform, false);
        textGO.transform.localPosition = Vector3.zero;
        TextMesh mainTm = textGO.AddComponent<TextMesh>();
        mainTm.text = text;
        mainTm.fontSize = 38;
        mainTm.characterSize = 0.045f;
        mainTm.alignment = TextAlignment.Center;
        mainTm.anchor = TextAnchor.MiddleCenter;
        mainTm.color = color;
        MeshRenderer mmr = textGO.GetComponent<MeshRenderer>();
        if (mmr != null) mmr.sortingOrder = 50;

        float duration = 0.85f;
        float elapsed = 0f;
        Vector3 curPos = popupGO.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Ascender con desaceleración
            curPos.y += Time.deltaTime * 1.6f * (1f - t * 0.4f);
            popupGO.transform.position = curPos;

            // Escalar un poco al inicio
            float scale = t < 0.2f ? Mathf.Lerp(0.7f, 1.25f, t / 0.2f) : Mathf.Lerp(1.25f, 1.0f, (t - 0.2f) / 0.8f);
            popupGO.transform.localScale = Vector3.one * scale;

            // Desvanecer alpha al final
            if (t > 0.45f)
            {
                float alpha = Mathf.Lerp(1f, 0f, (t - 0.45f) / 0.55f);
                Color c = color;
                c.a = alpha;
                mainTm.color = c;

                Color sc = Color.black;
                sc.a = alpha * 0.8f;
                shadowTm.color = sc;
            }

            yield return null;
        }

        Destroy(popupGO);
    }

    void Update()
    {
        // Conteo suave de puntos en el HUD
        if (displayedScore != CurrentScore)
        {
            int diff = CurrentScore - displayedScore;
            int step = Mathf.Max(1, Mathf.RoundToInt(diff * Time.deltaTime * 12f));
            displayedScore = Mathf.RoundToInt(Mathf.MoveTowards(displayedScore, CurrentScore, step));
            UpdateHUDText();
        }
    }

    private void UpdateHUDText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"★ PUNTOS: {displayedScore:N0}";
        }
    }

    private void PunchScoreHUD()
    {
        if (punchCoroutine != null) StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        if (panelRect == null) yield break;

        Vector3 originalScale = Vector3.one;
        Vector3 punchScale = new Vector3(1.18f, 1.18f, 1f);

        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            panelRect.localScale = Vector3.Lerp(originalScale, punchScale, t / 0.12f);
            yield return null;
        }

        t = 0f;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            panelRect.localScale = Vector3.Lerp(punchScale, originalScale, t / 0.18f);
            yield return null;
        }

        panelRect.localScale = originalScale;
    }

    public void EnsureHUD()
    {
        if (scoreText != null && scoreCanvas != null) return;

        // Buscar canvas existente de vida o crear uno para el HUD
        GameObject canvasGO = GameObject.Find("HealthBarCanvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("ScoreCanvas");
            scoreCanvas = canvasGO.AddComponent<Canvas>();
            scoreCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scoreCanvas.sortingOrder = 30;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        else
        {
            scoreCanvas = canvasGO.GetComponent<Canvas>();
        }

        // Crear Root del Score HUD en la esquina superior derecha
        GameObject scoreRoot = GameObject.Find("ScoreHUD_Root");
        if (scoreRoot == null)
        {
            scoreRoot = new GameObject("ScoreHUD_Root");
            scoreRoot.transform.SetParent(canvasGO.transform, false);
        }

        panelRect = scoreRoot.GetComponent<RectTransform>();
        if (panelRect == null) panelRect = scoreRoot.AddComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-20, -20);
        panelRect.sizeDelta = new Vector2(210, 36);

        // Fondo oscuro con borde
        Image panelImg = scoreRoot.GetComponent<Image>();
        if (panelImg == null) panelImg = scoreRoot.AddComponent<Image>();
        panelImg.color = new Color(0.10f, 0.05f, 0.07f, 0.88f);

        // Texto de puntuación
        GameObject textGO = scoreRoot.transform.Find("ScoreText")?.gameObject;
        if (textGO == null)
        {
            textGO = new GameObject("ScoreText");
            textGO.transform.SetParent(scoreRoot.transform, false);
        }

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        if (textRT == null) textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(10, 0);
        textRT.offsetMax = new Vector2(-10, 0);

        scoreText = textGO.GetComponent<Text>();
        if (scoreText == null) scoreText = textGO.AddComponent<Text>();

        // Usar fuente disponible del sistema
        Font defaultFont = null;
        try { defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch {}
        if (defaultFont == null)
        {
            try { defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch {}
        }
        if (defaultFont != null) scoreText.font = defaultFont;
        scoreText.fontSize = 17;
        scoreText.fontStyle = FontStyle.Bold;
        scoreText.alignment = TextAnchor.MiddleCenter;
        scoreText.color = new Color(1f, 0.88f, 0.3f, 1f); // Dorado

        UpdateHUDText();
    }
}
