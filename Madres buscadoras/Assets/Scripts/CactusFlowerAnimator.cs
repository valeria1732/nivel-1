using UnityEngine;

/// <summary>
/// Anima las flores del cactus haciendo que abran y cierren suavemente
/// usando una escala sinusoidal. Si se asignan sprites separados de flores
/// abiertas/cerradas, los alterna ademas del efecto de escala.
/// </summary>
public class CactusFlowerAnimator : MonoBehaviour
{
    [Header("Sprites de flores (opcional)")]
    [Tooltip("Sprite con flores abiertas")]
    public Sprite floresAbiertas;
    [Tooltip("Sprite con flores cerradas o botones")]
    public Sprite floresCerradas;

    [Header("Animacion de apertura")]
    [Tooltip("Duracion en segundos de cada ciclo completo abrir->cerrar")]
    public float cycleDuration = 3f;
    [Tooltip("Tiempo que permanece abierta (0-1, proporcion del ciclo)")]
    [Range(0f, 1f)]
    public float openRatio = 0.6f;
    [Tooltip("Desfase aleatorio al iniciar para que no todos los cactus se muevan igual")]
    public bool randomOffset = true;

    [Header("Efecto de escala en flores")]
    [Tooltip("Escala minima de las flores (cerradas)")]
    public float scaleMin = 0.85f;
    [Tooltip("Escala maxima de las flores (abiertas)")]
    public float scaleMax = 1f;

    SpriteRenderer sr;
    float timeOffset;
    Vector3 originalScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        timeOffset = randomOffset ? Random.Range(0f, cycleDuration) : 0f;
    }

    void Update()
    {
        float t = ((Time.time + timeOffset) % cycleDuration) / cycleDuration; // 0..1

        float openValue;
        if (t < openRatio)
        {
            // Abriendo: sube suavemente de 0 a 1
            openValue = Mathf.SmoothStep(0f, 1f, t / openRatio);
        }
        else
        {
            // Cerrando: baja suavemente de 1 a 0
            openValue = Mathf.SmoothStep(1f, 0f, (t - openRatio) / (1f - openRatio));
        }

        // Si hay sprites separados, alternar segun si esta mas abierta o cerrada
        if (sr != null && floresAbiertas != null && floresCerradas != null)
        {
            sr.sprite = openValue >= 0.5f ? floresAbiertas : floresCerradas;
        }

        // Efecto de escala suave (funciona siempre)
        float scaleFactor = Mathf.Lerp(scaleMin, scaleMax, openValue);
        transform.localScale = new Vector3(
            originalScale.x * scaleFactor,
            originalScale.y * scaleFactor,
            originalScale.z
        );
    }
}
