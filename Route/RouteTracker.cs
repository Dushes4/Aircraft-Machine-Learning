using UnityEngine;

public class RouteTracker : MonoBehaviour
{
    [SerializeField]
    private Route route;

    [SerializeField]
    private int currentCheckpointId;
    private Vector3 prevPos;
    private bool hasPrevPos = false;

    public Route Route => route;
    public int CurrentCheckpointId => currentCheckpointId;

    public void SetRoute(Route route)
    {
        this.route = route;
    }

    public void ResetProgress(int startCheckpointId = 0)
    {
        if (route == null)
        {
            currentCheckpointId = 0;
            hasPrevPos = false;
            return;
        }

        int clampedId = Mathf.Clamp(startCheckpointId, 0, route.GetCheckpointCount() - 1);
        currentCheckpointId = clampedId;
        hasPrevPos = false;
    }

    public int GetNextCheckpointId()
    {
        if (route == null) return 0;
        return (currentCheckpointId + 1) % route.GetCheckpointCount();
    }

    public Transform GetNextCheckpointTransform()
    {
        if (route == null) return null;
        return route.GetCheckpointTransform(GetNextCheckpointId());
    }

    public bool TryAdvanceByRadius(Vector3 position, float radius)
    {
        Transform next = GetNextCheckpointTransform();
        if (next == null) return false;

        if (!hasPrevPos)
        {
            prevPos = position;
            hasPrevPos = true;
            return false;
        }

        Vector3 cpPos = next.position;
        Vector3 n = next.forward;

        Vector3 a = prevPos;
        Vector3 b = position;

        float da = Vector3.Dot(a - cpPos, n);
        float db = Vector3.Dot(b - cpPos, n);

        bool advanced = false;
        const float eps = 1e-5f;

        if (Mathf.Abs(da) <= eps && Mathf.Abs(db) <= eps)
        {
            Vector3 a2 = Vector3.ProjectOnPlane(a - cpPos, n);
            Vector3 b2 = Vector3.ProjectOnPlane(b - cpPos, n);

            Vector3 ab = b2 - a2;
            float abLen2 = ab.sqrMagnitude;

            float t = 0f;
            if (abLen2 > eps)
                t = Mathf.Clamp01(Vector3.Dot(-a2, ab) / abLen2);

            Vector3 closest = a2 + ab * t;

            if (closest.magnitude <= radius)
            {
                currentCheckpointId = GetNextCheckpointId();
                advanced = true;
            }
        }
        else if (da * db <= 0f)
        {
            float denom = (da - db);
            if (Mathf.Abs(denom) > eps)
            {
                float t = Mathf.Clamp01(da / denom);
                Vector3 hit = Vector3.Lerp(a, b, t);

                Vector3 inPlane = Vector3.ProjectOnPlane(hit - cpPos, n);
                if (inPlane.magnitude <= radius)
                {
                    currentCheckpointId = GetNextCheckpointId();
                    advanced = true;
                }
            }
            else
            {
                Vector3 a2 = Vector3.ProjectOnPlane(a - cpPos, n);
                Vector3 b2 = Vector3.ProjectOnPlane(b - cpPos, n);
                Vector3 ab = b2 - a2;
                float abLen2 = ab.sqrMagnitude;

                float t = 0f;
                if (abLen2 > eps)
                    t = Mathf.Clamp01(Vector3.Dot(-a2, ab) / abLen2);

                Vector3 closest = a2 + ab * t;

                if (closest.magnitude <= radius)
                {
                    currentCheckpointId = GetNextCheckpointId();
                    advanced = true;
                }
            }
        }

        prevPos = position;
        return advanced;
    }


    public bool NotifyCheckpointTriggered(Collider other)
    {
        Transform next = GetNextCheckpointTransform();
        if (next == null) return false;

        if (other.transform == next || other.gameObject == next.gameObject)
        {
            currentCheckpointId = GetNextCheckpointId();
            return true;
        }

        return false;
    }

    public Vector3 GetVectorToNext(Transform origin)
    {
        Transform next = GetNextCheckpointTransform();
        if (next == null) return Vector3.zero;
        return next.position - origin.position;
    }

    public Vector3 GetVectorToNextLocal(Transform origin)
    {
        return origin.InverseTransformDirection(GetVectorToNext(origin));
    }

    public Vector3 GetNextCheckpointForwardLocal(Transform origin)
    {
        Transform next = GetNextCheckpointTransform();
        if (next == null) return Vector3.zero;
        return origin.InverseTransformDirection(next.forward);
    }

    public float GetDistanceToNext(Transform origin)
    {
        return GetVectorToNext(origin).magnitude;
    }
    public Transform GetCurrentCheckpointTransform()
    {
        if (route == null) return null;
        return route.GetCheckpointTransform(currentCheckpointId);
    }

    public int GetNextNextCheckpointId()
    {
        if (route == null) return 0;
        return (currentCheckpointId + 2) % route.GetCheckpointCount();
    }

    public Transform GetNextNextCheckpointTransform()
    {
        if (route == null) return null;
        return route.GetCheckpointTransform(GetNextNextCheckpointId());
    }

    public Vector3 GetVectorToNextNext(Transform origin)
    {
        Transform t = GetNextNextCheckpointTransform();
        if (t == null) return Vector3.zero;
        return t.position - origin.position;
    }

    public Vector3 GetVectorToNextNextLocal(Transform origin)
    {
        return origin.InverseTransformDirection(GetVectorToNextNext(origin));
    }

    public Vector3 GetNextNextCheckpointForwardLocal(Transform origin)
    {
        Transform t = GetNextNextCheckpointTransform();
        if (t == null) return Vector3.zero;
        return origin.InverseTransformDirection(t.forward);
    }

    public float GetDistanceToNextNext(Transform origin)
    {
        return GetVectorToNextNext(origin).magnitude;
    }

}
