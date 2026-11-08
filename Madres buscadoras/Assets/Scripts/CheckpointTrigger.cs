using UnityEngine;

/// <summary>
/// Punto de control y guardado con estandarte de búsqueda ("Hasta Encontrarte"):
/// - Se activa automáticamente al pasar caminando.
/// - Guarda la posición para reaparición en caso de perder los 3 corazones.
/// </summary>
public class CheckpointTrigger : MonoBehaviour
{
    [Header("Índice del Checkpoint (0, 1, 2)")]
    public int checkpointIndex = 0;
    public string checkpointName = "Altar de la Esperanza";

    private bool isActivated = false;
    private SpriteRenderer flagRenderer;

    void Start()
    {
        // Buscar o crear la bandera visual
        EnsureVisualMarker();

        if (GameState.CurrentCheckpointIndex >= checkpointIndex)
        {
            isActivated = true;
            HighlightMarker();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isActivated) return;
        if (!collision.CompareTag("Player")) return;

        isActivated = true;
        GameState.SetCheckpoint(checkpointIndex, transform.position);
        HighlightMarker();

        ScoreManager.AddScore(200, transform.position + Vector3.up * 2f, $"PUNTO DE CONTROL: {checkpointName}");
    }

    private void HighlightMarker()
    {
        if (flagRenderer != null)
        {
            flagRenderer.color = new Color(1f, 0.95f, 0.4f, 1f); // Resplandor dorado
        }
    }

    private void EnsureVisualMarker()
    {
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D bc = gameObject.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(2f, 4f);
        }

        // Crear poste y bandera de tela blanca
        GameObject post = new GameObject("MarkerVisual");
        post.transform.SetParent(transform, false);
        flagRenderer = post.AddComponent<SpriteRenderer>();
        flagRenderer.sortingOrder = 2;
        flagRenderer.sprite = CreateFlagSprite();
        flagRenderer.color = new Color(0.9f, 0.9f, 0.85f, 0.85f);
    }

    private static Sprite CreateFlagSprite()
    {
        int w = 24;
        int h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color clear = new Color(0, 0, 0, 0);
        Color wood = new Color(0.45f, 0.3f, 0.18f, 1f);
        Color cloth = new Color(0.96f, 0.95f, 0.92f, 1f);
        Color ribbon = new Color(0.2f, 0.7f, 0.3f, 1f); // Verde esperanza

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                // Mástil de madera
                if (x >= 4 && x <= 6)
                {
                    tex.SetPixel(x, y, wood);
                }
                // Bandera blanca ondeante con listón
                else if (x > 6 && x < 22 && y >= 26 && y <= 44)
                {
                    if (y >= 41 && y <= 43)
                    {
                        tex.SetPixel(x, y, ribbon);
                    }
                    else
                    {
                        tex.SetPixel(x, y, cloth);
                    }
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.2f, 0.1f), 24f);
    }
}
