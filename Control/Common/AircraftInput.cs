using UnityEngine;

public struct AircraftInput
{
    public float thrust; // 0..1
    public float pitch;  // -1..1
    public float roll;   // -1..1
    public float yaw;    // -1..1
    public float flaps;  // 0..1 (0, 0.5, 1)
    public float brakes; // 0..1
}
