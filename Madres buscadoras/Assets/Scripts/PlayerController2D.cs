using System;
using System.Collections;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movimiento Horizontal")]
    public float speed = 4f;

    [Header("Salto")]
    public float jumpForce = 8.5f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer = ~0; // Por defecto colisiona con todo excepto jugador
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Excavación")]
    public KeyCode digKey = KeyCode.F;
    public float defaultDigDuration = 1.5f;
    public GameObject digEffectPrefab;

    [Header("Combate con Pico (Tecla V)")]
    public KeyCode attackKey = KeyCode.V;
    public float attackCooldown = 0.32f;
    public int pickaxeDamage = 35;
    public float attackRange = 1.35f;
    public Vector2 hitOffset = new Vector2(0.9f, 0.4f);
    public LayerMask targetLayers = ~0;
    private float lastAttackTime = -10f;
    public bool IsAttacking { get; private set; }

    [Header("Efectos Visuales")]
    public bool enableSquashAndStretch = true;

    // Estados públicos
    public float Horizontal { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsDigging { get; private set; }
    public bool IsJumping => !IsGrounded && rb.linearVelocity.y > 0.1f;
    public bool IsFalling => !IsGrounded && rb.linearVelocity.y < -0.1f;
    public bool IsFacingRight => sr == null || !sr.flipX;
    public int FacingDirection => IsFacingRight ? 1 : -1;

    // Eventos
    public event Action OnJump;
    public event Action OnLand;
    public event Action<bool> OnDigStateChanged; // true = inició, false = terminó

    Rigidbody2D rb;
    SpriteRenderer sr;
    Collider2D col;
    Vector3 originalScale;

    float coyoteCounter;
    float jumpBufferCounter;
    bool wasGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        ApplyMadreBuscadoraCharacter();
    }

    void Start()
    {
        ApplyMadreBuscadoraCharacter();
        Level1Manager.EnsureExtendedLevelSetup();
        PresidentEnemy.EnsureSpawnRuntime();
        ScoreManager.EnsureInstance();
    }

    /// <summary>
    /// Configura el sprite oficial de la Madre Buscadora con su sombrero, chaleco,
    /// mochila, varilla en T y pico, con proporciones pixel-art naturales y animación fluida.
    /// </summary>
    private static Sprite cachedCleanPlayerSprite;
    private static Sprite[] cachedCleanSheetFrames;

    /// <summary>
    /// Remueve el pico estático pintado en la esquina inferior derecha del sprite original
    /// para que el personaje SOLO tenga UN pico (el que se mueve con la tecla V).
    /// </summary>
    private static Sprite RemoveStaticPickaxeFromSprite(Sprite orig)
    {
        if (orig == null || orig.texture == null) return orig;
        try
        {
            Rect r = orig.rect;
            int w = Mathf.RoundToInt(r.width);
            int h = Mathf.RoundToInt(r.height);
            int rx = Mathf.RoundToInt(r.x);
            int ry = Mathf.RoundToInt(r.y);

            Texture2D cleanTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            cleanTex.filterMode = orig.texture.filterMode;

            Color[] pixels = orig.texture.GetPixels(rx, ry, w, h);
            for (int y = 0; y < h; y++)
            {
                float v = (float)y / h;
                if (v >= 0.23f && v <= 0.51f)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float u = (float)x / w;
                        if (u > 0.73f)
                        {
                            pixels[y * w + x] = Color.clear;
                        }
                    }
                }
            }

            cleanTex.SetPixels(pixels);
            cleanTex.Apply();

            Vector2 normPivot = new Vector2(orig.pivot.x / r.width, orig.pivot.y / r.height);
            return Sprite.Create(cleanTex, new Rect(0, 0, w, h), normPivot, orig.pixelsPerUnit);
        }
        catch
        {
            return orig;
        }
    }

    /// <summary>
    /// Genera 6 frames de animación reales y amplios (Idle + 4 pasos de caminata + 1 pose de salto)
    /// proporcionales al tamaño de la textura para que la muñeca mueva claramente piernas, brazos y cuerpo.
    /// </summary>
    private static Sprite[] GenerateWalkCycleFrames(Sprite baseSprite)
    {
        if (baseSprite == null || baseSprite.texture == null)
            return new Sprite[] { baseSprite, baseSprite, baseSprite, baseSprite, baseSprite, baseSprite };

        try
        {
            Texture2D srcTex = baseSprite.texture;
            int w = srcTex.width;
            int h = srcTex.height;
            Color[] srcPixels = srcTex.GetPixels();
            Vector2 normPivot = new Vector2(baseSprite.pivot.x / baseSprite.rect.width, baseSprite.pivot.y / baseSprite.rect.height);

            // Desplazamientos proporcionales a la resolución real de la textura (ej. 568x1024)
            int sX = Mathf.Max(8, Mathf.RoundToInt(w * 0.095f)); // ~54 px de zancada horizontal
            int sY = Mathf.Max(8, Mathf.RoundToInt(h * 0.055f)); // ~56 px de elevación de pierna/rodilla
            int bY = Mathf.Max(4, Mathf.RoundToInt(h * 0.026f)); // ~26 px de rebote del torso
            int aX = Mathf.Max(5, Mathf.RoundToInt(w * 0.050f)); // ~28 px de braceo con la varilla

            // (leftLegDX, leftLegDY, rightLegDX, rightLegDY, torsoDY, armDX)
            (int ldx, int ldy, int rdx, int rdy, int tdy, int adx)[] poses = new (int, int, int, int, int, int)[]
            {
                (  0,          0,          0,          0,         0,   0), // Frame 0: Idle
                (-sX,         sY,         sX,     sY / 4,        bY,  aX), // Frame 1: Gran zancada A (pierna izq adelante/arriba, der atrás)
                (-sX / 2, sY / 2,     sX / 2,     sY / 2, bY * 3 / 2, aX / 2), // Frame 2: Impulso alto A
                ( sX,     sY / 4,        -sX,         sY,        bY, -aX), // Frame 3: Gran zancada B (pierna izq atrás, der adelante/arriba)
                ( sX / 2, sY / 2,    -sX / 2,     sY / 2,    bY / 2, -aX / 2), // Frame 4: Impulso medio B
                (-sX * 3 / 4, sY + bY, sX * 3 / 4, sY + bY,      bY,  aX)  // Frame 5: Pose de Salto (ambas piernas flexionadas en el aire)
            };

            Sprite[] frames = new Sprite[poses.Length];
            frames[0] = baseSprite;

            int legTopY = Mathf.RoundToInt(h * 0.43f);
            int armBottomY = Mathf.RoundToInt(h * 0.36f);
            int armTopY = Mathf.RoundToInt(h * 0.66f);
            int midX = Mathf.RoundToInt(w * 0.50f);

            for (int f = 1; f < poses.Length; f++)
            {
                var p = poses[f];
                Texture2D frameTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                frameTex.filterMode = srcTex.filterMode;
                Color[] dst = new Color[w * h];

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color c = srcPixels[y * w + x];
                        if (c.a <= 0.01f) continue;

                        int nx = x;
                        int ny = y;

                        if (y < legTopY)
                        {
                            // Movimiento amplio de piernas y botas desde la cadera hasta el suelo
                            float legFactor = 1f - ((float)y / legTopY);
                            if (x < midX)
                            {
                                nx = x + Mathf.RoundToInt(p.ldx * legFactor);
                                ny = y + Mathf.RoundToInt(p.ldy * legFactor);
                            }
                            else
                            {
                                nx = x + Mathf.RoundToInt(p.rdx * legFactor);
                                ny = y + Mathf.RoundToInt(p.rdy * legFactor);
                            }
                        }
                        else
                        {
                            ny = y + p.tdy;
                            // Braceo del brazo izquierdo y la varilla buscadora
                            if (y >= armBottomY && y <= armTopY && x < midX)
                            {
                                float armFactor = 1f - Mathf.Abs((y - (armBottomY + armTopY) * 0.5f) / ((armTopY - armBottomY) * 0.5f));
                                nx = x + Mathf.RoundToInt(p.adx * Mathf.Clamp01(armFactor));
                            }
                        }

                        if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                        {
                            dst[ny * w + nx] = c;
                        }
                    }
                }

                frameTex.SetPixels(dst);
                frameTex.Apply();
                frames[f] = Sprite.Create(frameTex, new Rect(0, 0, w, h), normPivot, baseSprite.pixelsPerUnit);
            }

            return frames;
        }
        catch
        {
            return new Sprite[] { baseSprite, baseSprite, baseSprite, baseSprite, baseSprite, baseSprite };
        }
    }

    public void ApplyMadreBuscadoraCharacter()
    {
        if (cachedCleanPlayerSprite == null)
        {
            Sprite rawSprite = Resources.Load<Sprite>("madre_buscadora_player");
#if UNITY_EDITOR
            if (rawSprite == null)
                rawSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Personajes/madre_buscadora_player.png");
#endif
            if (rawSprite == null)
                rawSprite = Resources.Load<Sprite>("esperanza_sprite");

            cachedCleanPlayerSprite = RemoveStaticPickaxeFromSprite(rawSprite);
        }

        Sprite newSprite = cachedCleanPlayerSprite;

        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null && newSprite != null)
        {
            sr.sprite = newSprite;
            sr.sortingOrder = 6;
            sr.color = Color.white;

            // Normalizar escala local del hijo SPRITE a 1:1
            if (sr.transform != transform)
            {
                sr.transform.localScale = Vector3.one;
                sr.transform.localPosition = Vector3.zero;
            }
        }

        // Configurar SimpleSpriteAnimator con frames de caminata reales (piernas y paso animados + sin pico duplicado)
        SimpleSpriteAnimator ssa = GetComponentInChildren<SimpleSpriteAnimator>();
        if (ssa == null && sr != null)
        {
            ssa = sr.gameObject.AddComponent<SimpleSpriteAnimator>();
        }
        if (ssa != null)
        {
            ssa.enabled = true;
            ssa.player = this;
            if (cachedCleanSheetFrames == null || cachedCleanSheetFrames.Length < 6)
            {
                cachedCleanSheetFrames = GenerateWalkCycleFrames(cachedCleanPlayerSprite);
            }

            if (cachedCleanSheetFrames != null && cachedCleanSheetFrames.Length >= 6)
            {
                ssa.idle = cachedCleanSheetFrames[0];
                ssa.walk = new Sprite[] { cachedCleanSheetFrames[1], cachedCleanSheetFrames[2], cachedCleanSheetFrames[3], cachedCleanSheetFrames[4] };
                ssa.jumpSprite = cachedCleanSheetFrames[5];
                ssa.fps = 12f;
            }
        }

        // Asegurar velocidad ágil de caminata y buen impulso de salto
        if (speed < 6.0f) speed = 6.0f;
        if (jumpForce < 11.5f) jumpForce = 11.5f;

        // Proporciones naturales y firmes de la Madre Buscadora
        transform.localScale = new Vector3(1.15f, 1.15f, 1f);
        originalScale = transform.localScale;

        // Ajustar colisiones exactas a la silueta
        CapsuleCollider2D capCol = GetComponent<CapsuleCollider2D>();
        if (capCol != null)
        {
            capCol.size = new Vector2(0.85f, 2.3f);
            capCol.offset = new Vector2(0f, 1.15f);
        }
        else
        {
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(0.85f, 2.3f);
                box.offset = new Vector2(0f, 1.15f);
            }
        }

        // Ocultar y neutralizar rigs de brazos artificiales flotantes viejos
        ArmAnimator aa = GetComponent<ArmAnimator>();
        if (aa != null)
        {
            aa.enabled = false;
            if (aa.armsRig != null) aa.armsRig.gameObject.SetActive(false);
        }
        GameObject armsRigRoot = GameObject.Find("ArmsRig");
        if (armsRigRoot != null) armsRigRoot.SetActive(false);

        // Activar y asegurar el Machete en la muñeca que se mueve con la tecla V
        EnsureWristMacheteSetup();
    }

    private PickaxeItem wristMachete;

    private void EnsureWristMacheteSetup()
    {
        if (wristMachete == null)
        {
            wristMachete = FindAnyObjectByType<PickaxeItem>(FindObjectsInactive.Include);
        }

        if (wristMachete == null)
        {
            GameObject pickaxeHolder = GameObject.Find("PickaxeHolder");
            if (pickaxeHolder == null)
            {
                pickaxeHolder = new GameObject("PickaxeHolder");
            }
            pickaxeHolder.SetActive(true);
            wristMachete = pickaxeHolder.GetComponent<PickaxeItem>();
            if (wristMachete == null)
            {
                wristMachete = pickaxeHolder.AddComponent<PickaxeItem>();
            }
        }
        else
        {
            wristMachete.gameObject.SetActive(true);
            wristMachete.enabled = true;
        }

        wristMachete.player = this;
        wristMachete.attackKey = attackKey;
        wristMachete.ApplyMacheteVisuals();
    }

    void Update()
    {
        // Si está en diálogo o excavando, se bloquean controles de movimiento
        bool inputBlocked = DialogueManager.IsTalking || IsDigging;

        // --- ATAQUE CON PICO (Tecla V) ---
        if (!inputBlocked && Input.GetKeyDown(attackKey) && !IsAttacking && (Time.time - lastAttackTime >= attackCooldown))
        {
            StartCoroutine(ExecutePickaxeAttack());
        }

        // --- VUELTA DE 360 DEL PICO (Tecla O) ---
        if (!inputBlocked && Input.GetKeyDown(KeyCode.O) && !IsAttacking && (Time.time - lastAttackTime >= 0.15f))
        {
            StartCoroutine(ExecutePickaxe360Spin());
        }

        // --- FRASES A LOS ENEMIGOS AL ACERCARSE ---
        CheckNearbyEnemiesAndSpeak();

        // --- AGACHARSE (Tecla S o Flecha Abajo) ---
        bool wantsCrouch = !inputBlocked && IsGrounded && (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow));
        if (wantsCrouch)
        {
            Horizontal *= 0.35f;
        }

        // --- ENTRADA HORIZONTAL ---
        Horizontal = inputBlocked ? 0f : (wantsCrouch ? Horizontal : Input.GetAxisRaw("Horizontal"));
        if (sr != null)
        {
            if (Horizontal > 0.01f) sr.flipX = false;
            else if (Horizontal < -0.01f) sr.flipX = true;
        }

        // --- ANIMACIÓN DINÁMICA DEL CUERPO DE LA CHICA DEL PICO ---
        AnimateEsperanzaBody(wantsCrouch);

        // --- VERIFICACIÓN DE SUELO ---
        CheckGrounded();

        // --- TEMPORIZADORES DE SALTO (Coyote & Buffer) ---
        if (IsGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        if (!inputBlocked && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetButtonDown("Jump")))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // --- EJECUTAR SALTO ---
        if (jumpBufferCounter > 0f && coyoteCounter > 0f && !IsDigging && !DialogueManager.IsTalking)
        {
            ExecuteJump();
        }

        // --- EXCAVACIÓN LIBRE (Tecla F) ---
        if (!inputBlocked && IsGrounded && Input.GetKeyDown(digKey))
        {
            // Solo si no hay un punto de excavación interactivo cercano que lo maneje por su cuenta
            if (!IsNearActiveExcavationSpot())
            {
                StartDigging(defaultDigDuration, () =>
                {
                    // Diálogo informativo al excavar en tierra vacía
                    if (DialogueManager.Instance != null)
                    {
                        DialogueManager.Instance.Play("Esperanza", new string[] {
                            "Excavé aquí, pero solo encontré tierra seca y piedras.",
                            "Debo seguir adelante y derrotar a los enemigos que bloquean el camino."
                        });
                    }
                });
            }
        }
    }

    private void AnimateEsperanzaBody(bool wantsCrouch)
    {
        if (IsDigging || IsAttacking) return;

        if (wantsCrouch)
        {
            transform.localScale = new Vector3(originalScale.x * 1.15f, originalScale.y * 0.65f, originalScale.z);
            if (sr != null && sr.transform != transform) sr.transform.localRotation = Quaternion.identity;
            return;
        }

        bool isWalking = IsGrounded && Mathf.Abs(Horizontal) > 0.05f;
        if (isWalking)
        {
            float walkWave = Mathf.Sin(Time.time * 16f);
            float sx = originalScale.x * (1f + walkWave * 0.085f);
            float sy = originalScale.y * (1f - walkWave * 0.085f);
            transform.localScale = new Vector3(sx, sy, originalScale.z);

            if (sr != null && sr.transform != transform)
            {
                float strideTilt = walkWave * 8.5f - (Horizontal * 4.5f);
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, strideTilt);
                sr.transform.localPosition = new Vector3(0f, Mathf.Abs(walkWave) * 0.12f, 0f);
            }
        }
        else if (!IsGrounded)
        {
            transform.localScale = new Vector3(originalScale.x * 0.88f, originalScale.y * 1.14f, originalScale.z);
            if (sr != null && sr.transform != transform)
            {
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, -Horizontal * 8.0f);
                sr.transform.localPosition = new Vector3(0f, 0.10f, 0f);
            }
        }
        else
        {
            float breathe = Mathf.Sin(Time.time * 3.5f) * 0.03f;
            transform.localScale = new Vector3(originalScale.x * (1f - breathe * 0.5f), originalScale.y * (1f + breathe), originalScale.z);
            if (sr != null && sr.transform != transform)
            {
                sr.transform.localRotation = Quaternion.Lerp(sr.transform.localRotation, Quaternion.identity, Time.deltaTime * 12f);
                sr.transform.localPosition = Vector3.Lerp(sr.transform.localPosition, Vector3.zero, Time.deltaTime * 12f);
            }
        }
    }

    void FixedUpdate()
    {
        if (IsDigging)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(Horizontal * speed, rb.linearVelocity.y);
    }

    void CheckGrounded()
    {
        bool groundedNow = false;

        if (col != null)
        {
            Bounds bounds = col.bounds;
            Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y - 0.04f);
            Vector2 boxSize = new Vector2(bounds.size.x * 0.85f, 0.22f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
            if (hits != null)
            {
                foreach (var h in hits)
                {
                    if (h != null && h != col && !h.isTrigger && !h.transform.IsChildOf(transform))
                    {
                        groundedNow = true;
                        break;
                    }
                }
            }
        }

        if (!groundedNow && groundCheck != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius);
            if (hits != null)
            {
                foreach (var h in hits)
                {
                    if (h != null && h != col && !h.isTrigger && !h.transform.IsChildOf(transform))
                    {
                        groundedNow = true;
                        break;
                    }
                }
            }
        }

        if (!wasGrounded && groundedNow)
        {
            OnLand?.Invoke();
            if (enableSquashAndStretch) StartCoroutine(SquashEffect(1.18f, 0.82f, 0.1f));
        }

        wasGrounded = IsGrounded;
        IsGrounded = groundedNow;
    }

    void ExecuteJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        OnJump?.Invoke();

        if (enableSquashAndStretch) StartCoroutine(SquashEffect(0.85f, 1.2f, 0.12f));
    }

    /// <summary>
    /// Rebote vertical al pisar o saltar sobre un enemigo.
    /// </summary>
    public void Bounce(float force = 8.5f)
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        }
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        OnJump?.Invoke();

        SpeakMotherPhrase(true);

        if (enableSquashAndStretch) StartCoroutine(SquashEffect(0.85f, 1.25f, 0.12f));
    }

    public void StartDigging(float duration, Action onComplete = null)
    {
        if (IsDigging) return;
        StartCoroutine(DigRoutine(duration, onComplete));
    }

    IEnumerator DigRoutine(float duration, Action onComplete)
    {
        IsDigging = true;
        OnDigStateChanged?.Invoke(true);

        // Feedback visual mientras excava (vibración leve / agacharse)
        Vector3 baseScale = originalScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float wobble = Mathf.Sin(elapsed * 25f) * 0.05f;
            transform.localScale = new Vector3(baseScale.x * (1f + wobble), baseScale.y * 0.85f, baseScale.z);
            yield return null;
        }

        transform.localScale = baseScale;
        IsDigging = false;
        OnDigStateChanged?.Invoke(false);

        onComplete?.Invoke();
    }

    IEnumerator SquashEffect(float scaleX, float scaleY, float dur)
    {
        Vector3 targetScale = new Vector3(originalScale.x * scaleX, originalScale.y * scaleY, originalScale.z);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t / dur);
            yield return null;
        }
        t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t / dur);
            yield return null;
        }
        transform.localScale = originalScale;
    }

    bool IsNearActiveExcavationSpot()
    {
        // Revisa si hay algún ExcavationSpot activo al alcance
        ExcavationSpot[] spots = FindObjectsByType<ExcavationSpot>();
        foreach (var s in spots)
        {
            if (s.IsPlayerInRange && !s.IsCompleted) return true;
        }
        return false;
    }

    // Frases auténticas de Madres Buscadoras que Esperanza les dice a los enemigos
    private static readonly string[] frasesMadresBuscadoras = new string[]
    {
        "¡Ayuden a buscar a mi hijo!",
        "¡Ayúdenme a buscar a mi hijo, no me detengan!",
        "¡No me cansaré de buscarte, hijo mío!",
        "¡Vivos se los llevaron, vivos los queremos!",
        "¡Ayuden a buscar a mi hijo! ¿Dónde están?",
        "¡Hasta encontrarte, corazón!",
        "¡Mi hijo tiene derecho a volver a casa!",
        "¡No están solos, sus madres los buscamos!",
        "¿Dónde están nuestros hijos? ¡Ayuden a buscarlos!",
        "¡Ni un paso atrás en la búsqueda de mi hijo!",
        "¡Por mi hijo no me rindo jamás!",
        "¡Las madres buscadoras jamás nos rendiremos!",
        "¡La tierra guarda verdades, ayuden a buscar a mi hijo!",
        "¡Seguiré buscando hasta encontrarte!"
    };

    private int currentPhraseIndex = 0;
    private float lastAutoSpeakTime = -10f;
    private GameObject speechBubbleGO;
    private SpriteRenderer speechBubbleBgSr;
    private TextMesh speechMainTm;
    private Coroutine hideBubbleCoroutine;
    private static Sprite cachedSpeechBubbleSprite;

    /// <summary>
    /// Cuando Esperanza se acerca a un enemigo en el camino, les dice directamente
    /// "¡Ayuden a buscar a mi hijo!" y frases de Madres Buscadoras.
    /// </summary>
    private void CheckNearbyEnemiesAndSpeak()
    {
        if (Time.time - lastAutoSpeakTime < 3.6f) return;

        bool enemyClose = false;
        PresidentVariantEnemy[] variants = FindObjectsByType<PresidentVariantEnemy>();
        if (variants != null)
        {
            foreach (var v in variants)
            {
                if (v != null && !v.IsDead && v.gameObject.activeInHierarchy)
                {
                    float dist = Mathf.Abs(v.transform.position.x - transform.position.x);
                    if (dist <= 6.2f)
                    {
                        enemyClose = true;
                        break;
                    }
                }
            }
        }

        if (!enemyClose)
        {
            PresidentEnemy[] bosses = FindObjectsByType<PresidentEnemy>();
            if (bosses != null)
            {
                foreach (var b in bosses)
                {
                    if (b != null && !b.IsDead && b.gameObject.activeInHierarchy)
                    {
                        float dist = Mathf.Abs(b.transform.position.x - transform.position.x);
                        if (dist <= 7.5f)
                        {
                            enemyClose = true;
                            break;
                        }
                    }
                }
            }
        }

        if (enemyClose)
        {
            lastAutoSpeakTime = Time.time;
            SpeakMotherPhrase(false);
        }
    }

    public void SpeakMotherPhrase(bool isCombatHit)
    {
        string frase = frasesMadresBuscadoras[currentPhraseIndex % frasesMadresBuscadoras.Length];
        currentPhraseIndex++;
        lastAutoSpeakTime = Time.time;

        ShowSpeechBubbleOverHead(frase);
    }

    private static string WrapBubbleText(string input, int maxCharsPerLine = 24)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxCharsPerLine) return input;
        string[] words = input.Split(' ');
        string line1 = "";
        string line2 = "";
        foreach (string w in words)
        {
            if ((line1.Length + w.Length + 1) <= maxCharsPerLine || string.IsNullOrEmpty(line1))
            {
                line1 = string.IsNullOrEmpty(line1) ? w : (line1 + " " + w);
            }
            else
            {
                line2 = string.IsNullOrEmpty(line2) ? w : (line2 + " " + w);
            }
        }
        return string.IsNullOrEmpty(line2) ? line1 : (line1 + "\n" + line2);
    }

    /// <summary>
    /// Genera un sprite de globo de conversación tipo cómic (blanco redondeado con borde oscuro y rabillo inferior).
    /// </summary>
    private static Sprite GetOrCreateSpeechBubbleSprite()
    {
        if (cachedSpeechBubbleSprite != null) return cachedSpeechBubbleSprite;

        int w = 240;
        int h = 92;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int i = 0; i < w * h; i++) tex.SetPixel(i % w, i / w, Color.clear);

        Color borderCol = new Color(0.12f, 0.10f, 0.08f, 1f);
        Color fillCol = new Color(0.99f, 0.98f, 0.94f, 0.97f);
        Color shadeCol = new Color(0.91f, 0.88f, 0.80f, 0.97f);

        int boxBottom = 18;
        int boxTop = h - 3;
        int boxLeft = 4;
        int boxRight = w - 5;
        int cornerRadius = 14;

        // 1. Cuerpo redondeado del globo de conversación
        for (int y = boxBottom; y <= boxTop; y++)
        {
            for (int x = boxLeft; x <= boxRight; x++)
            {
                int dx = 0;
                int dy = 0;
                if (x < boxLeft + cornerRadius) dx = (boxLeft + cornerRadius) - x;
                else if (x > boxRight - cornerRadius) dx = x - (boxRight - cornerRadius);

                if (y < boxBottom + cornerRadius) dy = (boxBottom + cornerRadius) - y;
                else if (y > boxTop - cornerRadius) dy = y - (boxTop - cornerRadius);

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= cornerRadius)
                {
                    bool isBorder = (dist >= cornerRadius - 2.6f) ||
                                    (x <= boxLeft + 2) || (x >= boxRight - 2) ||
                                    (y <= boxBottom + 2) || (y >= boxTop - 2);
                    if (isBorder)
                        tex.SetPixel(x, y, borderCol);
                    else if (y <= boxBottom + 6)
                        tex.SetPixel(x, y, shadeCol);
                    else
                        tex.SetPixel(x, y, fillCol);
                }
            }
        }

        // 2. Rabillo / pico del globo apuntando hacia abajo a la cabeza de Esperanza (y = 2..20)
        int tailTipX = w / 2 - 10;
        for (int y = 2; y <= boxBottom + 2; y++)
        {
            float t = (float)(y - 2) / (boxBottom - 2); // 0 en la punta, 1 en la unión con el globo
            int leftX = Mathf.RoundToInt(Mathf.Lerp(tailTipX, w / 2 - 14, t));
            int rightX = Mathf.RoundToInt(Mathf.Lerp(tailTipX, w / 2 + 6, t));

            for (int x = leftX - 2; x <= rightX + 2; x++)
            {
                if (x < leftX || x > rightX || y <= 4)
                    tex.SetPixel(x, y, borderCol);
                else
                    tex.SetPixel(x, y, fillCol);
            }
        }

        tex.Apply();
        cachedSpeechBubbleSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.0f), 52f);
        return cachedSpeechBubbleSprite;
    }

    private void ShowSpeechBubbleOverHead(string text)
    {
        if (speechBubbleGO == null)
        {
            speechBubbleGO = new GameObject("Esperanza_GloboConversacion");
            speechBubbleGO.transform.SetParent(transform, false);
            speechBubbleGO.transform.localPosition = new Vector3(0f, 2.35f, 0f);

            // Fondo del globo de conversación
            GameObject bgObj = new GameObject("BubbleBackground");
            bgObj.transform.SetParent(speechBubbleGO.transform, false);
            bgObj.transform.localPosition = Vector3.zero;
            speechBubbleBgSr = bgObj.AddComponent<SpriteRenderer>();
            speechBubbleBgSr.sprite = GetOrCreateSpeechBubbleSprite();
            speechBubbleBgSr.sortingOrder = 57;

            // Texto negro nítido dentro del globo de conversación
            GameObject mainObj = new GameObject("BubbleText");
            mainObj.transform.SetParent(speechBubbleGO.transform, false);
            mainObj.transform.localPosition = new Vector3(0f, 1.02f, -0.01f);
            speechMainTm = mainObj.AddComponent<TextMesh>();
            speechMainTm.fontSize = 36;
            speechMainTm.characterSize = 0.042f;
            speechMainTm.fontStyle = FontStyle.Bold;
            speechMainTm.alignment = TextAlignment.Center;
            speechMainTm.anchor = TextAnchor.MiddleCenter;
            speechMainTm.color = new Color(0.10f, 0.08f, 0.06f, 1f);
            MeshRenderer mmr = mainObj.GetComponent<MeshRenderer>();
            if (mmr != null) mmr.sortingOrder = 59;
        }

        speechBubbleGO.SetActive(true);
        speechBubbleGO.transform.localRotation = Quaternion.identity;
        speechBubbleGO.transform.localScale = Vector3.one;

        string wrapped = WrapBubbleText(text, 25);
        speechMainTm.text = wrapped;

        if (hideBubbleCoroutine != null) StopCoroutine(hideBubbleCoroutine);
        hideBubbleCoroutine = StartCoroutine(HideBubbleAfterDelay(2.8f));
    }

    private IEnumerator HideBubbleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (speechBubbleGO != null) speechBubbleGO.SetActive(false);
    }

    private IEnumerator ExecutePickaxeAttack()
    {
        IsAttacking = true;
        lastAttackTime = Time.time;

        if (wristMachete == null) EnsureWristMacheteSetup();
        if (wristMachete != null) wristMachete.TriggerSwingFromPlayer();

        int facing = FacingDirection;
        // Movimiento de la muñeca al lanzar el machetazo con la tecla V
        transform.localScale = new Vector3(originalScale.x * 1.18f, originalScale.y * 0.96f, originalScale.z);
        if (sr != null)
        {
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, -6f * facing);
        }

        // Sonido de ataque
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFootstep();
        }

        Vector2 hitCenter = (Vector2)transform.position + new Vector2(hitOffset.x * facing, hitOffset.y);
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, attackRange, targetLayers);
        bool hitEnemy = false;
        foreach (var c in hits)
        {
            if (c == null || c.gameObject == gameObject || c.transform.IsChildOf(transform)) continue;

            IDamageable dmg = c.GetComponent<IDamageable>() ?? c.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(pickaxeDamage);
                hitEnemy = true;
                Debug.Log($"[MadreBuscadora] ¡Machetazo a {c.name}! Daño: {pickaxeDamage}");
            }
        }

        // Al atacar o golpear un enemigo, Esperanza les dice una frase de Madre Buscadora
        SpeakMotherPhrase(hitEnemy);

        yield return new WaitForSeconds(0.12f);
        transform.localScale = originalScale;
        if (sr != null)
        {
            sr.transform.localRotation = Quaternion.identity;
        }
        yield return new WaitForSeconds(0.08f);
        IsAttacking = false;
    }

    private IEnumerator ExecutePickaxe360Spin()
    {
        IsAttacking = true;
        lastAttackTime = Time.time;

        if (wristMachete == null) EnsureWristMacheteSetup();
        if (wristMachete != null) wristMachete.TriggerSpin360FromPlayer();

        // Impulso dinámico de la muñeca y cuerpo durante el giro de 360 grados del pico
        transform.localScale = new Vector3(originalScale.x * 1.15f, originalScale.y * 1.06f, originalScale.z);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFootstep();
        }

        Vector2 hitCenter = (Vector2)transform.position + new Vector2(0f, 1.0f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, attackRange * 1.4f, targetLayers);
        bool hitEnemy = false;
        foreach (var c in hits)
        {
            if (c == null || c.gameObject == gameObject || c.transform.IsChildOf(transform)) continue;

            IDamageable dmg = c.GetComponent<IDamageable>() ?? c.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(pickaxeDamage);
                hitEnemy = true;
            }
        }

        SpeakMotherPhrase(hitEnemy);

        yield return new WaitForSeconds(0.32f);
        transform.localScale = originalScale;
        IsAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        int facing = FacingDirection;
        Vector2 hitCenter = (Vector2)transform.position + new Vector2(hitOffset.x * facing, hitOffset.y);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
        Gizmos.DrawWireSphere(hitCenter, attackRange);
    }
}
