using System;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class MCamera : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform mouseFollower;
    [SerializeField] private Camera cam;
    private Transform shakeTr;
    private Transform targetGroup;
    private CinemachineVirtualCamera cmv;
    private static float Intensity;
    private static float RandI;
    private float t = 0.5f;
    private static bool Shake;
    private Ray r;

    private void Start()
    {
        cmv = GetComponent<CinemachineVirtualCamera>();
        targetGroup = cmv.m_Follow;
        shakeTr = mouseFollower;
        cam.transparencySortAxis = Vector3.up;
    }

    public void Update()
    {
        mouseFollower.position = PlayerAim.GetWorldMousePos();
        if (Shake)
        {
            if (t <= 0.05) Shake = false;
            shakeTr.position = GetPosition();
            cmv.m_Follow = shakeTr;
            cmv.m_LookAt = shakeTr;
            t -= Time.fixedTime / 2;
        }
        else
        {
            cmv.m_Follow = targetGroup;
            t = 0.5f;
        }

    }

    private Vector3 GetPosition()
    {
        var position = player.position;
        r = new Ray(cam.transform.position, Vector3.Normalize(mouseFollower.position - position));
        return r.GetPoint(2 * Intensity);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(r);
    }

    public static void ShakeCamera(float intensity1, float randIntensity1)
    {
        Shake = true;
        RandI = randIntensity1;
        Intensity = intensity1;
    }
}