using UnityEngine;

/// <summary>
/// Piedra rodante que desciende por la terracería (Zona 3):
/// - Rueda con física 2D.
/// - Causa 1 corazón de daño y empuje si impacta a Esperanza.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class RollingBoulder : MonoBehaviour
{
    public float rollForce = -3.5f;
    public float lifetime = 14f;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.mass = 8.0f;
        rb.linearVelocity = new Vector2(rollForce, 0f);
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            HealthSystem hs = col.gameObject.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.TakeDamage(1);
            }

            Rigidbody2D pRb = col.gameObject.GetComponent<Rigidbody2D>();
            if (pRb != null)
            {
                pRb.linearVelocity = new Vector2(-4.5f, 4f);
            }

            ScoreManager.AddScore(0, col.transform.position + Vector3.up * 1.2f, "¡Piedra rodante! -1 ❤️");
        }
    }
}
