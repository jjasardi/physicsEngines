using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System;

/*
    Accelerates the cube to which it is attached, modelling an harmonic oscillator.
    Writes the position, velocity and acceleration of the cube to a CSV file.
    
    Remark: For use in "Physics Engines" module at ZHAW, part of physics lab
    Author: jasarard, funhotho, unveryoh
    Version: 1.0
*/

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

    // Start is called before the first frame update
    void Start()
    {
        cubeRigidBody = GetComponent<Rigidbody>();
        cubeRigidBody2 = GameObject.Find("Cube2").GetComponent<Rigidbody>();
        wallRigidBody = GameObject.Find("Wall").GetComponent<Rigidbody>();
        initialPosition = cubeRigidBody.position.x;
        timeSeries = new List<List<float>>();
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
        float cubePosX = cubeRigidBody.position.x + cubeRigidBody.transform.localScale.x / 2;
        float deltaX = 0f;
        float springLocation = wallRigidBody.position.x - compressedLength;

        // If the cube has collided with the wall, apply a spring force
        if (cubeRigidBody.position.x >= springLocation && !isStuck)
        {
            // Calculate the spring force
            accel = false;
            deltaX = cubePosX - initialPosition;
            springForce = -springConstant * cubeRigidBody.position.x;

            // Apply the spring force
            cubeRigidBody.AddForce(new Vector3(springForce, 0f, 0f));
        }

        currentTimeStep += Time.deltaTime;
        timeSeries.Add(new List<float>() { currentTimeStep, cubeRigidBody.position.x, cubeRigidBody.velocity.x, springConstant});
        timeSeries.Add(new List<float>() { currentTimeStep, deltaX, springForce, cubeRigidBody.velocity.x });
        timeSeries.Add(new List<float>() { currentTimeStep, cubeRigidBody2.position.x, cubeRigidBody2.velocity.x });
    }

    void OnApplicationQuit()
    {
        WriteTimeSeriesToCsv();
    }

    void WriteTimeSeriesToCsv()
    {
        using var streamWriter = new StreamWriter("time_series_cube.csv");
        streamWriter.WriteLine("t,x(t),v(t),F(t) (added)");

        foreach (var timeStep in timeSeries)
        {
            streamWriter.WriteLine(string.Join(",", timeStep));
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
