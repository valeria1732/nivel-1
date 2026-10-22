using System.Collections;
using UnityEngine;

/// <summary>
/// Botella de agua fresca para supervivencia:
/// - Recupera 1 corazón de vida (máximo 3).
/// - Suma al contador de botellas del nivel (X/10).
/// - Animación sutil de flotación a ras de suelo.
/// - Desaparece inmediatamente al ser recogida con animación y feedback sonoro/visual.
/// - Si ya fue recogida en esta sesión, se destruye automáticamente.
/// </summary>
public class WaterBottleItem : MonoBehaviour
{
    [Header("Identificador Único")]
    public string bottleId = "bottle_z1_camino";

    [Header("Animación a ras de suelo")]
    public float bobSpeed = 2.6f;
    public float bobHeight = 0.08f;

    private Vector3 basePosition;
    private SpriteRenderer sr;
    private Collider2D col;
    private bool isCollected = false;
    private static Sprite cachedBottleSprite;

    void Start()
    {
        basePosition = transform.position;
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        if (sr != null && sr.sprite == null)
        {
            sr.sprite = GetBottleSprite();
            sr.sortingOrder = 5;
        }

        // Si ya fue recolectada, eliminar del escenario inmediatamente
        if (GameState.IsBottleCollected(bottleId))
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (isCollected) return;

        // Flotación suave muy sutil a ras de suelo
        float newY = basePosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(basePosition.x, newY, basePosition.z);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        bool isPlayer = collision.CompareTag("Player") ||
                        collision.GetComponent<PlayerController2D>() != null ||
                        collision.GetComponentInParent<PlayerController2D>() != null;

        if (!isPlayer) return;

        isCollected = true;
        if (col != null) col.enabled = false;

        // Recuperar vida en HealthSystem (1 corazón)
        HealthSystem hs = collision.GetComponent<HealthSystem>() ?? collision.GetComponentInParent<HealthSystem>();
        if (hs == null)
        {
            hs = Object.FindAnyObjectByType<HealthSystem>();
        }

        if (hs != null)
        {
            hs.Heal(1);
        }

        // Registrar en GameState
        GameState.CollectWaterBottle(bottleId);
        ScoreManager.AddScore(250, transform.position + Vector3.up * 0.9f, "+1 ❤️ ¡AGUA FRESCA!");

        // Desaparecer inmediatamente con efecto suave de escala y desvanecimiento
        StartCoroutine(CollectAndDestroyRoutine());
    }

    private IEnumerator CollectAndDestroyRoutine()
    {
        float dur = 0.15f;
        float elapsed = 0f;
        Vector3 initialScale = transform.localScale;
        Color initialColor = sr != null ? sr.color : Color.white;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
            transform.position += Vector3.up * (Time.deltaTime * 1.8f);

            if (sr != null)
            {
                Color c = initialColor;
                c.a = 1f - t;
                sr.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    public static Sprite GetBottleSprite()
    {
        if (cachedBottleSprite != null) return cachedBottleSprite;

        // Crear sprite procedural de botella de agua limpia y cristalina
        int w = 24;
        int h = 40;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color clear = new Color(0, 0, 0, 0);
        Color glass = new Color(0.35f, 0.75f, 0.95f, 0.9f);
        Color water = new Color(0.18f, 0.55f, 0.92f, 0.95f);
        Color highlight = new Color(0.85f, 0.95f, 1f, 0.95f);
        Color cap = new Color(0.1f, 0.25f, 0.6f, 1f);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                tex.SetPixel(x, y, clear);

                // Tapa
                if (y >= 34 && x >= 8 && x <= 15)
                {
                    tex.SetPixel(x, y, cap);
                }
                // Cuello
                else if (y >= 28 && y < 34 && x >= 9 && x <= 14)
                {
                    tex.SetPixel(x, y, (x == 10) ? highlight : glass);
                }
                // Cuerpo de la botella
                else if (y < 28 && y >= 3 && x >= 5 && x <= 18)
                {
                    if (x == 5 || x == 18 || y == 3)
                    {
                        tex.SetPixel(x, y, glass);
                    }
                    else if (x == 7)
                    {
                        tex.SetPixel(x, y, highlight); // Reflejo
                    }
                    else
                    {
                        tex.SetPixel(x, y, water);
                    }
                }
            }
        }
        tex.Apply();

        cachedBottleSprite = Sprite.Create(
            tex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            32f
        );

        return cachedBottleSprite;
    }
}
