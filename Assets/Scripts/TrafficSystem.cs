using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public enum TrafficMode { None, Normal, Smart}

public class TrafficSystem : MonoBehaviour
{
    #region Vars
    SimulationUI simulationUI;
    
    [Header("Simulation Settings")]
    [Space(10)]

    [Range(1f, 20f)]
    public float simulationSpeed;
    public float sampleTime;

    [Header("Global Settings")]
    [Space(10)]

    public Crossway crossway;
    public TrafficMode trafficMode;

    [Header("Normal Settings")]
    [Space(10)]

    public int greenDuration;
    public int yellowDuration;
    public int redDuration;

    [Header("Smart Settings")]
    [Space(10)]

    public Vector2 greenDurationRange;

    [HideInInspector]
    public static TrafficSystem Instance;
    void Awake() => Instance = this;
    
    [HideInInspector]
    public bool simulationEnded;
    [HideInInspector]
    public bool debugMode;
    [HideInInspector]
    public bool isPaused;

    bool finishedSemaphoreLoop;
    int vehiclesPassed;
    float idlingTime;
    float elapsedTime;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        simulationUI = SimulationUI.Instance;
        Time.timeScale = simulationSpeed;

        if (trafficMode == TrafficMode.Smart)
            StartCoroutine("TrafficSystemControl");
        else
            StartCoroutine("NormalTrafficControl");

        StartCoroutine("MonitorSimulation", sampleTime * 60);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            debugMode = !debugMode;

        if (Input.GetKeyDown("escape"))
            {
                if (isPaused)
                    Resume();
                else
                    Pause();
            }
            
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0;
        simulationUI.ShowPauseWindow(simulationSpeed, elapsedTime / 60f);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = simulationSpeed;
        simulationUI.CloseSettingsWindow();
    }

    IEnumerator MonitorSimulation(float duration)
    {
        while (elapsedTime < duration && !simulationEnded)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        simulationEnded = true;
        yield return new WaitForSeconds(0.1f);

        float elapsedMinutes = elapsedTime / 60;
        simulationUI.ShowResults(idlingTime / vehiclesPassed, vehiclesPassed / elapsedMinutes, elapsedMinutes);
    }

    IEnumerator NormalTrafficControl()
    {
        Semaphore _currentSemaphore;

        // Semaphores in order of opening and closing
        Semaphore[] semaphores = { crossway.northSemaphore, crossway.eastSemaphore,
                                   crossway.southSemaphore, crossway.westSemaphore};

        yield return new WaitForSeconds(redDuration);

        while (true)
        {
            for (int i = 0; i < semaphores.Length; i++)
            {
                _currentSemaphore = semaphores[i];
                //float g = _currentSemaphore[0].GetComponentInParent<RoadSegment>().vehiclesPerMinute >= 6 ? greenDuration + 6 : greenDuration;

                // Run OpenAndCloseSemaphore() and wait
                StartCoroutine(LoopSemaphoreStates(_currentSemaphore, greenDuration, redDuration, yellowDuration));
                yield return new WaitUntil(() => finishedSemaphoreLoop);
                finishedSemaphoreLoop = false;
            }
        }

    }

    IEnumerator TrafficSystemControl()
    {
        float regularLoopDuration = (greenDuration + yellowDuration + redDuration);

        Semaphore _currentSemaphore;
        RoadSegment _currentRoad;

        // Semaphores in order of opening and closing
        Semaphore[] semaphores = { crossway.northSemaphore, crossway.eastSemaphore,
                                   crossway.southSemaphore, crossway.westSemaphore};

        RoadSegment[] segments = { crossway.northSemaphore.transform.parent.GetComponentInChildren<RoadSegment>(), crossway.eastSemaphore.transform.parent.GetComponentInChildren<RoadSegment>(),
                                   crossway.southSemaphore.transform.parent.GetComponentInChildren<RoadSegment>(), crossway.westSemaphore.transform.parent.GetComponentInChildren<RoadSegment>()};

        yield return new WaitForSeconds(redDuration);
        bool firstLoop = true;

        while (true)
        {
            // Id of the semaphore that'll be opened
            int openId = 0;

            for (int i = 0; i < semaphores.Length; i++)
            {
                if (segments[i].priority > segments[openId].priority)
                    openId = i;

                if (segments[i].idlingVehicles != 0 && semaphores[i].timeSinceOpened >= regularLoopDuration * 3f)
                {
                    openId = i;
                    break;
                }
            }

            if (firstLoop) yield return new WaitUntil(() => segments[openId].priority != 0);
            firstLoop = false;

            _currentSemaphore = semaphores[openId];
            _currentRoad = segments[openId];

            float vehiclesCount = _currentRoad.idlingVehicles;
            int roadCapacity = _currentRoad.capacity;

            float openTime = (greenDurationRange.y - greenDurationRange.x) * (vehiclesCount / roadCapacity);
            openTime += greenDurationRange.x;

            // Run LoopSemaphoreStates() and wait
            StartCoroutine(LoopSemaphoreStates(_currentSemaphore, openTime, redDuration, yellowDuration));
            yield return new WaitUntil(() => finishedSemaphoreLoop);
            finishedSemaphoreLoop = false;         
        }
    }

    IEnumerator LoopSemaphoreStates(Semaphore _s, float _green, float _red, float _yellow)
    {
        _s.SetState(LightState.yellow);
        _s.SetState(LightState.yellow);
        yield return new WaitForSeconds(_yellow);

        _s.SetState(LightState.green);
        _s.SetState(LightState.green);
        yield return new WaitForSeconds(_green);

        _s.SetState(LightState.red);
        _s.SetState(LightState.red);
        yield return new WaitForSeconds(_red);
        
        finishedSemaphoreLoop = true;
    }

    public void VehicleFinishedPath() => vehiclesPassed++;
    public void AddIdlingTime(float _time) => idlingTime += _time;
}
