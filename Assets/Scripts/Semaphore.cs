using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum LightState { green, yellow, red }

public class Semaphore : MonoBehaviour
{
    #region Vars

    public Renderer[] lights;
    public Light spotLight;

    [HideInInspector]
    public bool isOpen => currentState == LightState.green;
    private LightState currentState;

    public float timeSinceOpened;
    void FixedUpdate() => timeSinceOpened += currentState == LightState.red ? Time.fixedDeltaTime : 0;

    void Awake() => SetState(LightState.red);
    #endregion

    public void SetState(LightState state)
    {
        currentState = state;
        UpdateLights();
    }

    // Lit the active light, unlit the others
    void UpdateLights()
    {
        switch (currentState)
        {
            // Set light to red
            case LightState.red:
                lights[0].material.EnableKeyword("_EMISSION");  
                lights[1].material.DisableKeyword("_EMISSION");
                lights[2].material.DisableKeyword("_EMISSION");
                spotLight.color = Color.red;
                break;

            // Set light to yellow
            case LightState.yellow:
                lights[0].material.DisableKeyword("_EMISSION");
                lights[1].material.EnableKeyword("_EMISSION");
                lights[2].material.DisableKeyword("_EMISSION");
                spotLight.color = Color.yellow;
                break;

            // Set light to green
            case LightState.green:
                lights[0].material.DisableKeyword("_EMISSION");
                lights[1].material.DisableKeyword("_EMISSION");
                lights[2].material.EnableKeyword("_EMISSION");
                spotLight.color = Color.green;
                timeSinceOpened = 0;
                break;

            // Just in case
            default:
                Debug.LogError("Illegal enum value: " + currentState);
                break;
        }
    }
}
