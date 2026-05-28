using System.Collections.Generic;
using UnityEngine;

public class Route : MonoBehaviour
{
    [Header("Editor Generation (Radial Fourier Curve)")]
    [Tooltip("Prefab with Checkpoint component")]
    public Checkpoint checkpointPrefab;

    [Tooltip("Approx area size (meters). Curve will be scaled to fit inside this rectangle.")]
    public Vector2 areaSize = new Vector2(3000f, 3000f);

    [Header("Direction")]
    [Tooltip("Generate checkpoints clockwise (off = counter-clockwise)")]
    public bool clockwise = false;

    [Tooltip("Distance between checkpoints along the curve (meters)")]
    public float checkpointSpacing = 120f;

    [Tooltip("How many harmonics to use (more = more wiggles).")]
    [Range(1, 16)]
    public int harmonics = 7;

    [Tooltip("Amplitude decay power. Smaller -> more wiggles.")]
    [Range(0.1f, 6f)]
    public float amplitudeDecayPower = 1.0f;

    [Tooltip("Overall noise strength (relative).")]
    [Range(0f, 1f)]
    public float noiseStrength = 0.5f;

    [Tooltip("Random seed (same seed => same route).")]
    public int seed = 1234;

    [Header("Height (optional)")]
    [Tooltip("Max height difference between neighboring checkpoints (meters). Set 0 to disable height changes.")]
    [Min(0f)]
    public float maxNeighborHeightDelta = 10f;

    [Header("Gizmos")]
    public bool drawGizmos = true;

    private List<Checkpoint> _checkpoints = new List<Checkpoint>();

    private void Awake()
    {
        _checkpoints.Clear();

        Transform routeTransform = this.transform;

        int checkpointId = 0;
        foreach (Transform checkpointTransform in routeTransform)
        {
            Checkpoint checkpoint = checkpointTransform.GetComponent<Checkpoint>();
            if (checkpoint == null) continue;

            _checkpoints.Add(checkpoint);
            checkpoint.AddToRoute(this, checkpointId);
            checkpointId += 1;
        }
    }

    public int GetCheckpointCount() => _checkpoints.Count;
    public Transform GetCheckpointTransform(int id) => _checkpoints[id].transform;
    public int GetNextCheckpointId(int id) => (id + 1) % GetCheckpointCount();

    public List<Vector3> GenerateRadialFourierRouteLocal()
    {
        float spacing = Mathf.Max(0.1f, checkpointSpacing);

        const float baseRadiusFactor = 0.35f;
        const int denseSamples = 2048;

        int nDense = denseSamples;

        float halfMin = 0.5f * Mathf.Min(areaSize.x, areaSize.y);
        float r0 = Mathf.Max(1f, halfMin * baseRadiusFactor);

        System.Random rng = new System.Random(seed);

        float[] ak = new float[harmonics + 1];
        float[] phik = new float[harmonics + 1];

        float decayPow = Mathf.Max(0.0001f, amplitudeDecayPower);

        for (int k = 1; k <= harmonics; k++)
        {
            float decay = Mathf.Pow(k, decayPow);
            float amp = noiseStrength * r0 / decay;

            float u = (float)rng.NextDouble() * 2f - 1f;
            ak[k] = u * amp;
            phik[k] = (float)rng.NextDouble() * Mathf.PI * 2f;
        }

        Vector3[] dense = new Vector3[nDense];
        for (int i = 0; i < nDense; i++)
        {
            float t = (float)i / nDense * Mathf.PI * 2f;
            dense[i] = EvalLocalXZ(t, r0, ak, phik);
        }

        FitToAreaDense(dense, areaSize);
        float[] cum = new float[nDense + 1];
        cum[0] = 0f;
        float total = 0f;

        for (int i = 1; i <= nDense; i++)
        {
            Vector3 a = dense[i - 1];
            Vector3 b = dense[i % nDense];
            total += Vector3.Distance(a, b);
            cum[i] = total;
        }

        int count = Mathf.Max(3, Mathf.CeilToInt(total / spacing));
        float step = total / count;

        var pts = new List<Vector3>(count);

        int seg = 0;
        for (int c = 0; c < count; c++)
        {
            float target = c * step;

            while (seg < nDense - 1 && cum[seg + 1] < target) seg++;

            float l0 = cum[seg];
            float l1 = cum[seg + 1];
            float alpha = (l1 - l0) > 1e-6f ? (target - l0) / (l1 - l0) : 0f;

            Vector3 p0 = dense[seg];
            Vector3 p1 = dense[(seg + 1) % nDense];

            pts.Add(Vector3.Lerp(p0, p1, alpha));
        }
        if (maxNeighborHeightDelta > 0f && pts.Count >= 2)
            ApplyHeightRandomWalk(ref pts, maxNeighborHeightDelta, rng);

        return pts;
    }

    private static void ApplyHeightRandomWalk(ref List<Vector3> pts, float maxDelta, System.Random rng)
    {
        int n = pts.Count;
        float[] y = new float[n];

        y[0] = 0f;
        for (int i = 1; i < n; i++)
        {
            float u = (float)rng.NextDouble() * 2f - 1f;
            float step = u * maxDelta;
            y[i] = y[i - 1] + step;
        }

        float endOffset = y[n - 1] - y[0];
        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0f : (float)i / (n - 1);
            y[i] -= endOffset * t;
        }

        for (int i = 0; i < n; i++)
        {
            Vector3 p = pts[i];
            p.y = y[i];
            pts[i] = p;
        }
    }

    private Vector3 EvalLocalXZ(float theta, float r0, float[] ak, float[] phik)
    {
        float r = r0;
        for (int k = 1; k < ak.Length; k++)
            r += ak[k] * Mathf.Cos(k * theta + phik[k]);

        r = Mathf.Max(r, r0 * 0.05f);

        float x = r * Mathf.Cos(theta);
        float z = r * Mathf.Sin(theta);
        return new Vector3(x, 0f, z);
    }

    private static void FitToAreaDense(Vector3[] pts, Vector2 area)
    {
        if (pts == null || pts.Length == 0) return;

        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;

        for (int i = 0; i < pts.Length; i++)
        {
            Vector3 p = pts[i];
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minZ = Mathf.Min(minZ, p.z);
            maxZ = Mathf.Max(maxZ, p.z);
        }

        float sizeX = Mathf.Max(1e-3f, maxX - minX);
        float sizeZ = Mathf.Max(1e-3f, maxZ - minZ);

        float sx = area.x / sizeX;
        float sz = area.y / sizeZ;
        float s = Mathf.Min(sx, sz);

        Vector3 center = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);

        for (int i = 0; i < pts.Length; i++)
            pts[i] = (pts[i] - center) * s;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        var pts = GenerateRadialFourierRouteLocal();
        if (pts == null || pts.Count < 2) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        for (int i = 0; i < pts.Count; i++)
        {
            Vector3 a = pts[i];
            Vector3 b = pts[(i + 1) % pts.Count];
            Gizmos.DrawLine(a, b);
        }
    }
}
