using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Enemigo Presidente (Jefe con Animaciones AMLO completas):
/// - Patrullaje y alerta con gestos característicos (Patrol & Aggro).
/// - Invocación y lanzamiento del Ganso Volador ("¡Me canso ganso!").
/// - Ataque cuerpo a cuerpo con Bate de Béisbol ("¡Macaneo!").
/// - Ráfagas de proyectiles con frases ("¡Otros datos!").
/// - Reacción de impacto (Hurt) y Derrota con estrellas y cartel "KO".
/// - Recompensas de vida y puntos por esquivar o saltar.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PresidentEnemy : MonoBehaviour, IDamageable
{
    [Header("Posicionamiento y Movimiento de Frente")]
    public float idealCombatDistance = 3.2f;
    public float meleeAttackDistance = 2.6f;
    public float baseMoveSpeed = 2.6f;
    public float enragedMoveSpeed = 3.6f;
    public float detectionRange = 26f;

    public float moveSpeed => isEnraged ? enragedMoveSpeed : baseMoveSpeed;

    [Header("Configuración de Ataque")]
    public float normalAttackCooldown = 1.50f;
    public float enragedAttackCooldown = 0.95f;
    public int maxHealth = 100;
    public float stunOnHitDuration = 0.28f;

    [Header("Referencias")]
    public Transform targetPlayer;
    public SpriteRenderer spriteRenderer;

    // --- Sprites animados AMLO ---
    private static Sprite[] patrolSprites;
    private static Sprite[] gooseSprites;
    private static Sprite[] batSprites;
    private static Sprite[] hurtDefeatSprites;
    private static bool spritesLoaded = false;

    // --- Frases por ráfaga ---
    private readonly string[][] phrasePresets = new string[][]
    {
        new string[] { "¡En México...", "...no hay...", "...desaparecidos!" },
        new string[] { "¡Aquí todo...", "...está en...", "...calma!" },
        new string[] { "¡Son solo...", "...otros...", "...datos!" },
        new string[] { "¡Abrazos...", "...y no...", "...balazos!" }
    };
    private int currentPhraseIndex = 0;

    // --- Estados internos ---
    private Rigidbody2D rb;
    private Collider2D col;
    private int currentHealth;
    private bool isDead = false;
    public bool IsDead => isDead;
    private bool isStunned = false;
    private bool isAttacking = false;
    private bool isRestarting = false;
    private bool isEnraged = false;
    private Color originalColor = Color.white;
    private float walkAnimTimer = 0f;
    private int walkFrame = 0;

    // --- Barra de vida flotante ---
    private GameObject hpBarRoot;
    private SpriteRenderer hpBarFill;
    private TextMesh hpText;

    /// <summary>
    /// Carga todos los sprites de animación de AMLO desde disco o AssetDatabase.
    /// </summary>
    public static void LoadAMLOSprites()
    {
        if (spritesLoaded && patrolSprites != null && patrolSprites.Length >= 4) return;

        patrolSprites = LoadSpriteArray("patrol", 4);
        gooseSprites = LoadSpriteArray("goose", 6);
        batSprites = LoadSpriteArray("bat", 6);
        hurtDefeatSprites = LoadSpriteArray("hurt_defeat", 5);

        spritesLoaded = true;
    }

    private static Sprite[] LoadSpriteArray(string prefix, int count)
    {
        Sprite[] arr = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            string assetPath = $"Assets/Sprites/Personajes/AMLO/{prefix}_{i}.png";
#if UNITY_EDITOR
            arr[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#endif
            if (arr[i] == null)
            {
                arr[i] = Resources.Load<Sprite>($"{prefix}_{i}");
            }
            if (arr[i] == null)
            {
                string fullPath = Path.Combine(Application.dataPath, $"Sprites/Personajes/AMLO/{prefix}_{i}.png");
                if (File.Exists(fullPath))
                {
                    byte[] bytes = File.ReadAllBytes(fullPath);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(bytes))
                    {
                        arr[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    }
                }
            }
        }
        return arr;
    }

    public static Sprite GetDefaultSprite()
    {
        LoadAMLOSprites();
        if (patrolSprites != null && patrolSprites.Length > 0 && patrolSprites[0] != null)
            return patrolSprites[0];

        return Resources.Load<Sprite>("enemigo_presidenta");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureSpawnRuntime()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            var pc = Object.FindAnyObjectByType<PlayerController2D>();
            if (pc != null) player = pc.gameObject;
        }
        if (player == null) return;

        // Si no existe en la escena, spawnear en la zona final del nivel (x ~ 68)
        if (Object.FindAnyObjectByType<PresidentEnemy>() == null)
        {
            SpawnAt(new Vector3(68f, -2.15f, 0f), player.transform);
        }
    }

    public static GameObject SpawnAt(Vector3 pos, Transform player = null, string customName = "Enemigo_Presidente")
    {
        GameObject existing = GameObject.Find(customName);
        if (existing != null) return existing;

        GameObject go = new GameObject(customName);
        go.tag = "Enemy";
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1.18f, 1.18f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        LoadAMLOSprites();
        sr.sprite = GetDefaultSprite();
        sr.sortingOrder = 15;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3.5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D col = go.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.85f, 1.45f);
        col.offset = new Vector2(0f, 0.05f);

        PresidentEnemy pe = go.AddComponent<PresidentEnemy>();
        pe.baseMoveSpeed = 2.6f;
        pe.enragedMoveSpeed = 3.6f;
        pe.detectionRange = 26f;
        pe.normalAttackCooldown = 1.50f;
        pe.enragedAttackCooldown = 0.95f;
        pe.maxHealth = 100;
        pe.spriteRenderer = sr;
        pe.targetPlayer = player;

        return go;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        LoadAMLOSprites();

        if (spriteRenderer != null)
        {
            if (spriteRenderer.sprite == null)
                spriteRenderer.sprite = GetDefaultSprite();

            spriteRenderer.sortingOrder = 15;
            originalColor = spriteRenderer.color;
        }

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3.5f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        LoadAMLOSprites();

        if (targetPlayer == null) FindPlayer();

        CreateFloatingHealthBar();
        StartCoroutine(AttackLoopRoutine());
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null)
        {
            var pc = Object.FindAnyObjectByType<PlayerController2D>();
            if (pc != null) p = pc.gameObject;
        }
        if (p != null) targetPlayer = p.transform;
    }

    void Update()
    {
        if (isRestarting || isDead) return;

        if (targetPlayer == null)
        {
            FindPlayer();
            return;
        }

        // Modo Furia (< 50% HP)
        bool wasEnraged = isEnraged;
        isEnraged = (currentHealth <= (maxHealth * 0.5f));
        if (!wasEnraged && isEnraged)
        {
            TriggerEnrageMode();
        }

        // Efecto visual en furia
        if (isEnraged && spriteRenderer != null && !isStunned && !isAttacking)
        {
            float pulse = Mathf.PingPong(Time.time * 5f, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, new Color(1f, 0.5f, 0.5f, 1f), pulse * 0.5f);
        }

        // Animación de caminata dinámica con balanceo y rebote
        if (!isAttacking && !isStunned && rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.15f)
        {
            walkAnimTimer += Time.deltaTime * (isEnraged ? 8f : 5.5f);
            if (walkAnimTimer >= 1f)
            {
                walkAnimTimer = 0f;
                walkFrame = (walkFrame + 1) % 2;
                if (patrolSprites != null && patrolSprites.Length > 1 && patrolSprites[walkFrame] != null)
                {
                    spriteRenderer.sprite = patrolSprites[walkFrame];
                }
            }

            float tilt = Mathf.Sin(Time.time * (isEnraged ? 15f : 11f)) * (isEnraged ? 6.5f : 4.5f);
            transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
        }
        else if (!isAttacking && !isStunned && patrolSprites != null && patrolSprites.Length > 0 && patrolSprites[0] != null)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * 10f);
            // Si está cerca y mirando, usar gesto de señalar
            float dist = Mathf.Abs(targetPlayer.position.x - transform.position.x);
            if (dist < 8f && patrolSprites.Length > 2 && patrolSprites[2] != null)
            {
                spriteRenderer.sprite = isEnraged && patrolSprites.Length > 3 ? patrolSprites[3] : patrolSprites[2];
            }
            else
            {
                spriteRenderer.sprite = patrolSprites[0];
            }
        }

        // Barra de vida
        if (hpBarRoot != null)
        {
            hpBarRoot.transform.position = transform.position + new Vector3(0, 1.45f, 0);
            hpBarRoot.transform.rotation = Quaternion.identity;
        }
    }

    void TriggerEnrageMode()
    {
        ScoreManager.SpawnFloatingPopup(transform.position + new Vector3(0, 1.8f, 0), "¡MODO FURIA!", Color.red);
        if (hpText != null)
        {
            hpText.text = "Presidente (¡Furia!)";
            hpText.color = Color.red;
        }
    }

    void FixedUpdate()
    {
        if (isRestarting || isDead || isStunned || isAttacking)
        {
            if (rb != null && !isRestarting)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            return;
        }

        if (targetPlayer == null) return;

        float diffX = targetPlayer.position.x - transform.position.x;
        float distance = Mathf.Abs(diffX);

        if (detectionRange > 0 && distance > detectionRange)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Orientación y avance siempre de frente hacia la jugadora
        float dirToPlayer = Mathf.Sign(diffX);
        if (spriteRenderer != null)
        {
            // El sprite original de AMLO mira hacia la derecha (+X)
            spriteRenderer.flipX = (dirToPlayer < 0);
        }

        float speed = moveSpeed;
        if (distance > 0.75f)
        {
            rb.linearVelocity = new Vector2(dirToPlayer * speed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    // ====================================================
    //  CICLO DE COMBATE Y ATAQUES
    // ====================================================
    private IEnumerator AttackLoopRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        while (!isDead && !isRestarting)
        {
            float currentCooldown = isEnraged ? enragedAttackCooldown : normalAttackCooldown;
            yield return new WaitForSeconds(currentCooldown);

            if (isDead || isRestarting || isStunned) continue;

            if (targetPlayer == null) FindPlayer();
            if (targetPlayer == null) continue;

            float distance = Mathf.Abs(targetPlayer.position.x - transform.position.x);
            if (distance > detectionRange) continue;

            // Si está muy cerca, ataque con bate de béisbol
            if (distance <= meleeAttackDistance)
            {
                yield return StartCoroutine(MeleeBatAttackRoutine());
            }
            // Si está a distancia media/larga, prioridad alta a lanzar ráfagas de gansos voladores
            else
            {
                int attackChoice = Random.Range(0, 4);
                if (attackChoice < 3) // 75% probabilidad de ráfaga de gansos
                {
                    yield return StartCoroutine(GooseThrowRoutine());
                }
                else
                {
                    yield return StartCoroutine(LaunchPhraseSequence());
                }
            }
        }
    }

    // --- 1. ATAQUE CON RÁFAGA DE GANSOS VOLADORES (TRIPLE DIFICULTAD) ---
    private IEnumerator GooseThrowRoutine()
    {
        isAttacking = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        float facingDir = targetPlayer != null && targetPlayer.position.x < transform.position.x ? -1f : 1f;
        if (spriteRenderer != null) spriteRenderer.flipX = (facingDir < 0);

        // Animar la secuencia de sacar el ganso y prepararlo (goose_0 a goose_5)
        if (gooseSprites != null && gooseSprites.Length >= 6)
        {
            float frameDelay = isEnraged ? 0.05f : 0.07f;
            for (int i = 0; i < 5; i++)
            {
                if (isDead || isStunned) yield break;
                if (gooseSprites[i] != null) spriteRenderer.sprite = gooseSprites[i];
                yield return new WaitForSeconds(frameDelay);
            }

            if (gooseSprites[5] != null) spriteRenderer.sprite = gooseSprites[5];
        }

        ScoreManager.SpawnFloatingPopup(transform.position + Vector3.up * 1.5f, "¡ME CANSO GANSO!", new Color(1f, 0.85f, 0.2f, 1f));

        // Lanzar Gansos Voladores (2 normal, 3 en modo furia)
        int gooseCount = isEnraged ? 3 : 2;
        float[] heights = new float[] { 0.05f, 0.45f, -0.15f, 0.30f };
        for (int g = 0; g < gooseCount; g++)
        {
            if (isDead || isStunned) yield break;

            facingDir = targetPlayer != null && targetPlayer.position.x < transform.position.x ? -1f : 1f;
            if (spriteRenderer != null) spriteRenderer.flipX = (facingDir < 0);

            Vector3 spawnPos = transform.position + new Vector3(facingDir * 0.9f, heights[g % heights.Length], 0f);
            Vector2 projDir = new Vector2(facingDir, 0f);
            float gooseSpeed = (isEnraged ? 5.8f : 4.8f) + (g * 0.35f);
            PhraseProjectile.SpawnGoose(spawnPos, projDir, gooseSpeed);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDamage();
            }

            yield return new WaitForSeconds(isEnraged ? 0.22f : 0.28f);
        }

        yield return new WaitForSeconds(0.20f);
        isAttacking = false;
    }

    // --- 2. ATAQUE CUERPO A CUERPO CON BATE ("MACANEO") ---
    private IEnumerator MeleeBatAttackRoutine()
    {
        isAttacking = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        float facingDir = targetPlayer != null && targetPlayer.position.x < transform.position.x ? -1f : 1f;
        if (spriteRenderer != null) spriteRenderer.flipX = (facingDir < 0);

        // Anticipación de bateo (bat_0 a bat_3)
        if (batSprites != null && batSprites.Length >= 6)
        {
            float frameDelay = isEnraged ? 0.07f : 0.10f;
            for (int i = 0; i <= 3; i++)
            {
                if (isDead || isStunned) yield break;
                if (batSprites[i] != null) spriteRenderer.sprite = batSprites[i];
                yield return new WaitForSeconds(frameDelay);
            }

            // Swing del bate (bat_4 y bat_5)
            if (batSprites[4] != null) spriteRenderer.sprite = batSprites[4];

            // Impacto al jugador si aún está dentro del rango
            if (targetPlayer != null)
            {
                float curDist = Mathf.Abs(targetPlayer.position.x - transform.position.x);
                if (curDist <= meleeAttackDistance + 0.4f)
                {
                    HealthSystem hs = targetPlayer.GetComponent<HealthSystem>();
                    if (hs != null) hs.TakeDamage(20);

                    Rigidbody2D prb = targetPlayer.GetComponent<Rigidbody2D>();
                    if (prb != null)
                    {
                        prb.linearVelocity = new Vector2(facingDir * 8f, 5f);
                    }

                    ScoreManager.AddScore(0, targetPlayer.position + Vector3.up, "-20 HP ¡MACANEO!");
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayDamage();
                }
            }

            yield return new WaitForSeconds(frameDelay);
            if (batSprites.Length > 5 && batSprites[5] != null) spriteRenderer.sprite = batSprites[5];
        }

        yield return new WaitForSeconds(0.35f);
        isAttacking = false;
    }

    // --- 3. ATAQUE CON FRASES ---
    private IEnumerator LaunchPhraseSequence()
    {
        isAttacking = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        float facingDir = targetPlayer != null && targetPlayer.position.x < transform.position.x ? -1f : 1f;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (facingDir < 0);
            if (patrolSprites != null && patrolSprites.Length > 3 && patrolSprites[3] != null)
                spriteRenderer.sprite = patrolSprites[3]; // Gesto de gritar
        }

        string[] parts = phrasePresets[currentPhraseIndex];
        currentPhraseIndex = (currentPhraseIndex + 1) % phrasePresets.Length;

        float curPhraseSpeed = isEnraged ? 5.2f : 4.0f;
        float curDelay = isEnraged ? 0.60f : 0.85f;

        for (int i = 0; i < parts.Length; i++)
        {
            if (isDead || isRestarting) yield break;

            string partText = parts[i];
            Vector3 spawnPos = transform.position + new Vector3(facingDir * 0.85f, -0.3f, 0f);
            Vector2 projDir = new Vector2(facingDir, 0f);

            PhraseProjectile.Spawn(spawnPos, projDir, partText, curPhraseSpeed);

            yield return new WaitForSeconds(curDelay);
        }

        yield return new WaitForSeconds(isEnraged ? 0.6f : 1.1f);
        isAttacking = false;
    }

    // ====================================================
    //  STOMP (PISADA) Y DAÑO
    // ====================================================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlayerContact(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandlePlayerContact(other.gameObject);
    }

    private void HandlePlayerContact(GameObject target)
    {
        if (isDead || isRestarting) return;

        bool isPlayer = target.CompareTag("Player") ||
                        target.GetComponent<PlayerController2D>() != null ||
                        target.GetComponentInParent<PlayerController2D>() != null;

        if (!isPlayer) return;

        PlayerController2D playerCtrl = target.GetComponent<PlayerController2D>() ?? target.GetComponentInParent<PlayerController2D>();
        Rigidbody2D playerRb = target.GetComponent<Rigidbody2D>() ?? target.GetComponentInParent<Rigidbody2D>();

        if (playerCtrl == null || playerRb == null) return;

        // Pisada sobre la cabeza estilo Mario
        float playerY = playerCtrl.transform.position.y;
        float presTopY = transform.position.y + 0.55f;
        bool cameFromAbove = (playerY > presTopY) && (playerRb.linearVelocity.y <= 0.25f);

        if (cameFromAbove)
        {
            playerCtrl.Bounce(9.2f);
            TakeDamage(35);

            HealthSystem hs = target.GetComponent<HealthSystem>();
            if (hs != null) hs.Heal(15);

            ScoreManager.AddScore(200, transform.position + new Vector3(0, 1.4f, 0), "+200 PTS ¡PISADA! +15 HP");
        }
        else
        {
            // Daño por contacto si se toca de lado
            HealthSystem hs = target.GetComponent<HealthSystem>();
            if (hs != null) hs.TakeDamage(10);

            float pushDir = Mathf.Sign(target.transform.position.x - transform.position.x);
            playerRb.linearVelocity = new Vector2(pushDir * 5f, 3.5f);
        }
    }

    // ====================================================
    //  RECIBIR DAÑO Y DERROTA
    // ====================================================
    public void TakeDamage(int damage)
    {
        if (isDead || isRestarting) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        UpdateHealthBarUI();
        StartCoroutine(HurtRoutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator HurtRoutine()
    {
        isStunned = true;

        // Sprite de impacto (hurt_defeat_0)
        if (hurtDefeatSprites != null && hurtDefeatSprites.Length > 0 && hurtDefeatSprites[0] != null)
        {
            spriteRenderer.sprite = hurtDefeatSprites[0];
        }

        if (spriteRenderer != null) spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(0.18f);

        // Sprite mareado con estrellas (hurt_defeat_1)
        if (hurtDefeatSprites != null && hurtDefeatSprites.Length > 1 && hurtDefeatSprites[1] != null)
        {
            spriteRenderer.sprite = hurtDefeatSprites[1];
        }

        yield return new WaitForSeconds(stunOnHitDuration);

        if (spriteRenderer != null && !isDead) spriteRenderer.color = originalColor;
        isStunned = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[PresidentEnemy] ¡El Presidente ha sido derrotado y desaparece!");
        ScoreManager.AddScore(1000, transform.position + new Vector3(0, 1.5f, 0), "+1000 PTS ¡DERROTADO!");

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (col != null)
        {
            col.enabled = false;
        }

        if (hpBarRoot != null)
        {
            Destroy(hpBarRoot);
        }

        Level1Manager.NotifyEnemyDefeated(gameObject);
        StartCoroutine(DefeatRoutine());
    }

    private IEnumerator DefeatRoutine()
    {
        float dur = 0.20f;
        float elapsed = 0f;
        Vector3 initialScale = transform.localScale;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    // ====================================================
    //  BARRA DE VIDA FLOTANTE
    // ====================================================
    private void CreateFloatingHealthBar()
    {
        hpBarRoot = new GameObject("President_HP_Bar");
        hpBarRoot.transform.position = transform.position + new Vector3(0, 1.55f, 0);

        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(hpBarRoot.transform, false);
        SpriteRenderer bgSr = bgGO.AddComponent<SpriteRenderer>();
        bgSr.sprite = PhraseProjectile.GetOrCreateBannerSprite();
        bgSr.sortingOrder = 28;
        bgSr.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        bgGO.transform.localScale = new Vector3(1.6f, 0.28f, 1f);

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(hpBarRoot.transform, false);
        hpBarFill = fillGO.AddComponent<SpriteRenderer>();
        hpBarFill.sprite = PhraseProjectile.GetOrCreateBannerSprite();
        hpBarFill.sortingOrder = 29;
        hpBarFill.color = new Color(0.95f, 0.2f, 0.25f, 1f);
        fillGO.transform.localScale = new Vector3(1.52f, 0.20f, 1f);

        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(hpBarRoot.transform, false);
        textGO.transform.localPosition = new Vector3(0, 0.24f, 0);
        hpText = textGO.AddComponent<TextMesh>();
        hpText.text = "Presidente";
        hpText.fontSize = 28;
        hpText.characterSize = 0.038f;
        hpText.alignment = TextAlignment.Center;
        hpText.anchor = TextAnchor.MiddleCenter;
        hpText.color = new Color(1f, 0.9f, 0.5f, 1f);
        MeshRenderer tmr = textGO.GetComponent<MeshRenderer>();
        if (tmr != null) tmr.sortingOrder = 30;
    }

    private void UpdateHealthBarUI()
    {
        if (hpBarFill != null)
        {
            float pct = Mathf.Clamp01((float)currentHealth / maxHealth);
            hpBarFill.transform.localScale = new Vector3(1.52f * pct, 0.20f, 1f);

            if (pct < 0.35f) hpBarFill.color = Color.red;
            else if (pct < 0.65f) hpBarFill.color = new Color(1f, 0.6f, 0.1f, 1f);
        }
    }
}
