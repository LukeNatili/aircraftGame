using UnityEngine;

public class BillboardPrompt : MonoBehaviour
{
    private Transform cam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
