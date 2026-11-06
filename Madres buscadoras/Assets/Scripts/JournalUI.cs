using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Diario de Búsqueda de Esperanza (Cuaderno de Campo):
/// - Se abre con la tecla 'J' o botón del HUD.
/// - Pestaña 1: Evidencias y Pistas (8 pistas con fichas detalladas).
/// - Pestaña 2: Mapa del Territorio (6 zonas conectadas y ubicación actual).
/// - Pestaña 3: Misiones de Investigación (4 retos del nivel 1).
/// - Pausa el juego mientras se consulta para leer con calma y respeto.
/// </summary>
public class JournalUI : MonoBehaviour
{
    public static JournalUI Instance { get; private set; }

    private GameObject journalRoot;
    private bool isOpen = false;
    private int currentTab = 0; // 0 = Mapa, 1 = Misiones

    private Text tabMapaText;
    private Text tabMisionesText;
    private GameObject contentArea;

    private string selectedClueId = "pista_nota_poste";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoInit()
    {
        if (Object.FindAnyObjectByType<JournalUI>() == null)
        {
            GameObject go = new GameObject("JournalUI");
            go.AddComponent<JournalUI>();
            DontDestroyOnLoad(go);
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Alternar Diario con la tecla J (o cerrar con Esc si está abierto)
        if (Input.GetKeyDown(KeyCode.J))
        {
            ToggleJournal();
        }
        else if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseJournal();
        }
    }

    public void ToggleJournal()
    {
        if (isOpen) CloseJournal();
        else OpenJournal();
    }

    public void OpenJournal()
    {
        isOpen = true;
        Time.timeScale = 0f;

        if (journalRoot == null)
        {
            BuildJournalUI();
        }

        journalRoot.SetActive(true);
        RefreshTabContent();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSFX();
        }
    }

    public void CloseJournal()
    {
        isOpen = false;
        Time.timeScale = 1f;

        if (journalRoot != null)
        {
            journalRoot.SetActive(false);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSFX();
        }
    }

    private void SetTab(int tabIndex)
    {
        currentTab = tabIndex;
        RefreshTabContent();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClickSFX();
    }

    private void RefreshTabContent()
    {
        if (contentArea == null) return;

        // Limpiar contenido previo
        foreach (Transform child in contentArea.transform)
        {
            Destroy(child.gameObject);
        }

        // Colores de pestañas activas
        Color activeTabCol = new Color(1f, 0.88f, 0.4f, 1f);
        Color inactiveTabCol = new Color(0.7f, 0.65f, 0.6f, 1f);

        if (tabMapaText != null) tabMapaText.color = (currentTab == 0) ? activeTabCol : inactiveTabCol;
        if (tabMisionesText != null) tabMisionesText.color = (currentTab == 1) ? activeTabCol : inactiveTabCol;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (currentTab == 0)
        {
            BuildMapTab(defaultFont);
        }
        else
        {
            BuildMissionsTab(defaultFont);
        }
    }

    private void BuildCluesTab(Font font)
    {
        // Panel dividido: Izquierda lista (ancho 280), Derecha detalle (ancho 480)
        GameObject leftList = new GameObject("CluesList");
        leftList.transform.SetParent(contentArea.transform, false);
        RectTransform leftRT = leftList.AddComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0, 0);
        leftRT.anchorMax = new Vector2(0.36f, 1);
        leftRT.offsetMin = new Vector2(10, 10);
        leftRT.offsetMax = new Vector2(-10, -10);

        int idx = 0;
        foreach (var clue in GameState.GetAllClues())
        {
            string cid = clue.id;
            GameObject btnGO = new GameObject($"ClueBtn_{cid}");
            btnGO.transform.SetParent(leftList.transform, false);

            RectTransform btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0, 1);
            btnRT.anchorMax = new Vector2(1, 1);
            btnRT.pivot = new Vector2(0, 1);
            btnRT.anchoredPosition = new Vector2(0, -idx * 50);
            btnRT.sizeDelta = new Vector2(0, 44);

            Image bg = btnGO.AddComponent<Image>();
            bg.color = (cid == selectedClueId) ? new Color(0.32f, 0.22f, 0.15f, 0.95f) : new Color(0.18f, 0.14f, 0.12f, 0.85f);

            Button btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                selectedClueId = cid;
                RefreshTabContent();
            });

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            RectTransform txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(8, 0);
            txtRT.offsetMax = new Vector2(-8, 0);

            Text t = txtGO.AddComponent<Text>();
            t.font = font;
            t.fontSize = 12;
            t.alignment = TextAnchor.MiddleLeft;

            if (clue.isDiscovered)
            {
                t.color = clue.isPrincipal ? new Color(1f, 0.85f, 0.25f, 1f) : new Color(0.95f, 0.92f, 0.88f, 1f);
                t.text = (clue.isPrincipal ? "[★] " : "[•] ") + clue.title;
            }
            else
            {
                t.color = new Color(0.55f, 0.5f, 0.45f, 0.7f);
                t.text = "[?] Indicio sin descubrir";
            }

            idx++;
        }

        // Panel de Detalle Derecho
        GameObject rightDetail = new GameObject("ClueDetail");
        rightDetail.transform.SetParent(contentArea.transform, false);
        RectTransform rightRT = rightDetail.AddComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.38f, 0);
        rightRT.anchorMax = new Vector2(1, 1);
        rightRT.offsetMin = new Vector2(10, 10);
        rightRT.offsetMax = new Vector2(-10, -10);

        Image detailBg = rightDetail.AddComponent<Image>();
        detailBg.color = new Color(0.14f, 0.11f, 0.09f, 0.92f);

        GameObject detailTextGO = new GameObject("DetailText");
        detailTextGO.transform.SetParent(rightDetail.transform, false);
        RectTransform dtRT = detailTextGO.AddComponent<RectTransform>();
        dtRT.anchorMin = Vector2.zero;
        dtRT.anchorMax = Vector2.one;
        dtRT.offsetMin = new Vector2(18, 16);
        dtRT.offsetMax = new Vector2(-18, -16);

        Text dt = detailTextGO.AddComponent<Text>();
        dt.font = font;
        dt.fontSize = 14;
        dt.color = new Color(0.96f, 0.92f, 0.85f, 1f);
        dt.lineSpacing = 1.25f;

        GameState.ClueData selectedClue = null;
        foreach (var c in GameState.GetAllClues())
        {
            if (c.id == selectedClueId) { selectedClue = c; break; }
        }

        if (selectedClue != null && selectedClue.isDiscovered)
        {
            dt.text =
                $"<b><size=18>{selectedClue.title}</size></b>\n" +
                $"<color=#E0AA38>Ubicación: {selectedClue.zoneName}</color>\n" +
                $"<color=#88D566>Categoría: {(selectedClue.isPrincipal ? "★ EVIDENCIA OBLIGATORIA" : "• MEMORIA Y CONTEXTO")}</color>\n\n" +
                $"<b>Resumen de hallazgo:</b>\n{selectedClue.summary}\n\n" +
                $"<b>Contenido de la evidencia:</b>\n<i>\"{selectedClue.fullText}\"</i>";
        }
        else
        {
            dt.text =
                "<b><size=18>INDÍCIO NO DESCUBIERTO</size></b>\n\n" +
                "Continúa explorando casas abandonadas, caminos de terracería y zonas remotas para registrar esta evidencia en el diario.\n\n" +
                "<i>«Buscar no es rendirse; cada papel y cada prenda reconstruye la memoria.»</i>";
        }
    }

    private void BuildMapTab(Font font)
    {
        GameObject mapGO = new GameObject("MapOverview");
        mapGO.transform.SetParent(contentArea.transform, false);
        RectTransform mRT = mapGO.AddComponent<RectTransform>();
        mRT.anchorMin = Vector2.zero;
        mRT.anchorMax = Vector2.one;
        mRT.offsetMin = new Vector2(20, 20);
        mRT.offsetMax = new Vector2(-20, -20);

        Image mBg = mapGO.AddComponent<Image>();
        mBg.color = new Color(0.12f, 0.09f, 0.08f, 0.95f);

        GameObject textGO = new GameObject("MapText");
        textGO.transform.SetParent(mapGO.transform, false);
        RectTransform tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(20, 20);
        tRT.offsetMax = new Vector2(-20, -20);

        Text mt = textGO.AddComponent<Text>();
        mt.font = font;
        mt.fontSize = 15;
        mt.color = new Color(0.95f, 0.90f, 0.80f, 1f);
        mt.lineSpacing = 1.25f;

        float playerX = 0f;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerX = p.transform.position.x;
        string currentZone = GameState.GetCurrentZoneName(playerX);

        mt.text =
            "<b><size=20>CROQUIS REGIONAL: DOLORES HIDALGO Y BRECHAS RURALES</size></b>\n" +
            $"<color=#E0AA38>Ubicación Actual de Esperanza: {currentZone}</color>\n\n" +
            "┌─── <b>[Zona 1: Entrada al pueblo]</b> (Km 0) ── Letrero carretero y primera botella.\n" +
            "│      └── Primer enemigo presidente (patrullero básico).\n" +
            "├─── <b>[Zona 2: Calles silenciosas]</b> (Km 1) ── Viviendas abandonadas y tienda.\n" +
            "│      └── [Pista Principal 1]: Foto grupal tras la puerta cerrada.\n" +
            "├─── <b>[Zona 3: Camino de terracería]</b> (Km 3) ── Cactus, piedras rodantes y desvíos.\n" +
            "│      └── [Checkpoint #1] Altar de la Esperanza.\n" +
            "├─── <b>[Zona 4: El rancho abandonado]</b> (Km 4.5) ── Pasillo con caja móvil y bodega con clave.\n" +
            "│      └── [Pista Principal 2]: Talón de boleto y bitácora de flete.\n" +
            "├─── <b>[Zona 5: Área de búsqueda]</b> (Km 6) ── Anomalías en el terreno y rastreo.\n" +
            "│      └── [Checkpoint #2] Caseta de bombeo.\n" +
            "└─── <b>[Zona 6: El primer hallazgo]</b> (Km 7) ── Retén del presidente y evidencia final.\n" +
            "       └── [Pista Principal 3]: Coordenadas a la sierra para el Nivel 2.";
    }

    private void BuildMissionsTab(Font font)
    {
        GameObject misGO = new GameObject("MissionsOverview");
        misGO.transform.SetParent(contentArea.transform, false);
        RectTransform misRT = misGO.AddComponent<RectTransform>();
        misRT.anchorMin = Vector2.zero;
        misRT.anchorMax = Vector2.one;
        misRT.offsetMin = new Vector2(20, 20);
        misRT.offsetMax = new Vector2(-20, -20);

        Image mBg = misGO.AddComponent<Image>();
        mBg.color = new Color(0.12f, 0.09f, 0.08f, 0.95f);

        GameObject textGO = new GameObject("MissionsText");
        textGO.transform.SetParent(misGO.transform, false);
        RectTransform tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(20, 20);
        tRT.offsetMax = new Vector2(-20, -20);

        Text mt = textGO.AddComponent<Text>();
        mt.font = font;
        mt.fontSize = 15;
        mt.color = new Color(0.95f, 0.90f, 0.80f, 1f);
        mt.lineSpacing = 1.35f;

        mt.text =
            "<b><size=20>OBJETIVOS DE LA MADRE BUSCADORA: NIVEL 1</size></b>\n\n" +
            "<b>• Misión Principal: Eliminar a los enemigos</b>\n" +
            "   Derrota a los presidentes patrulleros y al jefe final del retén para pasar el Nivel 1.\n\n" +
            "<b>• Combate con Pico:</b>\n" +
            "   Presiona la <b>Tecla V</b> para asestar un golpe contundente con el pico y dañar enemigos o serpientes.\n\n" +
            "<b>• Supervivencia:</b>\n" +
            "   Dispones de <b>3 Corazones</b> de vida. Recolecta botellas de agua fresca (💧) para restaurar corazones.\n\n" +
            "<b>• Condición de Victoria:</b>\n" +
            "   Únicamente al derrotar a los enemigos se desbloqueará la victoria: <b>«¡HAS PASADO EL NIVEL 1!»</b>.";
    }

    private void BuildJournalUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        journalRoot = new GameObject("JournalUIRoot");
        journalRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRT = journalRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        Image modalBg = journalRoot.AddComponent<Image>();
        modalBg.color = new Color(0.04f, 0.03f, 0.02f, 0.85f);

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Cuaderno central
        GameObject book = new GameObject("JournalBookFrame");
        book.transform.SetParent(journalRoot.transform, false);
        RectTransform bookRT = book.AddComponent<RectTransform>();
        bookRT.anchorMin = new Vector2(0.5f, 0.5f);
        bookRT.anchorMax = new Vector2(0.5f, 0.5f);
        bookRT.sizeDelta = new Vector2(860, 520);

        Image bookBg = book.AddComponent<Image>();
        bookBg.color = new Color(0.18f, 0.14f, 0.11f, 0.98f);

        // Barra de Título Superior
        GameObject titleGO = new GameObject("JournalTitle");
        titleGO.transform.SetParent(book.transform, false);
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -12);
        titleRT.sizeDelta = new Vector2(-40, 44);

        Text titleText = titleGO.AddComponent<Text>();
        titleText.text = "CUADERNO DE BÚSQUEDA Y MEMORIA — ESPERANZA";
        titleText.font = defaultFont;
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.90f, 0.5f, 1f);

        // Botón Cerrar [X]
        GameObject closeBtnGO = new GameObject("CloseButton");
        closeBtnGO.transform.SetParent(book.transform, false);
        RectTransform closeRT = closeBtnGO.AddComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.anchoredPosition = new Vector2(-22, -22);
        closeRT.sizeDelta = new Vector2(32, 32);

        Image closeBg = closeBtnGO.AddComponent<Image>();
        closeBg.color = new Color(0.5f, 0.2f, 0.2f, 1f);

        Button closeBtn = closeBtnGO.AddComponent<Button>();
        closeBtn.onClick.AddListener(CloseJournal);

        GameObject xGO = new GameObject("XText");
        xGO.transform.SetParent(closeBtnGO.transform, false);
        RectTransform xRT = xGO.AddComponent<RectTransform>();
        xRT.anchorMin = Vector2.zero;
        xRT.anchorMax = Vector2.one;
        Text xt = xGO.AddComponent<Text>();
        xt.text = "X";
        xt.font = defaultFont;
        xt.fontSize = 16;
        xt.alignment = TextAnchor.MiddleCenter;
        xt.color = Color.white;

        // Fila de Pestañas
        GameObject tabRow = new GameObject("TabRow");
        tabRow.transform.SetParent(book.transform, false);
        RectTransform trRT = tabRow.AddComponent<RectTransform>();
        trRT.anchorMin = new Vector2(0, 1);
        trRT.anchorMax = new Vector2(1, 1);
        trRT.pivot = new Vector2(0.5f, 1);
        trRT.anchoredPosition = new Vector2(0, -60);
        trRT.sizeDelta = new Vector2(-40, 36);

        tabMapaText = CreateTabButton(tabRow, "1. MAPA DE ZONAS", 0, defaultFont);
        tabMisionesText = CreateTabButton(tabRow, "2. OBJETIVOS Y COMBATE", 1, defaultFont);

        // Área Central de Contenido
        contentArea = new GameObject("ContentArea");
        contentArea.transform.SetParent(book.transform, false);
        RectTransform caRT = contentArea.AddComponent<RectTransform>();
        caRT.anchorMin = Vector2.zero;
        caRT.anchorMax = Vector2.one;
        caRT.offsetMin = new Vector2(20, 20);
        caRT.offsetMax = new Vector2(-20, -105);
    }

    private Text CreateTabButton(GameObject parent, string label, int tabIdx, Font font)
    {
        GameObject btnGO = new GameObject($"TabBtn_{tabIdx}");
        btnGO.transform.SetParent(parent.transform, false);
        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(tabIdx * 0.5f, 0);
        rt.anchorMax = new Vector2((tabIdx + 1) * 0.5f, 1);
        rt.offsetMin = new Vector2(4, 0);
        rt.offsetMax = new Vector2(-4, 0);

        Image bg = btnGO.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.20f, 0.16f, 1f);

        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(() => SetTab(tabIdx));

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;

        Text t = txtGO.AddComponent<Text>();
        t.text = label;
        t.font = font;
        t.fontSize = 13;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.85f, 0.80f, 0.70f, 1f);

        return t;
    }
}
