using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    // Start is called before the first frame update

    private Route route;
    private int id;

    /*private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<AgentController>(out AgentController agent))
        {
            route.enterCheckpoint(this, agent);
        }
        //Debug.Log(other);
    }*/

    public void AddToRoute(Route route, int checkpointId)
    {
        this.route = route;
        this.id = checkpointId;
    }
    public int GetId()
    {
        return this.id;
    }

    /*
    public int GetNextId()
    {
        return (this.id + 1) % this.route.GetCheckpointCount();
    }*/

}
