using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicEngine : MonoBehaviour
{
    public struct EngineMetrics
    {
        public bool running;
        public float throttle;
        public float revs;
        public float power;
    }
    protected float fuelÑonsumption = 10;
    protected float maxRevs = 2500;
    [SerializeField]
    protected float maxPower = 150;

    protected float throttle;
    [SerializeField]
    protected bool running;
    [SerializeField]
    protected float revs;
    protected float altitude;

    void Start()
    {

    }
    void Update()
    {
        
    }
    void FixedUpdate()
    {
        UpdateAltitude();
        UpdateRevs();
    }

    public float GetPower()
    {
        return GetSeaLevelPropPowerRatio() * GetFixedLevelPropPowerRatio() * maxPower;
    }
    public float GetFuelÑonsumption()
    {
        return ((revs / maxRevs * fuelÑonsumption) / 60 / 60 / 200);
    }
    public EngineMetrics GetEngineMetrics()
    {
        EngineMetrics metrics = new EngineMetrics();
        metrics.running = running;
        metrics.revs = revs;
        metrics.power = GetPower();
        metrics.throttle = throttle;
        return metrics;
    }

    public void SetThrottle(float throttle)
    {
        this.throttle = Mathf.Clamp(throttle, 0, 1);
    }
    public void SetRevs(float revs)
    {
        this.revs = Mathf.Clamp(revs, 0, maxRevs);
    }
    public void SetEngineRunningState(bool state)
    {
        this.running = state;
    }


    private void UpdateRevs()
    {
        if (running)
        {
            if (throttle * maxRevs > revs || revs < maxRevs * 0.25F)
            {
                revs += 10;
            }
            if (throttle * maxRevs < revs && revs > maxRevs * 0.25F)
            {
                revs -= 15;
            }
        }
        else
        {
            revs -= 15;
        }
        revs = Mathf.Clamp(revs, 0, maxRevs);
    }
    private void UpdateAltitude()
    {
        altitude = gameObject.transform.position.y;
    }
    private float GetSeaLevelPropPowerRatio()
    {
        return 0.0004F * revs; 
    }
    private float GetFixedLevelPropPowerRatio()
    {
        return 1 - 0.0002F * altitude;
    }
}
