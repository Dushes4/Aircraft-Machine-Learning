using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{

    private Route route;
    private int id;

    public void AddToRoute(Route route, int checkpointId)
    {
        this.route = route;
        this.id = checkpointId;
    }
    public int GetId()
    {
        return this.id;
    }
}
