using System.Collections;
using UnityEngine;

/// <summary>
/// Proyectil compacto de frases/palabras lanzado por la Presidenta.
/// Tamaño reducido y aerodinámico para esquivar con precisión de plataforma.
/// Recompensas:
/// - Cada salto exitoso por encima otorga VIDA (+10 HP) y PUNTOS (+100 PTS).
/// - Destruir la frase con el pico (tecla V) otorga +250 PTS y +5 HP.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class PhraseProjectile : MonoBehaviour, IDamageable
{
    [Header("Movimiento")]
    public Vector2 moveDirection = Vector2.left;
    public float speed = 4.0f;
    public float lifetime = 6.0f;
    public float wobbleAmplitude = 0.04f;
    public float wobbleFrequency = 9f;

    [Header("Daño")]
    public int damageToPlayer = 15;
    public float playerKnockback = 4.2f;

    [Header("Recompensas por Salto")]
    public int healthRewardOnJump = 10;
    public int pointsRewardOnJump = 100;
    public int pointsRewardOnDestroy = 250;

    [Header("Texto")]
    public string phraseText = "¡En México...";

    // Componentes internos
    private BoxCollider2D boxCol;
    private SpriteRenderer bannerSr;
    private TextMesh textMesh;
    private TextMesh shadowTextMesh;
    private Transform targetPlayer;
    private PlayerController2D playerCtrl;

    private float spawnTime;
    private float baseY;
    private bool isDestroyed = false;
    private bool hasRewardedJump = false;
    private float wobbleOffset;

    // Textura compacta compartida
    private static Sprite cachedBannerSprite;

    /// <summary>
    /// Genera una pequeña insignia estilizada con borde dorado brillante y fondo oscuro.
    /// </summary>
    public static Sprite GetOrCreateBannerSprite()
    {
        if (cachedBannerSprite != null) return cachedBannerSprite;

        int w = 96;
        int h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color borderColor = new Color(1f, 0.88f, 0.35f, 1f); // Dorado brillante
        Color innerDark = new Color(0.14f, 0.05f, 0.08f, 0.94f); // Fondo institucional
        Color highlightTop = new Color(0.48f, 0.12f, 0.18f, 0.96f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = (x < 2 || x >= w - 2 || y < 2 || y >= h - 2);
                if (isBorder)
                {
                    tex.SetPixel(x, y, borderColor);
                }
                else
                {
                    Color fill = Color.Lerp(innerDark, highlightTop, (float)y / h);
                    tex.SetPixel(x, y, fill);
                }
            }
        }
        tex.Apply();

        cachedBannerSprite = Sprite.Create(
            tex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            50f, // Pixels Per Unit
            0,
            SpriteMeshType.FullRect,
            new Vector4(4, 4, 4, 4)
        );

        return cachedBannerSprite;
    }

    [Header("Tipo Ganso")]
    public bool isGoose = false;
    private static Sprite cachedGooseSprite;

    public static Sprite GetGooseSprite()
    {
        if (cachedGooseSprite != null) return cachedGooseSprite;
#if UNITY_EDITOR
        cachedGooseSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Personajes/AMLO/proj_goose.png");
#endif
        if (cachedGooseSprite == null)
        {
            cachedGooseSprite = Resources.Load<Sprite>("proj_goose");
        }
        if (cachedGooseSprite == null)
        {
            string p = System.IO.Path.Combine(Application.dataPath, "Sprites/Personajes/AMLO/proj_goose.png");
            if (System.IO.File.Exists(p))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(p);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    cachedGooseSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 70f);
                }
            }
        }
        return cachedGooseSprite;
    }

    /// <summary>
    /// Instancia el Ganso Volador ("¡Me canso ganso!").
    /// </summary>
    public static GameObject SpawnGoose(Vector3 pos, Vector2 dir, float projSpeed = 4.5f)
    {
        GameObject go = new GameObject("Ganso_Volador");
        go.transform.position = pos;

        PhraseProjectile proj = go.AddComponent<PhraseProjectile>();
        proj.isGoose = true;
        proj.phraseText = "¡Me canso ganso!";
        proj.moveDirection = dir.normalized;
        proj.speed = projSpeed;
        proj.damageToPlayer = 18;
        proj.pointsRewardOnJump = 150;
        proj.healthRewardOnJump = 15;
        proj.pointsRewardOnDestroy = 300;

        return go;
    }

    /// <summary>
    /// Instancia programáticamente el proyectil compacto.
    /// </summary>
    public static GameObject Spawn(Vector3 pos, Vector2 dir, string text, float projSpeed = 4.0f)
    {
        GameObject go = new GameObject("Frase_Bala_" + text.Trim());
        go.transform.position = pos;

        PhraseProjectile proj = go.AddComponent<PhraseProjectile>();
        proj.phraseText = text;
        proj.moveDirection = dir.normalized;
        proj.speed = projSpeed;

        return go;
    }

    void Awake()
    {
        boxCol = GetComponent<BoxCollider2D>();
        boxCol.isTrigger = true;
        wobbleOffset = Random.Range(0f, 10f);
    }

    void Start()
    {
        spawnTime = Time.time;
        baseY = transform.position.y;
        FindPlayer();
        SetupVisuals();
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null)
        {
            playerCtrl = FindAnyObjectByType<PlayerController2D>();
            if (playerCtrl != null) p = playerCtrl.gameObject;
        }
        else
        {
            playerCtrl = p.GetComponent<PlayerController2D>();
        }

        if (p != null) targetPlayer = p.transform;
    }

    void SetupVisuals()
    {
        if (isGoose)
        {
            // 1. Sprite de Ganso Volador
            GameObject gooseGO = new GameObject("GooseSprite");
            gooseGO.transform.SetParent(transform, false);
            gooseGO.transform.localPosition = Vector3.zero;

            bannerSr = gooseGO.AddComponent<SpriteRenderer>();
            bannerSr.sprite = GetGooseSprite();
            bannerSr.sortingOrder = 24;
            bannerSr.flipX = (moveDirection.x > 0);
            gooseGO.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            if (boxCol != null)
            {
                boxCol.size = new Vector2(1.1f, 0.65f);
                boxCol.offset = Vector2.zero;
            }

            // Letrero pequeño sobre el ganso
            GameObject textGO = new GameObject("GooseText");
            textGO.transform.SetParent(transform, false);
            textGO.transform.localPosition = new Vector3(0, 0.5f, 0);

            textMesh = textGO.AddComponent<TextMesh>();
            textMesh.text = phraseText;
            textMesh.fontSize = 24;
            textMesh.characterSize = 0.030f;
            textMesh.color = new Color(1f, 0.95f, 0.4f, 1f);
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;

            MeshRenderer mr = textGO.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 26;
            return;
        }

        // Dimensiones reducidas ("sea pequeño")
        float width = Mathf.Clamp(phraseText.Length * 0.095f + 0.42f, 1.15f, 2.5f);
        float height = 0.38f;

        // 1. Sprite de Fondo (Insignia Compacta)
        GameObject bannerGO = new GameObject("Banner");
        bannerGO.transform.SetParent(transform, false);
        bannerGO.transform.localPosition = Vector3.zero;

        bannerSr = bannerGO.AddComponent<SpriteRenderer>();
        bannerSr.sprite = GetOrCreateBannerSprite();
        bannerSr.sortingOrder = 24;
        bannerGO.transform.localScale = new Vector3(width, height, 1f);

        // Collider ajustado con precisión pequeña
        if (boxCol != null)
        {
            boxCol.size = new Vector2(width * 0.88f, height * 0.85f);
            boxCol.offset = Vector2.zero;
        }

        // 2. Sombra del texto
        GameObject shadowGO = new GameObject("ShadowText");
        shadowGO.transform.SetParent(transform, false);
        shadowGO.transform.localPosition = new Vector3(0.022f, -0.022f, 0f);

        shadowTextMesh = shadowGO.AddComponent<TextMesh>();
        shadowTextMesh.text = phraseText;
        shadowTextMesh.fontSize = 32;
        shadowTextMesh.characterSize = 0.034f;
        shadowTextMesh.color = new Color(0f, 0f, 0f, 0.95f);
        shadowTextMesh.alignment = TextAlignment.Center;
        shadowTextMesh.anchor = TextAnchor.MiddleCenter;

        MeshRenderer smr = shadowGO.GetComponent<MeshRenderer>();
        if (smr != null) smr.sortingOrder = 25;

        // 3. Texto principal
        GameObject textGO2 = new GameObject("Text");
        textGO2.transform.SetParent(transform, false);
        textGO2.transform.localPosition = Vector3.zero;

        textMesh = textGO2.AddComponent<TextMesh>();
        textMesh.text = phraseText;
        textMesh.fontSize = 32;
        textMesh.characterSize = 0.034f;
        textMesh.color = new Color(1f, 0.96f, 0.82f, 1f);
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;

        MeshRenderer mr2 = textGO2.GetComponent<MeshRenderer>();
        if (mr2 != null) mr2.sortingOrder = 26;
    }

    void Update()
    {
        if (isDestroyed) return;

        // 1. Desplazamiento horizontal
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);

        // 2. Flotación oscilatoria sutil
        float bob = Mathf.Sin((Time.time + wobbleOffset) * wobbleFrequency) * wobbleAmplitude;
        Vector3 cur = transform.position;
        cur.y = baseY + bob;
        transform.position = cur;

        // 3. Detección de salto exitoso por encima ("por cada saltos te de vida y puntos")
        CheckPlayerJumpOver();

        // 4. Límite de tiempo
        if (Time.time - spawnTime >= lifetime)
        {
            StartCoroutine(FadeAndDestroy());
        }
    }

    private void CheckPlayerJumpOver()
    {
        if (hasRewardedJump || isDestroyed) return;

        if (targetPlayer == null)
        {
            FindPlayer();
            if (targetPlayer == null) return;
        }

        float dx = transform.position.x - targetPlayer.position.x;

        // Comprobar si el proyectil está cruzando o ha cruzado verticalmente la posición del jugador
        bool isCrossing = (moveDirection.x < 0 && dx <= 0.2f && dx >= -1.4f) ||
                          (moveDirection.x > 0 && dx >= -0.2f && dx <= 1.4f);

        if (isCrossing)
        {
            float playerY = targetPlayer.position.y;
            float projY = transform.position.y;

            // Si el jugador está por encima del proyectil saltando
            bool isAbove = (playerY > projY + 0.32f);

            if (isAbove)
            {
                hasRewardedJump = true;
                OnJumpRewardSuccess();
            }
        }
    }

    private void OnJumpRewardSuccess()
    {
        // 1. Otorga VIDA al jugador
        HealthSystem hs = targetPlayer != null ?
            (targetPlayer.GetComponent<HealthSystem>() ?? targetPlayer.GetComponentInParent<HealthSystem>()) : null;

        if (hs != null)
        {
            hs.Heal(healthRewardOnJump);
            ScoreManager.SpawnFloatingPopup(targetPlayer.position + new Vector3(0, 1.35f, 0), $"+{healthRewardOnJump} HP", new Color(0.2f, 1f, 0.4f, 1f));
        }

        // 2. Otorga PUNTOS al jugador
        Vector3 scorePos = targetPlayer != null ? targetPlayer.position + new Vector3(0, 0.75f, 0) : transform.position;
        ScoreManager.AddScore(pointsRewardOnJump, scorePos, $"+{pointsRewardOnJump} PTS");

        // 3. Feedback visual en el proyectil (destello dorado de evasión)
        StartCoroutine(DodgeFlashRoutine());
    }

    private IEnumerator DodgeFlashRoutine()
    {
        if (bannerSr != null) bannerSr.color = new Color(0.3f, 1f, 0.4f, 1f); // Verde de salto exitoso
        if (textMesh != null) textMesh.color = Color.white;

        yield return new WaitForSeconds(0.2f);

        if (bannerSr != null && !isDestroyed) bannerSr.color = Color.white;
        if (textMesh != null && !isDestroyed) textMesh.color = new Color(1f, 0.96f, 0.82f, 1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed) return;

        bool isPlayer = other.CompareTag("Player") ||
                        other.GetComponent<PlayerController2D>() != null ||
                        other.GetComponentInParent<PlayerController2D>() != null;

        if (isPlayer)
        {
            GameObject playerGO = other.gameObject;
            var pc = playerGO.GetComponent<PlayerController2D>() ?? playerGO.GetComponentInParent<PlayerController2D>();
            if (pc != null) playerGO = pc.gameObject;

            // Daño al jugador si no esquivó
            HealthSystem hs = playerGO.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.TakeDamage(damageToPlayer);
            }

            // Knockback
            Rigidbody2D prb = playerGO.GetComponent<Rigidbody2D>();
            if (prb != null)
            {
                float pushDir = Mathf.Sign(playerGO.transform.position.x - transform.position.x);
                prb.linearVelocity = new Vector2(pushDir * playerKnockback, 3.2f);
            }

            StartCoroutine(ImpactEffect());
        }
    }

    /// <summary>
    /// Destruir la frase con el pico (Tecla V).
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;

        // Recompensa por romper la frase con el pico
        ScoreManager.AddScore(pointsRewardOnDestroy, transform.position + new Vector3(0, 0.4f, 0), $"+{pointsRewardOnDestroy} PTS");

        if (targetPlayer != null)
        {
            HealthSystem hs = targetPlayer.GetComponent<HealthSystem>() ?? targetPlayer.GetComponentInParent<HealthSystem>();
            if (hs != null)
            {
                hs.Heal(5);
                ScoreManager.SpawnFloatingPopup(targetPlayer.position + new Vector3(0, 1.1f, 0), "+5 HP", Color.green);
            }
        }

        StartCoroutine(ShatterEffect());
    }

    private IEnumerator ShatterEffect()
    {
        isDestroyed = true;
        if (boxCol != null) boxCol.enabled = false;

        if (textMesh != null) textMesh.color = Color.yellow;
        if (bannerSr != null) bannerSr.color = Color.yellow;

        float t = 0f;
        Vector3 startScale = transform.localScale;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, startScale * 1.35f, t / 0.12f);
            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator ImpactEffect()
    {
        isDestroyed = true;
        if (boxCol != null) boxCol.enabled = false;

        if (bannerSr != null) bannerSr.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        yield return new WaitForSeconds(0.08f);
        Destroy(gameObject);
    }

    private IEnumerator FadeAndDestroy()
    {
        isDestroyed = true;
        if (boxCol != null) boxCol.enabled = false;

        float elapsed = 0f;
        float dur = 0.25f;
        Color initialBanner = bannerSr != null ? bannerSr.color : Color.white;
        Color initialText = textMesh != null ? textMesh.color : Color.white;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / dur);

            if (bannerSr != null)
            {
                Color c = initialBanner;
                c.a = alpha;
                bannerSr.color = c;
            }
            if (textMesh != null)
            {
                Color c = initialText;
                c.a = alpha;
                textMesh.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
