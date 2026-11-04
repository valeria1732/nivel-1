using System.Collections;
using UnityEngine;

/// <summary>
/// Variantes de Enemigos Presidentes en las diferentes zonas:
/// - Van siempre DE FRENTE hacia la jugadora (Esperanza), encarándola directamente sin retroceder ni dar la espalda.
/// - PatrolBasic: Avanza de frente con paso firme y embiste con telegrafiado al acercarse.
/// - Pursuer: Corre de frente a gran velocidad persiguiendo a la jugadora.
/// - ZoneHeavy: Avanza de frente y lanza ondas expansivas frontales con megáfono.
/// Incluye sistema de pisada en la cabeza estilo Mario y combate con pico (tecla V).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PresidentVariantEnemy : MonoBehaviour, IDamageable
{
    public enum PresidentType
    {
        PatrolBasic,
        Pursuer,
        ZoneHeavy
    }

    [Header("Configuración de Tipo")]
    public PresidentType presidentType = PresidentType.PatrolBasic;
    public string enemyTitle = "Presidente Patrullero";

    [Header("Movimiento Firme de Frente")]
    public float patrolSpeed = 2.6f;
    public float chaseSpeed = 3.8f;
    public float patrolDistance = 5.0f;
    public float activeDistance = 24.0f; // Se activan por tramos al acercarse para no amontonarse

    [Header("Combate y Anticipación")]
    public float attackTelegraphTime = 0.42f;
    public float attackCooldown = 1.35f;
    public int maxHealth = 45;

    private int currentHealth;
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private SpriteRenderer sr;
    private Transform playerTransform;

    private bool isAttacking = false;
    private bool isPaused = false;
    private bool isDead = false;
    public bool IsDead => isDead;
    private float lastAttackTime = -99f;
    private float lastDamageGivenTime = -99f;
    private float animPhaseOffset = 0f;
    private float lastHopTime = -99f;
    private Vector3 baseLocalScale = Vector3.one;

    private GameObject alertSign;
    private static Sprite presidentSprite;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3.5f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (col != null)
        {
            col.size = new Vector2(0.9f, 1.8f);
            col.offset = new Vector2(0f, 0.9f);
        }
    }

    void Start()
    {
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (sr.sprite == null) sr.sprite = GetPresidentSprite();
        sr.sortingOrder = 4;

        currentHealth = maxHealth;
        baseLocalScale = transform.localScale;
        animPhaseOffset = Random.Range(0f, Mathf.PI * 2f);

        CreateAlertSign();
        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            var pc = Object.FindAnyObjectByType<PlayerController2D>();
            if (pc != null) player = pc.gameObject;
        }
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        if (isDead) return;

        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        if (isAttacking || isPaused) return;

        float diffX = playerTransform.position.x - transform.position.x;
        float distToPlayer = Mathf.Abs(diffX);

        // Si está dentro de la distancia activa, avanzar siempre de frente hacia Esperanza
        if (distToPlayer <= activeDistance)
        {
            float dir = Mathf.Sign(diffX);

            // IMPORTANTE: enemigo_presidenta.png mira naturalmente a la IZQUIERDA.
            // Para mirar a la izquierda (dir < 0) -> flipX = false.
            // Para mirar a la derecha (dir > 0) -> flipX = true.
            sr.flipX = (dir > 0);

            switch (presidentType)
            {
                case PresidentType.PatrolBasic:
                    HandlePatrolBasic(distToPlayer, dir);
                    break;
                case PresidentType.Pursuer:
                    HandlePursuer(distToPlayer, dir);
                    break;
                case PresidentType.ZoneHeavy:
                    HandleZoneHeavy(distToPlayer, dir);
                    break;
            }

            // Animación dinámica de zancada, trote e inclinación de combate
            AnimateDynamicWalk(true, dir);
        }
        else
        {
            // Fuera de alcance, mantener guardia con respiración viva mirando al jugador
            float dir = Mathf.Sign(diffX);
            sr.flipX = (dir > 0);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            AnimateDynamicWalk(false, dir);
        }
    }

    private void AnimateDynamicWalk(bool moving, float dir)
    {
        if (moving)
        {
            float freq = presidentType == PresidentType.Pursuer ? 16f : 12f;
            float wave = Mathf.Sin(Time.time * freq + animPhaseOffset);
            float absWave = Mathf.Abs(wave);

            // Squash & Stretch elástico de trote
            float sx = baseLocalScale.x * (1f + (0.09f * (1f - absWave)));
            float sy = baseLocalScale.y * (0.93f + (0.14f * absWave));
            transform.localScale = new Vector3(sx, sy, baseLocalScale.z);

            // Balanceo rítmico e inclinación hacia adelante al correr
            float tiltAngle = (wave * 7.5f) - (dir * 5.0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, tiltAngle);
        }
        else
        {
            float breath = Mathf.Sin(Time.time * 3.5f + animPhaseOffset) * 0.04f;
            transform.localScale = new Vector3(baseLocalScale.x * (1f - breath * 0.5f), baseLocalScale.y * (1f + breath), baseLocalScale.z);
            transform.localRotation = Quaternion.identity;
        }
    }

    private void HandlePatrolBasic(float dist, float dir)
    {
        // Avanzar de frente hacia el jugador
        rb.linearVelocity = new Vector2(dir * patrolSpeed, rb.linearVelocity.y);

        // Al acercarse a distancia de combate, embestida frontal
        if (dist <= 2.6f && Time.time - lastAttackTime > attackCooldown)
        {
            StartCoroutine(TelegraphedAttackRoutine(dir));
        }
    }

    private void HandlePursuer(float dist, float dir)
    {
        // Perseguir de frente a toda velocidad con pequeños saltos agresivos
        float vy = rb.linearVelocity.y;
        if (dist < 9.0f && dist > 3.0f && Mathf.Abs(vy) < 0.08f && Time.time - lastHopTime > 1.35f)
        {
            lastHopTime = Time.time;
            vy = 4.6f; // Salto corto de persecución
        }
        rb.linearVelocity = new Vector2(dir * chaseSpeed, vy);

        if (dist <= 2.4f && Time.time - lastAttackTime > attackCooldown)
        {
            StartCoroutine(TelegraphedAttackRoutine(dir));
        }
    }

    private void HandleZoneHeavy(float dist, float dir)
    {
        // Avanzar siempre de frente con paso firme hacia la jugadora
        rb.linearVelocity = new Vector2(dir * (patrolSpeed * 0.95f), rb.linearVelocity.y);

        // Ataque de onda burocrática frontal
        if (dist <= 5.5f && Time.time - lastAttackTime > attackCooldown)
        {
            StartCoroutine(MegaphoneShockwaveRoutine(dir));
        }
    }

    private IEnumerator TelegraphedAttackRoutine(float attackDir)
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        // Mirar de frente y agacharse tomando impulso
        sr.flipX = (attackDir > 0);
        transform.localScale = new Vector3(baseLocalScale.x * 1.22f, baseLocalScale.y * 0.78f, baseLocalScale.z);
        transform.localRotation = Quaternion.Euler(0f, 0f, attackDir * 8f);

        // Advertencia: signo '!' y parpadeo de alerta
        if (alertSign != null) alertSign.SetActive(true);
        sr.color = new Color(1f, 0.85f, 0.2f, 1f);

        yield return new WaitForSeconds(attackTelegraphTime);

        if (isDead) yield break;

        if (alertSign != null) alertSign.SetActive(false);
        sr.color = Color.white;

        // Estirarse y lanzarse en embestida rápida DE FRENTE con ligero salto
        transform.localScale = new Vector3(baseLocalScale.x * 0.88f, baseLocalScale.y * 1.18f, baseLocalScale.z);
        transform.localRotation = Quaternion.Euler(0f, 0f, -attackDir * 12f);
        rb.linearVelocity = new Vector2(attackDir * 9.2f, 2.6f);

        // Verificar impacto frontal
        if (playerTransform != null && Mathf.Abs(transform.position.x - playerTransform.position.x) < 2.0f)
        {
            CheckAndDamagePlayer(playerTransform.gameObject, attackDir);
        }

        yield return new WaitForSeconds(0.32f);

        if (!isDead)
        {
            transform.localScale = baseLocalScale;
            transform.localRotation = Quaternion.identity;
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        isAttacking = false;
    }

    private IEnumerator MegaphoneShockwaveRoutine(float shockwaveDir)
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        lastAttackTime = Time.time;

        sr.flipX = (shockwaveDir > 0);

        if (alertSign != null) alertSign.SetActive(true);
        sr.color = new Color(0.95f, 0.35f, 0.35f, 1f);

        yield return new WaitForSeconds(attackTelegraphTime);

        if (isDead) yield break;

        if (alertSign != null) alertSign.SetActive(false);
        sr.color = Color.white;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyAlert();
        }

        // Onda expansiva frontal hacia Esperanza
        if (playerTransform != null && Mathf.Abs(transform.position.x - playerTransform.position.x) < 5.5f)
        {
            CheckAndDamagePlayer(playerTransform.gameObject, shockwaveDir, 7.0f, 4.2f);
            ScoreManager.AddScore(0, playerTransform.position + Vector3.up * 1.5f, "¡Onda burocrática!");
        }

        yield return new WaitForSeconds(0.40f);
        isAttacking = false;
    }

    // ====================================================
    //  COLISIONES, PISADA MARIO Y DAÑO
    // ====================================================
    void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlayerContact(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandlePlayerContact(other.gameObject);
    }

    private void HandlePlayerContact(GameObject target)
    {
        if (isDead) return;

        bool isPlayer = target.CompareTag("Player") ||
                        target.GetComponent<PlayerController2D>() != null ||
                        target.GetComponentInParent<PlayerController2D>() != null;

        if (!isPlayer) return;

        PlayerController2D playerCtrl = target.GetComponent<PlayerController2D>() ?? target.GetComponentInParent<PlayerController2D>();
        Rigidbody2D playerRb = target.GetComponent<Rigidbody2D>() ?? target.GetComponentInParent<Rigidbody2D>();

        if (playerCtrl == null || playerRb == null) return;

        // Pisada sobre la cabeza estilo Mario
        float playerY = playerCtrl.transform.position.y;
        float enemyTopY = transform.position.y + 0.6f;
        bool cameFromAbove = (playerY > enemyTopY) && (playerRb.linearVelocity.y <= 0.25f);

        if (cameFromAbove)
        {
            playerCtrl.Bounce(8.5f);
            TakeDamage(30);

            ScoreManager.AddScore(200, transform.position + Vector3.up * 1.4f, "+200 PTS ¡PISADA!");
            if (AudioManager.Instance != null) AudioManager.Instance.PlayFootstep();
        }
        else
        {
            // Daño por contacto frontal / lateral
            float pushDir = Mathf.Sign(target.transform.position.x - transform.position.x);
            CheckAndDamagePlayer(target, pushDir);
        }
    }

    private void CheckAndDamagePlayer(GameObject playerGO, float pushDir, float forceX = 5.2f, float forceY = 3.2f)
    {
        if (Time.time - lastDamageGivenTime < 0.75f) return;
        lastDamageGivenTime = Time.time;

        HealthSystem hs = playerGO.GetComponent<HealthSystem>() ?? playerGO.GetComponentInParent<HealthSystem>();
        if (hs != null)
        {
            hs.TakeDamage(1);
        }

        Rigidbody2D pRb = playerGO.GetComponent<Rigidbody2D>() ?? playerGO.GetComponentInParent<Rigidbody2D>();
        if (pRb != null)
        {
            if (Mathf.Abs(pushDir) < 0.1f) pushDir = -1f;
            pRb.linearVelocity = new Vector2(pushDir * forceX, forceY);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        StartCoroutine(DamageFlashRoutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        ScoreManager.AddScore(500, transform.position + Vector3.up * 1.5f, "+500 PTS ¡SUPERADO!");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDamage();
        }

        Level1Manager.NotifyEnemyDefeated(gameObject);
        StartCoroutine(DefeatShrinkRoutine());
    }

    private IEnumerator DefeatShrinkRoutine()
    {
        float dur = 0.22f;
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

    private IEnumerator DamageFlashRoutine()
    {
        if (sr != null) sr.color = new Color(1f, 0.25f, 0.25f, 1f);
        yield return new WaitForSeconds(0.12f);
        if (sr != null && !isDead) sr.color = Color.white;
    }

    private void CreateAlertSign()
    {
        alertSign = new GameObject("AlertExclamation");
        alertSign.transform.SetParent(transform, false);
        alertSign.transform.localPosition = new Vector3(0, 1.9f, 0);

        TextMesh tm = alertSign.AddComponent<TextMesh>();
        tm.text = "!";
        tm.fontSize = 32;
        tm.characterSize = 0.12f;
        tm.fontStyle = FontStyle.Bold;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = new Color(1f, 0.2f, 0.2f, 1f);

        MeshRenderer mr = alertSign.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 20;

        alertSign.SetActive(false);
    }

    private static Sprite GetPresidentSprite()
    {
        if (presidentSprite != null) return presidentSprite;

#if UNITY_EDITOR
        presidentSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Personajes/enemigo_presidenta.png");
        if (presidentSprite == null)
            presidentSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Personajes/enemigo_amlo_clean.png");
#endif
        if (presidentSprite == null)
        {
            presidentSprite = Resources.Load<Sprite>("enemigo_presidenta");
        }
        return presidentSprite;
    }
}
