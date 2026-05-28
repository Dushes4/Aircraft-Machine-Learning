using Unity.InferenceEngine;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class AircraftInferenceController : MonoBehaviour
{
    [SerializeField] private Vector3 wind = Vector3.zero;
    [SerializeField] private float startForce = 0;
    [SerializeField] private int startCheckpointId = 0;
    [SerializeField] private Mode mode = Mode.Flight;

    [Header("Models")]
    [SerializeField] private ModelAsset flyingNN = null;
    [SerializeField] private ModelAsset landingNN = null;
    [SerializeField] private ModelAsset takeoffNN = null;

    [Header("Dependencies")]
    [SerializeField] private AircraftActuator actuator;
    [SerializeField] private AircraftActionDecoder actionDecoder;
    [SerializeField] private RouteTracker routeTracker;
    [SerializeField] private AircraftPhysics aircraftPhysics;
    [SerializeField] private GroundStateTracker groundStateTracker;
    [SerializeField] private TextMesh hudText;

    public enum Mode
    {
        Takeoff,
        Flight,
        Landing,
        Wait
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
        if (hudText != null)
        {
            hudText.text = Mathf.RoundToInt(aircraftPhysics.GetAircraftMetrics().speed).ToString();
        }

        if (mode == Mode.Takeoff && groundStateTracker != null && groundStateTracker.IsFlying)
        {
            mode = Mode.Flight;
        }
    }

    public void ResetEpisode()
    {
        aircraftPhysics.SetWind(wind);

        if (routeTracker?.Route != null)
        {
            int checkpointCount = routeTracker.Route.GetCheckpointCount();
            int clampedStart = Mathf.Clamp(startCheckpointId, 0, Mathf.Max(0, checkpointCount - 1));

            routeTracker.ResetProgress(clampedStart);
            Transform startPoint = routeTracker.GetCurrentCheckpointTransform();
            if (startPoint != null)
            {
                aircraftPhysics.Teleport(startPoint, startForce);
            }
        }
    }

    public Mode GetMode() => mode;

    public ModelAsset GetModelForMode()
    {
        switch (mode)
        {
            case Mode.Flight: return flyingNN;
            case Mode.Takeoff: return takeoffNN;
            case Mode.Landing: return landingNN;
            case Mode.Wait: return null;
            default: return null;
        }
    }

    public void ApplyActionsFlight(ActionBuffers actions)
    {
        if (!IsReadyForControl()) return;

        AircraftInput input = actionDecoder.DecodeFlight(actions);
        actuator.ApplyInput(input);
        if (routeTracker?.Route != null) routeTracker.TryAdvanceByRadius(transform.position, 30f);
    }

    public void ApplyActionsTakeoff(ActionBuffers actions)
    {
        if (!IsReadyForControl()) return;

        AircraftInput input = actionDecoder.DecodeTakeoff(actions);
        actuator.ApplyInput(input);
        if (routeTracker?.Route != null) routeTracker.TryAdvanceByRadius(transform.position, 30f);
    }

    public void ApplyActionsLanding(ActionBuffers actions)
    {
        if (!IsReadyForControl()) return;

        AircraftInput input = actionDecoder.DecodeLanding(actions);
        actuator.ApplyInput(input);
        if (routeTracker?.Route != null) routeTracker.TryAdvanceByRadius(transform.position, 30f);
    }

    private bool IsReadyForControl()
    {
        return actionDecoder != null && actuator != null && aircraftPhysics != null;
    }
}
