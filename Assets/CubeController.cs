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

    private Vector3 anchorPoint;
    private Boolean craneCableActiv = false;
    private Vector3 differenceVector;
    private float alpha;
    private float gravityAccelaration = 9.81f;
    private float ropeLength = 6.0f; //Seillänge
    private float startTime;

    private float initialPosition;
    private float currentTimeStep;
    private List<List<float>> timeSeries;

    // Start is called before the first frame update
    void Start()
    {
        cubeRigidBody = GetComponent<Rigidbody>();
        cubeRigidBody2 = GetComponent<Rigidbody>();
        //cubeRigidBody2 = GameObject.Find("Cube2").GetComponent<Rigidbody>();
        wallRigidBody = GameObject.Find("Wall").GetComponent<Rigidbody>();
        initialPosition = cubeRigidBody.position.x;
        timeSeries = new List<List<float>>();
    }

    // FixedUpdate is called once per fixed frame
    void FixedUpdate()
    {
        float cubePosX = cubeRigidBody.position.x + cubeRigidBody.transform.localScale.x / 2;
        float deltaX = 0f;
        float springLocation = wallRigidBody.position.x - compressedLength;

        if (accel)
        {
            cubeRigidBody.AddForce(new Vector3(accelerationForce, 0f, 0f));

            if (Mathf.Abs(cubeRigidBody.velocity.x) >= velocityThreshold)
            {
                accel = false;
                cubeRigidBody.velocity = new Vector3(velocityThreshold, 0f, 0f);
            }
        }

        // Before cube 1 has collided with the wall, apply a spring force
        if (cubeRigidBody.position.x >= springLocation && !isStuck && !craneCableActiv)
        {
            // Calculate the spring force
            accel = false;
            deltaX = cubePosX - initialPosition;
            springForce = -springConstant * cubeRigidBody.position.x;

            // Apply the spring force
            cubeRigidBody.AddForce(new Vector3(springForce, 0f, 0f));
        }

        if (cubeRigidBody.position.x <= -10 && !craneCableActiv)  //+ boolean damit nur 1 anchorpoint gesetzt wird
        {

            // anchorPoint = x pos, z pos gleich wie cube, y pos + 6.0f
            //boolean aktiv = true;
            anchorPoint = new Vector3(cubeRigidBody.position.x, (cubeRigidBody.position.y + ropeLength), cubeRigidBody.position.z);
            craneCableActiv = true;
            startTime = currentTimeStep;
        }

        // if (seilkraft aktiv) bool von oben {
        // alpha berechnen
        // differenceVector = anchor - rigidCube1.position
        // alpha = Mathf.Atan2(differenceVector.x, differenceVector.y)
        // }

        if (craneCableActiv)
        {
            differenceVector = anchorPoint - cubeRigidBody.position;
            //alpha = Mathf.Atan2(differenceVector.y, differenceVector.x);
            alpha = Mathf.Atan(differenceVector.y / differenceVector.x);

            //gewichtskraft berechnen //radialer anteil Formel
            //Mathf.abs()
            float gravitationalForce = Mathf.Abs(cubeRigidBody.mass * gravityAccelaration * Mathf.Cos(alpha));

            //zentripetal formel
            // mathf.abs(mass * mathf.pow(cube1.velocity.magnitude, 2)/R)...  R = seillänge 6meter
            float centripetalForce = Mathf.Abs(cubeRigidBody.mass * Mathf.Pow(cubeRigidBody.velocity.magnitude, 2) / ropeLength);

            //komponenten
            //Fhori
            //FVert
            float fHori = (gravitationalForce + centripetalForce) * Mathf.Sin(alpha);
            float fVert = (gravitationalForce + centripetalForce) * Mathf.Cos(alpha);

            //Friction:
            Vector3 unitVelocityX = cubeRigidBody.velocity / Mathf.Abs(cubeRigidBody.velocity.x);
            Vector3 unitVelocityY = cubeRigidBody.velocity / Mathf.Abs(cubeRigidBody.velocity.y);
            float SurfaceX = 2.25f;
            float SurfaceY = SurfaceX;
            float frictionForceX = -0.5f * SurfaceX * 1.2f * 1.1f * Mathf.Pow(Mathf.Abs(cubeRigidBody.velocity.x), 2) * unitVelocityX.x;
            float frictionForceY = -0.5f * SurfaceY * 1.2f * 1.1f * Mathf.Pow(Mathf.Abs(cubeRigidBody.velocity.y), 2) * unitVelocityY.y;
            Vector3 frictionForce = new Vector3(frictionForceX, frictionForceY, 0f);
            cubeRigidBody.AddForce(frictionForce);
            cubeRigidBody2.AddForce(frictionForce);

            Debug.Log($"fHori{fHori},\t fVerti:{fVert}, \t alpha:{alpha}, \t startTime:{currentTimeStep - startTime}");

            //cube1.addforce
            //cube2.addForce(Fhori,FVert,)
            cubeRigidBody.AddForce(new Vector3(fHori, fVert, 0f));
            cubeRigidBody2.AddForce(new Vector3(fHori, fVert, 0f));
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
