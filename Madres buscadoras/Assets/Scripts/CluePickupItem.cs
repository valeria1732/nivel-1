using UnityEngine;

/// <summary>
/// Objeto físico interactuable en el escenario que representa una de las 8 pistas:
/// - Muestra el indicador flotante [E] Investigar cuando Esperanza está cerca.
/// - Al pulsar 'E', registra la pista en el Diario de Búsqueda y suma puntuación.
/// </summary>
public class CluePickupItem : MonoBehaviour
{
    [Header("Identificador de Pista en GameState")]
    public string clueId = "pista_nota_poste";
    public string promptTitle = "Examinar evidencia";
    public float interactionDistance = 2.2f;

    [Header("Visuales")]
    public SpriteRenderer spriteRenderer;

    private Transform playerTransform;
    private GameObject promptUI;
    private TextMesh promptTextMesh;
    private bool isPlayerNear = false;

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        CreatePromptIndicator();

        // Buscar al jugador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        isPlayerNear = dist <= interactionDistance;

        if (promptUI != null)
        {
            // Solo mostrar prompt si no se ha descubierto aún y está cerca
            bool alreadyFound = GameState.Has(clueId);
            promptUI.SetActive(isPlayerNear && !alreadyFound);
        }

        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            Investigate();
        }
    }

    public void Investigate()
    {
        if (GameState.Has(clueId))
        {
            ScoreManager.AddScore(0, transform.position + Vector3.up * 1.2f, "Ya registrado en el diario");
            return;
        }

        GameState.Add(clueId);
        ScoreManager.AddScore(500, transform.position + Vector3.up * 1.5f, "+500 PTS ¡EVIDENCIA REGISTRADA!");

        if (promptUI != null) promptUI.SetActive(false);

        // Feedback visual de que ya fue inspeccionado
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.85f, 0.85f, 0.85f, 0.85f);
        }
    }

    private void CreatePromptIndicator()
    {
        promptUI = new GameObject("CluePrompt_" + clueId);
        promptUI.transform.SetParent(transform, false);
        promptUI.transform.localPosition = new Vector3(0, 1.4f, 0);

        promptTextMesh = promptUI.AddComponent<TextMesh>();
        promptTextMesh.text = $"[E] {promptTitle}";
        promptTextMesh.fontSize = 24;
        promptTextMesh.characterSize = 0.08f;
        promptTextMesh.alignment = TextAlignment.Center;
        promptTextMesh.anchor = TextAnchor.MiddleCenter;
        promptTextMesh.color = new Color(1f, 0.92f, 0.45f, 1f);

        promptUI.SetActive(false);
    }
}
