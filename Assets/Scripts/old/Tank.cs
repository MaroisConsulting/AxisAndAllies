using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Tank : MonoBehaviour
{
    public GameObject tank;
    public Vector3 targetLocation;
    public float turnSpeed = 90f;
    public float tankSpeed = 10;
    public float stopDistance = 1;
    public bool commandIssued = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //var renderer = tank.GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (targetLocation == null || !commandIssued)
            return;

        // Direction from this object to the target
        Vector3 direction = targetLocation - transform.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        // Determine the rotation needed to face the target
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Gradually rotate toward it
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );

        if (Vector3.Distance(tank.transform.position, targetLocation) > stopDistance)
        {
            tank.transform.Translate(0, 0, tankSpeed * Time.deltaTime);
        }
    }
}
