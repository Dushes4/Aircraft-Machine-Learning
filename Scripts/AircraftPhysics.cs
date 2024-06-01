using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AircraftPhysics : MonoBehaviour
{
    const float PREDICTION_TIMESTEP_FRACTION = 0.5f;

    [SerializeField]
    List<AeroSurface> aerodynamicSurfaces = null;
    [SerializeField]
    List<AeroSurface> controlSurfaces = null;
    [SerializeField]
    List<BasicEngine> engines = null;
    [SerializeField]
    List<WheelCollider> wheels = null;

    public Rigidbody rb;
    BiVector3 currentForceAndTorque;

    [SerializeField]
    float rollControlSensitivity = 0.2f;
    [SerializeField]
    float pitchControlSensitivity = 0.2f;
    [SerializeField]
    float yawControlSensitivity = 0.2f;
    [SerializeField]
    float flapControlSensitivity = 0.3f;
    [SerializeField]
    float powerConvertRatio = 16.66F;
    [SerializeField]
    float fuel = 5300F;
    private Vector3 wind;
    [SerializeField]
    float temp = 15F;
    [SerializeField]

    [Range(0, 1)]
    private float brakesTorque;
    [Range(-1, 1)]
    private float pitch;
    [Range(-1, 1)]
    private float yaw;
    [Range(-1, 1)]
    private float roll;
    [Range(0, 1)]
    private float flap;

    private bool teleported;

    public struct AircraftMetrics
    {
        public float thrust;
        public float revs;
        public float power;
        public float altitude;
        public float speed;
        public float pitch;
        public float roll;
        public float yaw;
        public float flap;
        public Vector3 force;
        public Vector3 torque;
    }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    private void FixedUpdate()
    {
        BurnFuel();
        UpdateControlSurfacesAngles();
        if (!teleported)
        {
            MoveAircraft();
        }
        teleported = false;
        ApplyBrakes();
    }

    public void Teleport(Transform place, float force)
    {
        SetControlSurfacesAngles(0,0,0,0);
        SetThrust(1);
        engines[0].SetRevs(2700);

        rb.position = place.position;
        rb.rotation = place.rotation;
        rb.velocity = place.forward * force;
        rb.angularVelocity = Vector3.zero;
        teleported = true;
    }


    public AircraftMetrics GetAircraftMetrics()
    {
        AircraftMetrics metrics = new AircraftMetrics();
        metrics.thrust = engines[0].GetEngineMetrics().throttle;
        metrics.revs = engines[0].GetEngineMetrics().revs;
        metrics.power = engines[0].GetPower();
        metrics.altitude = rb.transform.position.y;
        metrics.speed = rb.velocity.magnitude;
        metrics.pitch = pitch;
        metrics.roll = roll;
        metrics.yaw = yaw;
        metrics.flap = flap;
        return metrics;
    }
    public void SetThrust(float thrust)
    {
        foreach (var engine in engines)
        {
            engine.SetThrottle(thrust);
        }
    }
    public void SetWind(Vector3 wind)
    {
        this.wind = wind;
    }

    public void SetThrust(float thrust, int engineNum)
    {
        if (engines.Count >= engineNum)
        {
            engines[engineNum - 1].SetThrottle(thrust);
        }
    }
    public void SetControlSurfacesAngles(float pitch, float roll, float yaw, float flap)
    {
        this.pitch = pitch;
        this.roll = roll;
        this.yaw = yaw;
        this.flap = flap;
    }
    public void SetBrakesTorque(float brake)
    {
        brakesTorque = brake;
    }

    public Collider GetGround()
    {
        WheelHit hit;
        foreach (WheelCollider wheel in wheels)
        {
            if (wheel.GetGroundHit(out hit))
            {
                return hit.collider;
            }
        }
        return null;
    }

    private float GetTemp()
    {
        return temp;
    }

    private float GetAirDensity()
    {

        float uMinus = 0.029F;
        float g = 9.81F;
        float h = rb.transform.position.y;
        float R = 8.31F;
        float T = GetTemp() + 273;
        float p0 = 1.2F;
        float e = 2.7183F;

        return (p0 * Mathf.Pow(e, ((-uMinus * g * h) / (R * T))));
    }

    public Vector3 GetWind()
    {
        if (rb.transform.position.y > 1)
        {
            return wind;
        }
        else
        {
            return Vector3.zero;
        }
    }

    private void MoveAircraft()
    {
        float totalForce = 0;
        Vector3 total_engine_torque = Vector3.zero;
        foreach (var engine in engines)
        {
            totalForce += engine.GetPower();
            total_engine_torque += Vector3.Cross(engine.transform.position - rb.worldCenterOfMass, engine.GetPower() * powerConvertRatio * engine.transform.forward);

        }
        totalForce *= powerConvertRatio;

        BiVector3 forceAndTorqueThisFrame;

        forceAndTorqueThisFrame = CalculateAerodynamicForces(rb.velocity, rb.angularVelocity, GetWind(), GetAirDensity(), rb.worldCenterOfMass);
        Vector3 velocityPrediction = PredictVelocity(forceAndTorqueThisFrame.p + transform.forward * totalForce + Physics.gravity * rb.mass);

        
        Vector3 angularVelocityPrediction = PredictAngularVelocity(forceAndTorqueThisFrame.q + total_engine_torque);
        //Debug.DrawRay(transform.position, total_engine_torque, Color.yellow);



        BiVector3 forceAndTorquePrediction = CalculateAerodynamicForces(velocityPrediction, angularVelocityPrediction, GetWind(), GetAirDensity(), rb.worldCenterOfMass);
        currentForceAndTorque = (forceAndTorqueThisFrame + forceAndTorquePrediction) * 0.5f;

        rb.AddTorque(currentForceAndTorque.q);
        rb.AddForce(currentForceAndTorque.p);

        foreach (var engine in engines)
        {
            rb.AddForceAtPosition(engine.transform.forward * engine.GetPower() * powerConvertRatio, engine.transform.position);
            Debug.DrawRay(engine.transform.position, engine.transform.forward * engine.GetPower(), Color.yellow);
        }

    }
    private void BurnFuel()
    {
        foreach (var engine in engines)
        {
            fuel -= engine.GetFuelСonsumption();
        }
    }
    private void ApplyBrakes()
    {
        foreach (var wheel in wheels)
        {
            wheel.brakeTorque = brakesTorque;
            // small torque to wake up wheel collider
            wheel.motorTorque = 0.01f;
        }
    }
    private void UpdateControlSurfacesAngles()
    {
        foreach (var surface in controlSurfaces)
        {
            if (surface == null || !surface.IsControlSurface) continue;
            switch (surface.InputType)
            {
                case ControlInputType.Pitch:
                    surface.SetFlapAngle(pitch * pitchControlSensitivity * surface.InputMultiplyer);
                    break;
                case ControlInputType.Roll:
                    surface.SetFlapAngle(roll * rollControlSensitivity * surface.InputMultiplyer);
                    break;
                case ControlInputType.Yaw:
                    surface.SetFlapAngle(yaw * yawControlSensitivity * surface.InputMultiplyer);
                    break;
                case ControlInputType.Flap:
                    surface.SetFlapAngle(flap * flapControlSensitivity * surface.InputMultiplyer);
                    break;
            }
        }
    }
    private BiVector3 CalculateAerodynamicForces(Vector3 velocity, Vector3 angularVelocity, Vector3 wind, float airDensity, Vector3 centerOfMass)
    {
        BiVector3 forceAndTorque = new BiVector3();
        foreach (var surface in aerodynamicSurfaces)
        {
            Vector3 relativePosition = surface.transform.position - centerOfMass;
            forceAndTorque += surface.CalculateForces(-velocity + wind
                -Vector3.Cross(angularVelocity,
                relativePosition),
                airDensity, relativePosition);
        }
        return forceAndTorque;
    }

    private Vector3 PredictVelocity(Vector3 force)
    {
        return rb.velocity + Time.fixedDeltaTime * PREDICTION_TIMESTEP_FRACTION * force / rb.mass;
    }

    private Vector3 PredictAngularVelocity(Vector3 torque)
    {
        Quaternion inertiaTensorWorldRotation = rb.rotation * rb.inertiaTensorRotation;
        Vector3 torqueInDiagonalSpace = Quaternion.Inverse(inertiaTensorWorldRotation) * torque;
        Vector3 angularVelocityChangeInDiagonalSpace;
        angularVelocityChangeInDiagonalSpace.x = torqueInDiagonalSpace.x / rb.inertiaTensor.x;
        angularVelocityChangeInDiagonalSpace.y = torqueInDiagonalSpace.y / rb.inertiaTensor.y;
        angularVelocityChangeInDiagonalSpace.z = torqueInDiagonalSpace.z / rb.inertiaTensor.z;

        return rb.angularVelocity + Time.fixedDeltaTime * PREDICTION_TIMESTEP_FRACTION
            * (inertiaTensorWorldRotation * angularVelocityChangeInDiagonalSpace);
    }

#if UNITY_EDITOR
    // For gizmos drawing.
    public void CalculateCenterOfLift(out Vector3 center, out Vector3 force, Vector3 displayAirVelocity, float displayAirDensity)
    {
        Vector3 com;
        BiVector3 forceAndTorque;
        if (aerodynamicSurfaces == null)
        {
            center = Vector3.zero;
            force = Vector3.zero;
            return;
        }

        if (rb == null)
        {
            com = GetComponent<Rigidbody>().worldCenterOfMass;
            forceAndTorque = CalculateAerodynamicForces(-displayAirVelocity, Vector3.zero, Vector3.zero, displayAirDensity, com);
        }
        else
        {
            com = rb.worldCenterOfMass;
            forceAndTorque = currentForceAndTorque;
        }

        force = forceAndTorque.p;
        center = com + Vector3.Cross(forceAndTorque.p, forceAndTorque.q) / forceAndTorque.p.sqrMagnitude;
    }
#endif
}


