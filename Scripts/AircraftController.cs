using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static AircraftPhysics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Barracuda;

public class AircraftController : Agent
{
    [SerializeField]
    Route route = null;
    [SerializeField]
    Vector3 wind = Vector3.zero;
    [SerializeField]
    float startForce = 0;
    [SerializeField]
    int currentCheckpointId = 0;
    [SerializeField]
    Mode mode = Mode.Flight;
    [SerializeField]
    NNModel flyingNN = null;
    [SerializeField]
    NNModel landingNN = null;
    [SerializeField]
    NNModel takeoffNN = null;
    [SerializeField]
    GameObject targetAirfield = null;

    private float pitch;
    private float roll;
    private float yaw;
    private float flap;
    private float thrustPercent;
    private float brakesTorque;

    AircraftPhysics aircraftPhysics;
    Vector3 startPosition;
    Quaternion startRotation;

    float checkpointDistance;
    float nextCheckpointDist;
    float lastVerticalSpeed = 0;
    enum Mode
    {
        Takeoff,
        Flight,
        Landing,
        Wait
    }

    enum State
    {
        Flying,
        Landed,
        Unknown
    }
    State state = State.Unknown;
    int stateLandedI = 0;
    int stateLandedN = 0;

    private void Start()
    {
        aircraftPhysics = GetComponent<AircraftPhysics>();
    }

    private void Update()
    {

    }

    private void FixedUpdate()
    {
        transform.Find("Text").gameObject.GetComponent<TextMesh>().text = Mathf.RoundToInt(aircraftPhysics.GetAircraftMetrics().speed).ToString();
        UpdateState();
        if (mode == Mode.Takeoff && state == State.Flying)
        {
            SetMode(Mode.Flight);
        }
        Debug.Log(lastVerticalSpeed = aircraftPhysics.rb.velocity.y);
    }

    private void UpdateState()
    {
        if (aircraftPhysics.GetGround())
        {
            stateLandedN += 1;
        }
        if (stateLandedI >= 50)
        {
            if (stateLandedN == 0)
            {
                state = State.Flying;
            }
            else
            {
                state = State.Landed;
            }
            stateLandedN = 0;
            stateLandedI = 0;
        }
        stateLandedI += 1;
    }

    public override void OnEpisodeBegin()
    {
        aircraftPhysics.SetWind(wind);
        aircraftPhysics.Teleport(route.GetCheckpointTransform(currentCheckpointId), startForce);
        nextCheckpointDist = GetNextCheckpointDist(currentCheckpointId);
        SetMode(mode);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        switch (mode)
        {
            case Mode.Flight:
                OnActionReceivedFlight(actions);
                break;
            case Mode.Takeoff:
                OnActionReceivedTakeoff(actions);
                break;
            case Mode.Landing:
                OnActionReceivedLanding(actions);
                break;
            case Mode.Wait:
                OnActionReceivedLanding(actions);
                break;
        }
    }

    void SetMode(Mode goal_mode)
    {
        mode = goal_mode;
        switch (mode)
        {
            case Mode.Flight:
                SetModel("Aircraft Learning", flyingNN);
                break;
            case Mode.Takeoff:
                SetModel("Aircraft Learning", takeoffNN);
                break;
            case Mode.Landing:
                SetModel("Aircraft Learning", landingNN);
                break;
            case Mode.Wait:
                SetModel("Aircraft Learning", null);
                break;
        }
        aircraftPhysics.SetControlSurfacesAngles(0, 0, 0, 0);
    }

    public void OnActionReceivedFlight(ActionBuffers actions)
    {
        float thrust = 1;
        

        float pitch = actions.DiscreteActions[1];
        if (pitch == 2) pitch = -1f;
        float roll = actions.DiscreteActions[2];
        if (roll == 2) roll = -1f;
        float yaw = actions.DiscreteActions[3];
        if (yaw == 2) yaw = -1f;
        float flaps = 0;
        float brakes = 0;
        if (currentCheckpointId > 16)
        {
            thrust = 0;
        }

        if (currentCheckpointId > 17)
        {
            flaps = 0.5F;
            brakes = 0.5F;
        }
        if (currentCheckpointId > 21 && currentCheckpointId < 23)
        {
            yaw -= 0.5F;
            roll += 0.2F;
        }
        if (currentCheckpointId > 20)
        {
            pitch += 0.1F;
        }



        aircraftPhysics.SetThrust(thrust);
        aircraftPhysics.SetControlSurfacesAngles(pitch, roll, yaw, flaps);
        aircraftPhysics.SetBrakesTorque(brakes);

        int nextCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
        Vector3 nextCheckpointDir = transform.InverseTransformDirection(route.GetCheckpointTransform(nextCheckpointId).position - transform.position);

        if (nextCheckpointDir.magnitude < 30)
        {
            currentCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
        }

    }

    public void OnActionReceivedTakeoff(ActionBuffers actions)
    {
        float thrust = 1;
        float pitch = actions.DiscreteActions[1];
        if (pitch == 2) pitch = -1f;
        float roll = 0;
        float yaw = actions.DiscreteActions[3];
        if (yaw == 2) yaw = -1f;
        float flaps = actions.DiscreteActions[4];
        if (flaps == 2) flaps = 0.5F;
        float brakes = 0;

        aircraftPhysics.SetThrust(thrust);
        aircraftPhysics.SetControlSurfacesAngles(pitch, roll, yaw, flaps);
        aircraftPhysics.SetBrakesTorque(brakes);

        int nextCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
        Vector3 nextCheckpointDir = transform.InverseTransformDirection(route.GetCheckpointTransform(nextCheckpointId).position - transform.position);

        if (nextCheckpointDir.magnitude < 30)
        {
            currentCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
        }
    }

    public void OnActionReceivedLanding(ActionBuffers actions)
    {
        float thrust = actions.DiscreteActions[0];
        float pitch = actions.DiscreteActions[1];
        if (pitch == 2) pitch = -1f;
        float roll = actions.DiscreteActions[2];
        if (roll == 2) roll = -1f;
        float yaw = actions.DiscreteActions[3];
        if (yaw == 2) yaw = -1f;
        float flaps = actions.DiscreteActions[4];
        if (flaps == 2) flaps = 0.5F;
        float brakes = actions.DiscreteActions[5];

        aircraftPhysics.SetThrust(thrust);
        aircraftPhysics.SetControlSurfacesAngles(pitch, roll, yaw, flaps);
        aircraftPhysics.SetBrakesTorque(brakes);

        int nextCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
        Vector3 nextCheckpointDir = transform.InverseTransformDirection(route.GetCheckpointTransform(nextCheckpointId).position - transform.position);

        if (nextCheckpointDir.magnitude < 30)
        {
            currentCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.InverseTransformDirection(gameObject.GetComponent<Rigidbody>().velocity));
        sensor.AddObservation(GetNextCheckpointDir(currentCheckpointId));
        sensor.AddObservation(GetNextCheckpointOr(currentCheckpointId));
        sensor.AddObservation(transform.InverseTransformDirection(aircraftPhysics.GetWind()));
    }


    private Vector3 GetNextCheckpointDir(int id)
    {
        int nextCheckpointId = route.GetNextCheckpointId(id);
        Vector3 nextCheckpointDir = transform.InverseTransformDirection(
                                            route.GetCheckpointTransform(nextCheckpointId).position
                                            - transform.position);
        return nextCheckpointDir;
    }

    private float GetNextCheckpointDist(int id)
    {
        int nextCheckpointId = route.GetNextCheckpointId(id);
        float nextCheckpointDist = (route.GetCheckpointTransform(nextCheckpointId).position
                                            - transform.position).magnitude;
        return nextCheckpointDist;
    }

    private Vector3 GetNextCheckpointOr(int id)
    {
        int nextCheckpointId = route.GetNextCheckpointId(id);
        Vector3 nextCheckpointOr = transform.InverseTransformDirection(
                            route.GetCheckpointTransform(nextCheckpointId).forward);
        return nextCheckpointOr;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 velocity = collision.relativeVelocity;
        Debug.Log(velocity.magnitude);
    }


    private void OnTriggerEnter(Collider other)
    {
        int nextCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
        if (other.transform.CompareTag("Checkpoint") && other.gameObject == route.GetCheckpointTransform(nextCheckpointId).gameObject)
        {
            currentCheckpointId = nextCheckpointId;
            AddReward(1F);
            nextCheckpointDist = GetNextCheckpointDist(currentCheckpointId);
        }
    }

    private LearningController controller;
    private int id;

    public void AddToController(LearningController controller, int id)
    {
        this.controller = controller;
        this.id = id;
    }
    public int GetId()
    {
        return this.id;
    }
}
