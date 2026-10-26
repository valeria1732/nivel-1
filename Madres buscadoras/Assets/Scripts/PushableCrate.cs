using UnityEngine;

/// <summary>
/// Caja de madera empujable en el rancho (Zona 4):
/// - Bloquea un pasillo o sirve de escalón para acceder a una plataforma elevada.
/// - Responde a la física 2D del empuje del personaje.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PushableCrate : MonoBehaviour
{
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.mass = 4.5f;
        rb.linearDamping = 6.0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
        {
            sr.sprite = CreateCrateSprite();
            sr.sortingOrder = 3;
        }
    }

    private static Sprite CreateCrateSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color woodDark = new Color(0.38f, 0.22f, 0.12f, 1f);
        Color woodLight = new Color(0.55f, 0.35f, 0.20f, 1f);
        Color border = new Color(0.24f, 0.14f, 0.08f, 1f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                {
                    tex.SetPixel(x, y, border);
                }
                else if (x == y || x == (size - 1 - y)) // X de refuerzo
                {
                    tex.SetPixel(x, y, border);
                }
                else
                {
                    tex.SetPixel(x, y, ((x + y) % 6 == 0) ? woodDark : woodLight);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }
}
