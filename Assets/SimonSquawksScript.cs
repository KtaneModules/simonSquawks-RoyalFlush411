using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class SimonSquawksScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMNeedyModule needyModule;
    public KMAudio Audio;

    public LightInformation[] buttons;
    public Material[] materialOptions;
    public String[] colourNameOptions;
    public String[] colourInitialOptions;
    public AudioClip[] sfxOptions;
    private List<int> chosenIndices = new List<int>();
    private List<int> chosenIndices2 = new List<int>();

    public String[] press1SolutionOptions;
    public String[] press2SolutionOptions;

    public TextMesh[] squawkLetters;
    public Color[] squawkColours;
    private int[] flashes = new int[2];
    private int stage = 0;
    private String[] solution = new String[2];
    private String[] solutionLog = new String[2];

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool inactive = true;
    private bool active;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        needyModule = GetComponent<KMNeedyModule>();
        needyModule.OnNeedyActivation += OnNeedyActivation;
        needyModule.OnNeedyDeactivation += OnNeedyDeactivation;
        needyModule.OnTimerExpired += OnTimerExpired;
        foreach (LightInformation button in buttons)
        {
            LightInformation pressedButton = button;
            button.selectable.OnInteract += delegate () { ButtonPress(pressedButton); return false; };
        }
    }


    void Start()
    {
        foreach(LightInformation device in buttons)
        {
            device.colourIndex = UnityEngine.Random.Range(0,8);
            while(chosenIndices.Contains(device.colourIndex))
            {
                device.colourIndex = UnityEngine.Random.Range(0,8);
            }
            chosenIndices.Add(device.colourIndex);

            device.soundIndex = UnityEngine.Random.Range(0,8);
            while(chosenIndices2.Contains(device.soundIndex))
            {
                device.soundIndex = UnityEngine.Random.Range(0,8);
            }
            chosenIndices2.Add(device.soundIndex);

            float scalar = transform.lossyScale.x;
            device.lightObject.range *= scalar;
            device.renderer.material = materialOptions[device.colourIndex];
            device.lightColour = colourNameOptions[device.colourIndex];
            device.colourInitial = colourInitialOptions[device.colourIndex];
            device.connectedSound = sfxOptions[device.soundIndex];
            device.lightObject.enabled = false;
        }
        chosenIndices.Clear();
        chosenIndices2.Clear();
        Array.Sort(buttons, (a, b) => a.colourIndex.CompareTo(b.colourIndex));
        ColourizeText();
        StartCoroutine(IdleFlash());
    }

    void ColourizeText()
    {
        foreach(TextMesh letter in squawkLetters)
        {
            int index = UnityEngine.Random.Range(0,8);
            while(chosenIndices.Contains(index))
            {
                index = UnityEngine.Random.Range(0,8);
            }
            chosenIndices.Add(index);
            letter.color = squawkColours[index];
        }
        chosenIndices.Clear();
    }

    void OnNeedyActivation()
    {
        flashes[0] = UnityEngine.Random.Range(0,8);
        flashes[1] = UnityEngine.Random.Range(0,8);
        inactive = false;
        solution[0] = press1SolutionOptions[(flashes[0] * 8) + flashes[1]];
        solution[1] = press2SolutionOptions[(flashes[0] * 8) + flashes[1]];
        StartCoroutine(ActiveFlash());
        Debug.LogFormat("[Simons Squawks #{0}] Needy activated! Your colour flashes are {1} & {2}.", moduleId, buttons[flashes[0]].lightColour, buttons[flashes[1]].lightColour);
        for(int i = 0; i < buttons.Count(); i++)
        {
            if(solution[0] == buttons[i].colourInitial)
            {
                solutionLog[0] = buttons[i].lightColour;
                break;
            }
        }
        for(int i = 0; i < buttons.Count(); i++)
        {
            if(solution[1] == buttons[i].colourInitial)
            {
                solutionLog[1] = buttons[i].lightColour;
                break;
            }
        }
        Debug.LogFormat("[Simons Squawks #{0}] Press {1} then {2}.", moduleId, solutionLog[0], solutionLog[1]);
    }

    void OnNeedyDeactivation()
    {
        GetComponent<KMNeedyModule>().HandlePass();
        Debug.LogFormat("[Simons Squawks #{0}] Needy deactivated!", moduleId);
    }

    void OnTimerExpired()
    {
        Debug.LogFormat("[Simons Squawks #{0}] Strike! You ran out of time.", moduleId);
        GetComponent<KMNeedyModule>().HandleStrike();
        stage = 0;
        active = false;
        OnNeedyDeactivation();
        StartCoroutine(IdleFlash());
    }

    void ButtonPress(LightInformation device)
    {
        if(inactive)
        {
            return;
        }
        if(active)
        {
            active = false;
        }
        device.selectable.AddInteractionPunch();
        StartCoroutine(PressRoutine(device));
        if(solution[stage] == device.colourInitial)
        {
            stage++;
            Debug.LogFormat("[Simons Squawks #{0}] You pressed {1}. That is correct.", moduleId, device.lightColour);
        }
        else
        {
            Debug.LogFormat("[Simons Squawks #{0}] Strike! You pressed {1}. That is not correct.", moduleId, device.lightColour);
            GetComponent<KMNeedyModule>().HandleStrike();
            stage = 2;
        }
        if(stage == 2)
        {
            stage = 0;
            inactive = true;
            OnNeedyDeactivation();
            StartCoroutine(IdleFlash());
        }
    }

    IEnumerator PressRoutine(LightInformation device)
    {
        yield return new WaitForSeconds(0.1f);
        Audio.PlaySoundAtTransform(device.connectedSound.name, transform);
        device.lightObject.enabled = true;
        ColourizeText();
        yield return new WaitForSeconds(0.3f);
        device.lightObject.enabled = false;
        for(int i = 0; i < buttons.Count(); i ++)
        {
            squawkLetters[i].color = squawkColours[8];
        }
    }

    IEnumerator IdleFlash()
    {
        yield return new WaitUntil(() => inactive);
        for(int i = 0; i < buttons.Count(); i ++)
        {
            buttons[i].lightObject.enabled = false;
            squawkLetters[i].color = squawkColours[8];
        }
        yield return new WaitForSeconds(2f);
        while(inactive)
        {
            for(int i = 0; i < buttons.Count(); i ++)
            {
                buttons[i].lightObject.enabled = true;
            }
            ColourizeText();
            yield return new WaitForSeconds(0.3f);
            for(int i = 0; i < buttons.Count(); i ++)
            {
                buttons[i].lightObject.enabled = false;
                squawkLetters[i].color = squawkColours[8];
            }
            yield return new WaitForSeconds(2f);
        }
        active = true;
    }

    IEnumerator ActiveFlash()
    {
        yield return new WaitUntil(() => active);
        while(active)
        {
            for(int i = 0; i < 2; i++)
            {
                buttons[flashes[i]].lightObject.enabled = true;
                Audio.PlaySoundAtTransform(buttons[flashes[i]].connectedSound.name, transform);
                ColourizeText();
                yield return new WaitForSeconds(0.5f);
                buttons[flashes[i]].lightObject.enabled = false;
                for(int j = 0; j < buttons.Count(); j ++)
                {
                    squawkLetters[j].color = squawkColours[8];
                }
                if(!active)
                {
                    break;
                }
                yield return new WaitForSeconds(0.5f);
            }
            if(!active)
            {
                break;
            }
            yield return new WaitForSeconds(1.5f);
            if(!active)
            {
                break;
            }
        }
        inactive = true;
    }
}
