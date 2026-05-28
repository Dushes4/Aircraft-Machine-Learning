using Unity.MLAgents.Actuators;
using UnityEngine;

public class AircraftActionDecoder : MonoBehaviour
{
    private static float ClampAxis(float v) => Mathf.Clamp(v, -1f, 1f);
    private static float Clamp01(float v) => Mathf.Clamp01(v);

    public float DecodeAxis3(int action) => action == 2 ? -1f : action;

    public float DecodeFlaps3(int action)
    {
        if (action == 2) return 0.5f;
        return action;
    }

    public AircraftInput DecodeStandard(ActionBuffers actions)
    {
        return new AircraftInput
        {
            thrust = actions.DiscreteActions[0],
            pitch = DecodeAxis3(actions.DiscreteActions[1]),
            roll = DecodeAxis3(actions.DiscreteActions[2]),
            yaw = DecodeAxis3(actions.DiscreteActions[3]),
            flaps = DecodeFlaps3(actions.DiscreteActions[4]),
            brakes = actions.DiscreteActions[5]
        };
    }

    public AircraftInput DecodeFlight(ActionBuffers actions)
    {
        return new AircraftInput
        {
            thrust = actions.DiscreteActions[0],
            pitch = DecodeAxis3(actions.DiscreteActions[1]),
            roll = DecodeAxis3(actions.DiscreteActions[2]),
            yaw = DecodeAxis3(actions.DiscreteActions[3]),
            flaps = DecodeFlaps3(actions.DiscreteActions[4]),
            brakes = 0
        };
    }

    public AircraftInput DecodeTakeoff(ActionBuffers actions)
    {
        return new AircraftInput
        {
            thrust = actions.DiscreteActions[0],
            pitch = DecodeAxis3(actions.DiscreteActions[1]),
            roll = DecodeAxis3(actions.DiscreteActions[2]),
            yaw = DecodeAxis3(actions.DiscreteActions[3]),
            flaps = DecodeFlaps3(actions.DiscreteActions[4]),
            brakes = 0
        };
    }

    public AircraftInput DecodeLanding(ActionBuffers actions)
    {
        return new AircraftInput
        {
            thrust = actions.DiscreteActions[0],
            pitch = DecodeAxis3(actions.DiscreteActions[1]),
            roll = DecodeAxis3(actions.DiscreteActions[2]),
            yaw = DecodeAxis3(actions.DiscreteActions[3]),
            flaps = DecodeFlaps3(actions.DiscreteActions[4]),
            brakes = actions.DiscreteActions[5]
        };
    }

    public AircraftInput DecodeContinuousTPRY(
        ActionBuffers actions, float flapsFixed, float brakesFixed)
    {
        var c = actions.ContinuousActions;

        float pitch = c.Length > 0 ? c[0] : 0f;
        float roll = c.Length > 1 ? c[1] : 0f;
        float yaw = c.Length > 2 ? c[2] : 0f;
        float thrust = c.Length > 2 ? c[2] : 0f;

        return new AircraftInput
        {
            thrust = Clamp01(thrust),
            pitch = ClampAxis(pitch),
            roll = ClampAxis(roll),
            yaw = ClampAxis(yaw),
            flaps = Clamp01(flapsFixed),
            brakes = Clamp01(brakesFixed)
        };
    }
}
