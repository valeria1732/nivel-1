using System.Collections;
using UnityEngine;

/// <summary>
/// Controla el ÚNICO PICO en la mano de Esperanza.
/// - Usa el sprite limpio del pico (prop_pico) con pivote en la empuñadura para que
///   rote exactamente desde el guante de Esperanza.
/// - Al presionar la tecla V, levanta el pico con fuerza y descarga un picazo amplio
///   y claro hacia adelante/abajo con estela de impacto.
/// - Al caminar o saltar, el pico acompaña el movimiento de la mano con suavidad.
/// </summary>
public class PickaxeItem : MonoBehaviour
{
    [Header("Control")]
    [Tooltip("Tecla para golpear con el pico")]
    public KeyCode attackKey = KeyCode.V;
    [Tooltip("Tecla para dar vueltas de 360 grados con el pico")]
    public KeyCode spinKey = KeyCode.O;
    public float attackCooldown = 0.20f;
    public float spinDuration = 0.36f;

    [Header("Daño y Alcance")]
    public int damage = 60;
    public float attackRange = 1.95f;
    public Vector2 hitOffset = new Vector2(0.95f, 0.95f);
    public LayerMask targetLayers = ~0;

    [Header("Ángulos del Movimiento del Pico (Tecla V)")]
    [Tooltip("Ángulo en reposo sostenido en la mano")]
    public float restAngle = -18f;
    [Tooltip("Ángulo al levantar el pico hacia arriba para tomar impulso")]
    public float raiseAngle = 68f;
    [Tooltip("Ángulo al descargar el picazo hacia adelante y abajo")]
    public float strikeAngle = -92f;

    [Header("Tiempos de Animación (segundos)")]
    public float raiseDuration = 0.08f;
    public float strikeDuration = 0.08f;
    public float recoveryDuration = 0.14f;

    [Header("Oscilación al Caminar / Reposo")]
    public float idleBobSpeed = 3.0f;
    public float idleBobAngle = 8f;
    public float walkBobSpeed = 13.5f;
    public float walkBobAngle = 38f;

    [Header("Posición en la Mano y Referencias")]
    public PlayerController2D player;
    public SpriteRenderer spriteRenderer;
    [Tooltip("Posición exacta del guante delantero de Esperanza")]
    public Vector2 shoulderOffset = new Vector2(0.24f, 0.94f);
    public Vector3 baseScale = new Vector3(0.62f, 0.62f, 1f);

    [Header("Efectos Opcionales")]
    public ParticleSystem hitParticles;

    private bool isAttacking = false;
    private float currentAngle = -18f;
    private float lastAttackTime = -999f;
    private Vector2 swingOffset = Vector2.zero;
    private static Sprite pivotedPickaxeSprite;
    private static Sprite swingTrailSprite;

    void Awake()
    {
        // Eliminar cualquier otro PickaxeItem duplicado para garantizar que SOLO exista 1 pico
        PickaxeItem[] allPicks = FindObjectsByType<PickaxeItem>(FindObjectsInactive.Include);
        if (allPicks != null && allPicks.Length > 1)
        {
            foreach (var p in allPicks)
            {
                if (p != null && p != this)
                {
                    Destroy(p.gameObject);
                }
            }
        }

        // Si había objetos hijos extra, limpiarlos
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null && child.GetComponent<SpriteRenderer>() != null && child.gameObject != gameObject)
            {
                Destroy(child.gameObject);
            }
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (player == null)
            player = GetComponentInParent<PlayerController2D>() ?? FindAnyObjectByType<PlayerController2D>();

        shoulderOffset = new Vector2(0.24f, 0.94f);
        baseScale = new Vector3(0.62f, 0.62f, 1f);
        restAngle = -18f;
        raiseAngle = 68f;
        strikeAngle = -92f;
        walkBobSpeed = 13.5f;
        walkBobAngle = 38f;
        currentAngle = restAngle;
        if (damage < 60) damage = 60;
        if (attackRange < 1.95f) attackRange = 1.95f;
        attackCooldown = 0.20f;

        ApplyMacheteVisuals();
    }

    void Start()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerController2D>();

        ApplyMacheteVisuals();
    }

    /// <summary>
    /// Configura el sprite único del PICO con el pivote en el mango para que gire desde el guante.
    /// </summary>
    public void ApplyMacheteVisuals()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = GetOrCreatePivotedPickaxeSprite();
        spriteRenderer.sortingOrder = 14;
        spriteRenderer.color = Color.white;
    }

    private static Sprite GetOrCreatePivotedPickaxeSprite()
    {
        if (pivotedPickaxeSprite != null) return pivotedPickaxeSprite;

        Sprite raw = Resources.Load<Sprite>("prop_pico");
#if UNITY_EDITOR
        if (raw == null)
            raw = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Props/prop_pico.png");
#endif
        if (raw != null && raw.texture != null)
        {
            Texture2D tex = raw.texture;
            // Pivote en la empuñadura inferior izquierda (0.35, 0.22) para que gire desde la mano
            pivotedPickaxeSprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.35f, 0.22f),
                100f
            );
            return pivotedPickaxeSprite;
        }

        return raw;
    }

    void Update()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController2D>();
            if (player == null) return;
        }

        if (DialogueManager.IsTalking) return;

        // --- TECLA V: GOLPE DE PICO ---
        if (Input.GetKeyDown(attackKey) && !isAttacking && (Time.time - lastAttackTime >= attackCooldown))
        {
            StartCoroutine(PerformPickaxeSwing());
        }

        // --- TECLA O: VUELTA DE 360 GRADOS DEL PICO ---
        if ((Input.GetKeyDown(spinKey) || Input.GetKey(spinKey)) && !isAttacking && (Time.time - lastAttackTime >= 0.12f))
        {
            StartCoroutine(PerformPickaxe360Spin());
        }

        if (!isAttacking)
        {
            UpdateHandMovement();
        }
    }

    public void TriggerSwingFromPlayer()
    {
        if (!isAttacking && gameObject.activeInHierarchy)
        {
            StartCoroutine(PerformPickaxeSwing());
        }
    }

    public void TriggerSpin360FromPlayer()
    {
        if (!isAttacking && gameObject.activeInHierarchy)
        {
            StartCoroutine(PerformPickaxe360Spin());
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        int facing = player.FacingDirection;
        Vector3 targetPos = player.transform.position + new Vector3(
            (shoulderOffset.x + swingOffset.x) * facing,
            shoulderOffset.y + swingOffset.y,
            -0.05f
        );

        transform.position = targetPos;
        transform.localScale = new Vector3(facing * baseScale.x, baseScale.y, 1f);

        float finalAngle = facing > 0 ? currentAngle : -currentAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, finalAngle);
    }

    void UpdateHandMovement()
    {
        bool isMoving = Mathf.Abs(player.Horizontal) > 0.05f;
        float targetAngle;
        Vector2 targetOffset = Vector2.zero;

        if (player.IsDigging)
        {
            float digSine = Mathf.Sin(Time.time * 20f);
            targetAngle = restAngle + (digSine * 45f);
            targetOffset = new Vector2(0.05f, digSine * 0.05f);
        }
        else if (!player.IsGrounded)
        {
            targetAngle = 42f;
            targetOffset = new Vector2(0.06f, 0.12f);
        }
        else if (isMoving)
        {
            float walkSine = Mathf.Sin(Time.time * walkBobSpeed);
            targetAngle = restAngle + (walkSine * walkBobAngle);
            targetOffset = new Vector2(walkSine * 0.09f, Mathf.Abs(walkSine) * 0.07f);
        }
        else
        {
            float idleSine = Mathf.Sin(Time.time * idleBobSpeed);
            targetAngle = restAngle + (idleSine * idleBobAngle);
        }

        currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * 12f);
        swingOffset = Vector2.Lerp(swingOffset, targetOffset, Time.deltaTime * 12f);
    }

    IEnumerator PerformPickaxeSwing()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // 1. SUBIDA: Levanta el pico bien alto tomando impulso
        float elapsed = 0f;
        float startAngle = currentAngle;
        while (elapsed < raiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / raiseDuration);
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
            currentAngle = Mathf.Lerp(startAngle, raiseAngle, t);
            swingOffset = Vector2.Lerp(Vector2.zero, new Vector2(-0.05f, 0.12f), t);
            yield return null;
        }

        // Mostrar arco visual del picazo para que el movimiento se vea súper claro
        SpawnSwingTrail();

        // 2. BAJADA CONTUNDENTE: Descarga el picazo hacia adelante y abajo
        elapsed = 0f;
        while (elapsed < strikeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / strikeDuration);
            float easeT = t * t;
            currentAngle = Mathf.Lerp(raiseAngle, strikeAngle, easeT);
            swingOffset = Vector2.Lerp(new Vector2(-0.05f, 0.12f), new Vector2(0.16f, -0.06f), easeT);
            yield return null;
        }

        // --- IMPACTO ---
        CheckHit();

        yield return new WaitForSeconds(0.04f);

        // 3. RECUPERACIÓN SUAVE
        elapsed = 0f;
        float postStrikeAngle = currentAngle;
        Vector2 postOffset = swingOffset;
        while (elapsed < recoveryDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / recoveryDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            currentAngle = Mathf.Lerp(postStrikeAngle, restAngle, t);
            swingOffset = Vector2.Lerp(postOffset, Vector2.zero, t);
            yield return null;
        }

        currentAngle = restAngle;
        swingOffset = Vector2.zero;
        isAttacking = false;
    }

    /// <summary>
    /// Hace que el pico dé una vuelta completa de 360 grados al presionar la tecla O,
    /// golpeando a los enemigos alrededor con estela circular.
    /// </summary>
    IEnumerator PerformPickaxe360Spin()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        float startAngle = restAngle;
        float endAngle = restAngle - 360f;
        float elapsed = 0f;
        bool midHitDone = false;

        SpawnSwingTrail();

        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spinDuration);

            // Giro completo de 360 grados fluido y continuo
            currentAngle = Mathf.Lerp(startAngle, endAngle, t);

            // Movimiento circular de la muñeca acompañando el giro de 360°
            float rad = t * Mathf.PI * 2f;
            swingOffset = new Vector2(Mathf.Cos(rad) * 0.12f, Mathf.Sin(rad) * 0.12f);

            if (!midHitDone && t >= 0.45f)
            {
                midHitDone = true;
                SpawnSwingTrail();
                CheckHit360();
            }

            yield return null;
        }

        CheckHit360();

        currentAngle = restAngle;
        swingOffset = Vector2.zero;
        isAttacking = false;
    }

    void CheckHit360()
    {
        if (player == null) return;

        Vector2 center = (Vector2)player.transform.position + new Vector2(0f, 1.0f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, attackRange * 1.25f, targetLayers);

        foreach (var col in hits)
        {
            if (col == null || col.gameObject == player.gameObject || col.gameObject == gameObject) continue;

            IDamageable dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(damage);
            }
        }

        if (hitParticles != null) hitParticles.Play();
    }

    private void SpawnSwingTrail()
    {
        if (player == null) return;
        int facing = player.FacingDirection;

        GameObject trailGO = new GameObject("PickaxeSwingArc");
        trailGO.transform.position = player.transform.position + new Vector3(facing * 0.85f, 1.05f, -0.1f);
        trailGO.transform.localScale = new Vector3(facing * 1.2f, 1.2f, 1f);

        SpriteRenderer sr = trailGO.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreateSwingTrailSprite();
        sr.sortingOrder = 15;
        sr.color = new Color(1f, 0.94f, 0.65f, 0.88f);

        StartCoroutine(AnimateTrail(trailGO, sr));
    }

    private IEnumerator AnimateTrail(GameObject trailGO, SpriteRenderer sr)
    {
        float dur = 0.13f;
        float elapsed = 0f;
        Vector3 startScale = trailGO.transform.localScale * 0.8f;
        Vector3 endScale = trailGO.transform.localScale * 1.22f;

        while (elapsed < dur)
        {
            if (trailGO == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            trailGO.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            Color c = sr.color;
            c.a = Mathf.Lerp(0.9f, 0f, t);
            sr.color = c;
            yield return null;
        }

        if (trailGO != null) Destroy(trailGO);
    }

    private static Sprite GetOrCreateSwingTrailSprite()
    {
        if (swingTrailSprite != null) return swingTrailSprite;

        int size = 48;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0, 0, 0, 0);
        Vector2 center = new Vector2(12f, 24f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 diff = new Vector2(x, y) - center;
                float dist = diff.magnitude;
                float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

                if (angle >= -65f && angle <= 65f && dist >= 18f && dist <= 31f)
                {
                    float angleFactor = 1f - Mathf.Abs(angle) / 65f;
                    float radialCenter = Mathf.Lerp(22f, 27f, angleFactor);
                    float thickness = 3.8f * angleFactor;
                    float d = Mathf.Abs(dist - radialCenter);
                    if (d <= thickness)
                    {
                        float alpha = Mathf.Clamp01(1f - (d / thickness)) * angleFactor;
                        tex.SetPixel(x, y, new Color(1f, 0.96f, 0.75f, alpha));
                    }
                    else tex.SetPixel(x, y, clear);
                }
                else tex.SetPixel(x, y, clear);
            }
        }

        tex.Apply();
        swingTrailSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 36f);
        return swingTrailSprite;
    }

    void CheckHit()
    {
        if (player == null) return;

        int facing = player.FacingDirection;
        Vector2 hitCenter = (Vector2)player.transform.position + new Vector2(hitOffset.x * facing, hitOffset.y);

        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, attackRange, targetLayers);

        foreach (var col in hits)
        {
            if (col == null || col.gameObject == player.gameObject || col.gameObject == gameObject) continue;

            IDamageable dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(damage);
                continue;
            }
        }

        if (hitParticles != null) hitParticles.Play();
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        int facing = player.FacingDirection;
        Vector2 hitCenter = (Vector2)player.transform.position + new Vector2(hitOffset.x * facing, hitOffset.y);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.7f);
        Gizmos.DrawWireSphere(hitCenter, attackRange);
    }
}
