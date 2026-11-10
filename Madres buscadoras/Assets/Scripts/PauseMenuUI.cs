using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú de Pausa Profesional (Se abre con tecla 'Esc'):
/// - Reanudar juego.
/// - Ajustes de audio (Mute BGM/SFX).
/// - Reiniciar desde el último Punto de Control (Checkpoint).
/// - Volver al Menú Principal.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    private GameObject pauseRoot;
    private bool isPaused = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoInit()
    {
        if (Object.FindAnyObjectByType<PauseMenuUI>() == null)
        {
            GameObject go = new GameObject("PauseMenuUI");
            go.AddComponent<PauseMenuUI>();
            DontDestroyOnLoad(go);
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Si el diario está abierto, dejar que el diario se cierre primero
            if (JournalUI.Instance != null && Time.timeScale == 0f && !isPaused)
            {
                return;
            }
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseRoot == null)
        {
            BuildPauseUI();
        }

        pauseRoot.SetActive(true);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSFX();
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseRoot != null)
        {
            pauseRoot.SetActive(false);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClickSFX();
        }
    }

    public void RestartFromCheckpoint()
    {
        ResumeGame();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = GameState.CurrentCheckpointPosition;
            HealthSystem hs = player.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.FullHeal();
            }
            ScoreManager.AddScore(0, player.transform.position + Vector3.up * 1.5f, "Reiniciando en Punto de Control");
        }
    }

    public void ReturnToMainMenu()
    {
        ResumeGame();
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.LoadScene("WelcomeScreen");
        }
        else
        {
            SceneManager.LoadScene("WelcomeScreen");
        }
    }

    private void BuildPauseUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        pauseRoot = new GameObject("PauseMenuUIRoot");
        pauseRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRT = pauseRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        Image overlay = pauseRoot.AddComponent<Image>();
        overlay.color = new Color(0.04f, 0.03f, 0.02f, 0.82f);

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Caja central
        GameObject box = new GameObject("PauseFrame");
        box.transform.SetParent(pauseRoot.transform, false);
        RectTransform boxRT = box.AddComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(480, 420);

        Image boxBg = box.AddComponent<Image>();
        boxBg.color = new Color(0.12f, 0.09f, 0.08f, 0.96f);

        // Título
        GameObject titleGO = new GameObject("PauseTitle");
        titleGO.transform.SetParent(box.transform, false);
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1);
        titleRT.anchorMax = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -35);
        titleRT.sizeDelta = new Vector2(400, 40);

        Text title = titleGO.AddComponent<Text>();
        title.text = "PAUSA";
        title.font = defaultFont;
        title.fontSize = 28;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(1f, 0.88f, 0.4f, 1f);

        // Botones
        CreateMenuButton(box, "REANUDAR BÚSQUEDA", -95, ResumeGame, defaultFont);
        CreateMenuButton(box, "SILENCIAR AUDIO (MUTE)", -155, () =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        }, defaultFont);
        CreateMenuButton(box, "REINICIAR EN CHECKPOINT", -215, RestartFromCheckpoint, defaultFont);
        CreateMenuButton(box, "VOLVER AL MENÚ PRINCIPAL", -275, ReturnToMainMenu, defaultFont);
    }

    private void CreateMenuButton(GameObject parent, string label, float posY, UnityEngine.Events.UnityAction action, Font font)
    {
        GameObject btnGO = new GameObject($"Btn_{label}");
        btnGO.transform.SetParent(parent.transform, false);

        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, posY);
        rt.sizeDelta = new Vector2(360, 46);

        Image bg = btnGO.AddComponent<Image>();
        bg.color = new Color(0.24f, 0.18f, 0.14f, 1f);

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
        t.fontSize = 14;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.96f, 0.92f, 0.85f, 1f);
    }
}
