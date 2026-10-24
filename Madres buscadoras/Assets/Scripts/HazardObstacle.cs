using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona obstáculos peligrosos en el camino:
/// - Cactus con espinas (SpikeCactus)
/// - Zanjas con púas (RoadPit)
/// - Barricadas destructibles con el pico (Barricade)
/// - Conos viales
/// </summary>
public class HazardObstacle : MonoBehaviour, IDamageable
{
    public enum HazardType
    {
        SpikeCactus,
        RoadPit,
        Barricade,
        TrafficCone,
        PoisonMushroom
    }

    [Header("Tipo de Obstáculo")]
    public HazardType hazardType = HazardType.Barricade;
    public int damageToPlayer = 15;
    public float knockbackForce = 5.5f;
    public float damageCooldown = 0.85f;

    [Header("Destructibilidad (Con Pico tecla V)")]
    public bool isDestructible = false;
    public int obstacleHealth = 50;
    public int pointsOnDestroy = 150;

    [Header("Visuales")]
    public SpriteRenderer spriteRenderer;
    public Color hitFlashColor = new Color(1f, 0.4f, 0.4f, 1f);

    private float lastDamageTime = -99f;
    private Color originalColor = Color.white;
    private Vector3 originalScale;
    private bool isDestroyed = false;
    private float pulseOffset = 0f;
    private static Sprite cachedPoisonMushroomSprite;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        originalScale = transform.localScale;
        pulseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Start()
    {
        if (hazardType == HazardType.PoisonMushroom)
        {
            SetupPoisonMushroomVisualAndCollider();
        }
    }

    void Update()
    {
        if (isDestroyed) return;

        if (hazardType == HazardType.PoisonMushroom && spriteRenderer != null)
        {
            // Pulsación orgánica del hongo venenoso para que destaque en el suelo
            float t = Time.time * 4.2f + pulseOffset;
            float sx = originalScale.x * (1f + Mathf.Sin(t) * 0.08f);
            float sy = originalScale.y * (1f - Mathf.Sin(t) * 0.07f);
            transform.localScale = new Vector3(sx, sy, originalScale.z);

            float glow = (Mathf.Sin(t * 1.5f) + 1f) * 0.5f;
            spriteRenderer.color = Color.Lerp(Color.white, new Color(0.78f, 1f, 0.75f, 1f), glow * 0.35f);
        }
    }

    private void SetupPoisonMushroomVisualAndCollider()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = GetOrCreatePoisonMushroomSprite();
        spriteRenderer.sortingOrder = 5;
        originalColor = Color.white;
        transform.localScale = new Vector3(1.15f, 1.15f, 1f);
        originalScale = transform.localScale;

        Collider2D existingCol = GetComponent<Collider2D>();
        if (existingCol == null)
        {
            BoxCollider2D bc = gameObject.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(0.58f, 0.62f);
            bc.offset = new Vector2(0f, 0.32f);
        }
        else
        {
            existingCol.isTrigger = true;
        }
    }

    private static Sprite GetOrCreatePoisonMushroomSprite()
    {
        if (cachedPoisonMushroomSprite != null) return cachedPoisonMushroomSprite;

        int w = 48;
        int h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int i = 0; i < w * h; i++) tex.SetPixel(i % w, i / w, Color.clear);

        Color outline = new Color(0.12f, 0.06f, 0.16f, 1f);
        Color stemBase = new Color(0.88f, 0.85f, 0.74f, 1f);
        Color stemShade = new Color(0.72f, 0.68f, 0.58f, 1f);
        Color capPurple = new Color(0.58f, 0.14f, 0.72f, 1f);
        Color capDark = new Color(0.40f, 0.08f, 0.52f, 1f);
        Color toxicGreen = new Color(0.32f, 1.0f, 0.22f, 1f);
        Color sporeGlow = new Color(0.45f, 1.0f, 0.35f, 0.9f);

        // 1. Tallo del hongo (y = 2..19, x = 18..29)
        for (int y = 2; y <= 19; y++)
        {
            int halfW = y < 6 ? 6 : 5;
            for (int x = 24 - halfW; x <= 24 + halfW; x++)
            {
                if (x == 24 - halfW || x == 24 + halfW || y == 2)
                    tex.SetPixel(x, y, outline);
                else if (x > 25)
                    tex.SetPixel(x, y, stemShade);
                else
                    tex.SetPixel(x, y, stemBase);
            }
        }

        // 2. Sombrero venenoso abombado (y = 16..37)
        for (int y = 16; y <= 37; y++)
        {
            float ny = (y - 16f) / 21f; // 0 en la base del sombrero, 1 en la punta
            int halfW = Mathf.RoundToInt(Mathf.Sqrt(Mathf.Clamp01(1f - ny * ny)) * 18f);
            for (int x = 24 - halfW; x <= 24 + halfW; x++)
            {
                if (x == 24 - halfW || x == 24 + halfW || y == 16 || y == 37)
                    tex.SetPixel(x, y, outline);
                else if (y < 20 || x > 24 + halfW - 4)
                    tex.SetPixel(x, y, capDark);
                else
                    tex.SetPixel(x, y, capPurple);
            }
        }

        // 3. Manchas verdes tóxicas brillantes sobre el sombrero
        Vector2Int[] spots = new Vector2Int[]
        {
            new Vector2Int(16, 24),
            new Vector2Int(24, 30),
            new Vector2Int(31, 23),
            new Vector2Int(19, 32),
            new Vector2Int(28, 28),
            new Vector2Int(13, 20),
            new Vector2Int(34, 19)
        };
        int[] radii = new int[] { 3, 3, 3, 2, 2, 2, 2 };

        for (int s = 0; s < spots.Length; s++)
        {
            Vector2Int c = spots[s];
            int r = radii[s];
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy <= r * r)
                    {
                        int px = c.x + dx;
                        int py = c.y + dy;
                        if (px >= 0 && px < w && py >= 0 && py < h)
                            tex.SetPixel(px, py, toxicGreen);
                    }
                }
            }
        }

        // 4. Esporas venenosas flotando arriba del hongo
        Vector2Int[] spores = new Vector2Int[]
        {
            new Vector2Int(14, 40), new Vector2Int(22, 43), new Vector2Int(31, 41),
            new Vector2Int(18, 45), new Vector2Int(27, 45)
        };
        foreach (var sp in spores)
        {
            tex.SetPixel(sp.x, sp.y, sporeGlow);
            tex.SetPixel(sp.x + 1, sp.y, sporeGlow);
            tex.SetPixel(sp.x, sp.y + 1, sporeGlow);
        }

        tex.Apply();
        cachedPoisonMushroomSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), 48f);
        return cachedPoisonMushroomSprite;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlayerCollision(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandlePlayerCollision(other.gameObject);
    }

    void HandlePlayerCollision(GameObject other)
    {
        if (isDestroyed) return;
        if (!other.CompareTag("Player")) return;

        if (Time.time - lastDamageTime < damageCooldown) return;
        lastDamageTime = Time.time;

        // Si es cono, no daña pero vuela
        if (hazardType == HazardType.TrafficCone)
        {
            PushTrafficCone(other.transform);
            return;
        }

        // Si es un Hongo Venenoso y no lo saltaste, te envenena y mueres
        if (hazardType == HazardType.PoisonMushroom)
        {
            HealthSystem poisonHs = other.GetComponent<HealthSystem>();
            if (poisonHs != null)
            {
                poisonHs.KillByPoison();
            }
            return;
        }

        // Aplicar daño al jugador
        HealthSystem hs = other.GetComponent<HealthSystem>();
        if (hs != null)
        {
            hs.TakeDamage(damageToPlayer);
        }

        // Empuje (knockback)
        Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            float pushDir = Mathf.Sign(other.transform.position.x - transform.position.x);
            if (Mathf.Abs(pushDir) < 0.1f) pushDir = -1f;

            float upward = (hazardType == HazardType.RoadPit) ? 6f : 3.5f;
            playerRb.linearVelocity = new Vector2(pushDir * knockbackForce, upward);
        }

        // Mostrar texto flotante
        string text = (hazardType == HazardType.SpikeCactus) ? $"-{damageToPlayer} HP ¡Espinas!" :
                      (hazardType == HazardType.RoadPit) ? $"-{damageToPlayer} HP ¡Zanja!" :
                      $"-{damageToPlayer} HP";
        ScoreManager.AddScore(0, other.transform.position + Vector3.up * 1.2f, text);

        // Sonido
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDamage();
        }

        StartCoroutine(ShakeEffectRoutine());
    }

    private void PushTrafficCone(Transform player)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;

        float dir = Mathf.Sign(transform.position.x - player.position.x);
        rb.linearVelocity = new Vector2(dir * 5f, 4f);
        rb.angularVelocity = dir * -360f;

        ScoreManager.AddScore(50, transform.position + Vector3.up, "+50 PTS");
        Destroy(gameObject, 2.5f);
        isDestroyed = true;
    }

    // Interfaz IDamageable para golpear con el pico (V)
    public void TakeDamage(int damage)
    {
        if (!isDestructible || isDestroyed) return;

        obstacleHealth -= damage;
        StartCoroutine(HitFlashRoutine());

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDig();
        }

        if (obstacleHealth <= 0)
        {
            DestroyObstacle();
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer != null) spriteRenderer.color = hitFlashColor;
        transform.localScale = originalScale * 1.1f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = originalScale;
        if (spriteRenderer != null && !isDestroyed) spriteRenderer.color = originalColor;
    }

    private IEnumerator ShakeEffectRoutine()
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float ox = Random.Range(-0.06f, 0.06f);
            transform.position = startPos + new Vector3(ox, 0, 0);
            yield return null;
        }
        transform.position = startPos;
    }

    private void DestroyObstacle()
    {
        isDestroyed = true;
        ScoreManager.AddScore(pointsOnDestroy, transform.position + Vector3.up, $"+{pointsOnDestroy} PTS ¡Despejado!");
        StartCoroutine(BreakApartRoutine());
    }

    private IEnumerator BreakApartRoutine()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        float elapsed = 0f;
        float duration = 0.35f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 1f - t;
                spriteRenderer.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
