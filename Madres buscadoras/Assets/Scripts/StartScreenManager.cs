using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla la Pantalla de Inicio decorada original de Madres Buscadoras.
/// - Diseño limpio y original: Únicamente el botón de JUGAR.
/// - Reproduce el sonido de los desaparecidos en México en segundo plano.
/// - Respiración cinemática del fondo, brasas flotantes y pulsación del botón.
/// - Al hacer clic en JUGAR o pulsar Enter/Espacio/clic en pantalla inicia el juego inmediatamente en Nivel_1.
/// </summary>
public class StartScreenManager : MonoBehaviour
{
    [Header("Escena Objetivo")]
    public string targetSceneName = "Nivel_1";
    public bool loadSceneOnStart = true;

    [Header("Referencias de UI")]
    public CanvasGroup canvasGroup;
    public RectTransform backgroundTransform;
    public Image backgroundImage;
    public Button playButton;
    public Image buttonGlowImage;

    [Header("Animacion del Boton")]
    public float pulseSpeed = 3.5f;
    public float pulseScaleAmount = 0.05f;

    [Header("Respiracion Cinematica")]
    public bool enableBackgroundBreathe = true;
    public float breatheSpeed = 0.6f;
    public float breatheScaleAmount = 0.02f;

    [Header("Particulas de Polvo / Brasas Flotantes")]
    public bool enableEmbers = true;
    public int emberCount = 22;

    [Header("Transicion")]
    public float fadeDuration = 0.4f;

    bool gameStarted = false;
    Vector3 buttonBaseScale = Vector3.one;
    Vector3 bgBaseScale = Vector3.one;
    bool isHovered = false;

    // Sprite circular suave para las brasas
    static Sprite softCircleSprite;

    class Ember
    {
        public RectTransform rect;
        public Image img;
        public Vector2 velocity;
        public float swaySpeed;
        public float swayAmount;
        public float seed;
        public float baseAlpha;
    }
    List<Ember> embers = new List<Ember>();

    void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // 1. Asegurar referencia del fondo
        if (backgroundImage == null)
        {
            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.name.Contains("Background"))
                {
                    backgroundImage = img;
                    backgroundTransform = img.rectTransform;
                    break;
                }
            }
        }
        if (backgroundImage == null)
        {
            var bgGO = GameObject.Find("Background_Artwork");
            if (bgGO != null)
            {
                backgroundTransform = bgGO.GetComponent<RectTransform>();
                backgroundImage = bgGO.GetComponent<Image>();
            }
        }
        if (backgroundImage == null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>() ?? Object.FindAnyObjectByType<Canvas>();
            if (parentCanvas != null)
            {
                GameObject bgGO = new GameObject("Background_Artwork");
                bgGO.transform.SetParent(parentCanvas.transform, false);
                bgGO.transform.SetAsFirstSibling();
                backgroundTransform = bgGO.AddComponent<RectTransform>();
                backgroundTransform.anchorMin = Vector2.zero;
                backgroundTransform.anchorMax = Vector2.one;
                backgroundTransform.offsetMin = Vector2.zero;
                backgroundTransform.offsetMax = Vector2.zero;
                bgGO.AddComponent<CanvasRenderer>();
                backgroundImage = bgGO.AddComponent<Image>();
            }
        }

        if (backgroundTransform != null)
            bgBaseScale = backgroundTransform.localScale;

        // 2. Carga infalible de la portada decorada
        EnsureBackgroundLoaded();

        // 3. Reproducir el sonido de los desaparecidos en México
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDesaparecidosBGM();
        }

        // 4. Configurar Boton JUGAR original
        if (playButton == null)
            playButton = GetComponentInChildren<Button>(true);
        if (playButton == null)
            playButton = GameObject.Find("Button_START")?.GetComponent<Button>();

        if (playButton == null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>() ?? Object.FindAnyObjectByType<Canvas>();
            if (parentCanvas != null)
            {
                GameObject btnGO = new GameObject("Button_START");
                btnGO.transform.SetParent(parentCanvas.transform, false);
                btnGO.transform.SetAsLastSibling();
                RectTransform btnRT = btnGO.AddComponent<RectTransform>();
                btnRT.anchorMin = new Vector2(0.36f, 0.42f);
                btnRT.anchorMax = new Vector2(0.64f, 0.58f);
                btnRT.offsetMin = Vector2.zero;
                btnRT.offsetMax = Vector2.zero;
                btnGO.AddComponent<CanvasRenderer>();
                Image bImg = btnGO.AddComponent<Image>();
                bImg.color = new Color(1f, 1f, 1f, 0.001f);
                playButton = btnGO.AddComponent<Button>();
            }
        }

        // Asegurar escala interactiva en Canvas y CanvasGroup
        if (transform.localScale == Vector3.zero) transform.localScale = Vector3.one;
        Canvas pCanvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
        if (pCanvas != null && pCanvas.transform.localScale == Vector3.zero) pCanvas.transform.localScale = Vector3.one;
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        if (playButton != null)
        {
            if (playButton.transform.localScale == Vector3.zero)
                playButton.transform.localScale = Vector3.one;

            buttonBaseScale = playButton.transform.localScale;
            playButton.interactable = true;
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(StartGame);

            // Asignar el sprite decorado de btn_jugar_decorado si está disponible
            bool hasDecoratedSprite = false;
            Image btnImg = playButton.GetComponent<Image>();
            if (btnImg != null)
            {
                btnImg.raycastTarget = true;
                Sprite bSprite = Resources.Load<Sprite>("btn_jugar_decorado");
#if UNITY_EDITOR
                if (bSprite == null)
                    bSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/btn_jugar_decorado.png");
#endif
                if (bSprite != null)
                {
                    btnImg.sprite = bSprite;
                    btnImg.color = Color.white;
                    btnImg.preserveAspect = true;
                    hasDecoratedSprite = true;
                }
            }

            // Ocultar texto duplicado si ya se cargó el sprite decorado con "JUGAR"
            Text btnTxt = playButton.GetComponentInChildren<Text>(true);
            if (hasDecoratedSprite)
            {
                if (btnTxt != null) btnTxt.gameObject.SetActive(false);
            }
            else if (btnTxt == null)
            {
                GameObject txtGO = new GameObject("BtnText");
                txtGO.transform.SetParent(playButton.transform, false);
                btnTxt = txtGO.AddComponent<Text>();
                Font f = null;
                try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch {}
                if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch {} }
                btnTxt.font = f;
                btnTxt.text = "JUGAR";
                btnTxt.fontSize = 38;
                btnTxt.fontStyle = FontStyle.Bold;
                btnTxt.alignment = TextAnchor.MiddleCenter;
                btnTxt.color = Color.white;
                RectTransform rt = txtGO.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // Eventos de hover para feedback visual inmediato
            var trigger = playButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = playButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enter.callback.AddListener((_) => { isHovered = true; });
            trigger.triggers.Add(enter);

            var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
            exit.callback.AddListener((_) => { isHovered = false; });
            trigger.triggers.Add(exit);
        }

        Time.timeScale = 1f;

        // 5. Crear brasas flotantes
        if (enableEmbers)
        {
            CreateProceduralEmbers();
        }
    }

    void EnsureBackgroundLoaded()
    {
        if (backgroundImage == null) return;

        string path = System.IO.Path.Combine(Application.dataPath, "Sprites/UI/pantalla_inicio_decorada.png");
        if (!System.IO.File.Exists(path))
            path = System.IO.Path.Combine(Application.dataPath, "Sprites/UI/pantalla_inicio_bg.png");

        if (System.IO.File.Exists(path))
        {
            byte[] fileData = System.IO.File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(fileData))
            {
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                Sprite newSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                newSprite.name = "pantalla_inicio_decorada";
                backgroundImage.sprite = newSprite;
                backgroundImage.color = Color.white;
                return;
            }
        }
    }

    void CreateProceduralEmbers()
    {
        EnsureSoftCircleSprite();

        Transform parent = transform;
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null) parent = parentCanvas.transform;

        GameObject embersRoot = new GameObject("EmbersContainer");
        embersRoot.transform.SetParent(parent, false);
        embersRoot.transform.SetSiblingIndex(Mathf.Max(0, parent.childCount - 2));

        for (int i = 0; i < emberCount; i++)
        {
            GameObject eGO = new GameObject($"Ember_{i}");
            eGO.transform.SetParent(embersRoot.transform, false);

            RectTransform rt = eGO.AddComponent<RectTransform>();
            float size = Random.Range(6f, 16f);
            rt.sizeDelta = new Vector2(size, size);

            float startX = Random.Range(-960f, 960f);
            float startY = Random.Range(-540f, 540f);
            rt.anchoredPosition = new Vector2(startX, startY);

            Image img = eGO.AddComponent<Image>();
            img.sprite = softCircleSprite;
            Color emberColor = Color.Lerp(new Color(1f, 0.72f, 0.18f, 0.85f), new Color(1f, 0.28f, 0.08f, 0.95f), Random.value);
            img.color = emberColor;
            img.raycastTarget = false;

            embers.Add(new Ember
            {
                rect = rt,
                img = img,
                velocity = new Vector2(Random.Range(20f, 50f), Random.Range(35f, 85f)),
                swaySpeed = Random.Range(1.5f, 3.5f),
                swayAmount = Random.Range(20f, 40f),
                seed = Random.Range(0f, 100f),
                baseAlpha = Random.Range(0.45f, 0.95f)
            });
        }
    }

    void EnsureSoftCircleSprite()
    {
        if (softCircleSprite != null) return;

        int res = 32;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(res * 0.5f, res * 0.5f);
        float radius = res * 0.5f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float norm = Mathf.Clamp01(dist / radius);
                float alpha = Mathf.SmoothStep(1f, 0f, norm);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        softCircleSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

    void Update()
    {
        if (gameStarted) return;

        float t = Time.unscaledTime;

        // 1. Animacion del Boton (Pulso + Resplandor + Hover)
        if (playButton != null)
        {
            float targetHoverScale = isHovered ? 1.08f : 1.0f;
            float pulse = Mathf.Sin(t * pulseSpeed) * pulseScaleAmount;
            float s = targetHoverScale + pulse;
            playButton.transform.localScale = Vector3.Lerp(playButton.transform.localScale, buttonBaseScale * s, Time.unscaledDeltaTime * 10f);

            if (buttonGlowImage != null)
            {
                float glowAlpha = 0.4f + Mathf.Sin(t * (pulseSpeed * 1.2f)) * 0.35f;
                if (isHovered) glowAlpha = 0.9f;
                Color gc = buttonGlowImage.color;
                gc.a = glowAlpha;
                buttonGlowImage.color = gc;
            }
        }

        // 2. Respiracion Cinematica del Fondo
        if (enableBackgroundBreathe && backgroundTransform != null)
        {
            float bgS = 1f + Mathf.Sin(t * breatheSpeed) * breatheScaleAmount;
            backgroundTransform.localScale = bgBaseScale * bgS;

            float rot = Mathf.Sin(t * (breatheSpeed * 0.5f)) * 0.25f;
            backgroundTransform.localRotation = Quaternion.Euler(0, 0, rot);
        }

        // 3. Animacion de Brasas / Polvo del Desierto
        if (embers.Count > 0)
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < embers.Count; i++)
            {
                var e = embers[i];
                if (e.rect == null) continue;

                Vector2 pos = e.rect.anchoredPosition;
                pos.x += (e.velocity.x + Mathf.Sin(t * e.swaySpeed + e.seed) * e.swayAmount) * dt;
                pos.y += e.velocity.y * dt;

                if (pos.y > 580f || pos.x > 1050f)
                {
                    pos.y = -580f;
                    pos.x = Random.Range(-960f, 600f);
                }
                e.rect.anchoredPosition = pos;

                if (e.img != null)
                {
                    float alpha = e.baseAlpha * (0.6f + Mathf.PingPong(t * 2f + e.seed, 0.4f));
                    Color c = e.img.color;
                    c.a = alpha;
                    e.img.color = c;
                }
            }
        }

        // 4. Iniciar inmediatamente con un solo clic, toque o cualquier tecla
        bool wantsStart = Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.anyKeyDown || Input.touchCount > 0;
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        var touch = UnityEngine.InputSystem.Touchscreen.current;
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.leftButton.isPressed)) wantsStart = true;
        if (kb != null && kb.anyKey.wasPressedThisFrame) wantsStart = true;
        if (touch != null && (touch.primaryTouch.press.wasPressedThisFrame || touch.primaryTouch.press.isPressed)) wantsStart = true;
#endif

        if (wantsStart)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        if (gameStarted) return;
        gameStarted = true;
        Debug.Log("[StartScreenManager] ¡Iniciando Nivel 1!");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSFX();
        }

        Time.timeScale = 1f;
        string target = string.IsNullOrEmpty(targetSceneName) ? "Nivel_1" : targetSceneName;
        Debug.Log($"[StartScreenManager] Cargando escena de juego inmediatamente: {target}");
        try
        {
            SceneManager.LoadScene(target);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[StartScreenManager] No se pudo cargar por nombre '{target}', intentando índice 1: {e.Message}");
            SceneManager.LoadScene(1);
        }
    }

    IEnumerator StartGameRoutine()
    {
        Time.timeScale = 1f;

        // Efecto punch en el boton
        if (playButton != null)
        {
            float elapsedPunch = 0f;
            Vector3 startScale = playButton.transform.localScale;
            Vector3 pressedScale = buttonBaseScale * 0.90f;
            while (elapsedPunch < 0.12f)
            {
                elapsedPunch += Time.unscaledDeltaTime;
                playButton.transform.localScale = Vector3.Lerp(startScale, pressedScale, elapsedPunch / 0.12f);
                yield return null;
            }
        }

        // Desvanecimiento suave
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        string target = string.IsNullOrEmpty(targetSceneName) ? "Nivel_1" : targetSceneName;
        Debug.Log($"[StartScreenManager] Cargando escena de juego: {target}");

        try
        {
            SceneManager.LoadScene(target);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[StartScreenManager] Carga por nombre '{target}' falló ({ex.Message}). Intentando por índice 1...");
            SceneManager.LoadScene(1);
        }
    }
}
