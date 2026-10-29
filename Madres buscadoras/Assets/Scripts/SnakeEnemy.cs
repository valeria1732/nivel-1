using UnityEngine;

/// <summary>
/// Desactivado por solicitud del usuario: Las serpientes han sido retiradas del juego.
/// Cualquier instancia en escena se destruye inmediatamente al iniciar.
/// Se conservan los campos públicos para compatibilidad total con Unity y herramientas de edición.
/// </summary>
public class SnakeEnemy : MonoBehaviour, IDamageable
{
    [Header("Compatibilidad")]
    public float speed = 2.4f;
    public float leftBound = -8f;
    public float rightBound = 22f;
    public float startDelay = 0f;
    public int damageAmount = 15;
    public SpriteRenderer sr;

    void Awake()
    {
        // Las serpientes se destruyen de inmediato al cargar el juego
        Destroy(gameObject);
    }

    public void TakeDamage(int amount)
    {
        Destroy(gameObject);
    }
}
