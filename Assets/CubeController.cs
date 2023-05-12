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

    public float springK;   // N/m
    private float forceX;   // N
    private float startPos; // x-position

    private float currentTimeStep; // s

    private List<List<float>> timeSeries;

    public int pushForce; // N/m
    private bool accel = true;

    // Start is called before the first frame update
    void Start()
    {
        cubeRigidBody = GetComponent<Rigidbody>();
        timeSeries = new List<List<float>>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    // FixedUpdate can be called multiple times per frame
    void FixedUpdate()
    {
        if (Math.Abs(cubeRigidBody.velocity.x) > 2 && accel)
        {
            accel = false;
            pushForce = 0;
            cubeRigidBody.AddForce(new Vector3(cubeRigidBody.velocity.x * -1, 0f, 0f));
        }

        if (accel)
        {
            cubeRigidBody.AddForce(new Vector3(pushForce, 0f, 0f), ForceMode.Force);
        }

        float cubePosX = cubeRigidBody.position.x + cubeRigidBody.transform.localScale.x / 2;
        float deltaX = 0f;

        if (cubePosX > startPos)
        {
            deltaX = cubePosX - startPos;
            forceX = -deltaX * springK;
            cubeRigidBody.AddForce(new Vector3(forceX, 0f, 0f));
        }

        currentTimeStep += Time.deltaTime;
        timeSeries.Add(new List<float>() { currentTimeStep, cubeRigidBody.position.x, cubeRigidBody.velocity.x, pushForce });
        timeSeries.Add(new List<float>() { currentTimeStep, deltaX, forceX, cubeRigidBody.velocity.x });
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
        if (collision.rigidbody != cubeRigidBody) return;

        FixedJoint joint = gameObject.AddComponent<FixedJoint>();

        ContactPoint contact = collision.contacts[0];
        joint.anchor = transform.InverseTransformPoint(contact.point);
        joint.connectedBody = collision.contacts[0].otherCollider.transform.GetComponent<Rigidbody>();

        // Stops objects from continuing to collide and creating more joints
        joint.enableCollision = false;
    }
}
