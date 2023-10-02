using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SimulationUI : MonoBehaviour
{
    #region Vars
    public TrafficSystem trafficSystem;

    [Header("Main Window")]
    [Space(10)]
    [SerializeField] GameObject mainMenuWindow;
    [SerializeField] GameObject[] slides;
    

    [Header("Settings Window")]
    [Space(10)]
    [SerializeField] Button settingsStartBtn;
    [SerializeField] GameObject settingsWindow;
    [SerializeField] TextMeshProUGUI settingsSpeedText;

    [Header("Pause Window")]
    [Space(10)]
    [SerializeField] GameObject pauseWindow;
    [SerializeField] Slider pauseSpeedSlider;
    [SerializeField] TextMeshProUGUI pauseSpeedText;
    [SerializeField] TextMeshProUGUI pauseProgressText;

    [Header("Misc.")]
    [Space(10)]
    [SerializeField] CameraController camController;
    [SerializeField] GameObject resultsWindow;
    
    [HideInInspector]
    public static SimulationUI Instance;
    void Awake() => Instance = this;
    
    TrafficMode simulationMode = TrafficMode.None;
    float speed = 1, sampleTime = 0;
    #endregion
    
    #region Windows
    public void StartSlideShow() => StartCoroutine("MyPowerpointShow");
    IEnumerator MyPowerpointShow()
    {
        int i = 0;
        slides[0].SetActive(true);
        mainMenuWindow.SetActive(false);
        
        while (i < slides.Length)
        {
            yield return null;
            
            if (Input.GetKeyDown("right") || Input.GetKeyDown("left"))
            {
                slides[i].SetActive(false);
                i += (int)Input.GetAxisRaw("Horizontal");
                if (i == slides.Length)
                    continue;

                i = Mathf.Max(0, i);
                slides[i].SetActive(true);
            }
        }

        mainMenuWindow.SetActive(true);
    }

    public void ShowSettingsWindow()
    {
        simulationMode = TrafficMode.None;
        sampleTime = 0;
        speed = 1;

        mainMenuWindow.SetActive(false);
        settingsWindow.SetActive(true);
        UpdateStartButton();
    }

    public void ShowPauseWindow(float speed, float elapsedTime)
    {
        pauseWindow.SetActive(true);
        pauseSpeedSlider.value = speed;
        pauseSpeedText.text = $"Velocidade ({speed}X)";

        pauseProgressText.text = pauseProgressText.text.Replace("/t/", elapsedTime.ToString("0.0"));
        pauseProgressText.text = pauseProgressText.text.Replace("/T/", sampleTime.ToString());
    }

    public void CloseSettingsWindow()
    {
        pauseWindow.SetActive(false);
        pauseProgressText.text = "<color=yellow>Progresso:</color> /t/ / /T/ minutos";
    }

    public void ShowResults(float idle, float frequency, float elapsedTime)
    {
        resultsWindow.SetActive(true);
        TextMeshProUGUI _text = resultsWindow.GetComponentsInChildren<TextMeshProUGUI>()[1];

        _text.text = _text.text.Replace("/E/", idle.ToString("0.00"));
        _text.text = _text.text.Replace("/F/", frequency.ToString("0.00"));
        _text.text = _text.text.Replace("/T/", elapsedTime.ToString("0.0"));
        
        Time.timeScale = 0;
    }
    #endregion

    #region Simulation
    public void StartSimulation()
    {
        trafficSystem.sampleTime = sampleTime;
        trafficSystem.simulationSpeed = speed;
        trafficSystem.trafficMode = simulationMode;

        camController.enabled = true;
        settingsWindow.SetActive(false);
        trafficSystem.gameObject.SetActive(true);
    }

    public void RestartSimulation()
    {
        int sceneId = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(sceneId);
    }

    public void StopSimulation()
    {
        trafficSystem.Resume();
        trafficSystem.simulationEnded = true;
    }

    public void CloseApplication() => Application.Quit();
    #endregion

    #region Update Values
    public void ChangeSimulationMode(int value)
    {
        simulationMode = value == 0 ? TrafficMode.Normal : TrafficMode.Smart;
        UpdateStartButton();    
    }

    public void UpdateSpeed(float value) 
    {
        if (trafficSystem.isPaused)
        {
            pauseSpeedText.text = $"Velocidade ({value}X)";
            trafficSystem.simulationSpeed = value;
        }
        else
        {
            settingsSpeedText.text = $"Velocidade ({value}X)";
            UpdateStartButton(); 
        }

        speed = value;
    }

    public void UpdateSampleTime(string value)
    {
        if (value.Equals(""))
            sampleTime = 0;
        else
            sampleTime = int.Parse(value);
            
        UpdateStartButton();
    }

    void UpdateStartButton() => settingsStartBtn.interactable = simulationMode != TrafficMode.None && sampleTime > 0;
    #endregion
}
