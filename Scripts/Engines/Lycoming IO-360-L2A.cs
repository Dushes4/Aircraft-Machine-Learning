using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : BasicEngine
{
    private void Start()
    {
        fuelÑonsumption = 11;
        maxRevs = 2700;
        maxPower = 180;
    }
    private float getSeaLevelPropPowerRatio()
    {
        if (revs > 1800)
        {
            return (float)((0.0000000001638889 * Mathf.Pow(revs, 3)) - (0.0000005133888889 * Mathf.Pow(revs, 2)) + (revs * 0.0005749207222222) - 0.0000555555555556);
        }
        else 
        {
            return (float)(0.000154321 * revs);
        }
    }
    private float getFixedLevelPropPowerRatio()
    {
        return 1 - 0.00011667F * altitude;
    }

}
