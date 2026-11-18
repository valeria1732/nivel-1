#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Herramienta de Editor: configura la barra de vida y las serpientes en la escena activa.
/// Menu: Madres Buscadoras > Setup Completo de Escena
/// </summary>
public static class SceneSetupTool
{
    // ====================================================
    //  MENU PRINCIPAL
    // ====================================================
    [MenuItem("Madres Buscadoras/Setup Completo de Escena")]
    static void SetupAll()
    {
        SetupHealthBar();
        SetupPickaxe();
        SetupColoredCacti();
        SetupPresidentEnemy();
        SetupExtendedLevel1();
        Debug.Log("[SceneSetupTool] Setup completo terminado.");
    }

    [MenuItem("Madres Buscadoras/0 - Abrir Menu de Inicio")]
    public static void OpenMenuScene()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");
    }

    [MenuItem("Madres Buscadoras/10 - Abrir Nivel 1")]
    public static void OpenLevel1Scene()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Nivel_1.unity");
    }

    // ====================================================
    //  BARRA DE VIDA
    // ====================================================
    [MenuItem("Madres Buscadoras/1 - Crear Barra de Vida")]
    static void SetupHealthBar()
    {
        // Evitar duplicados
        if (GameObject.Find("HealthBarCanvas") != null)
        {
            Debug.LogWarning("[SceneSetupTool] La barra de vida ya existe en la escena.");
            return;
        }

        // --- Agregar HealthSystem al jugador ---
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<HealthSystem>() == null)
        {
            var hs = player.AddComponent<HealthSystem>();
            hs.maxHealth = 100;
            SpriteRenderer psr = player.GetComponentInChildren<SpriteRenderer>();
            if (psr != null) hs.characterSprite = psr;
            Debug.Log("[SceneSetupTool] HealthSystem agregado al jugador.");
        }

        // --- Canvas ---
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // --- Root de la barra (esquina superior izquierda) ---
        GameObject barRoot = new GameObject("HealthBarRoot");
        barRoot.transform.SetParent(canvasGO.transform, false);
        RectTransform barRT = barRoot.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0, 1);
        barRT.anchorMax = new Vector2(0, 1);
        barRT.pivot     = new Vector2(0, 1);
        barRT.anchoredPosition = new Vector2(20, -20);
        barRT.sizeDelta = new Vector2(220, 28);

        // --- Fondo ---
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(barRoot.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.05f, 0.05f, 0.85f);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // --- Relleno (Fill) ---
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barRoot.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color     = new Color(0.18f, 0.78f, 0.35f, 1f);
        fillImg.type      = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(2, 2);
        fillRT.offsetMax = new Vector2(-2, -2);

        // --- Icono de corazon (texto emoji como placeholder) ---
        GameObject heart = new GameObject("HeartIcon");
        heart.transform.SetParent(barRoot.transform, false);
        Text heartTxt = heart.AddComponent<Text>();
        heartTxt.text      = "♥";
        heartTxt.fontSize  = 20;
        heartTxt.color     = new Color(1f, 0.3f, 0.3f, 1f);
        heartTxt.alignment = TextAnchor.MiddleLeft;
        heartTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform heartRT = heart.GetComponent<RectTransform>();
        heartRT.anchorMin = new Vector2(0, 0);
        heartRT.anchorMax = new Vector2(0, 1);
        heartRT.sizeDelta = new Vector2(28, 0);
        heartRT.anchoredPosition = new Vector2(14, 0);

        // --- Etiqueta HP ---
        GameObject label = new GameObject("HPLabel");
        label.transform.SetParent(barRoot.transform, false);
        Text labelTxt = label.AddComponent<Text>();
        labelTxt.text      = "100 / 100";
        labelTxt.fontSize  = 11;
        labelTxt.color     = Color.white;
        labelTxt.alignment = TextAnchor.MiddleRight;
        labelTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(0, 0);
        labelRT.offsetMax = new Vector2(-4, 0);

        // --- Script HealthBar ---
        HealthBar hb = barRoot.AddComponent<HealthBar>();
        hb.fillImage = fillImg;
        hb.hpLabel   = labelTxt;

        // Marcar escena como modificada
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SceneSetupTool] Barra de vida creada correctamente.");
    }

    // ====================================================
    //  SERPIENTES (RETIRADAS)
    // ====================================================
    [MenuItem("Madres Buscadoras/2 - Limpiar Serpientes de la Escena")]
    static void SetupSnakes()
    {
        GameObject group = GameObject.Find("SnakeGroup");
        if (group != null) Object.DestroyImmediate(group);

        foreach (var s in Object.FindObjectsByType<SnakeEnemy>())
        {
            if (s != null && s.gameObject != null) Object.DestroyImmediate(s.gameObject);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SceneSetupTool] Serpientes retiradas de la escena.");
    }

    // ====================================================
    //  PICO EN LA MUNECA (TECLA V)
    // ====================================================
    [MenuItem("Madres Buscadoras/4 - Configurar Pico en la Muneca (Tecla V)")]
    public static void SetupPickaxe()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            var pc = Object.FindAnyObjectByType<PlayerController2D>();
            if (pc != null) player = pc.gameObject;
        }

        if (player == null)
        {
            Debug.LogError("[SceneSetupTool] No se encontro al jugador.");
            return;
        }

        GameObject pickaxeGO = GameObject.Find("PickaxeHolder");
        if (pickaxeGO == null)
        {
            pickaxeGO = new GameObject("PickaxeHolder");
        }

        // Cargar Sprite de pico con brazos
        Sprite picoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Props/prop_pico_brazos.png");
        if (picoSprite == null)
        {
            picoSprite = Resources.Load<Sprite>("prop_pico_brazos");
        }

        SpriteRenderer sr = pickaxeGO.GetComponent<SpriteRenderer>();
        if (sr == null) sr = pickaxeGO.AddComponent<SpriteRenderer>();
        sr.sprite = picoSprite;
        sr.sortingOrder = 10; // Frente al personaje

        PickaxeItem item = pickaxeGO.GetComponent<PickaxeItem>();
        if (item == null) item = pickaxeGO.AddComponent<PickaxeItem>();
        item.attackKey = KeyCode.V;
        item.player = player.GetComponent<PlayerController2D>();
        item.spriteRenderer = sr;
        item.damage = 25;
        item.attackRange = 1.3f;
        item.shoulderOffset = new Vector2(0.08f, 0.35f);
        item.baseScale = new Vector3(0.52f, 0.52f, 1f);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SceneSetupTool] ¡Pico con manos configurado en la muñeca con la tecla V!");
    }

    // ====================================================
    //  CACTUS DE FLORES DE COLORES
    // ====================================================
    [MenuItem("Madres Buscadoras/6 - Agregar Mas Cactus de Flores de Colores")]
    public static void SetupColoredCacti()
    {
        var cactiData = new (string name, string spritePath, Vector3 pos, float scale, bool flipX, float cycle)[]
        {
            ("Cactus_Amarillo_1", "Assets/Sprites/Props/prop_cactus_amarillo.png", new Vector3(-6.5f, -2.35f, 0.2f),  0.28f, false, 3.2f),
            ("Cactus_Rojo_1",     "Assets/Sprites/Props/prop_cactus_rojo.png",     new Vector3( 0.8f, -2.32f, 0.1f),  0.32f, false, 2.8f),
            ("Cactus_Naranja_1",  "Assets/Sprites/Props/prop_cactus_naranja.png",  new Vector3( 4.8f, -2.35f, 0.15f), 0.27f, true,  3.5f),
            ("Cactus_Morado_1",   "Assets/Sprites/Props/prop_cactus_morado.png",   new Vector3(11.2f, -2.33f, 0.1f),  0.33f, false, 3.1f),
            ("Cactus_Amarillo_2", "Assets/Sprites/Props/prop_cactus_amarillo.png", new Vector3(15.2f, -2.35f, 0.15f), 0.29f, false, 3.4f),
            ("Cactus_Rojo_2",     "Assets/Sprites/Props/prop_cactus_rojo.png",     new Vector3(18.5f, -2.31f, 0.2f),  0.35f, true,  2.9f),
        };

        foreach (var data in cactiData)
        {
            if (GameObject.Find(data.name) != null) continue;

            GameObject go = new GameObject(data.name);
            go.transform.position = data.pos;
            go.transform.localScale = new Vector3(data.flipX ? -data.scale : data.scale, data.scale, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(data.spritePath);
            sr.flipX = data.flipX;

            CactusFlowerAnimator anim = go.AddComponent<CactusFlowerAnimator>();
            anim.cycleDuration = data.cycle;
            anim.openRatio = 0.6f;
            anim.randomOffset = true;
            anim.scaleMin = 0.85f;
            anim.scaleMax = 1f;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SceneSetupTool] ¡Cactus de flores de colores configurados en la escena!");
    }

    // ====================================================
    //  PANTALLA DE INICIO (BOTON JUGAR)
    // ====================================================
    [MenuItem("Madres Buscadoras/7 - Crear Pantalla de Inicio (Boton JUGAR)")]
    public static void SetupStartScreen()
    {
        // 1. Asegurar EventSystem para procesar clicks
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[SceneSetupTool] EventSystem creado.");
        }

        // 2. Buscar Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cGO = new GameObject("HUD_Canvas");
            canvas = cGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cGO.AddComponent<CanvasScaler>();
            cGO.AddComponent<GraphicRaycaster>();
        }

        // Evitar duplicados
        Transform existing = canvas.transform.Find("StartScreenPanel");
        if (existing != null)
        {
            Debug.LogWarning("[SceneSetupTool] StartScreenPanel ya existe.");
            return;
        }

        // 3. Crear Panel de Pantalla de Inicio
        GameObject panel = new GameObject("StartScreenPanel");
        panel.transform.SetParent(canvas.transform, false);
        panel.transform.SetAsLastSibling(); // Delante de todo el HUD

        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // Fondo oscuro semitransparente que resalta el juego
        Image bgImg = panel.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.05f, 0.05f, 0.92f);

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Titulo del juego
        GameObject titleGO = new GameObject("GameTitle");
        titleGO.transform.SetParent(panel.transform, false);
        Text titleTxt = titleGO.AddComponent<Text>();
        titleTxt.text = "MADRES BUSCADORAS";
        titleTxt.font = defaultFont;
        titleTxt.fontSize = 58;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(0.98f, 0.88f, 0.58f, 1f); // Dorado cálido mexicano
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.5f);
        titleRT.anchorMax = new Vector2(0.5f, 0.5f);
        titleRT.anchoredPosition = new Vector2(0, 160);
        titleRT.sizeDelta = new Vector2(950, 90);

        // Subtítulo
        GameObject subGO = new GameObject("Subtitle");
        subGO.transform.SetParent(panel.transform, false);
        Text subTxt = subGO.AddComponent<Text>();
        subTxt.text = "Nivel 1: Dolores Hidalgo, GTO. — En búsqueda de la verdad";
        subTxt.font = defaultFont;
        subTxt.fontSize = 22;
        subTxt.fontStyle = FontStyle.Italic;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.color = new Color(0.88f, 0.80f, 0.70f, 0.95f);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.5f);
        subRT.anchorMax = new Vector2(0.5f, 0.5f);
        subRT.anchoredPosition = new Vector2(0, 95);
        subRT.sizeDelta = new Vector2(850, 45);

        // Boton JUGAR
        GameObject btnGO = new GameObject("PlayButton");
        btnGO.transform.SetParent(panel.transform, false);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.14f, 0.65f, 0.35f, 1f); // Verde esmeralda vivo
        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.20f, 0.82f, 0.44f, 1f);
        cb.pressedColor     = new Color(0.09f, 0.50f, 0.24f, 1f);
        btn.colors = cb;

        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = new Vector2(0, -15);
        btnRT.sizeDelta = new Vector2(280, 75);

        // Texto del Boton
        GameObject btnTxtGO = new GameObject("BtnText");
        btnTxtGO.transform.SetParent(btnGO.transform, false);
        Text btnTxt = btnTxtGO.AddComponent<Text>();
        btnTxt.text = "▶  JUGAR";
        btnTxt.font = defaultFont;
        btnTxt.fontSize = 32;
        btnTxt.fontStyle = FontStyle.Bold;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        btnTxt.color = Color.white;
        RectTransform btnTxtRT = btnTxtGO.GetComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero;
        btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.offsetMin = Vector2.zero;
        btnTxtRT.offsetMax = Vector2.zero;

        // Tarjeta de Controles
        GameObject ctrlGO = new GameObject("ControlsCard");
        ctrlGO.transform.SetParent(panel.transform, false);
        Image ctrlBg = ctrlGO.AddComponent<Image>();
        ctrlBg.color = new Color(0f, 0f, 0f, 0.50f);
        RectTransform ctrlRT = ctrlGO.GetComponent<RectTransform>();
        ctrlRT.anchorMin = new Vector2(0.5f, 0.5f);
        ctrlRT.anchorMax = new Vector2(0.5f, 0.5f);
        ctrlRT.anchoredPosition = new Vector2(0, -155);
        ctrlRT.sizeDelta = new Vector2(780, 95);

        GameObject ctrlTxtGO = new GameObject("ControlsText");
        ctrlTxtGO.transform.SetParent(ctrlGO.transform, false);
        Text ctrlTxt = ctrlTxtGO.AddComponent<Text>();
        ctrlTxt.text = "CONTROLES\nA / D o Flechas: Caminar   •   Espacio / W: Saltar\nV: Picar con las manos (atacar / excavar)   •   F: Investigar";
        ctrlTxt.font = defaultFont;
        ctrlTxt.fontSize = 17;
        ctrlTxt.alignment = TextAnchor.MiddleCenter;
        ctrlTxt.color = new Color(0.92f, 0.92f, 0.92f, 0.92f);
        RectTransform ctrlTxtRT = ctrlTxtGO.GetComponent<RectTransform>();
        ctrlTxtRT.anchorMin = Vector2.zero;
        ctrlTxtRT.anchorMax = Vector2.one;
        ctrlTxtRT.offsetMin = new Vector2(10, 10);
        ctrlTxtRT.offsetMax = new Vector2(-10, -10);

        // Script Manager
        var ssmType = System.Type.GetType("StartScreenManager") ?? System.Type.GetType("StartScreenManager, Assembly-CSharp");
        if (ssmType != null)
        {
            Component ssm = panel.AddComponent(ssmType);
            ssmType.GetField("canvasGroup")?.SetValue(ssm, cg);
            ssmType.GetField("playButton")?.SetValue(ssm, btn);
            ssmType.GetField("playButtonText")?.SetValue(ssm, btnTxt);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SceneSetupTool] ¡Pantalla de inicio con boton JUGAR creada con exito!");
    }

    // ====================================================
    //  ENEMIGO PRESIDENTE AMLO (RETÉN KM 4)
    // ====================================================
    [MenuItem("Madres Buscadoras/8 - Configurar Enemigo Presidente")]
    public static void SetupPresidentEnemy()
    {
        // Abrir Nivel 1 si no está abierto
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Nivel_1")
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Nivel_1.unity");
        }

        GameObject enemyGO = GameObject.Find("Enemigo_Presidente");
        if (enemyGO == null) enemyGO = GameObject.Find("Enemigo_Presidenta");
        if (enemyGO == null)
        {
            enemyGO = new GameObject("Enemigo_Presidente");
        }
        else
        {
            enemyGO.name = "Enemigo_Presidente";
        }

        enemyGO.tag = "Enemy";
        enemyGO.transform.position = new Vector3(70.0f, -2.15f, 0f);
        enemyGO.transform.localScale = new Vector3(1.18f, 1.18f, 1f);

        // SpriteRenderer con AMLO por defecto
        SpriteRenderer sr = enemyGO.GetComponent<SpriteRenderer>();
        if (sr == null) sr = enemyGO.AddComponent<SpriteRenderer>();

        Sprite presSprite = PresidentEnemy.GetDefaultSprite();
        if (presSprite != null) sr.sprite = presSprite;
        sr.sortingOrder = 15;

        // Rigidbody2D
        Rigidbody2D rb = enemyGO.GetComponent<Rigidbody2D>();
        if (rb == null) rb = enemyGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3.5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // CapsuleCollider2D
        CapsuleCollider2D col = enemyGO.GetComponent<CapsuleCollider2D>();
        if (col == null) col = enemyGO.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.85f, 1.45f);
        col.offset = new Vector2(0f, 0.05f);

        // PresidentEnemy script
        PresidentEnemy pe = enemyGO.GetComponent<PresidentEnemy>();
        if (pe == null) pe = enemyGO.AddComponent<PresidentEnemy>();
        pe.baseMoveSpeed = 2.2f;
        pe.idealCombatDistance = 4.2f;
        pe.meleeAttackDistance = 2.6f;
        pe.detectionRange = 45f;
        pe.spriteRenderer = sr;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) pe.targetPlayer = player.transform;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SceneSetupTool] ¡Enemigo Presidente AMLO configurado exitosamente en el retén del Nivel 1!");
    }

    // ====================================================
    //  EXTENDER NIVEL 1 COMPLETO CON OBSTÁCULOS Y FINAL
    // ====================================================
    [MenuItem("Madres Buscadoras/9 - Extender Nivel 1 y Colocar Obstaculos + AMLO + Doña Tere")]
    public static void SetupExtendedLevel1()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Nivel_1")
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Nivel_1.unity");
        }

        Level1Manager.EnsureExtendedLevelSetup();

        // Asegurar componente Level1Manager en la escena
        if (Object.FindAnyObjectByType<Level1Manager>() == null)
        {
            GameObject mgrGO = new GameObject("Level1Manager");
            mgrGO.AddComponent<Level1Manager>();
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SceneSetupTool] ¡Nivel 1 extendido con obstáculos, AMLO y Doña Tere configurado exitosamente!");
    }

    // ====================================================
    //  GUARDAR ESCENA
    // ====================================================
    [MenuItem("Madres Buscadoras/5 - Guardar Escena")]
    static void SaveScene()
    {
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[SceneSetupTool] Escena guardada.");
    }
}
#endif
