using UnityEngine;

[RequireComponent(typeof(AircraftPhysics))]
public sealed class GroundStateTracker : MonoBehaviour
{
    public enum FlightState
    {
        Unknown,
        Flying,
        Landed
    }

    [SerializeField, Min(1)]
    private int sampleWindow = 50;

    private AircraftPhysics aircraftPhysics;
    private int sampleIndex;
    private int groundedSamples;

    public FlightState State { get; private set; } = FlightState.Unknown;

    public bool IsFlying => State == FlightState.Flying;
    public bool IsLanded => State == FlightState.Landed;

    private void Awake()
    {
        aircraftPhysics = GetComponent<AircraftPhysics>();
    }

    private void FixedUpdate()
    {
        if (aircraftPhysics.GetGround() != null)
        {
            groundedSamples++;
        }

        sampleIndex++;
        if (sampleIndex < sampleWindow)
        {
            return;
        }

        State = groundedSamples == 0 ? FlightState.Flying : FlightState.Landed;

        groundedSamples = 0;
        sampleIndex = 0;
    }
}
