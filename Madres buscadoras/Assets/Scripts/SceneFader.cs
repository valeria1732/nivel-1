using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ponlo en una Image negra a pantalla completa dentro del Canvas
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance;
    public Image black;
    public float duration = 1f;

    void Awake() { Instance = this; }
    void Start() { StartCoroutine(Fade(1, 0)); }

    public void LoadScene(string name) { StartCoroutine(FadeAndLoad(name)); }

    IEnumerator FadeAndLoad(string n) { yield return Fade(0, 1); SceneManager.LoadScene(n); }

    IEnumerator Fade(float a, float b)
    {
        black.raycastTarget = true;
        for (float t = 0; t < duration; t += Time.deltaTime)
        { var c = black.color; c.a = Mathf.Lerp(a, b, t / duration); black.color = c; yield return null; }
        var f = black.color; f.a = b; black.color = f; black.raycastTarget = b > 0.5f;
    }
}
