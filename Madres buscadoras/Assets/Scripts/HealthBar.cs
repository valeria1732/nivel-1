using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Barra de vida HUD profesional.
/// Usa escala de ancla (anchorMax.x) para el relleno — no requiere sprite asignado.
/// 
/// Jerarquia en Canvas (Layer = UI):
///   HealthHUD  (este script)
///     Panel        — Image fondo oscuro semitransparente
///       Border     — Image borde delgado claro
///       BarBG      — Image fondo oscuro de la barra
///         Fill     — Image relleno (anchorMin=0,0 / anchorMax=1,1 inicialmente)
///       HeartIcon  — Text "❤"
///       HPText     — Text "100 / 100"
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("Referencia al Fill")]
    [Tooltip("RectTransform del relleno — su anchorMax.x se controla (0..1)")]
    public RectTransform fillRect;
    [Tooltip("Image del relleno para cambiar color")]
    public Image fillImage;
    [Tooltip("Text de HP numerico (opcional)")]
    public Text hpLabel;
    [Tooltip("Text del icono corazon (opcional)")]
    public Text heartIcon;

    [Header("Colores")]
    public Color colorFull   = new Color(0.22f, 0.85f, 0.40f, 1f);
    public Color colorMid    = new Color(0.95f, 0.78f, 0.10f, 1f);
    public Color colorLow    = new Color(0.92f, 0.20f, 0.20f, 1f);

    [Range(0f, 0.5f)]
    public float lowThreshold = 0.25f;
    [Range(0f, 1f)]
    public float midThreshold = 0.55f;

    [Header("Animacion")]
    public float lerpSpeed  = 8f;
    public float pulseSpeed = 3f;

    // ---- estado interno ----
    float targetFill = 1f;
    float displayFill = 1f;
    bool isLow = false;

    void Awake()
    {
        // El sistema oficial de vida son los Corazones en HealthHeartsHUD
        if (Object.FindAnyObjectByType<HealthHeartsHUD>() == null)
        {
            GameObject hudGO = new GameObject("HealthHeartsHUD");
            hudGO.AddComponent<HealthHeartsHUD>();
        }

        // Destruir cualquier barra rectangular vieja para que la vida sean exclusivamente Corazones
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.name.Contains("HealthBar"))
        {
            Destroy(parentCanvas.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnHealthChanged(int current, int max)
    {
        if (max <= 0) return;
        targetFill = Mathf.Clamp01((float)current / max);
        isLow      = targetFill <= lowThreshold;

        if (hpLabel != null)
            hpLabel.text = current + "/" + max;
    }

    void Update()
    {
        // Lerp del fill
        displayFill = Mathf.Lerp(displayFill, targetFill, Time.deltaTime * lerpSpeed);

        // Aplicar via anchorMax.x
        if (fillRect != null)
        {
            Vector2 aMax = fillRect.anchorMax;
            aMax.x = displayFill;
            fillRect.anchorMax = aMax;
        }

        // Color segun porcentaje
        if (fillImage != null)
        {
            Color c;
            if (displayFill <= lowThreshold)
                c = colorLow;
            else if (displayFill <= midThreshold)
                c = Color.Lerp(colorLow, colorMid,
                    (displayFill - lowThreshold) / (midThreshold - lowThreshold));
            else
                c = Color.Lerp(colorMid, colorFull,
                    (displayFill - midThreshold) / (1f - midThreshold));
            fillImage.color = c;
        }

        // Pulso cuando vida critica
        if (isLow)
        {
            float p = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.05f, p);
            if (heartIcon != null)
                heartIcon.color = Color.Lerp(new Color(1f, 0.3f, 0.3f, 1f),
                                             new Color(1f, 0.8f, 0.8f, 1f), p);
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one,
                                                Time.deltaTime * 10f);
            if (heartIcon != null)
                heartIcon.color = new Color(1f, 0.35f, 0.35f, 1f);
        }
    }
}
