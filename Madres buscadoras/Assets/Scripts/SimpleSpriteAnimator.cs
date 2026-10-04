using UnityEngine;

// Anima con los sprites cortados: idle + 4 frames de caminar + estados de salto y excavación opcionales.
public class SimpleSpriteAnimator : MonoBehaviour
{
    [Header("Sprites Básicos")]
    public Sprite idle;
    public Sprite[] walk;
    public float fps = 8f;

    [Header("Sprites de Acciones Especiales (Opcional)")]
    public Sprite jumpSprite;
    public Sprite[] digSprites;
    public float digFps = 6f;

    [Header("Referencia al Jugador")]
    public PlayerController2D player;   // opcional: si es NPC déjalo vacío
    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (sr == null) return;

        // Estado: Excavando
        if (player != null && player.IsDigging)
        {
            if (digSprites != null && digSprites.Length > 0)
            {
                sr.sprite = digSprites[(int)(Time.time * digFps) % digSprites.Length];
                return;
            }
        }

        // Estado: En el aire (Salto / Caída)
        if (player != null && !player.IsGrounded)
        {
            if (jumpSprite != null)
            {
                sr.sprite = jumpSprite;
                return;
            }
        }

        // Estado: Caminando
        bool moving = player != null && Mathf.Abs(player.Horizontal) > 0.01f;
        if (!moving || walk == null || walk.Length == 0)
        {
            if (idle != null) sr.sprite = idle;
            return;
        }

        // Animación de caminata
        sr.sprite = walk[(int)(Time.time * fps) % walk.Length];
    }
}
