using UnityEngine;

public class Orbit : MonoBehaviour
{
    public float rotationSpeed = 50f;
    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}