using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static AircraftPhysics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class AgentController : Agent
{
    [SerializeField]
    TrainingType training = TrainingType.NoTrain;
    [SerializeField]
    Route route = null;
    [SerializeField]
    bool randomSpawn = false;
    [SerializeField]
    bool randomWind = false;
    [SerializeField]
    float randomWindStreight = 0;
    [SerializeField]
    float startForce;

    [SerializeField]
    int flightStepTimeout = 1000;
    [SerializeField]
    int landingStepTimeout = 3000;
    [SerializeField]
    int takeoffStepTimeout = 3000;

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
    int currentCheckpointId;
    float nextCheckpointDist;

    private float nextStepTimeout;
    private int stepTimeout = 0;

    Vector3 lastVelocity = Vector3.zero;
     
    enum State
    {
        Flying,
        Landed,
        Unknown
    }
    State state = State.Unknown;
    int stateLandedI = 0;
    int stateLandedN = 0;
    enum TrainingType
    {
        Takeoff,
        Flight,
        Landing,
        NoTrain
    }
    

    private void Start()
    {
        aircraftPhysics = GetComponent<AircraftPhysics>();
    }


    private void Update()
    {
    
    }

    private void FixedUpdate()
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


        Vector3 g = new Vector3(0, -9.81F, 0);
        if (lastVelocity == Vector3.zero)
        {
            lastVelocity = aircraftPhysics.rb.velocity;
        }
        float acceleration = ((aircraftPhysics.rb.velocity - lastVelocity + 
                                            g * (Time.fixedDeltaTime)).magnitude / Time.fixedDeltaTime)/ 9.81F;
        lastVelocity = aircraftPhysics.rb.velocity;
        transform.Find("Text").gameObject.GetComponent<TextMesh>().text = 
                            (Mathf.Round(acceleration * 10f) / 10f).ToString() + " G";
    }

    public override void OnEpisodeBegin()
    {
        if (training != TrainingType.NoTrain)
        {
            nextStepTimeout = StepCount + stepTimeout;
        }

        switch (training)
        {
            case TrainingType.Flight:
                stepTimeout = flightStepTimeout;
                break;
            case TrainingType.Takeoff:
                stepTimeout = takeoffStepTimeout;
                state = State.Landed;
                break;
            case TrainingType.Landing:
                stepTimeout = landingStepTimeout;
                break;
            case TrainingType.NoTrain:
                break;
        }

        currentCheckpointId = 0;
        if (randomSpawn)
        {
            currentCheckpointId = Random.Range(0, route.GetCheckpointCount() - 1);
        }

        if (randomWind)
        {
            Vector3 Wind = new Vector3(UnityEngine.Random.Range(randomWindStreight * 0.75F, randomWindStreight) 
                                                                * ((UnityEngine.Random.Range(0, 2) - 0.5F) * 2),
                                       0,
                                       UnityEngine.Random.Range(randomWindStreight * 0.75F, randomWindStreight) 
                                                                * ((UnityEngine.Random.Range(0, 2) - 0.5F) * 2));
            aircraftPhysics.SetWind(Wind);
        }
        else
        {
            aircraftPhysics.SetWind(Vector3.zero);
        }
        aircraftPhysics.Teleport(route.GetCheckpointTransform(currentCheckpointId), startForce);
        nextCheckpointDist = GetNextCheckpointDist(currentCheckpointId);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        // Thrust
        discreteActions[0] = Input.GetKey(KeyCode.Space) == true ? 1 : 0;
        // Pitch
        discreteActions[1] = 0;
        if (Input.GetAxis("Vertical") > 0)
        {
            discreteActions[1] = 1;
        }
        else if (Input.GetAxis("Vertical") < 0)
        {
            discreteActions[1] = -1;
        }
        // Roll
        discreteActions[2] = 0;
        if (Input.GetAxis("Horizontal") > 0)
        {
            discreteActions[2] = 1;
        }
        else if (Input.GetAxis("Horizontal") < 0)
        {
            discreteActions[2] = -1;
        }
        // Yaw
        discreteActions[3] = 0;
        if (Input.GetAxis("Yaw") > 0)
        {
            discreteActions[3] = 1;
        }
        else if (Input.GetAxis("Yaw") < 0)
        {
            discreteActions[3] = -1;
        }
        // Flaps
        discreteActions[4] = Input.GetKey(KeyCode.F) == true ? 1 : 0;
        // Brakes
        discreteActions[5] = Input.GetKey(KeyCode.B) == true ? 1 : 0;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        switch (training)
        {
            case TrainingType.Flight:
                OnActionReceivedFlight(actions);
                break;
            case TrainingType.NoTrain:
                OnActionReceivedNoTrain(actions);
                break;
            case TrainingType.Takeoff:
                OnActionReceivedTakeoff(actions);
                break;
            case TrainingType.Landing:
                OnActionReceivedLanding(actions);
                break;
        }
    }

    public void OnActionReceivedNoTrain(ActionBuffers actions)
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

    public void OnActionReceivedFlight(ActionBuffers actions)
    {
        float thrust = actions.DiscreteActions[0];
        float pitch = actions.DiscreteActions[1];
        if (pitch == 2) pitch = -1f;
        float roll = actions.DiscreteActions[2];
        if (roll == 2) roll = -1f;
        float yaw = actions.DiscreteActions[3];
        if (yaw == 2) yaw = -1f;
        float flaps = actions.DiscreteActions[4];
        float brakes = actions.DiscreteActions[5];

        aircraftPhysics.SetThrust(thrust);
        aircraftPhysics.SetControlSurfacesAngles(pitch, roll, yaw, flaps);
        aircraftPhysics.SetBrakesTorque(brakes);

        AddReward(((aircraftPhysics.GetAircraftMetrics().speed / 80) - 1.5f) / 5000);

        if (StepCount > nextStepTimeout)
        {
            AddReward(-0.5F);
            controller.AgentUpdateDifficulty(GetCumulativeReward());
            EndEpisode();
        }

        int nextCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
        Vector3 nextCheckpointDir = transform.InverseTransformDirection(route.GetCheckpointTransform(nextCheckpointId).position - transform.position);
        nextCheckpointDist = nextCheckpointDir.magnitude;

        if (nextCheckpointDir.magnitude < controller.AgentGetDifficulty())
        {
            currentCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
            AddReward(1F);
            nextStepTimeout = StepCount + stepTimeout;
        }

    }

    public void OnActionReceivedTakeoff(ActionBuffers actions)
    {
        float thrust = actions.DiscreteActions[0];
        float pitch = actions.DiscreteActions[1];
        if (pitch == 2) pitch = -1f;
        float roll = 0;
        float yaw = actions.DiscreteActions[3];
        if (yaw == 2) yaw = -1f;
        float flaps = actions.DiscreteActions[4];
        if (flaps == 2) flaps = 0.5F;
        float brakes = actions.DiscreteActions[5];

        aircraftPhysics.SetThrust(thrust);
        aircraftPhysics.SetControlSurfacesAngles(pitch, roll, yaw, flaps);
        aircraftPhysics.SetBrakesTorque(brakes);
        AddReward(((aircraftPhysics.GetAircraftMetrics().speed/10) - 1f) / 5000);

        if (StepCount > nextStepTimeout)
        {
            AddReward(-0.5F);
            EndEpisode();
        }
        if (state == State.Flying)
        {
            AddReward(aircraftPhysics.GetAircraftMetrics().speed / 40);
            EndEpisode();
        }
        Collider coll = aircraftPhysics.GetGround();
        if (coll != null && !coll.transform.CompareTag("Airfield"))
        {
            AddReward(-1F);
            EndEpisode();
        }
        int nextCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
        Vector3 nextCheckpointDir = transform.InverseTransformDirection(
                                    route.GetCheckpointTransform(nextCheckpointId).position 
                                    - transform.position);
        nextCheckpointDist = nextCheckpointDir.magnitude;

        if (nextCheckpointDir.magnitude < controller.AgentGetDifficulty())
        {
            currentCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
            AddReward(0.25F);
            nextStepTimeout = StepCount + stepTimeout;
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

        AddReward((-(aircraftPhysics.GetAircraftMetrics().speed / 40)) / stepTimeout);

        if (StepCount > nextStepTimeout)
        {
            AddReward(-0.5F);
            EndEpisode();
        }

        Collider coll = aircraftPhysics.GetGround();
        if (coll != null && !coll.transform.CompareTag("Airfield"))
        {
            AddReward(-1F);
            EndEpisode();
        }

        if (state == State.Landed && aircraftPhysics.GetAircraftMetrics().speed < 5)
        {
            AddReward(+1F);
            EndEpisode();
        }

        int nextCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
        Vector3 nextCheckpointDir = transform.InverseTransformDirection(
                                    route.GetCheckpointTransform(nextCheckpointId).position 
                                    - transform.position);
        nextCheckpointDist = nextCheckpointDir.magnitude;

        if (nextCheckpointDir.magnitude < controller.AgentGetDifficulty())
        {
            currentCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
            AddReward(0.5F);
            nextStepTimeout = StepCount + stepTimeout;
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
        if (!collision.transform.CompareTag("Agent") || !collision.transform.CompareTag("Airfield"))
        {
            if (training != TrainingType.NoTrain)
            {
                AddReward(-1F);
                controller.AgentUpdateDifficulty(GetCumulativeReward());
                //Debug.Log("Collision");
                EndEpisode();
                return;
            }
        }
    }

    
    private void OnTriggerEnter(Collider other)
    {
        int nextCheckpointId = route.GetNextCheckpointId(currentCheckpointId);
        if (other.transform.CompareTag("Checkpoint") && other.gameObject == route.GetCheckpointTransform(nextCheckpointId).gameObject)
        {
            currentCheckpointId = nextCheckpointId;
            AddReward(1F);
            nextStepTimeout = StepCount + stepTimeout;
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
