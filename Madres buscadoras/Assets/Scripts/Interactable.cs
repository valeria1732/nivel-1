using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class Interactable : MonoBehaviour
{
    public string speaker = "Esperanza";
    [TextArea(2, 4)] public string[] lines;

    [Header("Requisito (opcional)")]
    public string requiresClue;
    [TextArea(2, 4)] public string[] linesIfLocked;

    [Header("Recompensa")]
    public string clueToGive;          // id de pista que se guarda en el cuaderno
    public bool disappearAfter;        // objetos recogibles
    public UnityEvent onInteract;

    public GameObject prompt;          // ícono [E] hijo del objeto
    bool inRange, used;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }
    void Start() { if (prompt) prompt.SetActive(false); }

    void OnTriggerEnter2D(Collider2D c) { if (c.CompareTag("Player")) { inRange = true; if (prompt && !used) prompt.SetActive(true); } }
    void OnTriggerExit2D(Collider2D c)  { if (c.CompareTag("Player")) { inRange = false; if (prompt) prompt.SetActive(false); } }

    void Update()
    {
        if (!inRange || DialogueManager.IsTalking || !Input.GetKeyDown(KeyCode.E)) return;

        bool locked = !string.IsNullOrEmpty(requiresClue) && !GameState.Has(requiresClue);
        if (locked) { DialogueManager.Instance.Play(speaker, linesIfLocked); return; }

        DialogueManager.Instance.Play(speaker, lines, () =>
        {
            GameState.Add(clueToGive);
            onInteract?.Invoke();
            if (disappearAfter) { used = true; if (prompt) prompt.SetActive(false); gameObject.SetActive(false); }
        });
    }
}
