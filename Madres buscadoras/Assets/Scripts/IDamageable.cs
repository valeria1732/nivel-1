/// <summary>
/// Interfaz para cualquier entidad u objeto en el juego que pueda recibir daño (enemigos, obstáculos, etc.).
/// </summary>
public interface IDamageable
{
    void TakeDamage(int amount);
}
