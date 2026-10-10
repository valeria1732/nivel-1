using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public float smooth = 6f;
    public float minX = -8f, maxX = 365f;   // límites del recorrido largo extendido
    public float yOffset = 1.5f;

    void Start()
    {
        if (maxX < 365f) maxX = 365f;
        if (!target)
        {
            FindTarget();
        }
    }

    void LateUpdate()
    {
        if (!target)
        {
            FindTarget();
            if (!target) return;
        }

        float x = Mathf.Clamp(target.position.x, minX, maxX);
        Vector3 goal = new Vector3(x, target.position.y + yOffset, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, goal, smooth * Time.deltaTime);
    }

    void FindTarget()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null)
        {
            PlayerController2D pc = Object.FindAnyObjectByType<PlayerController2D>();
            if (pc != null) p = pc.gameObject;
        }
        if (p != null) target = p.transform;
    }
}
