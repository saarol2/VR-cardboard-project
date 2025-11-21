using UnityEngine;

public class pointer : MonoBehaviour
{
    private Renderer rend;
    public void OnPointerEnter()
    {
        rend = GetComponent<Renderer>();
        rend.sharedMaterial.color = Color.red;
    }

    public void OnPointerExit()
    {
        rend.sharedMaterial.color = Color.white;
    }
}
