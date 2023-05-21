using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CubeController : MonoBehaviour
{
    public Rigidbody cubeRigidBody;
    public Rigidbody cubeRigidBody2;
    public Rigidbody wallRigidBody;

    private bool accel = true;
    private bool isStuck = false;

    public float accelerationForce; // N
    public float velocityThreshold; // m/s

    public float springConstant; // N/m
    public float compressedLength; // m
    private float springForce;

    private float initialPosition;
    private float currentTimeStep;
    private List<List<float>> timeSeries;

    private List<float> positions1; // Liste zur Speicherung der Orte des ersten Würfels
    private List<float> velocities1; // Liste zur Speicherung der Geschwindigkeiten des ersten Würfels
    private List<float> momenta1; // Liste zur Speicherung der Impulse des ersten Würfels

    private List<float> positions2; // Liste zur Speicherung der Orte des zweiten Würfels
    private List<float> velocities2; // Liste zur Speicherung der Geschwindigkeiten des zweiten Würfels
    private List<float> momenta2; // Liste zur Speicherung der Impulse des zweiten Würfels

    private List<float> totalMomenta; // Liste zur Speicherung des Gesamtimpulses

    // Start is called before the first frame update
    void Start()
    {
        cubeRigidBody = GetComponent<Rigidbody>();
        cubeRigidBody2 = GameObject.Find("Cube2").GetComponent<Rigidbody>();
        wallRigidBody = GameObject.Find("Wall").GetComponent<Rigidbody>();
        initialPosition = cubeRigidBody.position.x;
        timeSeries = new List<List<float>>();

        positions1 = new List<float>();
        velocities1 = new List<float>();
        momenta1 = new List<float>();

        positions2 = new List<float>();
        velocities2 = new List<float>();
        momenta2 = new List<float>();

        totalMomenta = new List<float>();
    }

    // FixedUpdate is called once per fixed frame
    void FixedUpdate()
    {
        if (accel)
        {
            cubeRigidBody.AddForce(new Vector3(accelerationForce, 0f, 0f));

            if (Mathf.Abs(cubeRigidBody.velocity.x) >= velocityThreshold)
            {
                accel = false;
                cubeRigidBody.velocity = new Vector3(velocityThreshold, 0f, 0f);
            }
        }
        float springLocation = wallRigidBody.position.x - compressedLength;

        // If the cube has collided with the wall, apply a spring force
        if (cubeRigidBody.position.x >= springLocation && !isStuck)
        {
            // Calculate the spring force
            accel = false;
            springForce = -springConstant * cubeRigidBody.position.x;

            // Apply the spring force
            cubeRigidBody.AddForce(new Vector3(springForce, 0f, 0f));
        }

        currentTimeStep += Time.deltaTime;
        timeSeries.Add(new List<float>() { currentTimeStep, cubeRigidBody.position.x, cubeRigidBody.velocity.x, springForce });

        positions1.Add(cubeRigidBody.position.x);
        velocities1.Add(cubeRigidBody.velocity.x);
        momenta1.Add(cubeRigidBody.velocity.x * cubeRigidBody.mass);

        positions2.Add(cubeRigidBody2.position.x);
        velocities2.Add(cubeRigidBody2.velocity.x);
        momenta2.Add(cubeRigidBody2.velocity.x * cubeRigidBody2.mass);

        totalMomenta.Add(momenta1[momenta1.Count - 1] + momenta2[momenta2.Count - 1]);
    }

    void OnApplicationQuit()
    {
        WriteTimeSeriesToCsv();
    }

    void WriteTimeSeriesToCsv()
    {
        using var streamWriter = new StreamWriter("time_series_cube.csv");
        streamWriter.WriteLine("t,x(t)_cube1,v(t)_cube1,F(t)_cube1,x(t)_cube2,v(t)_cube2,P(t)_cube1,P(t)_cube2,P(t)_total");

        for (int i = 0; i < timeSeries.Count; i++)
        {
            List<string> rowData = new List<string>
            {
                timeSeries[i][0].ToString(),
                timeSeries[i][1].ToString(),
                timeSeries[i][2].ToString(),
                timeSeries[i][3].ToString(),
                positions2[i].ToString(),
                velocities2[i].ToString(),
                momenta1[i].ToString(),
                momenta2[i].ToString(),
                totalMomenta[i].ToString()
            };

            streamWriter.WriteLine(string.Join(",", rowData));
            streamWriter.Flush();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Cube2")
        {
            Debug.Log("Collision detected with Cube2");
            isStuck = true;
            accelerationForce = 0f;

            FixedJoint joint = cubeRigidBody.gameObject.AddComponent<FixedJoint>();

            ContactPoint contact = collision.contacts[0];
            joint.anchor = cubeRigidBody2.transform.InverseTransformPoint(contact.point);
            joint.connectedBody = collision.contacts[0].otherCollider.transform.GetComponent<Rigidbody>();
        }
    }
}
