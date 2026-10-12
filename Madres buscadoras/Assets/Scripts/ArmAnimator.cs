using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Controla el ÚNICO par de brazos de la muñeca (Esperanza).
/// Asegura que la muñeca NUNCA tenga más de un par de brazos,
/// y que este par realice TODOS los movimientos de forma fluida y orgánica:
/// - Reposo (Idle): respiración suave y postura natural con el pico.
/// - Caminata (Walk): oscilación pendular en contrafase al ritmo del paso.
/// - Salto y Caída (Jump / Fall): brazos dinámicos que se alzan en el aire.
/// - Picazo (Tecla V): golpe completo con anticipación (carga hacia arriba/atrás),
///   bajada contundente con impacto y daño a enemigos, y recuperación suave.
/// - Excavación (Tecla F): picado rítmico continuo hacia el suelo.
/// </summary>
public class ArmAnimator : MonoBehaviour
{
    [Header("Referencias del Rig")]
    public Transform armsRig;
    public Transform leftArm;          // Brazo izquierdo (trasero)
    public Transform rightArm;         // Brazo derecho (delantero)
    public Transform pickaxeTransform;    // Pico (hijo del brazo derecho)

    [Header("Referencia al Jugador")]
    public PlayerController2D player;

    [Header("Sprites Opcionales (Inspector)")]
    public Sprite customArmSprite;
    public Sprite customPickaxeSprite;

    [Header("Posicionamiento de Hombros")]
    [Tooltip("Offset del hombro en espacio local de Esperanza")]
    public Vector2 shoulderOffset = new Vector2(0.05f, 0.38f);
    [Tooltip("Escala base del rig para compensar la escala no uniforme del cuerpo (0.528, 1.8)")]
    public Vector2 baseRigScale = new Vector2(1.89394f, 0.55556f);
    public Vector3 armScale = new Vector3(0.85f, 0.85f, 1f);
    public Vector3 pickaxeScale = new Vector3(0.52f, 0.52f, 1f);

    [Header("Parámetros de Caminata y Reposo")]
    public float idleSpeed = 2.0f;
    public float idleAngle = 5f;
    public float walkSpeed = 9.0f;
    public float swingAngle = 28f;

    [Header("Salto y En el Aire")]
    public float jumpArmAngle = -42f;  // Brazos levantados en salto
    public float fallArmAngle = -15f;  // Brazos abiertos en caída

    [Header("Excavación")]
    public float digSpeed = 16f;
    public float digAngle = 48f;

    [Header("Picazo y Combate (Tecla V)")]
    public KeyCode attackKey = KeyCode.V;
    public float attackCooldown = 0.30f;
    public int pickaxeDamage = 25;
    public float attackRange = 1.3f;
    public Vector2 hitOffset = new Vector2(0.8f, -0.2f);
    public LayerMask targetLayers = ~0;

    // Tiempos de animación del picazo
    public float raiseDuration = 0.10f;
    public float strikeDuration = 0.08f;
    public float recoveryDuration = 0.15f;

    // Ángulos del picazo
    public float raiseAngle = -85f;    // Arriba/atrás cargando el golpe
    public float strikeAngle = 60f;    // Abajo/adelante golpeando el suelo

    // Estados internos
    bool isAttacking = false;
    float lastAttackTime = -999f;
    bool hasHitThisSwing = false;

    float phase = 0f;
    float digPhase = 0f;

    float currentRightAngle = 0f;
    float currentLeftAngle = 0f;

    SpriteRenderer leftArmSR;
    SpriteRenderer rightArmSR;
    SpriteRenderer pickaxeSR;

    void Awake()
    {
        if (player == null)
            player = GetComponent<PlayerController2D>();

        if (player == null)
            player = FindAnyObjectByType<PlayerController2D>();

        EnsureRig();
    }

    /// <summary>
    /// Construye y conecta el rig de brazos único si no está en la escena.
    /// </summary>
    public void EnsureRig()
    {
        // 1. Buscar ArmsRig como hijo
        if (armsRig == null)
        {
            Transform existingRig = transform.Find("ArmsRig");
            if (existingRig != null)
            {
                armsRig = existingRig;
            }
            else
            {
                GameObject rigGO = new GameObject("ArmsRig");
                rigGO.transform.SetParent(transform, false);
                rigGO.transform.localPosition = new Vector3(shoulderOffset.x, shoulderOffset.y, -0.01f);
                rigGO.transform.localScale = new Vector3(baseRigScale.x, baseRigScale.y, 1f);
                armsRig = rigGO.transform;
            }
        }

        // Cargar sprites
        Sprite armSprite = customArmSprite;
        if (armSprite == null) armSprite = Resources.Load<Sprite>("prop_brazo");
        if (armSprite == null)
        {
#if UNITY_EDITOR
            armSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Props/prop_brazo.png");
#endif
        }

        Sprite picoSprite = customPickaxeSprite;
        if (picoSprite == null) picoSprite = Resources.Load<Sprite>("prop_pico");
        if (picoSprite == null)
        {
#if UNITY_EDITOR
            picoSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Props/prop_pico.png");
#endif
        }

        Material spriteMat = null;
        if (player != null)
        {
            SpriteRenderer bodySR = player.GetComponentInChildren<SpriteRenderer>();
            if (bodySR != null) spriteMat = bodySR.sharedMaterial;
        }

        // 2. Brazo Izquierdo (Trasero)
        if (leftArm == null && armsRig != null)
        {
            Transform existingLeft = armsRig.Find("Brazo_Izquierdo");
            if (existingLeft != null)
            {
                leftArm = existingLeft;
            }
            else
            {
                GameObject leftGO = new GameObject("Brazo_Izquierdo");
                leftGO.transform.SetParent(armsRig, false);
                leftGO.transform.localPosition = new Vector3(-0.08f, 0f, 0.04f);
                leftGO.transform.localScale = armScale;
                leftArm = leftGO.transform;

                leftArmSR = leftGO.AddComponent<SpriteRenderer>();
                leftArmSR.sprite = armSprite;
                leftArmSR.sortingOrder = -1; // Detrás del torso
                leftArmSR.color = new Color(0.85f, 0.85f, 0.90f, 1f);
                if (spriteMat != null) leftArmSR.sharedMaterial = spriteMat;
            }
        }

        if (leftArmSR == null && leftArm != null)
            leftArmSR = leftArm.GetComponent<SpriteRenderer>();

        // 3. Brazo Derecho (Delantero)
        if (rightArm == null && armsRig != null)
        {
            Transform existingRight = armsRig.Find("Brazo_Derecho");
            if (existingRight != null)
            {
                rightArm = existingRight;
            }
            else
            {
                GameObject rightGO = new GameObject("Brazo_Derecho");
                rightGO.transform.SetParent(armsRig, false);
                rightGO.transform.localPosition = new Vector3(0.08f, 0f, -0.04f);
                rightGO.transform.localScale = armScale;
                rightArm = rightGO.transform;

                rightArmSR = rightGO.AddComponent<SpriteRenderer>();
                rightArmSR.sprite = armSprite;
                rightArmSR.sortingOrder = 1; // Delante del torso
                rightArmSR.color = Color.white;
                if (spriteMat != null) rightArmSR.sharedMaterial = spriteMat;
            }
        }

        if (rightArmSR == null && rightArm != null)
            rightArmSR = rightArm.GetComponent<SpriteRenderer>();

        // 4. Pico (Hijo del brazo derecho)
        if (pickaxeTransform == null && rightArm != null)
        {
            Transform existingPico = rightArm.Find("Pico");
            if (existingPico != null)
            {
                pickaxeTransform = existingPico;
            }
            else
            {
                GameObject picoGO = new GameObject("Pico");
                picoGO.transform.SetParent(rightArm, false);
                picoGO.transform.localPosition = new Vector3(0.05f, -0.72f, -0.01f);
                picoGO.transform.localScale = pickaxeScale;
                picoGO.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
                pickaxeTransform = picoGO.transform;

                pickaxeSR = picoGO.AddComponent<SpriteRenderer>();
                pickaxeSR.sprite = picoSprite;
                pickaxeSR.sortingOrder = 2; // Delante de la mano
                if (spriteMat != null) pickaxeSR.sharedMaterial = spriteMat;
            }
        }

        if (pickaxeSR == null && pickaxeTransform != null)
            pickaxeSR = pickaxeTransform.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController2D>();
            if (player == null) return;
        }

        // Si está en diálogo, no atacar
        if (DialogueManager.IsTalking) return;

        // --- ENTRADA: TECLA V PARA PICAR ---
        if (Input.GetKeyDown(attackKey) && !isAttacking && (Time.time - lastAttackTime >= attackCooldown))
        {
            TriggerPickaxeSwing();
        }

        // Calcular los ángulos objetivo según el estado de la muñeca
        CalculateArmAngles();
    }

    void LateUpdate()
    {
        if (player == null) return;

        int facing = player.FacingDirection;

        // Orientar el rig en el hombro
        if (armsRig != null)
        {
            armsRig.localPosition = new Vector3(shoulderOffset.x * facing, shoulderOffset.y, -0.01f);
            armsRig.localScale = new Vector3(baseRigScale.x * facing, baseRigScale.y, 1f);
        }

        // Aplicar rotaciones
        if (rightArm != null)
            rightArm.localEulerAngles = new Vector3(0f, 0f, currentRightAngle);

        if (leftArm != null)
            leftArm.localEulerAngles = new Vector3(0f, 0f, currentLeftAngle);
    }

    void CalculateArmAngles()
    {
        // 1. Estado: Ataque con pico (Tecla V)
        if (isAttacking)
        {
            return;
        }

        bool isDigging  = player != null && player.IsDigging;
        bool isWalking  = player != null && Mathf.Abs(player.Horizontal) > 0.05f;
        bool isGrounded = player == null || player.IsGrounded;
        bool isJumping  = player != null && player.IsJumping;
        bool isFalling  = player != null && player.IsFalling;

        float targetRight = 0f;
        float targetLeft  = 0f;
        float lerpSpeed   = 10f;

        // 2. Estado: Excavando (Tecla F)
        if (isDigging)
        {
            digPhase += Time.deltaTime * digSpeed;
            float digSine = Mathf.Sin(digPhase);
            // Ambos brazos pican con fuerza contra el suelo
            targetRight = Mathf.Lerp(-15f, digAngle, (digSine + 1f) * 0.5f);
            targetLeft  = Mathf.Lerp(-20f, digAngle * 0.7f, (digSine + 1f) * 0.5f);
            lerpSpeed   = 22f;
        }
        // 3. Estado: En el aire (Salto o Caída)
        else if (!isGrounded)
        {
            if (isJumping)
            {
                // Al subir: ambos brazos se alzan dinámicamente
                targetRight = jumpArmAngle;
                targetLeft  = jumpArmAngle - 5f;
                lerpSpeed   = 12f;
            }
            else if (isFalling)
            {
                // Al caer: brazos ligeramente abiertos para equilibrio
                targetRight = fallArmAngle;
                targetLeft  = -fallArmAngle;
                lerpSpeed   = 8f;
            }
            else
            {
                targetRight = jumpArmAngle * 0.6f;
                targetLeft  = jumpArmAngle * 0.6f;
            }
        }
        // 4. Estado: Caminando
        else if (isWalking)
        {
            phase += Time.deltaTime * walkSpeed;
            float walkSine = Mathf.Sin(phase);
            // Péndulo en contrafase: un brazo va adelante mientras el otro va atrás
            targetRight = walkSine * swingAngle;
            targetLeft  = -walkSine * swingAngle;
            lerpSpeed   = 14f;
        }
        // 5. Estado: Reposo (Idle)
        else
        {
            phase += Time.deltaTime * idleSpeed;
            float idleSine = Mathf.Sin(phase);
            // Respiración sutil y oscilación orgánica
            targetRight = idleSine * idleAngle;
            targetLeft  = -Mathf.Sin(phase + 0.4f) * (idleAngle * 0.7f);
            lerpSpeed   = 6f;
        }

        // Interpolación suave hacia los ángulos objetivo
        currentRightAngle = Mathf.LerpAngle(currentRightAngle, targetRight, Time.deltaTime * lerpSpeed);
        currentLeftAngle  = Mathf.LerpAngle(currentLeftAngle,  targetLeft,  Time.deltaTime * lerpSpeed);
    }

    /// <summary>
    /// Inicia el golpe del picazo al presionar V.
    /// </summary>
    public void TriggerPickaxeSwing(Action onImpact = null)
    {
        if (isAttacking) return;
        StartCoroutine(SwingRoutine(onImpact));
    }

    IEnumerator SwingRoutine(Action onImpact)
    {
        isAttacking = true;
        hasHitThisSwing = false;
        lastAttackTime = Time.time;

        float startRight = currentRightAngle;
        float startLeft  = currentLeftAngle;

        // --- FASE 1: SUBIDA / ANTICIPACIÓN (Ambos brazos levantan el pico hacia atrás/arriba) ---
        float elapsed = 0f;
        while (elapsed < raiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / raiseDuration);
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            currentRightAngle = Mathf.Lerp(startRight, raiseAngle, t);
            currentLeftAngle  = Mathf.Lerp(startLeft,  raiseAngle * 0.65f, t);
            yield return null;
        }

        // --- FASE 2: GOLPE CONTUNDENTE HACIA ABAJO ---
        elapsed = 0f;
        while (elapsed < strikeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / strikeDuration);
            // Aceleración cúbica para impacto pesado
            t = t * t * t;

            currentRightAngle = Mathf.Lerp(raiseAngle, strikeAngle, t);
            currentLeftAngle  = Mathf.Lerp(raiseAngle * 0.65f, strikeAngle * 0.6f, t);
            yield return null;
        }

        // --- MOMENTO DEL IMPACTO (Daño contra enemigos / serpientes) ---
        if (!hasHitThisSwing)
        {
            hasHitThisSwing = true;
            PerformHitDetection();
            onImpact?.Invoke();
        }

        // Pausa breve de impacto para sensación de fuerza (freeze frame sutil)
        yield return new WaitForSeconds(0.03f);

        // --- FASE 3: RECUPERACIÓN SUAVE A REPOSO ---
        elapsed = 0f;
        float postRight = currentRightAngle;
        float postLeft  = currentLeftAngle;
        while (elapsed < recoveryDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / recoveryDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            currentRightAngle = Mathf.Lerp(postRight, 0f, t);
            currentLeftAngle  = Mathf.Lerp(postLeft,  0f, t);
            yield return null;
        }

        currentRightAngle = 0f;
        currentLeftAngle  = 0f;
        isAttacking = false;
    }

    /// <summary>
    /// Detecta y aplica daño a enemigos (serpientes, destructibles) en rango del golpe.
    /// </summary>
    void PerformHitDetection()
    {
        if (player == null) return;

        int facing = player.FacingDirection;
        Vector2 hitCenter = (Vector2)player.transform.position + new Vector2(hitOffset.x * facing, hitOffset.y);

        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, attackRange, targetLayers);
        foreach (var col in hits)
        {
            if (col == null || col.gameObject == player.gameObject || (armsRig != null && col.transform.IsChildOf(armsRig)))
                continue;

            // 1. Interfaz IDamageable
            IDamageable dmg = col.GetComponent<IDamageable>();
            if (dmg == null) dmg = col.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(pickaxeDamage);
                Debug.Log($"[Pickaxe] ¡Golpe con el pico a {col.name}! Daño: {pickaxeDamage}");
                continue;
            }

            // 2. SnakeEnemy
            SnakeEnemy snake = col.GetComponent<SnakeEnemy>();
            if (snake == null) snake = col.GetComponentInParent<SnakeEnemy>();
            if (snake != null)
            {
                snake.TakeDamage(pickaxeDamage);
                Debug.Log($"[Pickaxe] ¡Serpiente {col.name} golpeada con el pico!");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        int facing = player.FacingDirection;
        Vector2 hitCenter = (Vector2)player.transform.position + new Vector2(hitOffset.x * facing, hitOffset.y);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
        Gizmos.DrawWireSphere(hitCenter, attackRange);
    }
}
