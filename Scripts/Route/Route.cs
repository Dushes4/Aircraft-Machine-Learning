using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Route : MonoBehaviour
{
    private List<Checkpoint> _checkpoints = new List<Checkpoint>();

    private void Awake()
    {
        Debug.Log("Start");

        Transform routeTransform = this.transform;

        int checkpointId = 0;
        foreach (Transform checkpointTransform in routeTransform)
        {
            Checkpoint checkpoint = checkpointTransform.GetComponent<Checkpoint>();
            _checkpoints.Add(checkpoint);
            checkpoint.AddToRoute(this, checkpointId);
            checkpointId += 1;
        }
    }

    public int GetCheckpointCount()
    {
        return _checkpoints.Count;
    }

    public Transform GetCheckpointTransform(int id)
    {
        return _checkpoints[id].transform;
    }

    public int GetNextCheckpointId(int id)
    {
        return (id + 1) % GetCheckpointCount();
    }

    /*
    public void enterCheckpoint(Checkpoint checkpoint, AgentController agent)
    {
        if (getTargetCheckpoint(agent) == null || getTargetCheckpoint(agent) == checkpoint)
        {
            Debug.Log("CheckPoint " + checkpoint + " | Next " + _checkpoints[(checkpoint.id + 1) % _checkpoints.Count]);
            
        }

        foreach (AgentInfo info in _agentsInfo)
        {
            if (info.agent == agent && info.targetCheckpointId == checkpoint.id)
            {
                info.targetCheckpointId = (info.targetCheckpointId + 1) % _checkpoints.Count;
            }
        }
        agent.onEnterTargetCheckpoint();
    }

    public void ResetCheckpoint(AgentController agent)
    {
        foreach (AgentInfo info in _agentsInfo)
        {
            if (info.agent == agent)
            {
                info.targetCheckpointId = 0;
            }
        }
    }

    public void AddAgent(AgentController agent)
    {
        AgentInfo info = new AgentInfo(agent);
        _agentsInfo.Add(info);
    }

    public Checkpoint GetTargetCheckpoint(AgentController agent)
    {
        foreach (AgentInfo info in _agentsInfo)
        {
            if (info.agent == agent)
            {
                return _checkpoints[info.targetCheckpointId];
            }
        }
        Debug.Log("NO AIRCRAFT INFO");
        return null;
    }
    */


}
