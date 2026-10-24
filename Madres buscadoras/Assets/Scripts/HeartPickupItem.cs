using System.Collections;
using UnityEngine;

/// <summary>
/// Corazón de vida coleccionable a ras de suelo:
/// - Recupera 1 corazón de vida (hasta 5 corazones de salud máxima).
/// - Flotación suave y brillo visible.
/// - Desaparece inmediatamente al ser recogido con sonido y texto flotante.
/// </summary>
public class HeartPickupItem : MonoBehaviour
{
    [Header("Identificador")]
    public string heartId = "heart_1";

    [Header("Animación a ras de suelo")]
    public float bobSpeed = 3.0f;
    public float bobHeight = 0.08f;

    private Vector3 basePosition;
    private SpriteRenderer sr;
    private Collider2D col;
    private bool isCollected = false;
    private static Sprite cachedHeartSprite;

    void Start()
    {
        basePosition = transform.position;
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        if (sr != null && sr.sprite == null)
        {
            sr.sprite = GetHeartSprite();
            sr.sortingOrder = 6;
        }
    }

    void Update()
    {
        if (isCollected) return;

        float newY = basePosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(basePosition.x, newY, basePosition.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        bool isPlayer = other.CompareTag("Player") ||
                        other.GetComponent<PlayerController2D>() != null ||
                        other.GetComponentInParent<PlayerController2D>() != null;

        if (!isPlayer) return;

        isCollected = true;
        if (col != null) col.enabled = false;

        HealthSystem hs = other.GetComponent<HealthSystem>() ?? other.GetComponentInParent<HealthSystem>();
        if (hs == null) hs = Object.FindAnyObjectByType<HealthSystem>();

        if (hs != null)
        {
            hs.Heal(1);
        }

        ScoreManager.AddScore(200, transform.position + Vector3.up * 1f, "+1 ❤️ ¡CORAZÓN DE VIDA!");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayObjective();
        }

        StartCoroutine(CollectRoutine());
    }

    private IEnumerator CollectRoutine()
    {
        float dur = 0.16f;
        float elapsed = 0f;
        Vector3 initialScale = transform.localScale;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
            transform.position += Vector3.up * (Time.deltaTime * 2.0f);
            yield return null;
        }

        Destroy(gameObject);
    }

    public static Sprite GetHeartSprite()
    {
        if (cachedHeartSprite != null) return cachedHeartSprite;

        int size = 24;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color clear = new Color(0, 0, 0, 0);
        Color red = new Color(0.98f, 0.15f, 0.22f, 1f);
        Color darkRed = new Color(0.65f, 0.05f, 0.12f, 1f);
        Color highlight = new Color(1f, 0.7f, 0.75f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, clear);

                float nx = (x - 11.5f) / 10f;
                float ny = (y - 10f) / 10f;
                float formula = Mathf.Pow(nx * nx + ny * ny - 1f, 3f) - (nx * nx * ny * ny * ny);

                if (formula <= 0f)
                {
                    if (y >= 16 && x >= 7 && x <= 9)
                        tex.SetPixel(x, y, highlight);
                    else if (formula >= -0.25f)
                        tex.SetPixel(x, y, darkRed);
                    else
                        tex.SetPixel(x, y, red);
                }
            }
        }
        tex.Apply();

        cachedHeartSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 24f);
        return cachedHeartSprite;
    }
}

