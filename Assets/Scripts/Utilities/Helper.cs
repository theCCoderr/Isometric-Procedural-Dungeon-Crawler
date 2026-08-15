using System.Collections.Generic;
using UnityEngine;

public class Helper : MonoBehaviour
{
    private static readonly Dictionary<float, WaitForSeconds> WaitDictionary = new Dictionary<float, WaitForSeconds>();

    static long x = 123456789, y = 362436069, z = 521288629;

    public static long Xorshf96()
    {
        //period 2^96-1
        x ^= x << 16;
        x ^= x >> 5;
        x ^= x << 1;

        var t = x;
        x = y;
        y = z;
        z = t ^ x ^ y;

        return z;
    }

    public static WaitForSeconds GetWait(float time)
    {
        if (WaitDictionary.TryGetValue(time, out var wait)) return wait;

        WaitDictionary[time] = new WaitForSeconds(time);
        return WaitDictionary[time];
    }


    public static Vector3 Iso2(Vector3 v)
    {
        var rotation = Quaternion.Euler(60f, 0f, 45f);
        var isoMatrix = Matrix4x4.Rotate(rotation);
        var result = isoMatrix.MultiplyPoint3x4(v);
        return result;
    }

    public static Quaternion[] CalculateRotations(int bulletNum, float spreadAngle, Transform gunTrans)
    {
        var angleStep = spreadAngle / bulletNum;
        var aimingAngle = gunTrans.rotation.eulerAngles.z;
        var centeringOffset = spreadAngle / 2 - angleStep / 2;
        //offsets every projectile so the spread is                                                                                                                         //centered on the mouse cursor

        var rotations = new Quaternion[(int) bulletNum];
        for (var i = 0; i < bulletNum; i++)
        {
            var currentBulletAngle = angleStep * i;
            rotations[i] = Quaternion.Euler(new Vector3(0, 0, aimingAngle + currentBulletAngle - centeringOffset));
        }

        return rotations;
    }
}