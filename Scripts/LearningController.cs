using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LearningController : MonoBehaviour
{
    private List<AgentController> _agnets = new List<AgentController>();


    private int difficulty = 5;
    private List<float> radiuses = new List<float> { 50, 40, 30, 25, 20, 15, 0 };

    private int success = 0;

    public float target_reward = 3;


    private void Awake()
    {
        Transform learningControllerTransform = this.transform;

        int id = 0;
        foreach (Transform agentTransform in learningControllerTransform)
        {
            AgentController agent = agentTransform.GetComponent<AgentController>();
            _agnets.Add(agent);
            agent.AddToController(this, id);
            id += 1;
        }
    }

    public float AgentUpdateDifficulty(float reward)
    {
        if (reward > target_reward)
        {
            success++;
        }
        if (success > 80 && difficulty < radiuses.Count - 1)
        {
            difficulty++;
            success = 0;
            Debug.Log("Difficulty increased r = " + radiuses[difficulty]);
        }
        return radiuses[difficulty];
    }

    public float AgentGetDifficulty()
    {
        return radiuses[difficulty];
    }

}
