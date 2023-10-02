using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Waypoint
{
    public RoadPoint point;
    public bool waitSemaphore;
}

public class Vehicle : MonoBehaviour
{
    #region Vars

    [Header("Colors")]
    [Space(10)]

    public int[] materialsIds;
    public Color[] windowColors;


    [Header("Engine")]
    [Space(10)]

    public AnimationCurve acceleration;
    public AnimationCurve deceleration;

    MeshRenderer rndr;
    Rigidbody rig;

    public float maxAcceleration;
    public float maxSpeed, turnSpeed;
    public float brakeTorque;

    public float speed => rig.velocity.magnitude;

    float targetSpeed;
    float _accel;
    

    [Header("Wheels")]
    [Space(10)]

    public WheelCollider[] wheels;
    public Transform[] tires;
    

    [Header("Paths")]
    [Space(10)]

    public Waypoint[] nodes;
    public int waypointThreshold;

    [HideInInspector]
    public RoadPoint currentNode;
    RoadSegment currentSegment;

    TrafficSystem trafficSystem;
    bool semaphoreNode;
    int reachedNodes;


    [Header("AI")]
    [Space(10)]

    public Transform sensor;
    bool sensorIsTriggered;

    public bool braking;
    public bool idling;

    float idlingTimer;
    bool spawned;


    [Header("Debug")]
    [Space(10)]

    public bool debugMode;
    public MeshRenderer debugMesh;


    void Start()
    {
        rig = GetComponent<Rigidbody>();
        rndr = GetComponentInChildren<MeshRenderer>();
        trafficSystem = TrafficSystem.Instance;
    }

    #endregion

    #region Game Logic

    // Initialize Vehicle
    public void OnSpawn()
    {
        // Check if path isn't empty
        if (nodes.Length <= 0)
        {
            Debug.Log($"[ERROR] Vehicle '{transform.name}'s path is empty");
            return;
        }

        currentNode = nodes[0].point;
        currentSegment = currentNode.segment;

        semaphoreNode = nodes[0].waitSemaphore;
        currentSegment.AddVehicle(this);

        spawned = true;

        idlingTimer = 0;
        targetSpeed = maxSpeed;
        SetVehicleColor(materialsIds);
    }


    // Handles the game logic
    void FixedUpdate()
    {
       if (!spawned)
            OnSpawn();

        if (trafficSystem.simulationEnded)
            DespawnVehicle();

        // Update the driver AI
        UpdateDriverState();
        UpdatePathProgress();

        // Move the vehicle
        UpdateEngine(currentNode.transform);
        SteerWheels(currentNode.transform);
        UpdateTiresTransforms();

        // Debug mode
        debugMode = trafficSystem.debugMode;
        debugMesh.enabled = debugMode;
        
        if (debugMode)
            UpdateDebugMesh();
    }

    #endregion

    #region DriverAI

    // Check if its need to brake, slow down or accelerate
    void UpdateDriverState()
    {
        float nodeDistance = Vector3.Distance(transform.position, currentNode.transform.position);
        bool semaphoreIsRed = semaphoreNode ? !currentSegment.semaphore.isOpen : false;

        // Start idling when close to a semaphore
        idling = semaphoreNode && semaphoreIsRed && nodeDistance <= 4.5f;
        CheckProximitySensor();


        // Start breaking if vehicles is too fast
        targetSpeed = idling ? 0 : targetSpeed;
        braking = speed > targetSpeed;
        UpdateBrakeState();


        // Slow down vehicle if turning or close to a semaphore
        if (!idling && Mathf.Abs(wheels[0].steerAngle) > 15)
            targetSpeed = turnSpeed;
        else if (!idling && semaphoreNode && nodeDistance <= 25f * speed/maxSpeed)
            targetSpeed = semaphoreIsRed ? turnSpeed : turnSpeed * 1.5f;
        else if (!idling)
            targetSpeed = maxSpeed;

        idlingTimer += idling && targetSpeed <= 1f? Time.fixedDeltaTime : 0; 
    }

    // Handles the waypoint progression
    void UpdatePathProgress()
    {
        float nodeDistance = Vector3.Distance(transform.position, currentNode.transform.position);
        bool reachedWaypoint;

        // Check if reached the waypoint
        if (semaphoreNode)
            reachedWaypoint = nodeDistance < 1.8f && !idling;
        else
            reachedWaypoint = nodeDistance <= waypointThreshold;

        // Target the next waypoint
        if (!reachedWaypoint) return;
        reachedNodes++;

        // Despawn vehicle if path is completed
        if (reachedNodes >= nodes.Length)
            DespawnVehicle();

        // Update current waypoint data
        currentNode = nodes[reachedNodes].point;
        semaphoreNode = nodes[reachedNodes].waitSemaphore;

        // Update road segment data
        if (currentSegment != currentNode.segment)
            currentSegment.RemoveVehicle(this);
        currentSegment = currentNode.segment;
    }

    // Slow down or start idling if sensor trigger another vehicle
    void CheckProximitySensor()
    {
        RaycastHit hit;
        float rayLength;  

        // Reset sensor X axis rotation
        Vector3 newAngles = sensor.eulerAngles;
        newAngles.x = 0;
        sensor.eulerAngles = newAngles;

        // Cast the ray and get it data
        float maxDist = Mathf.Clamp(speed / maxSpeed * 10, 2.35f, 10f);
        bool raycastHit = Physics.Raycast(sensor.position, transform.forward, out hit, maxDist);

        sensorIsTriggered = raycastHit && hit.transform.CompareTag("Vehicle");
        rayLength = Vector3.Distance(sensor.position, hit.point);

        // If triggered
        if (sensorIsTriggered)
        {
            // Decide to slow down or stop based in the ray lenght
            braking = true;
            idling = rayLength / maxDist <= .55f;
            targetSpeed = idling ? targetSpeed : turnSpeed;

            // Drawn the raycast if in debug mode
            if (debugMode)
            {
                Color _color = idling ? Color.red : Color.yellow;
                Debug.DrawLine(sensor.position, hit.point, _color);
            }
        }
        else if (debugMode)
        {
            Vector3 endPoint = sensor.position + transform.forward * maxDist;
            Debug.DrawLine(sensor.position, endPoint, Color.green);
        }
    }

    #endregion

    #region Visuals

    // Generate and Apply random colors for the Vehicle
    void SetVehicleColor(int[] ids)
    {
        Material[] mats = new Material[3];

        // Set the materials according to the given ids
        mats[0] = rndr.materials[ids[0]];
        mats[1] = rndr.materials[ids[1]];

        // Set bodywork to a random color
        Color bodyworkColor = Random.ColorHSV(0, 1);
        mats[0].color = bodyworkColor;

        // Set window to a random color between the give ones
        Color glassColor = windowColors[Random.Range(0, windowColors.Length)];
        mats[1].color = glassColor;
    }

    // Update the tires position and rotation
    void UpdateTiresTransforms()
    {
        for (int i = 0; i < tires.Length; i++)
        {
            Vector3 pos;
            Quaternion rot;

            wheels[i].GetWorldPose(out pos, out rot);
            tires[i].position = pos;
            tires[i].rotation = rot;
        };
    }

    #endregion

    #region Engine & Wheels

    // Start or stop breaking
    void UpdateBrakeState()
    {
        Material mat = rndr.materials[materialsIds[2]];
        float deltaSpeed = Mathf.Abs(targetSpeed - speed);

        // Decide brake "intensity" based on situation
        rig.drag = deltaSpeed >= 10f && braking ? 0.15f : 0;
        rig.drag = (deltaSpeed >= 6.35f || semaphoreNode) && idling ? 2f : rig.drag;
        rig.drag = idling && sensorIsTriggered ? 3.5f : rig.drag;

        // Lit or unlit the brakes light
        if (braking && deltaSpeed >= 1f)
            mat.EnableKeyword("_EMISSION");
        else
            mat.DisableKeyword("_EMISSION");

        // Reset acceleration if breaking
        _accel = braking ? 0 : _accel;

        // Set wheels torque
        wheels.ToList().ForEach(x => {
            x.brakeTorque = braking || idling ? brakeTorque * 1000 : 0;
            x.motorTorque = braking ? 0 : x.motorTorque;
        });

    }

    // Rotate wheels to target within a -40� and +40� degree range
    void SteerWheels(Transform _target)
    {
        // The targetPos in the tire's local space
        Vector3 _relativePos = transform.InverseTransformPoint(_target.position);

        // Sin calculation
        float steerAngle = (_relativePos.x / _relativePos.magnitude) * 40f;

        wheels[0].steerAngle = steerAngle;
        wheels[1].steerAngle = steerAngle;

    }

    // Accelerate the car toward target speed
    void UpdateEngine(Transform _target)
    {
        Vector3 desiredSpeed;
        float move;

        // Calculate the higher possible velocity to reach the target
        desiredSpeed = (_target.position - transform.position).normalized * targetSpeed;
        move = Mathf.Clamp(desiredSpeed.magnitude - speed, 0, maxAcceleration);

        _accel += Time.fixedDeltaTime;
        _accel = Mathf.Clamp(_accel, 0, acceleration.keys[acceleration.keys.Length - 1].time);

        move *= _accel;

        // Set wheels torque
        wheels[0].motorTorque = 100 * (braking ? 0 : 1) * move;
        wheels[1].motorTorque = 100 * (braking ? 0 : 1) * move;

        wheels[2].motorTorque = move * 850;
        wheels[3].motorTorque = move * 850;

    }

    #endregion

    #region Misc.

    // Change the debug mesh colors according to the situation
    void UpdateDebugMesh()
    {
        // Set color to red if idling
        if (idling)
            debugMesh.material.color = Color.red;

        // Set color to yellow if turning or breaking
        if (targetSpeed != maxSpeed && !idling)
            debugMesh.material.color = Color.yellow;

        // Set color to black if "breaking too hard"
        if (rig.drag >= 3)
            debugMesh.material.color = Color.black;

        // Set color to green if targeting maxSpeed
        if (targetSpeed == maxSpeed)
            debugMesh.material.color = Color.green;
    }

    // Reset vehicle paraments and disable it
    void DespawnVehicle()
    {
        spawned = false;
        braking = false;
        idling = false;

        _accel = 0;
        rig.drag = 0;
        reachedNodes = 0;

        wheels.ToList().ForEach(x => {x.brakeTorque = 0; x.motorTorque = 0;});

        trafficSystem.VehicleFinishedPath();
        trafficSystem.AddIdlingTime(idlingTimer);

        transform.SetParent(ObjectPooler.Instance.transform); 
        gameObject.SetActive(false);
    }

    #endregion
}
