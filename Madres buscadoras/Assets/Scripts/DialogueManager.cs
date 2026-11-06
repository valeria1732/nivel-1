using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public static bool IsTalking { get; private set; }

    public GameObject panel;
    public TMP_Text speakerText;
    public TMP_Text bodyText;
    public float charDelay = 0.02f;

    string[] lines; int index; bool typing; Coroutine co; System.Action onEnd;

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
        IsTalking = false;
    }

    public void Play(string speaker, string[] l, System.Action end = null)
    {
        if (IsTalking || l == null || l.Length == 0) return;
        lines = l;
        index = 0;
        onEnd = end;
        IsTalking = true;

        if (speakerText != null) speakerText.text = speaker;
        if (panel != null) panel.SetActive(true);
        Show();
    }

    void Show()
    {
        if (lines == null || index >= lines.Length) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Type(lines[index]));
    }

    IEnumerator Type(string s)
    {
        typing = true;
        if (bodyText != null) bodyText.text = "";
        foreach (char c in s)
        {
            if (bodyText != null) bodyText.text += c;
            yield return new WaitForSeconds(charDelay);
        }
        typing = false;
    }

    void Update()
    {
        if (!IsTalking) return;
        // Sin temporizador: el jugador avanza con ESPACIO a su ritmo (feedback de Camila)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (typing)
            {
                if (co != null) StopCoroutine(co);
                if (bodyText != null && lines != null && index < lines.Length)
                {
                    bodyText.text = lines[index];
                }
                typing = false;
                return;
            }
            index++;
            if (lines != null && index >= lines.Length)
            {
                End();
                return;
            }
            Show();
        }
    }

    void End()
    {
        if (panel != null) panel.SetActive(false);
        var cb = onEnd;
        onEnd = null;
        StartCoroutine(Release(cb));
    }

    // Espera un frame para que la misma pulsación no reabra el diálogo
    IEnumerator Release(System.Action cb)
    {
        yield return null;
        IsTalking = false;
        cb?.Invoke();
    }
}
