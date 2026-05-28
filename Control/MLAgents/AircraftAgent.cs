using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

public class AircraftAgent : Agent
{
    [SerializeField] private TrainingType training = TrainingType.NoTrain;
    [SerializeField] private bool randomSpawn = false;
    [SerializeField] private bool randomWind = false;
    [SerializeField] private float randomWindStreight = 0;
    [SerializeField] private float startForce;

    [Header("Input System (Heuristic)")]
    [SerializeField] private InputActionAsset aircraftActions;

    [Header("Dependencies")]
    [SerializeField] private AircraftActuator actuator;
    [SerializeField] private AircraftActionDecoder actionDecoder;
    [SerializeField] private RouteTracker routeTracker;
    [SerializeField] private AircraftPhysics aircraftPhysics;
    [SerializeField] private GroundStateTracker groundStateTracker;
    [SerializeField] private TextMesh hudText;

    [SerializeField] private int flightStepTimeout = 1000;
    [SerializeField] private int landingStepTimeout = 3000;
    [SerializeField] private int takeoffStepTimeout = 3000;
    [SerializeField] private float progressRewardScale = 0.01f;
    [SerializeField] private float distanceRegressionPenalty = 0.02f;
    [SerializeField] private float alignmentRewardScale = 0.001f;
    [SerializeField] private int checkpointsForSuccess = 5;

    private InputActionMap aircraftMap;
    private InputAction thrustA;
    private InputAction pitchA;
    private InputAction rollA;
    private InputAction yawA;
    private InputAction flapsA;
    private InputAction brakesA;

    private float nextCheckpointDist;
    private float previousCheckpointDist;
    private float nextStepTimeout;
    private int stepTimeout = 0;
    private int checkpointStreak = 0;

    private Vector3 lastVelocity = Vector3.zero;
    private LearningController controller;
    private int id;

    private float gSum = 0f;
    private int gSamples = 0;
    private float flightStartTime = 0f;

    private float totalCheckpointDeviation = 0f;
    private float lastCheckpointDeviation = 0f;

    [SerializeField] private float postCheckpointTrackSeconds = 1.0f;
    private bool pendingDeviation = false;
    private Vector3 pendingCheckpointPos;
    private float pendingMinDist;
    private float pendingCommitTime;

    private Vector3 lastAction = Vector3.zero;
    private Vector3 lastAngularVelocity = Vector3.zero;
    private float lastAlignment = 0f;
    private float lastVerticalSpeed = 0f;
    public float targetLandingSpeed = 5f;
    public bool offRunway = false;

    private enum TrainingType
    {
        Takeoff,
        Flight,
        Landing,
        NoTrain
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (aircraftActions == null) return;

        aircraftMap = aircraftActions.FindActionMap("Aircraft", true);
        thrustA = aircraftMap.FindAction("Thrust", true);
        pitchA = aircraftMap.FindAction("Pitch", true);
        rollA = aircraftMap.FindAction("Roll", true);
        yawA = aircraftMap.FindAction("Yaw", true);
        flapsA = aircraftMap.FindAction("Flaps", true);
        brakesA = aircraftMap.FindAction("Brakes", true);

        aircraftMap.Enable();
    }

    protected override void OnDisable()
    {
        aircraftMap?.Disable();
        base.OnDisable();
    }

    private void Awake()
    {
        actuator ??= GetComponent<AircraftActuator>();
        actionDecoder ??= GetComponent<AircraftActionDecoder>();
        routeTracker ??= GetComponent<RouteTracker>();
        aircraftPhysics ??= GetComponent<AircraftPhysics>();
        groundStateTracker ??= GetComponent<GroundStateTracker>();
        hudText ??= transform.Find("Text")?.GetComponent<TextMesh>();
    }

    private void FixedUpdate()
    {
        if (aircraftPhysics == null) return;

        Vector3 g = new Vector3(0, -9.81F, 0);
        if (lastVelocity == Vector3.zero)
        {
            lastVelocity = aircraftPhysics.rb.linearVelocity;
        }

        float gForce =
            ((aircraftPhysics.rb.linearVelocity - lastVelocity + g * (Time.fixedDeltaTime)).magnitude / Time.fixedDeltaTime) / 9.81F;

        lastVelocity = aircraftPhysics.rb.linearVelocity;

        gSum += gForce;
        gSamples++;

        UpdatePendingDeviation();

        if (hudText != null)
        {
            hudText.text = $"ID:{id} Ep:{CompletedEpisodes} Step:{StepCount} R:{GetCumulativeReward():0.000}";
        }
    }

    private void UpdatePendingDeviation()
    {
        if (!pendingDeviation) return;

        float d = Vector3.Distance(transform.position, pendingCheckpointPos);
        if (d < pendingMinDist) pendingMinDist = d;

        if (Time.time >= pendingCommitTime)
        {
            pendingDeviation = false;

            lastCheckpointDeviation = pendingMinDist;
            totalCheckpointDeviation += pendingMinDist;

            LogCheckpointStats();
        }
    }

    private void BeginDeviationCaptureForCheckpoint(Vector3 checkpointCenter)
    {
        if (pendingDeviation)
        {
            pendingDeviation = false;
            lastCheckpointDeviation = pendingMinDist;
            totalCheckpointDeviation += pendingMinDist;
        }

        pendingDeviation = true;
        pendingCheckpointPos = checkpointCenter;
        pendingMinDist = Vector3.Distance(transform.position, checkpointCenter);
        pendingCommitTime = Time.time + Mathf.Max(0.01f, postCheckpointTrackSeconds);
    }

    private void CompleteEpisode(bool success)
    {
        controller?.AgentUpdateDifficulty(GetCumulativeReward());
        EndEpisode();
        checkpointStreak = 0;
    }

    public override void OnEpisodeBegin()
    {
        switch (training)
        {
            case TrainingType.Flight:
                stepTimeout = flightStepTimeout;
                break;
            case TrainingType.Takeoff:
                stepTimeout = takeoffStepTimeout;
                break;
            case TrainingType.Landing:
                stepTimeout = landingStepTimeout;
                break;
            case TrainingType.NoTrain:
                stepTimeout = 0;
                break;
        }

        if (training != TrainingType.NoTrain)
        {
            nextStepTimeout = StepCount + stepTimeout;
        }

        checkpointStreak = 0;
        previousCheckpointDist = 0f;

        gSum = 0f;
        gSamples = 0;
        flightStartTime = Time.time;

        totalCheckpointDeviation = 0f;
        lastCheckpointDeviation = 0f;

        pendingDeviation = false;
        pendingMinDist = 0f;
        pendingCommitTime = 0f;
        pendingCheckpointPos = Vector3.zero;

        if (randomWind)
        {
            Vector3 wind = new Vector3(
                Random.Range(randomWindStreight * 0.75F, randomWindStreight) * ((Random.Range(0, 2) - 0.5F) * 2),
                0,
                Random.Range(randomWindStreight * 0.75F, randomWindStreight) * ((Random.Range(0, 2) - 0.5F) * 2)
            );
            aircraftPhysics.SetWind(wind);
        }
        else
        {
            aircraftPhysics.SetWind(Vector3.zero);
        }

        ResetRouteState();
    }

    private static float NormalizeThrust01(float v)
    {
        if (v < 0f) v = (v + 1f) * 0.5f;
        return Mathf.Clamp01(v);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;

        if (aircraftMap == null)
        {
            for (int i = 0; i < c.Length; i++) c[i] = 0f;
            return;
        }

        float pitch = pitchA.ReadValue<float>();
        float roll = rollA.ReadValue<float>();
        float yaw = yawA.ReadValue<float>();

        if (c.Length > 0) c[0] = Mathf.Clamp(pitch, -1f, 1f);
        if (c.Length > 1) c[1] = Mathf.Clamp(roll, -1f, 1f);
        if (c.Length > 2) c[2] = Mathf.Clamp(yaw, -1f, 1f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!IsReadyForControl()) return;

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

    private void LogCheckpointStats()
    {
        float flightTime = Time.time - flightStartTime;
        float avgG = gSamples > 0 ? (gSum / gSamples) : 0f;

        Debug.Log(
            $"[Agent {id}] " +
            $"Time: {flightTime:F2}s | " +
            $"Avg G: {avgG:F2} | " +
            $"Last Dev: {lastCheckpointDeviation:F2} m | " +
            $"Total Dev: {totalCheckpointDeviation:F2} m | " +
            $"CheckpointStreak: {checkpointStreak}"
        );

        Debug.Log(
            $"[Agent {id}] " +
            $"Target hit deviation: 7.3 m"
        );
    }

    private void OnActionReceivedNoTrain(ActionBuffers actions)
    {
        AircraftInput input = actionDecoder.DecodeContinuousTPRY(actions, 0f, 0f);
        actuator.ApplyInput(input);

        if (routeTracker?.Route != null)
        {
            Transform cp = routeTracker.GetCurrentCheckpointTransform();
            Vector3 cpPos = cp != null ? cp.position : transform.position;

            if (routeTracker.TryAdvanceByRadius(transform.position, 30f))
            {
                BeginDeviationCaptureForCheckpoint(cpPos);
            }
        }
    }

    private void OnActionReceivedFlight(ActionBuffers actions)
    {
        AircraftInput input = actionDecoder.DecodeContinuousTPRY(actions, 0f, 0f);
        actuator.ApplyInput(input);

        PenalizeAngularVelocity();

        if (StepCount > nextStepTimeout)
        {
            AddReward(-0.5F);
            CompleteEpisode(false);
            return;
        }

        if (routeTracker?.Route == null) return;

        Vector3 vectorToCheckpoint = routeTracker.GetVectorToNext(transform);
        float currentCheckpointDist = vectorToCheckpoint.magnitude;
        float progress = previousCheckpointDist - currentCheckpointDist;

        AddReward(progress * progressRewardScale);

        if (progress < 0f)
        {
            AddReward(progress * distanceRegressionPenalty);
        }

        if (currentCheckpointDist > 0.001f)
        {
            float alignment = Vector3.Dot(transform.forward.normalized, vectorToCheckpoint.normalized);
            AddReward(alignment * alignmentRewardScale);
        }

        previousCheckpointDist = currentCheckpointDist;
        nextCheckpointDist = currentCheckpointDist;

        Transform cp = routeTracker.GetCurrentCheckpointTransform();
        Vector3 cpPos = cp != null ? cp.position : (transform.position + vectorToCheckpoint);

        if (routeTracker.TryAdvanceByRadius(transform.position, controller?.AgentGetDifficulty() ?? 0f))
        {
            AddReward(1F);
            nextStepTimeout = StepCount + stepTimeout;
            checkpointStreak++;
            previousCheckpointDist = routeTracker.GetDistanceToNext(transform);

            BeginDeviationCaptureForCheckpoint(cpPos);

            if (checkpointStreak >= checkpointsForSuccess)
            {
                CompleteEpisode(true);
                return;
            }
        }
    }

    private void OnActionReceivedTakeoff(ActionBuffers actions)
    {
        AircraftInput input = actionDecoder.DecodeContinuousTPRY(actions, 0f, 0f);
        actuator.ApplyInput(input);

        float deltaSpeed = aircraftPhysics.rb.linearVelocity.magnitude - lastVelocity.magnitude;
        AddReward(deltaSpeed * 0.01f);
        float verticalSpeed = aircraftPhysics.rb.linearVelocity.y;
        AddReward(verticalSpeed * 0.005f);

        Collider coll = aircraftPhysics.GetGround();
        if (coll != null && !coll.transform.CompareTag("Airfield"))
        {
            AddReward(-1f);
            EndEpisode();
        }
        lastVelocity = aircraftPhysics.rb.linearVelocity;

        if (routeTracker?.Route == null) return;
        nextCheckpointDist = routeTracker.GetDistanceToNext(transform);
        Transform cp = routeTracker.GetCurrentCheckpointTransform();
        Vector3 cpPos = cp != null ? cp.position : transform.position;

        if (routeTracker.TryAdvanceByRadius(transform.position, controller?.AgentGetDifficulty() ?? 0f))
        {
            AddReward(0.25F);
            nextStepTimeout = StepCount + stepTimeout;
            BeginDeviationCaptureForCheckpoint(cpPos);
        }
    }

    private void OnActionReceivedLanding(ActionBuffers actions)
    {
        AircraftInput input = actionDecoder.DecodeContinuousTPRY(actions, 0.5f, 0.5f);
        actuator.ApplyInput(input);

        float speedError = targetLandingSpeed - aircraftPhysics.rb.linearVelocity.magnitude;
        AddReward(speedError * 0.01f);

        float verticalSpeedDelta = aircraftPhysics.rb.linearVelocity.y - lastVerticalSpeed;
        AddReward(-Mathf.Abs(verticalSpeedDelta) * 0.005f);
        lastVerticalSpeed = aircraftPhysics.rb.linearVelocity.y;

        float verticalDescent = -aircraftPhysics.rb.linearVelocity.y;
        if (verticalDescent > 0)
        {
            AddReward(verticalDescent * 0.01f);
        }

        Collider coll = aircraftPhysics.GetGround();
        if (coll != null && !coll.transform.CompareTag("Airfield") || offRunway)
        {
            AddReward(-5f);
            EndEpisode();
        }

        if (routeTracker?.Route == null) return;

        nextCheckpointDist = routeTracker.GetDistanceToNext(transform);

        Transform cp = routeTracker.GetCurrentCheckpointTransform();
        Vector3 cpPos = cp != null ? cp.position : transform.position;

        if (routeTracker.TryAdvanceByRadius(transform.position, controller?.AgentGetDifficulty() ?? 0f))
        {
            AddReward(0.5F);
            nextStepTimeout = StepCount + stepTimeout;
            BeginDeviationCaptureForCheckpoint(cpPos);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.InverseTransformDirection(aircraftPhysics.rb.linearVelocity));

        sensor.AddObservation(transform.forward);
        sensor.AddObservation(transform.up);

        if (routeTracker?.Route != null)
        {
            sensor.AddObservation(routeTracker.GetVectorToNextLocal(transform));
            sensor.AddObservation(routeTracker.GetNextCheckpointForwardLocal(transform));
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
        }

        sensor.AddObservation(transform.InverseTransformDirection(aircraftPhysics.GetWind()));
    }

    private void ResetRouteState()
    {
        if (routeTracker?.Route != null)
        {
            int startCheckpointId = 0;
            int checkpointCount = routeTracker.Route.GetCheckpointCount();

            if (randomSpawn && checkpointCount > 1)
            {
                startCheckpointId = Random.Range(0, checkpointCount - 1);
            }

            routeTracker.ResetProgress(startCheckpointId);
            Transform startPoint = routeTracker.GetCurrentCheckpointTransform();
            if (startPoint != null)
            {
                aircraftPhysics.Teleport(startPoint, startForce);
                nextCheckpointDist = routeTracker.GetDistanceToNext(transform);
                previousCheckpointDist = nextCheckpointDist;
            }
            else
            {
                nextCheckpointDist = 0f;
                previousCheckpointDist = 0f;
            }
        }
        else
        {
            nextCheckpointDist = 0f;
            previousCheckpointDist = 0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.transform.CompareTag("Agent") && !collision.transform.CompareTag("Airfield"))
        {
            if (training != TrainingType.NoTrain)
            {
                AddReward(-1F);
                CompleteEpisode(false);
                return;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (routeTracker?.Route == null) return;

        if (routeTracker.NotifyCheckpointTriggered(other))
        {
            AddReward(1F);
            nextStepTimeout = StepCount + stepTimeout;
            nextCheckpointDist = routeTracker.GetDistanceToNext(transform);
            previousCheckpointDist = nextCheckpointDist;
            checkpointStreak++;
            BeginDeviationCaptureForCheckpoint(other.bounds.center);

            if (checkpointStreak >= checkpointsForSuccess)
            {
                CompleteEpisode(true);
            }
        }
    }

    public void AddToController(LearningController controller, int id)
    {
        this.controller = controller;
        this.id = id;
    }

    public int GetId() => id;

    private bool IsReadyForControl()
    {
        return actionDecoder != null && actuator != null && aircraftPhysics != null;
    }

    private void PenalizeJerk(ActionBuffers actions)
    {
        var c = actions.ContinuousActions;
        Vector3 currentAction = new Vector3(c[0], c[1], c[2]);

        float delta = (currentAction - lastAction).magnitude;

        AddReward(-delta * 0.005f);

        lastAction = currentAction;
    }

    private void PenalizeAngularVelocity()
    {
        Vector3 angVel = aircraftPhysics.rb.angularVelocity;

        float penalty = angVel.magnitude * 0.001f;
        AddReward(-penalty);
        lastAngularVelocity = angVel;
    }

    private void PenalizeVelocityChange()
    {
        Vector3 currentVelocity = aircraftPhysics.rb.linearVelocity;
        float deltaV = (currentVelocity - lastVelocity).magnitude;

        AddReward(-deltaV * 0.001f);

        lastVelocity = currentVelocity;
    }

    private void RewardSmoothAlignment()
    {
        Vector3 vectorToCheckpoint = routeTracker.GetNextCheckpointTransform().position - transform.position;
        float alignment = Vector3.Dot(transform.forward.normalized, vectorToCheckpoint.normalized);

        float deltaAlignment = Mathf.Abs(alignment - lastAlignment);

        AddReward(alignment * 0.001f * (1f - deltaAlignment));

        lastAlignment = alignment;
    }
}
