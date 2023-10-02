using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RoadSegment : MonoBehaviour
{
    #region Vars
    TrafficSystem trafficSystem;

    public Semaphore semaphore;
    public TextMesh debugText;

    public int idlingVehicles { get; private set; }
    public void VehicleStartedIdling() => idlingVehicles++;
    public void VehicleStoppedIdling() => idlingVehicles--;

    private List<Vehicle> vehiclesInSegment = new List<Vehicle>();
    
    public float vehiclesPerMinute;
    public int capacity = 9;

    [HideInInspector]
    public float priority;
    [HideInInspector]
    public float totalVehicleCount;

    void Start() 
    {
        debugText = transform.parent.GetComponentInChildren<TextMesh>();
        trafficSystem = TrafficSystem.Instance;
    }
    #endregion


    public void AddVehicle(Vehicle _vehicle)
    {
        totalVehicleCount++;
        vehiclesInSegment.Add(_vehicle);
    }

    public void RemoveVehicle(Vehicle _vehicle)
    {
        vehiclesInSegment.Remove(_vehicle);
    }

    void FixedUpdate()
    {
        idlingVehicles = 0;
        foreach (var item in vehiclesInSegment)
        {
            if (item.idling)
                idlingVehicles++;
        }

        if (semaphore == null || trafficSystem.trafficMode == TrafficMode.Normal)
            return;
        
        priority = idlingVehicles * semaphore.timeSinceOpened;
        debugText.text = priority.ToString("0.00");
        debugText.gameObject.SetActive(trafficSystem.debugMode);
    }
}
