using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
public class Clock : MonoBehaviour
{

    public Transform hour;
    public Transform minute;
    public Transform second;
    public EventReference ClockSound;

    public Vector3 rotationVector = new Vector3(0, 0, 1);

    int lastSecond = -1;



    void Start()
    {



        second.SetAsLastSibling();

    }

    bool automaticMotion;
    void Update()
    {


        float f_hour = (float)System.DateTime.Now.Hour;

        float f_minute = (float)System.DateTime.Now.Minute;

        float f_second = (float)System.DateTime.Now.Second;

        float miliseconds = (float)System.DateTime.Now.Millisecond;


        float ang = 0;

        if (automaticMotion)
            f_second += (miliseconds / 1000f);
        else
        {
            if (miliseconds < 32)
                ang += 1.5f;
        }

        hour.localEulerAngles = -rotationVector * ((f_hour + (f_minute / 60)) / 12) * 360;

        minute.localEulerAngles = -rotationVector * (f_minute / 60 * 360);

        second.localEulerAngles = -rotationVector * ((f_second / 60 * 360) + ang);
        int currentSecond = (int)f_second;
        if (currentSecond != lastSecond)
        {
            RuntimeManager.PlayOneShot(ClockSound, transform.position);
            lastSecond = currentSecond;
        }

    }
}
