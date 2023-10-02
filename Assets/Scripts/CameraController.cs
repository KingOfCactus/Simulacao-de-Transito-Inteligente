using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    [Space(10)]
    public float moveSpeed;
    public Vector2 rotSpeed;
    Vector3 rot;

    [Header("Zoom")]
    [Space(10)]
    public float zoomSpeed;

    public Vector2 zoomRange;
    public Vector2 topZoomRange;
    
    Camera _cam;
    float zoom;

    Transform camTarget;
    Vector3 initialPos;
    Vector3 topViewRot;

    bool rotating;
    bool inTopView;

    // Start is called before the first frame update
    void Start()
    {
        rot = Vector3.zero;
        rot.z = -30f;
        rot.y = +45f;

        topViewRot = Vector3.zero;
        topViewRot.z = -90f;

        _cam = Camera.main;
        camTarget = transform.GetChild(0);

        zoom = _cam.orthographicSize;
        initialPos = camTarget.position;

        SetTargetPosition(initialPos);
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale <= 0)
            return;

        Vector3 input;
        input = new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"),
                           -Input.GetAxis("Mouse ScrollWheel"));

        if (Input.GetKeyDown("r"))
            SetTargetPosition(initialPos);

        if (!rotating && !inTopView && Input.GetKey("q"))
            StartCoroutine("Rotate", +90f);
        if (!rotating && !inTopView && Input.GetKey("e"))
            StartCoroutine("Rotate", -90f);


        if (Input.GetMouseButton(0))
        {
            if (!inTopView)
            {
                camTarget.transform.Translate(input.y * moveSpeed * (zoom/zoomRange.y) * Time.unscaledDeltaTime, 0,
                                             -input.x * moveSpeed * (zoom/zoomRange.y) * Time.unscaledDeltaTime);
            }
            else
            {
                camTarget.transform.Translate(-input.y * moveSpeed * Time.unscaledDeltaTime, 0,
                                               input.x * moveSpeed * Time.unscaledDeltaTime);
            }
        }


        zoom += input.z * zoomSpeed * Time.unscaledDeltaTime;
        zoom = Mathf.Clamp(zoom, zoomRange.x, zoomRange.y);

        _cam.orthographicSize = zoom;
        camTarget.rotation = Quaternion.Euler(inTopView ? topViewRot : rot);
    }


    IEnumerator Rotate(float degrees)
    {
        rotating = true;

        float oldRot = rot.y;
        float newRot = rot.y + degrees;

        for (float t = 0; t < 0.5f;  t += Time.unscaledDeltaTime)
        {
            rot.y = Mathf.SmoothStep(oldRot, newRot, t * 2);
            yield return null;
        }

        rotating = false;
        rot.y = newRot;
    }

    IEnumerator ChangeView()
    {
        rotating = true;
        
        float target = !inTopView ? topViewRot.z : rot.z;
        float oldRot = !inTopView ? rot.z : topViewRot.z;

        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            rot.y = Mathf.SmoothStep(oldRot, target, t * 2);
            yield return null;
        }

        rot.z = target;
        inTopView = !inTopView;
    }

    public void SetTargetPosition(Vector3 _target)
    {
        camTarget.position = _target;
        _cam.transform.LookAt(camTarget.position);
    }

    public void FollowTarget(Transform _target)
    {
        camTarget.position = _target.position;
        _cam.transform.LookAt(camTarget.position);
    }
}
