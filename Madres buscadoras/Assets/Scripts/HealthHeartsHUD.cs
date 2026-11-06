using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD Profesional Canónico de Nivel 1:
/// - 3 Corazones en la esquina superior izquierda.
/// - Contador de Botellas de Agua (💧 X/8).
/// - Contador de Pistas Encontradas (📁 X/8 | Principales: Y/3).
/// - Nombre dinámico de la zona actual según posición X.
/// - Objetivo dinámico de misión actual.
/// - Botones de acceso directo al Diario [J] y Pausa [Esc].
/// </summary>
public class HealthHeartsHUD : MonoBehaviour
{
    public static HealthHeartsHUD Instance { get; private set; }

    [Header("Referencias de UI")]
    private Text[] heartTexts;
    private Text waterCounterText;
    private Text zoneNameText = null;
    private Text objectiveText = null;
    private Button journalButton;
    private Button pauseButton;

    private Transform playerTransform;
    private float lastZoneCheck = 0f;

    void Awake()
    {
        Instance = this;
    }

    private System.Action<int> onWaterCollectedHandler;
    private System.Action<string> onClueAddedHandler;

    void Start()
    {
        EnsureHUDHierarchy();

        onWaterCollectedHandler = (c) => RefreshHUD();
        onClueAddedHandler = (id) => RefreshHUD();

        // Suscribirse a eventos de GameState
        GameState.OnStateUpdated += RefreshHUD;
        GameState.OnWaterCollected += onWaterCollectedHandler;
        GameState.OnClueAdded += onClueAddedHandler;

        HealthSystem hs = Object.FindAnyObjectByType<HealthSystem>();
        if (hs != null)
        {
            hs.onHealthChanged.AddListener(UpdateHearts);
            UpdateHearts(hs.currentHearts, hs.maxHearts);
        }

        RefreshHUD();
    }

    void OnDestroy()
    {
        GameState.OnStateUpdated -= RefreshHUD;
        if (onWaterCollectedHandler != null) GameState.OnWaterCollected -= onWaterCollectedHandler;
        if (onClueAddedHandler != null) GameState.OnClueAdded -= onClueAddedHandler;
    }

    void Update()
    {
        // Actualizar zona dinámicamente según la posición del jugador cada 0.3s
        if (Time.time - lastZoneCheck > 0.3f)
        {
            lastZoneCheck = Time.time;
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (playerTransform != null && zoneNameText != null)
            {
                zoneNameText.text = "📍 " + GameState.GetCurrentZoneName(playerTransform.position.x);
            }
        }
    }

    public void UpdateHearts(int currentHearts, int maxHearts)
    {
        if (hpTitleText != null)
        {
            hpTitleText.text = $"VIDA: {maxHearts} CORAZONES";
        }

        if (heartTexts == null || heartTexts.Length != maxHearts)
        {
            if (heartsParent != null)
            {
                Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                BuildHeartsUI(maxHearts, heartsParent, defaultFont);
            }
        }

        if (heartTexts == null) return;

        for (int i = 0; i < heartTexts.Length; i++)
        {
            if (heartTexts[i] == null) continue;

            if (i < currentHearts)
            {
                heartTexts[i].text = "❤️";
                heartTexts[i].color = new Color(1f, 0.22f, 0.22f, 1f);
            }
            else
            {
                heartTexts[i].text = "🖤";
                heartTexts[i].color = new Color(0.35f, 0.35f, 0.35f, 0.65f);
            }
        }
    }

    private Transform heartsParent;
    private Text hpTitleText;

    private void BuildHeartsUI(int count, Transform parent, Font font)
    {
        if (heartTexts != null)
        {
            for (int i = 0; i < heartTexts.Length; i++)
            {
                if (heartTexts[i] != null && heartTexts[i].gameObject != null)
                {
                    Destroy(heartTexts[i].gameObject);
                }
            }
        }

        heartTexts = new Text[count];
        for (int i = 0; i < count; i++)
        {
            GameObject hGO = new GameObject($"Heart_{i}");
            hGO.transform.SetParent(parent, false);
            RectTransform hRT = hGO.AddComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 1);
            hRT.anchorMax = new Vector2(0, 1);
            hRT.pivot = new Vector2(0, 1);
            hRT.anchoredPosition = new Vector2(14 + i * 32, -26);
            hRT.sizeDelta = new Vector2(30, 30);

            heartTexts[i] = hGO.AddComponent<Text>();
            heartTexts[i].text = "❤️";
            heartTexts[i].font = font;
            heartTexts[i].fontSize = 24;
            heartTexts[i].alignment = TextAnchor.MiddleCenter;
            heartTexts[i].color = new Color(1f, 0.22f, 0.22f, 1f);
        }

        if (waterCounterText != null)
        {
            RectTransform wRT = waterCounterText.GetComponent<RectTransform>();
            if (wRT != null)
            {
                wRT.anchoredPosition = new Vector2(14 + count * 32 + 12, -26);
            }
        }
    }

    public void RefreshHUD()
    {
        if (waterCounterText != null)
        {
            waterCounterText.text = $"💧 {GameState.WaterBottlesCount} / {GameState.TotalWaterBottles}";
        }

        if (objectiveText != null)
        {
            if (Level1Manager.isVictoryTriggered)
            {
                objectiveText.text = "🏆 ¡FELICIDADES! NIVEL TERMINADO";
            }
            else if (string.IsNullOrEmpty(currentObjectiveOverride))
            {
                objectiveText.text = "🎯 Derrota a los enemigos para superar el Nivel 1";
            }
            else
            {
                objectiveText.text = currentObjectiveOverride;
            }
        }
    }

    private string currentObjectiveOverride = "";

    public void UpdateObjective(string newObj)
    {
        currentObjectiveOverride = newObj;
        if (objectiveText != null)
        {
            objectiveText.text = newObj;
        }
    }

    private void EnsureHUDHierarchy()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cGO = new GameObject("HUD_MainCanvas");
            canvas = cGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cGO.AddComponent<CanvasScaler>();
            cGO.AddComponent<GraphicRaycaster>();
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. Tarjeta de Salud y Suministros (Esquina Superior Izquierda)
        GameObject leftCard = new GameObject("HUD_LeftPanel");
        leftCard.transform.SetParent(canvas.transform, false);

        RectTransform leftRT = leftCard.AddComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0, 1);
        leftRT.anchorMax = new Vector2(0, 1);
        leftRT.pivot = new Vector2(0, 1);
        leftRT.anchoredPosition = new Vector2(20, -20);
        leftRT.sizeDelta = new Vector2(360, 72);

        Image leftBg = leftCard.AddComponent<Image>();
        leftBg.color = new Color(0.10f, 0.08f, 0.07f, 0.88f);

        // Desactivar cualquier barra rectangular legacy
        GameObject oldBar = GameObject.Find("HealthBarCanvas");
        if (oldBar != null) oldBar.SetActive(false);
        GameObject oldRoot = GameObject.Find("HealthBarRoot");
        if (oldRoot != null) oldRoot.SetActive(false);
        GameObject oldHUD = GameObject.Find("HealthHUD");
        if (oldHUD != null) oldHUD.SetActive(false);

        // Título de la sección de Corazones
        GameObject hpTitleGO = new GameObject("HPTitle");
        hpTitleGO.transform.SetParent(leftCard.transform, false);
        RectTransform titleRT = hpTitleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(0, 1);
        titleRT.pivot = new Vector2(0, 1);
        titleRT.anchoredPosition = new Vector2(16, -6);
        titleRT.sizeDelta = new Vector2(180, 18);

        hpTitleText = hpTitleGO.AddComponent<Text>();
        hpTitleText.font = defaultFont;
        hpTitleText.fontSize = 11;
        hpTitleText.fontStyle = FontStyle.Bold;
        hpTitleText.color = new Color(1f, 0.8f, 0.8f, 0.95f);
        hpTitleText.text = "VIDA: 5 CORAZONES";

        // Contador de Agua
        GameObject waterGO = new GameObject("WaterCounter");
        waterGO.transform.SetParent(leftCard.transform, false);
        RectTransform wRT = waterGO.AddComponent<RectTransform>();
        wRT.anchorMin = new Vector2(0, 1);
        wRT.anchorMax = new Vector2(0, 1);
        wRT.pivot = new Vector2(0, 1);
        wRT.anchoredPosition = new Vector2(185, -26);
        wRT.sizeDelta = new Vector2(150, 32);

        waterCounterText = waterGO.AddComponent<Text>();
        waterCounterText.font = defaultFont;
        waterCounterText.fontSize = 16;
        waterCounterText.fontStyle = FontStyle.Bold;
        waterCounterText.color = new Color(0.4f, 0.85f, 1f, 1f);
        waterCounterText.text = $"💧 0 / {GameState.TotalWaterBottles}";

        // Fila dinámica de Corazones (5 corazones)
        heartsParent = leftCard.transform;
        BuildHeartsUI(5, heartsParent, defaultFont);
        waterCounterText.fontStyle = FontStyle.Bold;
        waterCounterText.color = new Color(0.4f, 0.85f, 1f, 1f);
        waterCounterText.text = $"💧 0 / {GameState.TotalWaterBottles}";

        // Ocultar cualquier panel derecho viejo si existía
        GameObject oldRightPanel = GameObject.Find("HUD_RightPanel");
        if (oldRightPanel != null) Object.Destroy(oldRightPanel);

        // Botones Flotantes de Barra Inferior: [J] Diario y [Esc] Pausa
        CreateBottomBarButtons(canvas, defaultFont);
    }

    private void CreateBottomBarButtons(Canvas canvas, Font font)
    {
        GameObject bottomBar = new GameObject("HUD_BottomShortcuts");
        bottomBar.transform.SetParent(canvas.transform, false);

        RectTransform bRT = bottomBar.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 0);
        bRT.anchorMax = new Vector2(0.5f, 0);
        bRT.pivot = new Vector2(0.5f, 0);
        bRT.anchoredPosition = new Vector2(0, 14);
        bRT.sizeDelta = new Vector2(340, 36);

        Image bg = bottomBar.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.05f, 0.85f);

        GameObject txtGO = new GameObject("ShortcutsText");
        txtGO.transform.SetParent(bottomBar.transform, false);
        RectTransform txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        Text shortcuts = txtGO.AddComponent<Text>();
        shortcuts.font = font;
        shortcuts.fontSize = 13;
        shortcuts.alignment = TextAnchor.MiddleCenter;
        shortcuts.color = new Color(0.95f, 0.88f, 0.72f, 1f);
        shortcuts.text = "[E] Interactuar    •    [J] Diario de Búsqueda    •    [Esc] Menú Pausa";
    }
}
