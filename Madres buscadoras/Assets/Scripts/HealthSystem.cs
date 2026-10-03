using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de 5 Corazones para Madres Buscadoras:
/// - 5 corazones de vida (máximo 5).
/// - Cada golpe de enemigo o peligro resta 1 corazón.
/// - 1.0 segundo de invulnerabilidad tras recibir daño (iFrames con parpadeo).
/// - Al perder los 5 corazones, reaparece en el último punto de control sin perder pistas.
/// - Botellas de agua y corazones en el camino curan 1 corazón (hasta 5).
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Configuración de Corazones")]
    public int maxHearts = 5;
    public int currentHearts = 5;

    // Propiedades de compatibilidad con herramientas de edición y scripts previos
    public int maxHealth
    {
        get => maxHearts;
        set => maxHearts = value;
    }
    public int currentHealth
    {
        get => currentHearts;
        set => currentHearts = value;
    }

    [Header("Invulnerabilidad (iFrames)")]
    public float iFrameDuration = 1.6f;
    public bool isInvincible = false;

    [Header("Feedback Visual")]
    public SpriteRenderer characterSprite;
    public Color damageFlashColor = new Color(1f, 0.25f, 0.25f, 0.9f);
    public float flashInterval = 0.12f;

    [Header("Eventos")]
    public UnityEvent<int, int> onHealthChanged;  // (currentHearts, maxHearts)
    public UnityEvent onDamage;
    public UnityEvent onHeal;
    public UnityEvent onDeathAndRespawn;

    private Color originalColor = Color.white;
    private bool isRespawning = false;

    void Awake()
    {
        if (characterSprite == null)
            characterSprite = GetComponentInChildren<SpriteRenderer>();

        if (characterSprite != null)
            originalColor = characterSprite.color;

        // Garantizar 5 corazones de vida aunque la escena tenga valores viejos serializados
        if (maxHearts < 5)
        {
            maxHearts = 5;
        }
        if (iFrameDuration < 1.6f)
        {
            iFrameDuration = 1.6f;
        }
        currentHearts = maxHearts;
    }

    void Start()
    {
        currentHearts = maxHearts;
        onHealthChanged?.Invoke(currentHearts, maxHearts);
    }

    /// <summary>
    /// Aplica daño al personaje. Cada impacto resta 1 corazón íntegro.
    /// </summary>
    public void TakeDamage(int rawAmount = 1)
    {
        if (isInvincible || isRespawning) return;

        currentHearts = Mathf.Max(0, currentHearts - 1);
        onHealthChanged?.Invoke(currentHearts, maxHearts);
        onDamage?.Invoke();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDamage();
        }

        // Efecto visual de daño e invulnerabilidad
        StartCoroutine(IFrameFlickerRoutine());

        if (currentHearts <= 0)
        {
            StartCoroutine(RespawnSequenceRoutine());
        }
    }

    /// <summary>
    /// Envenenamiento letal al tocar un hongo venenoso sin saltarlo:
    /// envenena al personaje y muere de inmediato, regresando al inicio.
    /// </summary>
    public void KillByPoison()
    {
        if (isRespawning) return;

        currentHearts = 0;
        onHealthChanged?.Invoke(currentHearts, maxHearts);
        onDamage?.Invoke();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDamage();
        }

        if (characterSprite != null)
        {
            characterSprite.color = new Color(0.35f, 1f, 0.25f, 1f);
        }

        ScoreManager.SpawnFloatingPopup(transform.position + Vector3.up * 2.1f, "☠ ¡ENVENENADA POR HONGO! ☠", new Color(0.45f, 1f, 0.25f, 1f));
        StartCoroutine(RespawnSequenceRoutine());
    }

    /// <summary>
    /// Cura al personaje (por ejemplo, al recoger una botella de agua).
    /// </summary>
    public void Heal(int hearts = 1)
    {
        if (isRespawning) return;

        int prev = currentHearts;
        currentHearts = Mathf.Min(maxHearts, currentHearts + hearts);

        if (currentHearts != prev)
        {
            onHealthChanged?.Invoke(currentHearts, maxHearts);
            onHeal?.Invoke();
        }
    }

    public void FullHeal()
    {
        currentHearts = maxHearts;
        onHealthChanged?.Invoke(currentHearts, maxHearts);
    }

    /// <summary>
    /// Si te matan (pierdes todos los corazones), te manda de regreso al inicio del nivel.
    /// </summary>
    private IEnumerator RespawnSequenceRoutine()
    {
        isRespawning = true;
        isInvincible = true;
        onDeathAndRespawn?.Invoke();

        Debug.Log("[HealthSystem] Jugador sin corazones. Regresando al inicio del nivel...");

        // Bloquear movimiento temporalmente
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        ScoreManager.SpawnFloatingPopup(transform.position + Vector3.up * 1.8f, "¡VOLVIENDO AL INICIO!", new Color(1f, 0.3f, 0.3f, 1f));

        if (characterSprite != null)
        {
            characterSprite.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        }

        yield return new WaitForSeconds(0.55f);

        // Resetear progreso al inicio del nivel y recargar la escena desde el principio
        GameState.Reset();
        ScoreManager.ResetScore();
        transform.position = new Vector3(-2.0f, -2.15f, 0f);

        currentHearts = maxHearts;
        onHealthChanged?.Invoke(currentHearts, maxHearts);

        if (characterSprite != null)
        {
            characterSprite.color = originalColor;
        }

        isRespawning = false;
        isInvincible = false;

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    private IEnumerator IFrameFlickerRoutine()
    {
        isInvincible = true;
        float elapsed = 0f;

        while (elapsed < iFrameDuration)
        {
            if (characterSprite != null)
            {
                characterSprite.color = (characterSprite.color == originalColor) ? damageFlashColor : originalColor;
            }
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        if (characterSprite != null)
        {
            characterSprite.color = originalColor;
        }
        isInvincible = false;
    }
}
