using UnityEngine;

/// <summary>
/// Desactivado: Serpientes retiradas del juego.
/// Se conservan los campos públicos para compatibilidad total con Unity y serialización.
/// </summary>
public class SnakeSpawner : MonoBehaviour
{
    [Header("Compatibilidad")]
    public GameObject snakePrefab;
    public Transform[] spawnPoints;
    public bool randomSpawn = false;
    public int randomCount = 3;
    public float randomRangeX = 10f;
    public bool spawnOnStart = false;

    void Awake()
    {
        Destroy(gameObject);
    }
}
