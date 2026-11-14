using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestor Maestro del Nivel 1: «La búsqueda comienza»
/// - Despliega y conecta las 6 Zonas continuas (Km 0 a Km 7, X = -8 a X = 115).
/// - Distribuye las 10 Botellas de Agua de supervivencia a ras de suelo.
/// - Distribuye los 3 Puntos de Control (Checkpoints).
/// - Integra las 8 Pistas Coleccionables (3 Principales obligatorias + 5 Opcionales de memoria).
/// - Despliega los Presidentes enemigos agresivos (avanzan de frente) en el camino.
/// - Integra el HUD de 3 Corazones, Diario [J], Menú Pausa [Esc] y Pantalla de Victoria Profesional.
/// </summary>
public class Level1Manager : MonoBehaviour
{
    public static Level1Manager Instance { get; private set; }

    [Header("Escena Siguiente")]
    public string nextScene = "Nivel_2";

    private GameObject victoryCanvas;
    public bool isLevelFinished = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoInit()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Nivel_1"))
        {
            if (Object.FindAnyObjectByType<Level1Manager>() == null)
            {
                GameObject mgrGO = new GameObject("Level1Manager");
                mgrGO.AddComponent<Level1Manager>();
                Debug.Log("[Level1Manager] Auto-inicializado para Nivel 1 Profesional.");
            }
        }
    }

    public static int totalEnemiesDefeated = 0;
    public static bool isVictoryTriggered = false;

    void Awake()
    {
        Instance = this;
        totalEnemiesDefeated = 0;
        isVictoryTriggered = false;
        isLevelFinished = false;
    }

    void Start()
    {
        // 1. Configurar sistemas de UI profesional
        EnsureHUDAndMenus();

        // 2. Construir las 6 zonas continuas, obstáculos y coleccionables
        EnsureExtendedLevelSetup();
    }

    private void EnsureHUDAndMenus()
    {
        if (Object.FindAnyObjectByType<HealthHeartsHUD>() == null)
        {
            GameObject hudGO = new GameObject("HealthHeartsHUD");
            hudGO.AddComponent<HealthHeartsHUD>();
        }

        if (Object.FindAnyObjectByType<JournalUI>() == null)
        {
            GameObject jGO = new GameObject("JournalUI");
            jGO.AddComponent<JournalUI>();
            DontDestroyOnLoad(jGO);
        }

        if (Object.FindAnyObjectByType<PauseMenuUI>() == null)
        {
            GameObject pGO = new GameObject("PauseMenuUI");
            pGO.AddComponent<PauseMenuUI>();
            DontDestroyOnLoad(pGO);
        }
    }

    private float checkVictoryTimer = 0f;

    void Update()
    {
        if (isVictoryTriggered) return;

        checkVictoryTimer += Time.deltaTime;
        if (checkVictoryTimer >= 0.35f)
        {
            checkVictoryTimer = 0f;

            // Si la jugadora llega al final del recorrido (X >= 350), gana automáticamente
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.transform.position.x >= 350f)
            {
                TriggerLevelCompleted();
                return;
            }

            if (totalEnemiesDefeated > 0)
            {
                int rem = CountRemainingEnemies(null);
                if (rem <= 0)
                {
                    TriggerLevelCompleted();
                }
            }
        }
    }

    private static int CountRemainingEnemies(GameObject excludeEnemy)
    {
        PresidentVariantEnemy[] variants = Object.FindObjectsByType<PresidentVariantEnemy>();
        PresidentEnemy[] bosses = Object.FindObjectsByType<PresidentEnemy>();
        int remainingVariants = 0;
        if (variants != null)
        {
            foreach (var v in variants)
            {
                if (v != null && v.gameObject.activeInHierarchy && v.gameObject != excludeEnemy && !v.IsDead)
                    remainingVariants++;
            }
        }
        int remainingBosses = 0;
        if (bosses != null)
        {
            foreach (var b in bosses)
            {
                if (b != null && b.gameObject.activeInHierarchy && b.gameObject != excludeEnemy && !b.IsDead)
                    remainingBosses++;
            }
        }
        return remainingVariants + remainingBosses;
    }

    private static void TriggerLevelCompleted()
    {
        if (isVictoryTriggered) return;
        isVictoryTriggered = true;

        if (Instance == null)
        {
            Instance = Object.FindAnyObjectByType<Level1Manager>();
            if (Instance == null)
            {
                GameObject mgrGO = new GameObject("Level1Manager");
                Instance = mgrGO.AddComponent<Level1Manager>();
            }
        }

        Instance.isLevelFinished = true;
        Instance.StartCoroutine(Instance.VictorySequenceRoutine());
    }

    /// <summary>
    /// Victoria al derrotar a todos los enemigos o al Jefe Final del retén
    /// </summary>
    public static void NotifyEnemyDefeated(GameObject enemy)
    {
        totalEnemiesDefeated++;
        int remainingTotal = CountRemainingEnemies(enemy);
        bool isFinalBoss = enemy != null && enemy.name == "Enemigo_Presidente";
        Debug.Log($"[Level1Manager] Enemigo derrotado: {(enemy != null ? enemy.name : "Enemigo")}. Restantes: {remainingTotal}");

        // Actualizar HUD
        if (HealthHeartsHUD.Instance != null)
        {
            if (remainingTotal > 0 && !isFinalBoss)
                HealthHeartsHUD.Instance.UpdateObjective($"🎯 Enemigos derrotados: {totalEnemiesDefeated} | Restantes: {remainingTotal}");
            else
                HealthHeartsHUD.Instance.UpdateObjective("HAS GANADO");
        }

        // Gana al matar a todos los enemigos o al derrotar al Jefe Final del retén
        if (remainingTotal <= 0 || isFinalBoss)
        {
            TriggerLevelCompleted();
        }
        else
        {
            Vector3 pos = enemy != null ? enemy.transform.position : Vector3.zero;
            ScoreManager.AddScore(500, pos + Vector3.up * 1.5f, $"¡ENEMIGO DERROTADO! (Restantes: {remainingTotal})");
        }
    }

    public void RespawnPlayerAtCheckpoint()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = GameState.CurrentCheckpointPosition;
            HealthSystem hs = player.GetComponent<HealthSystem>();
            if (hs != null) hs.FullHeal();
        }
    }

    // ====================================================
    //  SECUENCIA Y PANTALLA DE CIERRE: HAS GANADO
    // ====================================================
    private IEnumerator VictorySequenceRoutine()
    {
        Debug.Log("[Level1Manager] HAS GANADO");

        // 1. Sonido de victoria
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayObjective();
        }

        // 2. Banner inmediato que solo diga: "HAS GANADO"
        ShowVictoryBanner("HAS GANADO");

        // 3. Puntos de victoria
        ScoreManager.AddScore(5000, Vector3.zero, "HAS GANADO");

        // 4. Guardar nivel desbloqueado
        PlayerPrefs.SetInt("NivelDesbloqueado", Mathf.Max(PlayerPrefs.GetInt("NivelDesbloqueado", 1), 2));
        PlayerPrefs.Save();

        yield return new WaitForSeconds(0.5f);

        CreateVictoryScreen();
    }

    public static void ShowVictoryBanner(string message)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cGO = new GameObject("VictoryBannerCanvas");
            canvas = cGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cGO.AddComponent<CanvasScaler>();
            cGO.AddComponent<GraphicRaycaster>();
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject bannerGO = new GameObject("LevelPassedBanner");
        bannerGO.transform.SetParent(canvas.transform, false);
        bannerGO.transform.SetAsLastSibling();

        RectTransform rt = bannerGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.62f);
        rt.anchorMax = new Vector2(0.5f, 0.62f);
        rt.sizeDelta = new Vector2(860, 115);

        Image img = bannerGO.AddComponent<Image>();
        img.color = new Color(0.10f, 0.08f, 0.04f, 0.95f);

        // Borde superior e inferior dorado
        GameObject topBar = new GameObject("TopGoldBar");
        topBar.transform.SetParent(bannerGO.transform, false);
        RectTransform topRT = topBar.AddComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0, 1);
        topRT.anchorMax = new Vector2(1, 1);
        topRT.sizeDelta = new Vector2(0, 4);
        topRT.anchoredPosition = new Vector2(0, -2);
        Image topImg = topBar.AddComponent<Image>();
        topImg.color = new Color(1f, 0.85f, 0.25f, 1f);

        GameObject botBar = new GameObject("BotGoldBar");
        botBar.transform.SetParent(bannerGO.transform, false);
        RectTransform botRT = botBar.AddComponent<RectTransform>();
        botRT.anchorMin = new Vector2(0, 0);
        botRT.anchorMax = new Vector2(1, 0);
        botRT.sizeDelta = new Vector2(0, 4);
        botRT.anchoredPosition = new Vector2(0, 2);
        Image botImg = botBar.AddComponent<Image>();
        botImg.color = new Color(1f, 0.85f, 0.25f, 1f);

        // Texto principal
        GameObject textGO = new GameObject("BannerText");
        textGO.transform.SetParent(bannerGO.transform, false);
        Text t = textGO.AddComponent<Text>();
        t.text = message;
        t.font = defaultFont;
        t.fontSize = 38;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(1f, 0.90f, 0.30f, 1f);

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        Object.Destroy(bannerGO, 5.0f);
    }

    private void CreateVictoryScreen()
    {
        if (victoryCanvas != null) return;

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cGO = new GameObject("HUD_MainCanvas");
            canvas = cGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cGO.AddComponent<CanvasScaler>();
            cGO.AddComponent<GraphicRaycaster>();
        }
        canvas.sortingOrder = 99;

        victoryCanvas = new GameObject("VictoryPanel_Felicidades");
        victoryCanvas.transform.SetParent(canvas.transform, false);
        victoryCanvas.transform.SetAsLastSibling();

        RectTransform panelRT = victoryCanvas.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        // Fondo oscuro semitransparente con tinte dorado
        Image bg = victoryCanvas.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.04f, 0.02f, 0.96f);

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Caja central limpia que solo dice "HAS GANADO"
        GameObject box = new GameObject("VictoryBox");
        box.transform.SetParent(victoryCanvas.transform, false);
        RectTransform boxRT = box.AddComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(720, 260);
        boxRT.anchoredPosition = Vector2.zero;

        Image boxBg = box.AddComponent<Image>();
        boxBg.color = new Color(0.10f, 0.07f, 0.04f, 0.99f);

        // --- Borde dorado superior e inferior ---
        MakeColorBar(box, "TopGold", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 6), new Vector2(0, -3), new Color(1f, 0.82f, 0.20f, 1f));
        MakeColorBar(box, "BotGold", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 6), new Vector2(0, 3), new Color(1f, 0.82f, 0.20f, 1f));

        // --- ÚNICO TEXTO: HAS GANADO ---
        GameObject ganadoGO = new GameObject("TitleHasGanado");
        ganadoGO.transform.SetParent(box.transform, false);
        Text ganadoT = ganadoGO.AddComponent<Text>();
        ganadoT.text = "HAS GANADO";
        ganadoT.font = defaultFont;
        ganadoT.fontSize = 68;
        ganadoT.fontStyle = FontStyle.Bold;
        ganadoT.alignment = TextAnchor.MiddleCenter;
        ganadoT.color = new Color(1f, 0.88f, 0.22f, 1f);
        RectTransform ganadoRT = ganadoGO.GetComponent<RectTransform>();
        ganadoRT.anchorMin = Vector2.zero;
        ganadoRT.anchorMax = Vector2.one;
        ganadoRT.sizeDelta = Vector2.zero;
        ganadoRT.anchoredPosition = Vector2.zero;
    }

    // Helper: barra de color para marcos
    private static void MakeColorBar(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPos, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPos;
        Image img = go.AddComponent<Image>();
        img.color = color;
    }

    private void CreateVictoryButton(GameObject parent, string label, float posY, UnityEngine.Events.UnityAction action, Font font, Color bgColor)
    {
        GameObject btnGO = new GameObject($"Btn_{label}");
        btnGO.transform.SetParent(parent.transform, false);

        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, posY);
        rt.sizeDelta = new Vector2(500, 42);

        Image bg = btnGO.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(action);

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
        t.color = Color.white;
    }

    // ====================================================
    //  CONSTRUCCIÓN INTEGRAL DE LAS 6 ZONAS
    // ====================================================
    public static void EnsureExtendedLevelSetup()
    {
        if (Instance == null && Object.FindAnyObjectByType<Level1Manager>() == null)
        {
            GameObject mgrGO = new GameObject("Level1Manager");
            Instance = mgrGO.AddComponent<Level1Manager>();
        }

        // 1. Límites de Cámara extendidos para el recorrido muy largo (X = -8 a X = 365)
        CameraFollow2D cam = Object.FindAnyObjectByType<CameraFollow2D>();
        if (cam != null)
        {
            cam.minX = -8f;
            cam.maxX = 365f;
        }

        // Limpiar completamente cualquier serpiente del escenario
        foreach (var s in Object.FindObjectsByType<SnakeEnemy>())
        {
            if (s != null && s.gameObject != null) Object.Destroy(s.gameObject);
        }
        foreach (var sp in Object.FindObjectsByType<SnakeSpawner>())
        {
            if (sp != null && sp.gameObject != null) Object.Destroy(sp.gameObject);
        }
        foreach (var go in Object.FindObjectsByType<GameObject>())
        {
            if (go != null && (go.name.StartsWith("Snake") || go.name.Contains("SnakeEnemy")))
            {
                Object.Destroy(go);
            }
        }

        // Limpiar cualquier objeto residual de pistas
        foreach (var cpi in Object.FindObjectsByType<CluePickupItem>())
        {
            if (cpi != null && cpi.gameObject != null) Object.Destroy(cpi.gameObject);
        }
        foreach (var go in Object.FindObjectsByType<GameObject>())
        {
            if (go != null && (go.name.StartsWith("ClueObject_") || go.name.StartsWith("Clue_")))
            {
                Object.Destroy(go);
            }
        }

        // 2. Carretera y suelo continuo de -15 a +120
        EnsureExtendedRoad();

        // 3. Fondos panorámicos
        EnsureExtendedBackgrounds();

        // 4. Zona 1: Entrada al Pueblo (Km 0)
        EnsureZone1_Entrada();

        // 5. Zona 2: Calles Silenciosas (Km 1 - 2)
        EnsureZone2_Calles();

        // 6. Zona 3: Camino de Terracería (Km 3 - 4)
        EnsureZone3_Terraceria();

        // 7. Zona 4: El Rancho Abandonado (Km 4.5 - 5.5)
        EnsureZone4_Rancho();

        // 8. Zona 5: Área de Búsqueda (Km 6)
        EnsureZone5_AreaBusqueda();

        // 9. Zona 6: Primer Hallazgo y Retén Final (Km 7)
        EnsureZone6_PrimerHallazgo();
    }

    private static void EnsureExtendedRoad()
    {
        GameObject road = GameObject.Find("tile_carretera_0");
        if (road != null)
        {
            road.transform.position = new Vector3(180f, -5.67f, 0f);
            road.transform.localScale = new Vector3(360f, 0.73f, 1f);

            BoxCollider2D bc = road.GetComponent<BoxCollider2D>();
            if (bc != null)
            {
                bc.size = new Vector2(2.56f, 2.56f);
            }
        }
    }

    private static void EnsureExtendedBackgrounds()
    {
        string[] bgNames = new string[] { "bg_casas_cerca", "bg_monte_medio_0", "bg_montanas_lejos_0", "bg_cielo_0", "bg_cielo" };
        float[] bgOffsets = new float[] { 22f, 44f, 66f, 88f, 110f, 132f, 154f, 176f, 198f, 220f, 242f, 264f, 286f, 308f, 330f, 352f };

        foreach (string baseName in bgNames)
        {
            GameObject baseBG = GameObject.Find(baseName);
            if (baseBG == null) continue;

            for (int i = 0; i < bgOffsets.Length; i++)
            {
                string extName = $"{baseName}_ext_{i}";
                if (GameObject.Find(extName) != null) continue;

                GameObject ext = Object.Instantiate(baseBG, baseBG.transform.parent);
                ext.name = extName;
                ext.transform.position = baseBG.transform.position + new Vector3(bgOffsets[i], 0, 0);

                ParallaxLayer pl = ext.GetComponent<ParallaxLayer>();
                if (pl != null)
                {
                    ParallaxLayer origPl = baseBG.GetComponent<ParallaxLayer>();
                    if (origPl != null) pl.factor = origPl.factor;
                }
            }
        }

        // Colocar carteles de "SE BUSCA" con fotos y nombres de personas en las fachadas de las casas de todo el recorrido
        EnsureMissingPersonPostersOnHouses();
    }

    private static Sprite[] cachedPosterSprites;

    private static readonly (string personName, string details)[] missingPeopleData = new (string, string)[]
    {
        ("MATEO HERNÁNDEZ", "19 años • Visto en Dolores"),
        ("SOFÍA RAMÍREZ", "21 años • Te buscamos hija"),
        ("DIEGO MORALES", "18 años • Estudiante"),
        ("VALERIA TORRES", "22 años • Tu madre te espera"),
        ("EMILIANO CRUZ", "20 años • Desaparecido en ruta"),
        ("CAMILA VÁZQUEZ", "17 años • Vuelve a casa"),
        ("SANTIAGO LÓPEZ", "23 años • Ayuda a encontrarlo"),
        ("LUCÍA MENDOZA", "19 años • No nos rendiremos"),
        ("DANIEL AGUILAR", "20 años • Familia te busca"),
        ("FERNANDA RÍOS", "21 años • Hasta encontrarte"),
        ("ALEJANDRO REYES", "24 años • Visto en el rancho"),
        ("MARIANA CASTRO", "18 años • Vivos los queremos")
    };

    private static void EnsureMissingPersonPostersOnHouses()
    {
        // Posiciones locales exactas sobre las paredes de las 8 casas de cada bloque bg_casas_cerca (38.4 x 10.8)
        Vector2[] houseWallOffsets = new Vector2[]
        {
            new Vector2(-15.5f, -1.35f),
            new Vector2(-11.2f, -1.20f),
            new Vector2(-7.5f,  -1.42f),
            new Vector2(-2.3f,  -1.25f),
            new Vector2( 2.4f,  -1.32f),
            new Vector2( 7.6f,  -1.28f),
            new Vector2( 12.6f, -1.42f),
            new Vector2( 17.4f, -1.22f)
        };

        string[] houseGroupNames = new string[]
        {
            "bg_casas_cerca",
            "bg_casas_cerca_ext_0",
            "bg_casas_cerca_ext_1",
            "bg_casas_cerca_ext_2",
            "bg_casas_cerca_ext_3",
            "bg_casas_cerca_ext_4",
            "bg_casas_cerca_ext_5",
            "bg_casas_cerca_ext_6",
            "bg_casas_cerca_ext_7",
            "bg_casas_cerca_ext_8",
            "bg_casas_cerca_ext_9",
            "bg_casas_cerca_ext_10",
            "bg_casas_cerca_ext_11",
            "bg_casas_cerca_ext_12",
            "bg_casas_cerca_ext_13",
            "bg_casas_cerca_ext_14",
            "bg_casas_cerca_ext_15"
        };

        int globalPosterIndex = 0;

        foreach (string groupName in houseGroupNames)
        {
            GameObject houseGroup = GameObject.Find(groupName);
            if (houseGroup == null) continue;

            for (int h = 0; h < houseWallOffsets.Length; h++)
            {
                string posterName = $"Cartel_SeBusca_{h}";
                if (houseGroup.transform.Find(posterName) != null)
                {
                    globalPosterIndex++;
                    continue;
                }

                var person = missingPeopleData[globalPosterIndex % missingPeopleData.Length];
                Sprite posterSprite = GetOrCreateMissingPosterSprite(globalPosterIndex % 8);
                globalPosterIndex++;

                GameObject posterGO = new GameObject(posterName);
                posterGO.transform.SetParent(houseGroup.transform, false);
                posterGO.transform.localPosition = new Vector3(houseWallOffsets[h].x, houseWallOffsets[h].y, -0.02f);
                posterGO.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

                SpriteRenderer sr = posterGO.AddComponent<SpriteRenderer>();
                sr.sprite = posterSprite;
                sr.sortingOrder = 1; // Delante de la fachada de la casa (0), detrás de personajes (3+)

                // Encabezado "SE BUSCA"
                GameObject headerGO = new GameObject("Header_SeBusca");
                headerGO.transform.SetParent(posterGO.transform, false);
                headerGO.transform.localPosition = new Vector3(0f, 0.56f, -0.01f);
                TextMesh headerTm = headerGO.AddComponent<TextMesh>();
                headerTm.text = "SE BUSCA";
                headerTm.fontSize = 36;
                headerTm.characterSize = 0.036f;
                headerTm.fontStyle = FontStyle.Bold;
                headerTm.alignment = TextAlignment.Center;
                headerTm.anchor = TextAnchor.MiddleCenter;
                headerTm.color = Color.white;
                MeshRenderer hmr = headerGO.GetComponent<MeshRenderer>();
                if (hmr != null) hmr.sortingOrder = 2;

                // Nombre de la persona y edad debajo de la foto
                GameObject nameGO = new GameObject("PersonName");
                nameGO.transform.SetParent(posterGO.transform, false);
                nameGO.transform.localPosition = new Vector3(0f, -0.45f, -0.01f);
                TextMesh nameTm = nameGO.AddComponent<TextMesh>();
                nameTm.text = person.personName + "\n" + person.details;
                nameTm.fontSize = 30;
                nameTm.characterSize = 0.027f;
                nameTm.fontStyle = FontStyle.Bold;
                nameTm.alignment = TextAlignment.Center;
                nameTm.anchor = TextAnchor.MiddleCenter;
                nameTm.color = new Color(0.10f, 0.07f, 0.05f, 1f);
                MeshRenderer nmr = nameGO.GetComponent<MeshRenderer>();
                if (nmr != null) nmr.sortingOrder = 2;
            }
        }
    }

    /// <summary>
    /// Genera un cartel de "SE BUSCA" en pixel-art con papel, encabezado rojo y foto/retrato de persona.
    /// </summary>
    private static Sprite GetOrCreateMissingPosterSprite(int variant)
    {
        if (cachedPosterSprites == null)
            cachedPosterSprites = new Sprite[8];

        int idx = Mathf.Abs(variant) % 8;
        if (cachedPosterSprites[idx] != null)
            return cachedPosterSprites[idx];

        int w = 48;
        int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color paperColor = new Color(0.97f, 0.95f, 0.89f, 1f);
        Color borderColor = new Color(0.22f, 0.18f, 0.15f, 1f);
        Color headerRed = new Color(0.78f, 0.12f, 0.12f, 1f);
        Color photoBg = new Color(0.80f, 0.86f, 0.92f, 1f);
        Color photoFrame = new Color(0.28f, 0.25f, 0.22f, 1f);
        Color tapeColor = new Color(0.92f, 0.85f, 0.58f, 0.9f);

        // Variantes de tonos de piel, cabello y ropa para cada retrato
        Color[] skinTones = new Color[]
        {
            new Color(0.86f, 0.65f, 0.48f, 1f),
            new Color(0.78f, 0.56f, 0.40f, 1f),
            new Color(0.90f, 0.70f, 0.54f, 1f),
            new Color(0.72f, 0.50f, 0.35f, 1f)
        };
        Color[] hairColors = new Color[]
        {
            new Color(0.14f, 0.10f, 0.08f, 1f),
            new Color(0.28f, 0.16f, 0.09f, 1f),
            new Color(0.08f, 0.08f, 0.10f, 1f),
            new Color(0.36f, 0.22f, 0.12f, 1f)
        };
        Color[] shirtColors = new Color[]
        {
            new Color(0.18f, 0.42f, 0.72f, 1f),
            new Color(0.72f, 0.22f, 0.28f, 1f),
            new Color(0.20f, 0.56f, 0.36f, 1f),
            new Color(0.58f, 0.32f, 0.66f, 1f),
            new Color(0.82f, 0.52f, 0.16f, 1f),
            new Color(0.22f, 0.58f, 0.64f, 1f),
            new Color(0.68f, 0.24f, 0.44f, 1f),
            new Color(0.28f, 0.36f, 0.52f, 1f)
        };

        Color skin = skinTones[idx % skinTones.Length];
        Color hair = hairColors[idx % hairColors.Length];
        Color shirt = shirtColors[idx % shirtColors.Length];
        bool longHair = (idx % 2 == 1);

        // 1. Papel base y borde
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (x <= 1 || x >= w - 2 || y <= 1 || y >= h - 2)
                    tex.SetPixel(x, y, borderColor);
                else
                    tex.SetPixel(x, y, paperColor);
            }
        }

        // 2. Franja roja superior ("SE BUSCA")
        for (int x = 3; x < w - 3; x++)
        {
            for (int y = h - 13; y < h - 3; y++)
            {
                tex.SetPixel(x, y, headerRed);
            }
        }

        // 3. Cinta adhesiva en las esquinas superiores
        for (int x = 4; x <= 11; x++)
            for (int y = h - 4; y < h; y++)
                tex.SetPixel(x, y, tapeColor);
        for (int x = w - 12; x <= w - 5; x++)
            for (int y = h - 4; y < h; y++)
                tex.SetPixel(x, y, tapeColor);

        // 4. Recuadro de la fotografía (x: 9..38, y: 22..48)
        for (int x = 9; x <= 38; x++)
        {
            for (int y = 22; y <= 48; y++)
            {
                if (x == 9 || x == 38 || y == 22 || y == 48)
                    tex.SetPixel(x, y, photoFrame);
                else
                    tex.SetPixel(x, y, photoBg);
            }
        }

        // 5. Dibujar retrato de la persona dentro de la foto
        // Hombros / camisa (y: 23..30)
        for (int x = 14; x <= 33; x++)
        {
            for (int y = 23; y <= 30; y++)
            {
                float distFromCenter = Mathf.Abs(x - 23.5f);
                if (distFromCenter <= 9.5f - (y - 23) * 0.45f)
                    tex.SetPixel(x, y, shirt);
            }
        }

        // Cuello (y: 30..33)
        for (int x = 21; x <= 26; x++)
            for (int y = 30; y <= 33; y++)
                tex.SetPixel(x, y, skin);

        // Cabello largo detrás si aplica
        if (longHair)
        {
            for (int x = 16; x <= 31; x++)
                for (int y = 27; y <= 43; y++)
                    if (Mathf.Abs(x - 23.5f) <= 7.5f)
                        tex.SetPixel(x, y, hair);
        }

        // Cabeza / rostro (centro en 23.5, 38)
        for (int x = 17; x <= 30; x++)
        {
            for (int y = 32; y <= 44; y++)
            {
                float dx = (x - 23.5f) / 6.0f;
                float dy = (y - 38.0f) / 6.2f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    tex.SetPixel(x, y, skin);
                }
            }
        }

        // Cabello superior
        for (int x = 17; x <= 30; x++)
        {
            for (int y = 41; y <= 46; y++)
            {
                float dx = (x - 23.5f) / 6.5f;
                float dy = (y - 41.5f) / 4.2f;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    tex.SetPixel(x, y, hair);
                }
            }
        }

        // Ojos, cejas y boca en la foto
        Color eyeColor = new Color(0.10f, 0.08f, 0.08f, 1f);
        tex.SetPixel(21, 38, eyeColor);
        tex.SetPixel(26, 38, eyeColor);
        tex.SetPixel(20, 39, hair);
        tex.SetPixel(21, 39, hair);
        tex.SetPixel(26, 39, hair);
        tex.SetPixel(27, 39, hair);
        // Boca
        for (int x = 22; x <= 25; x++)
            tex.SetPixel(x, 34, new Color(0.55f, 0.28f, 0.25f, 1f));

        // 6. Renglones sutiles en la parte inferior para el nombre
        Color lineTint = new Color(0.82f, 0.78f, 0.72f, 1f);
        for (int x = 7; x <= 40; x++)
        {
            tex.SetPixel(x, 15, lineTint);
            tex.SetPixel(x, 8, lineTint);
        }

        tex.Apply();
        cachedPosterSprites[idx] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 42f);
        return cachedPosterSprites[idx];
    }

    // --- ZONA 1: Entrada al Pueblo (X = 0 a 55) ---
    private static void EnsureZone1_Entrada()
    {
        // Letrero de Dolores Hidalgo
        GameObject letrero = GameObject.Find("prop_letrero_dolores_0");
        if (letrero == null)
        {
            letrero = new GameObject("prop_letrero_dolores_0");
            SpriteRenderer sr = letrero.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSpriteSafe("Assets/Sprites/Props/prop_letrero_dolores.png", "prop_letrero_dolores");
            sr.sortingOrder = 0;
            letrero.transform.position = new Vector3(0.5f, -2.15f, 0f);
        }

        // Checkpoint #0 (Inicio del pueblo)
        SpawnCheckpoint(0, "Acceso a Dolores", new Vector3(-2.0f, -2.4f, 0f));

        // Botellas 1 y 2 de 10 (a ras de suelo: y = -2.35)
        SpawnWaterBottle("bottle_z1_inicio", new Vector3(12.0f, -2.35f, 0f));
        SpawnWaterBottle("bottle_z1_camino", new Vector3(38.0f, -2.35f, 0f));

        // Corazones de vida coleccionables
        SpawnHeartPickup("z1_camino", new Vector3(20.0f, -2.35f, 0f));
        SpawnHeartPickup("z1_camino_b", new Vector3(44.0f, -2.35f, 0f));

        // Enemigos Zona 1 bien separados a lo largo de X = 7 a 55
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Presidente Patrullero", new Vector3(7.0f, -2.3f, 0f), 4.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Agente Agresivo Z1", new Vector3(11.0f, -2.3f, 0f), 6.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Guardia Pueblo A", new Vector3(15.0f, -2.3f, 0f), 4.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Perseguidor Entrada A", new Vector3(19.0f, -2.3f, 0f), 6.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Escolta Frontal Z1", new Vector3(23.0f, -2.3f, 0f), 6.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Presidente Vigilante", new Vector3(27.0f, -2.3f, 0f), 5.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Inspector de Entrada", new Vector3(31.5f, -2.3f, 0f), 3.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Agente Rápido Z1", new Vector3(35.0f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Guardia Pueblo B", new Vector3(39.0f, -2.3f, 0f), 4.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Presidente Perseguidor", new Vector3(43.0f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Comandante Entrada", new Vector3(46.5f, -2.3f, 0f), 4.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Guardia de Entrada", new Vector3(49.5f, -2.3f, 0f), 4.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Guardia Frontal Z1", new Vector3(52.5f, -2.3f, 0f), 4.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Cazador de Entrada", new Vector3(55.5f, -2.3f, 0f), 6.5f);
        // Hongos venenosos en Zona 1 (si no los saltas, te envenenan y mueres)
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(16.5f, -2.42f, 0f));
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(33.2f, -2.42f, 0f));
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(48.0f, -2.42f, 0f));

        // Más AMLOs en Zona 1
        PresidentEnemy.SpawnAt(new Vector3(14.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona1_A");
        PresidentEnemy.SpawnAt(new Vector3(29.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona1_B");
        PresidentEnemy.SpawnAt(new Vector3(41.5f, -2.15f, 0f), null, "AMLO_Gansos_Zona1_C");
        PresidentEnemy.SpawnAt(new Vector3(54.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona1_D");
    }

    // --- ZONA 2: Calles Silenciosas (X = 58 a 115) ---
    private static void EnsureZone2_Calles()
    {
        // Tienda
        GameObject tienda = GameObject.Find("prop_tienda_0");
        if (tienda == null)
        {
            tienda = new GameObject("prop_tienda_0");
            SpriteRenderer sr = tienda.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSpriteSafe("Assets/Sprites/Props/prop_tienda.png", "prop_tienda");
            sr.sortingOrder = 0;
            tienda.transform.position = new Vector3(62.0f, -2.15f, 0f);
            tienda.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        }

        // Botellas 3 y 4 de 10 (a ras de suelo: y = -2.35)
        SpawnWaterBottle("bottle_z2_tienda", new Vector3(65.0f, -2.35f, 0f));
        SpawnWaterBottle("bottle_z2_calles", new Vector3(95.0f, -2.35f, 0f));

        // Corazones de vida coleccionables
        SpawnHeartPickup("z2_calles", new Vector3(78.0f, -2.35f, 0f));
        SpawnHeartPickup("z2_calles_b", new Vector3(106.0f, -2.35f, 0f));

        // Enemigos Zona 2 bien separados a lo largo de X = 60 a 115
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Guardia Municipal", new Vector3(60.0f, -2.3f, 0f), 5.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Perseguidor Tienda", new Vector3(64.0f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Vigilante de Esquina", new Vector3(68.0f, -2.3f, 0f), 4.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Inspector de Tienda", new Vector3(72.0f, -2.3f, 0f), 3.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Presidente Perseguidor", new Vector3(76.0f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Cazador de Calles", new Vector3(80.0f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Guardia de Avenida", new Vector3(84.0f, -2.3f, 0f), 5.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Inspector de Calles", new Vector3(88.5f, -2.3f, 0f), 3.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Agente Frontal Z2", new Vector3(92.5f, -2.3f, 0f), 6.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Vigilante Urbano", new Vector3(96.5f, -2.3f, 0f), 4.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Escolta de Calle", new Vector3(100.5f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Custodio de Manzana", new Vector3(104.5f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Jefe de Manzana", new Vector3(108.5f, -2.3f, 0f), 4.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Asaltante de Cuadra", new Vector3(112.5f, -2.3f, 0f), 6.5f);
        // Hongos venenosos en Zona 2 (si no los saltas, te envenenan y mueres)
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(70.0f, -2.42f, 0f));
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(90.5f, -2.42f, 0f));
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(110.5f, -2.42f, 0f));

        // Más AMLOs en Zona 2
        PresidentEnemy.SpawnAt(new Vector3(74.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona2_A");
        PresidentEnemy.SpawnAt(new Vector3(86.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona2_B");
        PresidentEnemy.SpawnAt(new Vector3(102.5f, -2.15f, 0f), null, "AMLO_Gansos_Zona2_C");
        PresidentEnemy.SpawnAt(new Vector3(115.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona2_D");
    }

    // --- ZONA 3: Camino de Terracería (X = 118 a 175) ---
    private static void EnsureZone3_Terraceria()
    {
        // Cactus con espinas y obstáculos
        SpawnHazard(HazardObstacle.HazardType.SpikeCactus, new Vector3(125.0f, -2.35f, 0f));
        SpawnHazard(HazardObstacle.HazardType.RoadPit, new Vector3(152.0f, -3.2f, 0f));

        // Botellas 5 y 6 de 10 (a ras de suelo: y = -2.35)
        SpawnWaterBottle("bottle_z3_camino", new Vector3(132.0f, -2.35f, 0f));
        SpawnWaterBottle("bottle_z3_terraceria", new Vector3(162.0f, -2.35f, 0f));

        // Corazones de vida coleccionables
        SpawnHeartPickup("z3_terraceria", new Vector3(142.0f, -2.35f, 0f));
        SpawnHeartPickup("z3_terraceria_b", new Vector3(168.0f, -2.35f, 0f));

        // Checkpoint #1: Altar de la Esperanza
        SpawnCheckpoint(1, "Altar de la Esperanza (Km 3)", new Vector3(172.0f, -2.4f, 0f));

        // Enemigos Zona 3 bien separados a lo largo de X = 120 a 176
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Agente de Tránsito", new Vector3(120.0f, -2.3f, 0f), 7.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Guardia de Brecha A", new Vector3(124.0f, -2.3f, 0f), 5.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Centinela del Camino", new Vector3(128.0f, -2.3f, 0f), 4.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Corredor de Brecha", new Vector3(132.0f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Vigilante de Terracería", new Vector3(136.0f, -2.3f, 0f), 5.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Agente de Terracería Z3", new Vector3(140.0f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Bloqueador de Camino", new Vector3(144.0f, -2.3f, 0f), 4.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Guardia de Brecha", new Vector3(148.5f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Perseguidor de Terracería", new Vector3(152.5f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Vigilante de Polvo", new Vector3(156.5f, -2.3f, 0f), 5.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Guardia de Altar", new Vector3(160.5f, -2.3f, 0f), 4.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Presidente Rastreador", new Vector3(164.5f, -2.3f, 0f), 6.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Custodio del Km 3", new Vector3(168.5f, -2.3f, 0f), 4.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Comandante de Ruta", new Vector3(172.0f, -2.3f, 0f), 4.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Cazador de Ruta", new Vector3(176.0f, -2.3f, 0f), 6.5f);
        // Hongos venenosos en Zona 3 (si no los saltas, te envenenan y mueres)
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(130.0f, -2.42f, 0f));
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(150.5f, -2.42f, 0f));
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(166.5f, -2.42f, 0f));

        // Más AMLOs en Zona 3
        PresidentEnemy.SpawnAt(new Vector3(134.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona3_A");
        PresidentEnemy.SpawnAt(new Vector3(146.5f, -2.15f, 0f), null, "AMLO_Gansos_Zona3_B");
        PresidentEnemy.SpawnAt(new Vector3(158.5f, -2.15f, 0f), null, "AMLO_Gansos_Zona3_C");
        PresidentEnemy.SpawnAt(new Vector3(174.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona3_D");
    }

    // --- ZONA 4: El Rancho Abandonado (X = 178 a 235) ---
    private static void EnsureZone4_Rancho()
    {
        // Caja empujable en el pasillo del rancho
        if (GameObject.Find("CajaRanchoEmpujable") == null)
        {
            GameObject crate = new GameObject("CajaRanchoEmpujable");
            crate.transform.position = new Vector3(182.0f, -2.35f, 0f);
            crate.AddComponent<PushableCrate>();
        }

        // Botellas 7 y 8 de 10 (a ras de suelo: y = -2.35)
        SpawnWaterBottle("bottle_z4_rancho", new Vector3(192.0f, -2.35f, 0f));
        SpawnWaterBottle("bottle_z4_bodega", new Vector3(222.0f, -2.35f, 0f));

        // Corazones de vida coleccionables
        SpawnHeartPickup("z4_rancho", new Vector3(202.0f, -2.35f, 0f));
        SpawnHeartPickup("z4_rancho_b", new Vector3(228.0f, -2.35f, 0f));

        // Enemigos Zona 4 bien separados a lo largo de X = 180 a 235
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Guardaespaldas del Rancho", new Vector3(180.0f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Centinela de Entrada Rancho", new Vector3(184.0f, -2.3f, 0f), 4.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Guardia del Corral", new Vector3(188.0f, -2.3f, 0f), 4.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Comisionado Interventor", new Vector3(192.0f, -2.3f, 0f), 4.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Perseguidor de Troje", new Vector3(196.0f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Guardia Frontal Rancho", new Vector3(200.0f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Custodio de Establo", new Vector3(204.0f, -2.3f, 0f), 7.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Jefe de Bodega", new Vector3(208.5f, -2.3f, 0f), 4.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Escolta del Rancho", new Vector3(212.5f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Vigilante de Bodega", new Vector3(216.5f, -2.3f, 0f), 5.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Asaltante del Granero", new Vector3(220.5f, -2.3f, 0f), 7.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Presidente Perseguidor Veloz", new Vector3(224.5f, -2.3f, 0f), 7.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Guardia de Cerca", new Vector3(228.5f, -2.3f, 0f), 5.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Supervisor Pesado Z4", new Vector3(232.5f, -2.3f, 0f), 3.5f);
        // Hongos venenosos en Zona 4 (si no los saltas, te envenenan y mueres)
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(190.0f, -2.42f, 0f));
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(210.5f, -2.42f, 0f));
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(226.5f, -2.42f, 0f));

        // Más AMLOs en Zona 4
        PresidentEnemy.SpawnAt(new Vector3(194.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona4_A");
        PresidentEnemy.SpawnAt(new Vector3(206.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona4_B");
        PresidentEnemy.SpawnAt(new Vector3(218.5f, -2.15f, 0f), null, "AMLO_Gansos_Zona4_C");
        PresidentEnemy.SpawnAt(new Vector3(235.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona4_D");
    }

    // --- ZONA 5: Área de Búsqueda (X = 238 a 295) ---
    private static void EnsureZone5_AreaBusqueda()
    {
        // Caseta de bombeo y Checkpoint #2
        SpawnCheckpoint(2, "Caseta de Bombeo (Km 5)", new Vector3(240.0f, -2.4f, 0f));

        // Botella 9 de 10 (a ras de suelo: y = -2.35)
        SpawnWaterBottle("bottle_z5_visible", new Vector3(265.0f, -2.35f, 0f));

        // Corazones de vida coleccionables
        SpawnHeartPickup("z5_busqueda", new Vector3(252.0f, -2.35f, 0f));
        SpawnHeartPickup("z5_busqueda_b", new Vector3(280.0f, -2.35f, 0f));

        // Enemigos Zona 5 bien separados a lo largo de X = 242 a 295
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Escolta de Bombeo", new Vector3(242.0f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Guardia Institucional Pesado", new Vector3(246.0f, -2.3f, 0f), 3.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Vigilante del Valle", new Vector3(250.0f, -2.3f, 0f), 4.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Cazador del Llano", new Vector3(254.0f, -2.3f, 0f), 7.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Custodio de la Fosa", new Vector3(258.0f, -2.3f, 0f), 7.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Guardia de la Sierra Z5", new Vector3(262.0f, -2.3f, 0f), 7.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Comandante del Llano", new Vector3(266.0f, -2.3f, 0f), 3.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Inspector de Zona", new Vector3(270.5f, -2.3f, 0f), 4.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Agente de Bloqueo Z5", new Vector3(274.5f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Capitán de Terreno", new Vector3(278.5f, -2.3f, 0f), 3.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Guardia de Sendero", new Vector3(282.5f, -2.3f, 0f), 5.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Jefe de Sector Z5", new Vector3(286.5f, -2.3f, 0f), 4.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Perseguidor de la Sierra", new Vector3(290.5f, -2.3f, 0f), 7.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Centinela Veloz Z5", new Vector3(294.0f, -2.3f, 0f), 7.0f);
        // Hongos venenosos en Zona 5 (si no los saltas, te envenenan y mueres)
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(248.0f, -2.42f, 0f));
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(272.5f, -2.42f, 0f));
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(288.5f, -2.42f, 0f));

        // Más AMLOs en Zona 5
        PresidentEnemy.SpawnAt(new Vector3(256.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona5_A");
        PresidentEnemy.SpawnAt(new Vector3(268.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona5_B");
        PresidentEnemy.SpawnAt(new Vector3(284.5f, -2.15f, 0f), null, "AMLO_Gansos_Zona5_C");
        PresidentEnemy.SpawnAt(new Vector3(295.5f, -2.15f, 0f), null, "AMLO_Gansos_Zona5_D");
    }

    // --- ZONA 6: Primer Hallazgo y Retén Final (X = 298 a 356) ---
    private static void EnsureZone6_PrimerHallazgo()
    {
        // Botella 10 de 10 (a ras de suelo previo al retén: y = -2.35)
        SpawnWaterBottle("bottle_z6_hallazgo", new Vector3(305.0f, -2.35f, 0f));

        // Corazones de vida coleccionables previos al Jefe Final
        SpawnHeartPickup("z6_final", new Vector3(318.0f, -2.35f, 0f));
        SpawnHeartPickup("z6_final_b", new Vector3(338.0f, -2.35f, 0f));

        // Escolta del retén bien separados a lo largo de X = 300 a 348 + 3 AMLOs en el Retén Final
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Guardia Avanzado del Retén", new Vector3(300.0f, -2.3f, 0f), 4.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Escolta Frontal Retén A", new Vector3(304.0f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Inspector Pesado Retén", new Vector3(308.0f, -2.3f, 0f), 4.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Guardia de Élite del Retén", new Vector3(312.0f, -2.3f, 0f), 6.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.PatrolBasic, "Vigilante de Barrera", new Vector3(316.0f, -2.3f, 0f), 5.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Capitán del Retén", new Vector3(320.5f, -2.3f, 0f), 3.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Escolta Frontal Retén B", new Vector3(324.5f, -2.3f, 0f), 6.5f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Escolta Presidencial", new Vector3(328.5f, -2.3f, 0f), 6.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Defensor del Retén Final", new Vector3(332.5f, -2.3f, 0f), 6.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.ZoneHeavy, "Comandante Supremo Retén", new Vector3(337.0f, -2.3f, 0f), 4.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Guardaespaldas Final A", new Vector3(341.0f, -2.3f, 0f), 7.0f);
        SpawnPresidentVariant(PresidentVariantEnemy.PresidentType.Pursuer, "Guardaespaldas Final B", new Vector3(345.0f, -2.3f, 0f), 7.0f);

        // Hongos venenosos en Zona 6 (si no los saltas, te envenenan y mueres)
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(310.0f, -2.42f, 0f));
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(326.5f, -2.42f, 0f));
        SpawnHazard(HazardObstacle.HazardType.PoisonMushroom, new Vector3(343.0f, -2.42f, 0f));

        // Seis AMLOs Jefes en el Retén Final de la Zona 6 lanzando gansos
        PresidentEnemy.SpawnAt(new Vector3(306.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona6_A");
        PresidentEnemy.SpawnAt(new Vector3(318.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona6_B");
        PresidentEnemy.SpawnAt(new Vector3(330.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona6_C");
        PresidentEnemy.SpawnAt(new Vector3(335.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona6_D");
        PresidentEnemy.SpawnAt(new Vector3(342.0f, -2.15f, 0f), null, "AMLO_Gansos_Zona6_E");

        GameObject pres = GameObject.Find("Enemigo_Presidente");
        if (pres != null)
        {
            pres.transform.position = new Vector3(348.0f, -2.15f, 0f);
        }
        else
        {
            PresidentEnemy.SpawnAt(new Vector3(348.0f, -2.15f, 0f), null, "Enemigo_Presidente");
        }

        // Parada de Bus y Doña Tere
        EnsureDonaTereZone();
    }

    private static void EnsureDonaTereZone()
    {
        // Parada de bus
        GameObject parada = GameObject.Find("prop_parada_bus_0");
        if (parada == null)
        {
            parada = new GameObject("prop_parada_bus_0");
            SpriteRenderer sr = parada.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSpriteSafe("Assets/Sprites/Props/prop_parada_bus.png", "prop_parada_bus");
            sr.sortingOrder = 0;
            parada.transform.position = new Vector3(354.0f, -2.15f, 0f);
        }

        // Doña Tere
        GameObject donaTere = GameObject.Find("Dona_Tere");
        if (donaTere == null)
        {
            donaTere = new GameObject("Dona_Tere");
            donaTere.transform.position = new Vector3(356.0f, -2.25f, 0f);

            SpriteRenderer sr = donaTere.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSpriteSafe("Assets/Sprites/Personajes/dona_tere_sheet.png", "dona_tere_sheet");
            sr.sortingOrder = 3;

            CircleCollider2D cc = donaTere.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = 1.8f;

            Interactable inter = donaTere.AddComponent<Interactable>();
            inter.speaker = "Doña Tere";
            inter.lines = new string[] {
                "¡Esperanza! El retén de los presidentes vigila el camino.",
                "Debes derrotar a todos los enemigos para completar el Nivel 1."
            };
        }

        // Limpiar cualquier letrero fijo previo para que solo aparezca al matar a todos los enemigos
        GameObject letreroPrevio = GameObject.Find("Letrero_Final_NivelTerminado");
        if (letreroPrevio != null) Object.Destroy(letreroPrevio);
    }

    private static void SpawnWaterBottle(string id, Vector3 pos)
    {
        string goName = $"WaterBottle_{id}";
        if (GameObject.Find(goName) != null) return;

        GameObject bGO = new GameObject(goName);
        bGO.transform.position = pos;

        CircleCollider2D cc = bGO.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.65f;

        bGO.AddComponent<SpriteRenderer>();
        WaterBottleItem wbi = bGO.AddComponent<WaterBottleItem>();
        wbi.bottleId = id;
    }

    private static void SpawnHeartPickup(string id, Vector3 pos)
    {
        string goName = $"HeartPickup_{id}";
        if (GameObject.Find(goName) != null) return;

        GameObject hGO = new GameObject(goName);
        hGO.transform.position = pos;

        CircleCollider2D cc = hGO.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.60f;

        hGO.AddComponent<SpriteRenderer>();
        HeartPickupItem hpi = hGO.AddComponent<HeartPickupItem>();
        hpi.heartId = id;
    }

    private static void SpawnClueItem(string clueId, string title, Vector3 pos)
    {
        string goName = $"ClueObject_{clueId}";
        if (GameObject.Find(goName) != null) return;

        GameObject cGO = new GameObject(goName);
        cGO.transform.position = pos;

        CircleCollider2D cc = cGO.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 1.0f;

        CluePickupItem cpi = cGO.AddComponent<CluePickupItem>();
        cpi.clueId = clueId;
        cpi.promptTitle = title;
        cpi.interactionDistance = 2.2f;
    }

    private static void SpawnCheckpoint(int index, string name, Vector3 pos)
    {
        string goName = $"Checkpoint_{index}";
        if (GameObject.Find(goName) != null) return;

        GameObject cpGO = new GameObject(goName);
        cpGO.transform.position = pos;

        CheckpointTrigger ct = cpGO.AddComponent<CheckpointTrigger>();
        ct.checkpointIndex = index;
        ct.checkpointName = name;
    }

    private static void SpawnPresidentVariant(PresidentVariantEnemy.PresidentType type, string title, Vector3 pos, float patrolDist)
    {
        string goName = $"PresidentEnemy_{type}_{title}_{pos.x:F1}";
        if (GameObject.Find(goName) != null) return;

        GameObject pGO = new GameObject(goName);
        pGO.transform.position = pos;

        PresidentVariantEnemy pve = pGO.AddComponent<PresidentVariantEnemy>();
        pve.presidentType = type;
        pve.enemyTitle = title;
        pve.patrolDistance = patrolDist;
    }

    private static void SpawnHazard(HazardObstacle.HazardType type, Vector3 pos)
    {
        string goName = $"Hazard_{type}_{pos.x:F0}";
        if (GameObject.Find(goName) != null) return;

        GameObject hGO = new GameObject(goName);
        hGO.transform.position = pos;

        HazardObstacle ho = hGO.AddComponent<HazardObstacle>();
        ho.hazardType = type;
        ho.damageToPlayer = 1;
    }

    public static Sprite LoadSpriteSafe(string assetPath, string resourceFallback)
    {
        Sprite s = null;
#if UNITY_EDITOR
        s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#endif
        if (s == null) s = Resources.Load<Sprite>(resourceFallback);
        return s;
    }
}
