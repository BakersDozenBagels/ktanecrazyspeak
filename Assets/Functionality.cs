using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using KMHelper;

public class Functionality : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Info;
    public KMSelectable[] buttons;
    public MeshRenderer[] buttonRenderers;
    public Material[] buttonMats;
    public KMSelectable lightButton;
    public KMSelectable mainButton;
    public MeshRenderer mainButtonRenderer;
    public GameObject barControl;
    public MeshRenderer bar;

    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;

    //Action modes are: -1: Error 0: Strike 1: Normal 2: Loacation six
    private int[][] planetActions;

    private string[] PATTERNS = { "YellowSpots", "BlueWaves", "GrayCraters", "GreenOcean", "GreenSquiggles", "PurpleTriangles", "RedStripes", "PurpleSpiral" };
    private int[] planetPatterns = new int[6];

    private bool _lightsOn = false, _isSolved = false;

    //Loading screen
    void Start() {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += Activate;
        Init();
    }

    //Lights off
    void Awake()
    {
        mainButton.OnInteract += delegate
        {
            handleMain();
            return false;
        };
        lightButton.OnInteract += delegate
        {
            handleLight();
            return false;
        };
        for (int i = 0; i < buttons.Length; i++)
        {
            int j = i;
            buttons[i].OnInteract += delegate ()
            {
                handlePress(j);
                return false;
            };
        }
    }

    //Lights on
    void Activate()
    {
        StartCoroutine("timer");
        _lightsOn = true;
    }

    //Logical initialization
    void Init()
    {
        //Randomize order of ordered
        int[] ordered = { 1, 2, 3, 4, 5, 6 };
        for (int i = 5; i > 0; i--)
        {
            int j = Random.Range(0, i);
            int tmp = ordered[i];
            ordered[i] = ordered[j];
            ordered[j] = tmp;
        }

        //Insert a sun
        int rng = Random.Range(0, 4);
        ordered[rng] = 0;
        planetPatterns = ordered;

        Debug.LogFormat("[Nomai #{0}] Initialised with planet order: #0: {1} #1: {2} #2: {4} #3: {3} #4: {5} #5: {6}", _moduleId, PATTERNS[ordered[0]], PATTERNS[ordered[1]], PATTERNS[ordered[3]], PATTERNS[ordered[2]], PATTERNS[ordered[4]], PATTERNS[ordered[5]]);

        //Render planets
        for(int x = 0; x < buttonRenderers.Length; x++)
        {
            buttonRenderers[x].material = buttonMats[planetPatterns[x]];
        }
        mainButtonRenderer.material = buttonMats[planetPatterns[5]];

        planetActions = new int[6][] { new int[6] { 1, 1, 1, 1, 1, 1 }, new int[6] { 1, 1, 1, 1, 1, 1 }, new int[6] { 1, 1, 1, 1, 1, 1 }, new int[6] { 1, 1, 1, 1, 1, 1 }, new int[6] { 1, 1, 1, 1, 1, 1 }, new int[6] { 1, 1, 1, 1, 1, 1 } };

        //Make all actions valid, except for two per planet
        for (int i = 0; i < 6; i++)
        {
            int a = Random.Range(0, 5);
            int b = Random.Range(0, 4);
            if (b == a)
            {
                b = 5;
            }
            planetActions[i][a] = 0;
            planetActions[i][b] = 0;
        }

        //Add sun
        int sunPos = -1;

        for (int i = 0; i < 6; i++)
        {
            if (planetPatterns[i] == 0)
            {
                sunPos = i;
                for (int x = 0; x < 6; x++)
                {
                    planetActions[i][x] = -1;
                }
            }
        }
        for (int n = 0; n < 5; n++)
        {
            int j = n;
            if (n == sunPos) { j = 5; }
            planetActions[j][sunPos] = 0;
        }

        //Add sixth location
        int fromPlanet = Random.Range(0, 4);
        if (fromPlanet == sunPos) { fromPlanet = 5; }
        int toPlanet = Random.Range(0, 3);
        if (toPlanet == fromPlanet) { toPlanet = 5; }
        if (toPlanet == sunPos) { toPlanet = 4; }
        planetActions[fromPlanet][toPlanet] = 2;

        //Check each planet is visitable
        for (int i = 0; i < 5; i++)
        {
            int j = i;
            if (i == sunPos) { j = 5; }
            int goodNum = 0;
            for (int k = 0; k < 6; k++)
            {
                if (k == sunPos || k == j) { continue; }
                if (planetActions[k][j] == 1) { goodNum++; }
            }

            while (goodNum < 2)
            {
                for (int k = 0; k < 5; k++)
                {
                    if (planetActions[k][j] == 0)
                    {
                        planetActions[k][j] = 1;
                        goodNum++;
                        break;
                    }
                }
            }
        }
        Debug.LogFormat("[Nomai #{0}] Action table:", _moduleId);
        foreach (int[] x in planetActions)
        {
            Debug.LogFormat("[{0}{1}{2}{3}{4}{5}]", x[0], x[1], x[2], x[3], x[4], x[5]);
        }
    }

    IEnumerator timer()
    {
        for (int i = 0; i<220; i++)
        {
            barControl.gameObject.transform.localScale = new Vector3((220f-i)/220f,0.01f,0.01f);
            yield return new WaitForSeconds(0.1f);
        }
        barControl.gameObject.transform.localScale = new Vector3(0f, 0.01f, 0.01f);
        onTimerEnd();
        yield return null;
    } 

    IEnumerator reset()
    {
        for (int i = 0; i < 80; i++)
        {
            barControl.gameObject.transform.localScale = new Vector3(i / 80f, 0.01f, 0.01f);
            yield return new WaitForSeconds(0.1f);
        }
        barControl.gameObject.transform.localScale = new Vector3(1f, 0.01f, 0.01f);
        onTimerReset();
        StartCoroutine("timer");
        yield return null;
    }

    void handleMain()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, mainButton.transform);
        mainButton.AddInteractionPunch();
        if (!_lightsOn || _isSolved) { return; }
        Debug.LogFormat("[Nomai #{0}] Main button pressed.", _moduleId);
    }

    void handleLight()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, lightButton.transform);
        lightButton.AddInteractionPunch();
        if (!_lightsOn || _isSolved) { return; }
        Debug.LogFormat("[Nomai #{0}] Status light pressed.", _moduleId);
    }

    void handlePress(int id)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, buttons[id].transform);
        buttons[id].AddInteractionPunch();
        if (!_lightsOn || _isSolved) { return; }
        Debug.LogFormat("[Nomai #{0}] Planet {1} pressed.", _moduleId, id);
    }

    void onTimerEnd()
    {
        onFakeStrike();
    }

    void onTimerReset()
    {

        StopCoroutine("reset");
        StartCoroutine("timer");
    }

    void onFakeStrike()
    {

        StopCoroutine("timer");
        StartCoroutine("reset");
    }

    void onFakeSolve()
    {

    }
}
