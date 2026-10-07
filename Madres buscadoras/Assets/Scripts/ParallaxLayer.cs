using UnityEngine;

// factor 0 = fijo (cielo), 1 = se mueve igual que la cámara. Lejos ≈ 0.1, medio ≈ 0.4, casas ≈ 0.75
public class ParallaxLayer : MonoBehaviour
{
    public Transform cam;
    [Range(0f, 1f)] public float factor = 0.5f;
    float startX, camStartX;

    void Start() { if (!cam) cam = Camera.main.transform; startX = transform.position.x; camStartX = cam.position.x; }

    void LateUpdate()
    {
        float dx = (cam.position.x - camStartX) * factor;
        transform.position = new Vector3(startX + dx, transform.position.y, transform.position.z);
    }
}
