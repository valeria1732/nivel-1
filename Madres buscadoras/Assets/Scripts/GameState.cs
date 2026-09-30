using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor Integral del Estado de Búsqueda (Persistente entre escenas):
/// - 8 Pistas Coleccionables (3 Principales obligatorias + 5 Opcionales de contexto/memoria).
/// - 8 Botellas de Agua de supervivencia.
/// - Sistema de Checkpoints (3 puntos de control).
/// - Zonas activas (1 a 6) y objetivos dinámicos de misión.
/// - Acertijos resueltos (llave de la casa, clave bodega rancho, triangulación).
/// </summary>
public static class GameState
{
    [System.Serializable]
    public class ClueData
    {
        public string id;
        public string title;
        public string zoneName;
        public bool isPrincipal;
        public string summary;
        public string fullText;
        public bool isDiscovered;

        public ClueData(string id, string title, string zoneName, bool isPrincipal, string summary, string fullText)
        {
            this.id = id;
            this.title = title;
            this.zoneName = zoneName;
            this.isPrincipal = isPrincipal;
            this.summary = summary;
            this.fullText = fullText;
            this.isDiscovered = false;
        }
    }

    // --- Catálogo canónico de las 8 pistas ---
    private static readonly Dictionary<string, ClueData> clueCatalog = new Dictionary<string, ClueData>()
    {
        {
            "pista_nota_poste",
            new ClueData(
                "pista_nota_poste",
                "Nota en el poste vial",
                "Zona 1: Entrada al pueblo",
                false,
                "Hoja desgarrada fijada con alambre en el letrero.",
                "«Septiembre de 2023. Si alguien vio a los muchachos que salieron del taller hacia la carretera, por favor dejen razón en la capilla. Sus madres los esperamos.»"
            )
        },
        {
            "pista_foto_grupal",
            new ClueData(
                "pista_foto_grupal",
                "Última fotografía grupal",
                "Zona 2: Calles silenciosas",
                true,
                "Foto revelada de los jóvenes con un camión blanco de fondo.",
                "En el reverso se lee una placa vehicular y un número de ruta foránea. Esta fotografía confirma el punto exacto donde inició el trayecto y es la primera prueba irrefutable de su paradero."
            )
        },
        {
            "pista_testimonio_rosa",
            new ClueData(
                "pista_testimonio_rosa",
                "Testimonio de Doña Rosa",
                "Zona 2: Calles silenciosas",
                false,
                "Palabras entrecortadas de una vecina mayor tras la celosía.",
                "«Hija, aquí nadie habla porque las paredes tienen oídos. Pero de noche se oyen motores pesados subiendo hacia el rancho viejo de los Nogales. Llévate agua y ten cuidado con los que llevan saco y corbata.»"
            )
        },
        {
            "pista_credencial_estudiante",
            new ClueData(
                "pista_credencial_estudiante",
                "Credencial de estudiante",
                "Zona 3: Camino de terracería",
                false,
                "Plástico maltratado y manchado de arcilla roja.",
                "Credencial de la Escuela Preparatoria Regional. Pertenece a uno de los jóvenes buscados. Un recordatorio humano y desgarrador de los sueños interrumpidos."
            )
        },
        {
            "pista_registro_censurado",
            new ClueData(
                "pista_registro_censurado",
                "Registro oficial censurado",
                "Zona 4: El rancho abandonado",
                false,
                "Oficio con sellos institucionales cubiertos de tinta negra.",
                "Una orden de inspección fechada semanas después de los hechos, con firmas tachadas y la anotación manuscrita: «Sin novedad que reportar. Archivar expediente por instrucción superior.»"
            )
        },
        {
            "pista_talon_boleto",
            new ClueData(
                "pista_talon_boleto",
                "Talón de boleto y bitácora",
                "Zona 4: El rancho abandonado",
                true,
                "Bitácora de flete con registro de kilometraje y coordenadas.",
                "Muestra que el vehículo no siguió la carretera estatal, sino que se desvió hacia la zona de brechas baldías al este del rancho. Segunda evidencia clave obligatoria."
            )
        },
        {
            "pista_crucifijo_madera",
            new ClueData(
                "pista_crucifijo_madera",
                "Crucifijo de madera artesanal",
                "Zona 5: Área de búsqueda",
                false,
                "Pequeña cruz tallada a mano con un cordón deshilachado.",
                "Objeto de memoria hallado en la tierra. Las madres lo reconocen de inmediato: «Es la fe que no se rinde, el símbolo de que seguimos aquí de pie buscándote.»"
            )
        },
        {
            "pista_coordenadas_mapa",
            new ClueData(
                "pista_coordenadas_mapa",
                "Mapa con coordenadas a la sierra",
                "Zona 6: El primer hallazgo",
                true,
                "Croquis detallado con una fosa clandestina señalada en rojo.",
                "La evidencia final que concluye este primer nivel y revela la ubicación geográfica exacta para la búsqueda en el Nivel 2: La Fosa del Monte."
            )
        }
    };

    // --- Estado de Botellas de Agua ---
    private static readonly HashSet<string> collectedBottles = new HashSet<string>();
    public static int WaterBottlesCount => collectedBottles.Count;
    public const int TotalWaterBottles = 10;

    // --- Puntos de Control (Checkpoints) ---
    public static Vector3 CurrentCheckpointPosition = new Vector3(-3f, -2.4f, 0f);
    public static int CurrentCheckpointIndex = 0;

    // --- Acertijos y Misiones resueltas ---
    private static readonly HashSet<string> solvedPuzzles = new HashSet<string>();

    // --- Eventos ---
    public static event Action<string> OnClueAdded;
    public static event Action<int> OnWaterCollected;
    public static event Action<int, Vector3> OnCheckpointReached;
    public static event Action OnStateUpdated;

    // --- Consulta de Pistas ---
    public static bool Has(string id)
    {
        // Compatibilidad con identificadores cortos antiguos
        if (id == "foto_grupal") id = "pista_foto_grupal";
        if (id == "boleto_roto") id = "pista_talon_boleto";
        if (id == "colectivo") id = "pista_coordenadas_mapa";

        if (clueCatalog.TryGetValue(id, out ClueData cd))
        {
            return cd.isDiscovered;
        }
        return false;
    }

    public static int TotalCluesFound
    {
        get
        {
            int count = 0;
            foreach (var kvp in clueCatalog)
            {
                if (kvp.Value.isDiscovered) count++;
            }
            return count;
        }
    }

    public static int PrincipalCluesFound
    {
        get
        {
            int count = 0;
            foreach (var kvp in clueCatalog)
            {
                if (kvp.Value.isPrincipal && kvp.Value.isDiscovered) count++;
            }
            return count;
        }
    }

    public static IReadOnlyCollection<ClueData> GetAllClues() => clueCatalog.Values;

    public static void Add(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        // Mapeo retrocompatible
        if (id == "foto_grupal") id = "pista_foto_grupal";
        if (id == "boleto_roto") id = "pista_talon_boleto";
        if (id == "colectivo") id = "pista_coordenadas_mapa";

        if (clueCatalog.TryGetValue(id, out ClueData data))
        {
            if (!data.isDiscovered)
            {
                data.isDiscovered = true;
                OnClueAdded?.Invoke(id);
                OnStateUpdated?.Invoke();

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayClueFound();
                }

                Debug.Log($"[GameState] Pista descubierta: {data.title} ({TotalCluesFound}/8)");
            }
        }
    }

    // --- Botellas de Agua ---
    public static bool CollectWaterBottle(string bottleId)
    {
        if (string.IsNullOrEmpty(bottleId) || collectedBottles.Contains(bottleId)) return false;

        collectedBottles.Add(bottleId);
        OnWaterCollected?.Invoke(WaterBottlesCount);
        OnStateUpdated?.Invoke();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWaterPickup();
        }

        return true;
    }

    public static bool IsBottleCollected(string bottleId) => collectedBottles.Contains(bottleId);

    // --- Checkpoints ---
    public static void SetCheckpoint(int index, Vector3 position)
    {
        if (index > CurrentCheckpointIndex)
        {
            CurrentCheckpointIndex = index;
            CurrentCheckpointPosition = position;
            OnCheckpointReached?.Invoke(index, position);
            OnStateUpdated?.Invoke();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCheckpointReached();
            }

            Debug.Log($"[GameState] Checkpoint {index} activado en {position}");
        }
    }

    // --- Acertijos ---
    public static void SolvePuzzle(string puzzleId)
    {
        if (solvedPuzzles.Add(puzzleId))
        {
            OnStateUpdated?.Invoke();
            Debug.Log($"[GameState] Acertijo resuelto: {puzzleId}");
        }
    }

    public static bool IsPuzzleSolved(string puzzleId) => solvedPuzzles.Contains(puzzleId);

    // --- Detección de Zona por Coordenada X ---
    public static string GetCurrentZoneName(float playerX)
    {
        if (playerX < 12f) return "Zona 1: Entrada al pueblo";
        if (playerX < 32f) return "Zona 2: Las calles silenciosas";
        if (playerX < 50f) return "Zona 3: El camino de terracería";
        if (playerX < 68f) return "Zona 4: El rancho abandonado";
        if (playerX < 86f) return "Zona 5: El área de búsqueda";
        return "Zona 6: El primer hallazgo";
    }

    // --- Objetivo Dinámico Actual ---
    public static string GetCurrentObjective()
    {
        return "Derrota a los enemigos que bloquean el camino para pasar el Nivel 1";
    }

    // --- Reinicio para nueva partida ---
    public static void Reset()
    {
        foreach (var clue in clueCatalog.Values)
        {
            clue.isDiscovered = false;
        }
        collectedBottles.Clear();
        solvedPuzzles.Clear();
        CurrentCheckpointIndex = 0;
        CurrentCheckpointPosition = new Vector3(-3f, -2.4f, 0f);
        OnStateUpdated?.Invoke();
    }
}
