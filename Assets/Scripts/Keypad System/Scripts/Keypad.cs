using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FMODUnity;
using FMOD.Studio;

public class KeypadManager : MonoBehaviour
{

    public GameObject door;
    private bool doorOpen;

    [Header("Screen Settings")]
    public TextMeshPro screenText;

    [Header("Keypad Settings")]
    public string correctCode = "1234";
    public int maxCodeLength = 4;

    public EventReference FailSound;
    public EventReference WinSound;

    private string currentCode = "";

    public void ButtonPressed(string value)
    {
        if (currentCode.Length >= maxCodeLength)
        {
            currentCode = "";
        }

        currentCode += value;
        UpdateScreen();

        if (currentCode.Length == maxCodeLength)
        {
            StartCoroutine(CheckCodeCoroutine());
        }
    }

    private void UpdateScreen()
    {
        screenText.text = currentCode;
    }

    private IEnumerator CheckCodeCoroutine()
    {
        if (currentCode == correctCode)
        {
            Debug.Log("Code Correct! Unlock!");
            RuntimeManager.PlayOneShot(WinSound);
            screenText.color = Color.green; // Change text to green
            doorOpen = !doorOpen;
            door.GetComponent<Animator>().SetBool("IsOpen", doorOpen);
        }
        else
        {
            Debug.Log("Incorrect Code!");
            RuntimeManager.PlayOneShot(FailSound);
            screenText.color = Color.red;
        }

        yield return new WaitForSeconds(1f);


        currentCode = "";
        screenText.text = "";
        screenText.color = Color.white; 
    }
}
