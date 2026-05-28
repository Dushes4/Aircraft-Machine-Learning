using System;
using System.Collections.Generic;
using UnityEngine;

public class LearningController : MonoBehaviour
{
    private readonly List<AircraftAgent> _agents = new List<AircraftAgent>();

    [Serializable]
    public class LevelConfig
    {
        [Tooltip("Difficulty parameter for this level (e.g. checkpoint radius).")]
        public float radius = 50f;

        [Tooltip("Minimum average reward over the last N runs to advance from this level to the next.")]
        public float requiredAverageReward = 3f;

        [Tooltip("Number of last runs used to compute the moving average. If fewer runs collected, no check is performed.")]
        public int windowRuns = 80;
    }

    [Header("Per-level config (index = difficulty)")]
    [SerializeField]
    private List<LevelConfig> levels = new List<LevelConfig>
    {
        new LevelConfig { radius = 50f, requiredAverageReward = 3.0f, windowRuns = 80 },
        new LevelConfig { radius = 40f, requiredAverageReward = 3.5f, windowRuns = 80 },
        new LevelConfig { radius = 30f, requiredAverageReward = 4.0f, windowRuns = 80 },
        new LevelConfig { radius = 25f, requiredAverageReward = 4.5f, windowRuns = 80 },
        new LevelConfig { radius = 20f, requiredAverageReward = 5.0f, windowRuns = 80 },
        new LevelConfig { radius = 15f, requiredAverageReward = 5.5f, windowRuns = 80 },
        new LevelConfig { radius = 0f,  requiredAverageReward = 999f, windowRuns = 999 }
    };

    [SerializeField] private int difficulty = 0;

    private readonly Queue<float> _recentRewards = new Queue<float>(256);
    private float _recentSum = 0f;

    private void Awake()
    {
        int id = 0;
        foreach (Transform agentTransform in transform)
        {
            AircraftAgent agent = agentTransform.GetComponent<AircraftAgent>();
            _agents.Add(agent);
            agent.AddToController(this, id);
            id += 1;
        }

        ResetWindow();
    }

    public float AgentUpdateDifficulty(float episodeReward)
    {
        if (levels == null || levels.Count == 0)
            return 0f;

        difficulty = Mathf.Clamp(difficulty, 0, levels.Count - 1);
        PushReward(episodeReward);

        if (difficulty >= levels.Count - 1)
            return levels[difficulty].radius;

        LevelConfig cfg = levels[difficulty];
        int window = Mathf.Max(1, cfg.windowRuns);

        if (_recentRewards.Count < window)
            return cfg.radius;

        float avg = _recentSum / _recentRewards.Count;

        if (avg >= cfg.requiredAverageReward)
        {
            difficulty = Mathf.Min(difficulty + 1, levels.Count - 1);
            ResetWindow();

            Debug.Log($"Difficulty increased to {difficulty}. radius={levels[difficulty].radius}, avg={avg:0.###}");
        }

        return levels[difficulty].radius;
    }

    public float AgentGetDifficulty()
    {
        if (levels == null || levels.Count == 0) return 0f;
        difficulty = Mathf.Clamp(difficulty, 0, levels.Count - 1);
        return levels[difficulty].radius;
    }

    private void PushReward(float r)
    {
        LevelConfig cfg = levels[Mathf.Clamp(difficulty, 0, levels.Count - 1)];
        int window = Mathf.Max(1, cfg.windowRuns);

        _recentRewards.Enqueue(r);
        _recentSum += r;

        while (_recentRewards.Count > window)
        {
            _recentSum -= _recentRewards.Dequeue();
        }
    }

    private void ResetWindow()
    {
        _recentRewards.Clear();
        _recentSum = 0f;
    }
}
