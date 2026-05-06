using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    public Transform target;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        rb.MovePosition(target.position);
    }
}
