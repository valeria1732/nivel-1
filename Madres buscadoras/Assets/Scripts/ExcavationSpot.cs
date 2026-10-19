using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class ExcavationSpot : MonoBehaviour
{
    [Header("Identificación y Pista")]
    public string spotName = "Tierra removida";
    public string clueToGive = "objeto_enterrado";
    public string requiresClue = ""; // Si requiere tener una pista previa (ej. pala o mapa)

    [Header("Configuración de Excavación")]
    public KeyCode digKey = KeyCode.F;
    public float digDuration = 2.0f;
    public bool isCompleted { get; private set; }

    [Header("Diálogos al Descubrir")]
    public string speaker = "Esperanza";
    [TextArea(2, 4)]
    public string[] linesOnDiscovered = new string[] {
        "¡Encontré algo enterrado en este lugar!",
        "Podría ser una pista importante para la búsqueda."
    };
    [TextArea(2, 4)]
    public string[] linesIfLocked = new string[] {
        "La tierra aquí parece sospechosa, pero necesito algo más de información antes de excavar."
    };

    [Header("Visuales y Recompensas")]
    public GameObject prompt;                 // Indicador visual en pantalla o sobre el objeto [F]
    public SpriteRenderer groundSpriteRenderer;// Sprite del suelo/montículo
    public Sprite dugSprite;                  // Sprite del pozo o tierra ya excavada
    public GameObject rewardObjectToSpawn;    // Objeto o reliquia que aparece
    public ParticleSystem dirtParticles;      // Partículas de tierra al excavar

    [Header("Eventos")]
    public UnityEvent onExcavationStart;
    public UnityEvent onExcavationComplete;

    public bool IsPlayerInRange { get; private set; }
    public bool IsCompleted => isCompleted;

    PlayerController2D currentPlayer;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Start()
    {
        if (prompt != null) prompt.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IsPlayerInRange = true;
            currentPlayer = other.GetComponent<PlayerController2D>();
            if (prompt != null && !isCompleted) prompt.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IsPlayerInRange = false;
            currentPlayer = null;
            if (prompt != null) prompt.SetActive(false);
        }
    }

    void Update()
    {
        if (!IsPlayerInRange || isCompleted || DialogueManager.IsTalking) return;

        if (Input.GetKeyDown(digKey) || (digKey == KeyCode.F && Input.GetKeyDown(KeyCode.E)))
        {
            TryStartExcavation();
        }
    }

    public void TryStartExcavation()
    {
        if (isCompleted || DialogueManager.IsTalking) return;

        // Comprueba si se requiere alguna pista previa
        if (!string.IsNullOrEmpty(requiresClue) && !GameState.Has(requiresClue))
        {
            DialogueManager.Instance.Play(speaker, linesIfLocked);
            return;
        }

        if (prompt != null) prompt.SetActive(false);
        onExcavationStart?.Invoke();

        if (dirtParticles != null) dirtParticles.Play();

        if (currentPlayer != null)
        {
            currentPlayer.StartDigging(digDuration, CompleteExcavation);
        }
        else
        {
            StartCoroutine(FallbackDigRoutine());
        }
    }

    IEnumerator FallbackDigRoutine()
    {
        yield return new WaitForSeconds(digDuration);
        CompleteExcavation();
    }

    void CompleteExcavation()
    {
        isCompleted = true;
        if (dirtParticles != null) dirtParticles.Stop();

        // Cambiar sprite a agujero/tierra excavada si está asignado
        if (groundSpriteRenderer != null && dugSprite != null)
        {
            groundSpriteRenderer.sprite = dugSprite;
        }

        // Mostrar objeto desenterrado
        if (rewardObjectToSpawn != null)
        {
            rewardObjectToSpawn.SetActive(true);
        }

        // Registrar pista en el cuaderno
        if (!string.IsNullOrEmpty(clueToGive))
        {
            GameState.Add(clueToGive);
        }

        onExcavationComplete?.Invoke();

        // Mostrar diálogo de descubrimiento
        if (linesOnDiscovered != null && linesOnDiscovered.Length > 0 && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.Play(speaker, linesOnDiscovered);
        }
    }
}
