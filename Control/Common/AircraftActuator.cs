using UnityEngine;

[RequireComponent(typeof(AircraftPhysics))]
public class AircraftActuator : MonoBehaviour
{
    [SerializeField]
    private AircraftPhysics aircraftPhysics;

    private void Awake()
    {
        if (aircraftPhysics == null)
        {
            aircraftPhysics = GetComponent<AircraftPhysics>();
        }
    }

    public void ApplyInput(in AircraftInput input)
    {
        if (aircraftPhysics == null) return;

        aircraftPhysics.SetThrust(input.thrust);
        aircraftPhysics.SetControlSurfacesAngles(input.pitch, input.roll, input.yaw, input.flaps);
        aircraftPhysics.SetBrakesTorque(input.brakes);
    }
}
