using UnityEngine;
using Photon.Pun;

public class BallThrower : Interactive
{
    [Header("Throw Settings")]
    public float minThrowForce = 5f;
    public float maxThrowForce = 20f;
    public float powerBarSpeed = 2f;

    [Header("Visual Power Bar")]
    public float barLength = 1f;
    public float barOffset = 0.5f; // How far to side of ball
    public Color minPowerColor = Color.yellow;
    public Color maxPowerColor = Color.red;

    PhotonView pv;
    Rigidbody rb;
    Transform cam;
    LineRenderer powerBar;
    bool hasBeenThrown = false;
    bool isCharging = false;
    float currentPower = 0f;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
        
        // Create LineRenderer for power bar
        GameObject barObj = new GameObject("PowerBar");
        barObj.transform.SetParent(transform);
        powerBar = barObj.AddComponent<LineRenderer>();
        powerBar.startWidth = 0.05f;
        powerBar.endWidth = 0.05f;
        powerBar.positionCount = 2;
        powerBar.material = new Material(Shader.Find("Sprites/Default"));
        powerBar.enabled = false;
    }

    void Start()
    {
        cam = Camera.main ? Camera.main.transform : null;
        
        // Start with gravity off and kinematic
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Debug ownership
        if (pv != null)
        {
            Debug.Log($"BallThrower Start - IsMine: {pv.IsMine}, Owner: {pv.Owner?.ActorNumber}, Local: {PhotonNetwork.LocalPlayer.ActorNumber}");
        }
    }

    void Update()
    {
        // Only owner controls this ball
        if (pv != null && !pv.IsMine)
        {
            // Not my ball - don't move it
            return;
        }
        if (hasBeenThrown) return;

        if (!cam)
        {
            cam = Camera.main ? Camera.main.transform : null;
            if (!cam) return;
        }

        // Keep ball in front of camera until thrown
        transform.position = cam.position + cam.forward * 2f;
        transform.rotation = cam.rotation;

        // If charging, oscillate power and update bar
        if (isCharging)
        {
            float t = Mathf.PingPong(Time.time * powerBarSpeed, 1f);
            currentPower = Mathf.Lerp(minThrowForce, maxThrowForce, t);

            // Update power bar visual
            UpdatePowerBar(t);
        }
    }

    void UpdatePowerBar(float normalizedPower)
    {
        if (powerBar == null) return;

        // Position bar to the right of the ball, separate from ball
        Vector3 barBasePos = transform.position + transform.right * (barOffset + 0.15f); // Offset more to the right
        Vector3 barStart = barBasePos - Vector3.up * (barLength * 0.5f); // Start from bottom
        Vector3 barEnd = barBasePos - Vector3.up * (barLength * 0.5f) + Vector3.up * (barLength * normalizedPower);

        powerBar.SetPosition(0, barStart);
        powerBar.SetPosition(1, barEnd);

        // Color based on power
        Color barColor = Color.Lerp(minPowerColor, maxPowerColor, normalizedPower);
        powerBar.startColor = barColor;
        powerBar.endColor = barColor;
    }

    public new void Interact()
    {
        if (pv == null || hasBeenThrown) return;

        if (!isCharging)
        {
            // First interact: start charging
            isCharging = true;
            currentPower = minThrowForce;

            // Show power bar
            if (powerBar != null)
                powerBar.enabled = true;

            Debug.Log("Started charging throw power...");
        }
        else
        {
            // Second interact: throw the ball with current power
            Vector3 throwDirection = cam ? cam.forward : transform.forward;
            pv.RPC("ThrowBall", RpcTarget.AllBuffered, throwDirection, currentPower);
            
            hasBeenThrown = true;
            isCharging = false;

            // Hide power bar
            if (powerBar != null)
                powerBar.enabled = false;

            Debug.Log("Threw ball with force: " + currentPower);
        }
    }

    [PunRPC]
    void ThrowBall(Vector3 direction, float force)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(direction.normalized * force, ForceMode.VelocityChange);
        }
        hasBeenThrown = true;
    }
}
