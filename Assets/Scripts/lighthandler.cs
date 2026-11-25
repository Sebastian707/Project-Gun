using UnityEngine;
using System.Collections;

public class lightHandler : MonoBehaviour
{
    [SerializeField] private float minLightIntensity;
    [SerializeField] private float maxLightIntensity;
    public float lightChangeTime;
    public float flickerLength;
    public float minLightTimer = 0.15f;
    public float maxLightTimer = 3.3f;
    private Light lightComponent;

    public void Awake()
    {
        if (lightComponent == null)
        {
            lightComponent = GetComponent<Light>();
            maxLightIntensity = lightComponent.intensity;
            minLightIntensity = lightComponent.intensity / 10;
            lightChangeTime = Random.Range(minLightTimer, maxLightTimer);
            StartCoroutine(changeLight());
        }

    }
    public void FixedUpdate()
    {





    }
    public IEnumerator changeLight()
    {
        lightChangeTime = Random.Range(minLightTimer, maxLightTimer);

        float timerTimer = lightChangeTime;
        while (timerTimer >= 0)
        {
            timerTimer -= Time.fixedDeltaTime;
            lightComponent.intensity = Random.Range(minLightIntensity, maxLightIntensity);
            yield return timerTimer;

        }

        lightComponent.intensity = maxLightIntensity;
        timerTimer = Random.Range(minLightTimer, maxLightTimer);

        yield return new WaitForSeconds(timerTimer);
        StartCoroutine(changeLight());

    }
}