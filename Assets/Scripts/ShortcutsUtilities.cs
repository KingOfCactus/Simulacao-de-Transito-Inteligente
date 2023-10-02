using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShortcutsUtilities : MonoBehaviour
{
    TrafficSystem trafficSystem;
    bool inFullscreen;

    void Start() => inFullscreen = Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen;

    // Update is called once per frame
    void Update()
    {
        // if (Input.GetKeyDown("escape"))
        //     Application.Quit();

        if (Input.GetKeyDown("tab"))
        {
            inFullscreen = !inFullscreen;
            Screen.fullScreenMode = inFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;

            if (inFullscreen)
            {
                Resolution targetRes = Screen.resolutions[Screen.resolutions.Length - 1];
                Screen.SetResolution(targetRes.width, targetRes.height, inFullscreen);
            }
        }
    }
}
