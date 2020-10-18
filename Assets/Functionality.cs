using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using Nomai;

//Nobody is allowed to fix this code!!!

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
    public Material[] barMats;
    public GameObject barControl;
    public MeshRenderer bar;

    private int barColor = 0;

    private int goalColor = -1;

    private readonly string[] COLORS = { "Default", "Red", "Green", "Blue"};

    //Status light colors are: 0: Default 1: Red 2: Green 3: Blue

    public FakeStatusLight FakeStatusLight;

    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;

    //Action modes are: -1: Error 0: Strike 1: Normal 2: Loacation six
    private int[][] planetActions;

    private readonly string[] PATTERNS = { "YellowSpots", "BlueWaves", "GrayCraters", "GreenOcean", "GreenSquiggles", "PurpleTriangles", "RedStripes", "PurpleSpiral" };
    private int[] planetPatterns = new int[6];

    private bool _lightsOn = false, _isSolved = false, _isResetting = false, _isSixth = false, _isDeact = false;

    private volatile float timeRatio = 0f;
    private ArrayList timeEvents = new ArrayList();
    private ArrayList timeEventsTimes = new ArrayList();

    private StatusLightState currentStatusLightState = StatusLightState.Off;

    private int[] planetsOrder = { 0, 1, 2, 3, 4, 5 };

    private int deactMethod = 0;

    private Action[] previous3 = new Action[3];

    private int[] colorActionsMain = new int[7], colorActionsLight = new int[7];

    private bool case9 = false, _willDeact = false;

    private Action case1 = null;

    class Action
    {
        public int AtId;
        public int PressedId;
        public bool DidStrike;
        public int ToId;

        public Action(int AtId, int PressedId, bool DidStrike, int ToId)
        {
            this.AtId = AtId;
            this.PressedId = PressedId;
            this.DidStrike = DidStrike;
            this.ToId = ToId;
        }
    }

    //Loading screen
    void Start() {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += Activate;
        Init();

        FakeStatusLight = Instantiate(FakeStatusLight);
        FakeStatusLight.GetStatusLights(transform);
        FakeStatusLight.Module = Module;
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
        int[] ordered = { 6, 2, 3, 4, 5, 1 };
        for (int i = 5; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = ordered[i];
            ordered[i] = ordered[j];
            ordered[j] = tmp;
        }

        //Insert a sun
        int rng = Random.Range(0, 5);
        ordered[rng] = 0;
        planetPatterns = ordered;

        Debug.LogFormat("[Nomai #{0}] Initialised with planet order: #0: {1} #1: {2} #2: {4} #3: {3} #4: {5} #5(main): {6}", _moduleId, PATTERNS[ordered[0]], PATTERNS[ordered[1]], PATTERNS[ordered[3]], PATTERNS[ordered[2]], PATTERNS[ordered[4]], PATTERNS[ordered[5]]);

        //Render planets
        for (int x = 0; x < buttonRenderers.Length; x++)
        {
            buttonRenderers[x].material = buttonMats[planetPatterns[x]];
        }
        mainButtonRenderer.material = buttonMats[planetPatterns[5]];

        planetActions = new int[6][] { new int[6] { 1, 1, 1, 1, 1, 1 }, new int[6] { 1, 1, 1, 1, 1, 1 }, new int[6] { 1, 1, 1, 1, 1, 1 }, new int[6] { 1, 1, 1, 1, 1, 1 }, new int[6] { 1, 1, 1, 1, 1, 1 }, new int[6] { 1, 1, 1, 1, 1, 1 } };

        //Make all actions valid, except for two per planet
        for (int i = 0; i < 6; i++)
        {
            int a = Random.Range(0, 6);
            int b = Random.Range(0, 5);
            if (b == a)
            {
                b = 5;
            }
            planetActions[i][a] = 0;
            planetActions[i][b] = 0;
        }

        //Make self-actions error
        for (int i = 0; i < 6; i++)
        {
            planetActions[i][i] = -1;
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
        int fromPlanet = Random.Range(0, 5);
        if (fromPlanet == sunPos) { fromPlanet = 5; }
        int toPlanet = Random.Range(0, 4);
        if (toPlanet == fromPlanet) { toPlanet = 5; }
        planetActions[fromPlanet][toPlanet] = 2;

        if (toPlanet == sunPos) { toPlanet = 4; }
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
            Debug.LogFormat("[Nomai #{6}] [{0}{1}{2}{3}{4}{5}]", x[0], x[1], x[2], x[3], x[4], x[5], _moduleId);
        }

        //Determine deactivation condition.
        switch (ordered[5])
        {
            case 1:
                Regex a = new Regex("[" + KMBombInfoExtensions.GetSerialNumberLetters(Info).Join() + "]", RegexOptions.IgnoreCase);
                if (a.Match(KMBombInfoExtensions.GetIndicators(Info).Join()).Success)
                {
                    deactMethod = 1;
                    addColorsRandom(-1);
                }
                else
                {
                    deactMethod = 2;
                    //Make sure this method is possible if it occurs.
                    for (int i= 0; i < 6; i++)
                    {
                        if (planetActions[i][5] == 0)
                        {
                            planetActions[i][5] = 1;
                            break;
                        }
                    }
                    addColorsRandom(-1);
                }
                break;
            case 2:
                if (KMBombInfoExtensions.GetOffIndicators(Info).Count() >= 2)
                {
                    deactMethod = 3;
                    addColorsRandom(6);
                }
                else
                {
                    deactMethod = 4;
                    //Add colors
                    int count = 0;
                    for (int i = 0; i < 7; i++)
                    {
                        colorActionsLight[i] = Random.Range(0, 5) == 0 ? 1 : 0;
                        count += colorActionsLight[i] + colorActionsMain[i];
                        if (count > 5) { break; }
                    }
                    while (count < 3)
                    {
                        int i = Random.Range(0, 7);
                        if (colorActionsLight[i] == 0) { colorActionsLight[i] = 1; count++; }
                    }
                }
                break;
            case 3:
                if (KMBombInfoExtensions.GetOnIndicators(Info).Count() >= 2)
                {
                    deactMethod = 5;
                    addColorsRandom(-1);
                }
                else
                {
                    deactMethod = 6;
                    addColorsRandom(-1);
                }
                break;
            case 4:
                if (KMBombInfoExtensions.GetBatteryCount(Info) >= 3)
                {
                    deactMethod = 7;
                    addColorsRandom(-1);
                }
                else
                {
                    deactMethod = 8;
                    addColorsRandom(-1);
                }
                break;
            case 5:
                if (KMBombInfoExtensions.GetBatteryHolderCount(Info) >= 2)
                {
                    deactMethod = 9;
                    addColorsRandom(sunPos);
                }
                else
                {
                    deactMethod = 10;
                    addColorsRandom(-1);
                }
                break;
            case 6:
                Regex b = new Regex("[aeiou]", RegexOptions.IgnoreCase);
                if (b.Match(KMBombInfoExtensions.GetSerialNumberLetters(Info).Join()).Success)
                {
                    deactMethod = 11;
                    addColorsRandom(6);
                }
                else
                {
                    deactMethod = 12;
                    //Add colors
                    int count = 0;
                    for (int i = 0; i < 7; i++)
                    {
                        colorActionsLight[i] = Random.Range(0, 5) == 0 ? 1 : 0;
                        count += colorActionsMain[i] + colorActionsMain[i];
                        if (count > 5) { break; }
                    }
                    while (count < 3)
                    {
                        int i = Random.Range(0, 7);
                        if (colorActionsMain[i] == 0) { colorActionsMain[i] = 1; count++; }
                    }
                }
                break;
        }

        Debug.LogFormat("[Nomai #{0}] Deactivation method: {1}", _moduleId, deactMethod);

        goalColor = Random.Range(1,4);

        Debug.LogFormat("[Nomai #{0}] Goal color: {1}", _moduleId, COLORS[goalColor]);
    }

    IEnumerator timer()
    {
        for (float i = 0f; i<220f; i++)
        {
            timeRatio = (220f - i) / 220f;
            barControl.gameObject.transform.localScale = new Vector3(timeRatio,0.01f,0.01f);
            yield return new WaitForSeconds(0.1f);
        }
        timeRatio = 0f;
        barControl.gameObject.transform.localScale = new Vector3(0f, 0.01f, 0.01f);
        onTimerEnd();
        yield return null;
    } 

    IEnumerator reset()
    {
        for (float i = 0f; i < 80f; i++)
        {
            timeRatio = i / 80f;
            barControl.gameObject.transform.localScale = new Vector3(timeRatio, 0.01f, 0.01f);

            for (int e = timeEvents.Count -1; e >= 0; e--)
            {
                if (e < 0) { break; }

                if ((float)timeEventsTimes[e] <= timeRatio)
                {
                    if((string)timeEvents[e] == "FakeSolve")
                    {
                        FakeStatusLight.SetInActive();
                        currentStatusLightState = StatusLightState.Off;
                    }

                    if ((string)timeEvents[e] == "Swap 0")
                    {
                        planetSwap(0, false);
                    }

                    if ((string)timeEvents[e] == "Swap 1")
                    {
                        planetSwap(1, false);
                    }

                    if ((string)timeEvents[e] == "Swap 2")
                    {
                        planetSwap(2, false);
                    }

                    if ((string)timeEvents[e] == "Swap 3")
                    {
                        planetSwap(3, false);
                    }

                    if ((string)timeEvents[e] == "Swap 4")
                    {
                        planetSwap(4, false);
                    }

                    if ((string)timeEvents[e] == "Swap 7")
                    {
                        reRender();
                    }

                    if ((string)timeEvents[e] == "Color Change")
                    {
                        ChangeColor(-1);
                    }

                    if ((string)timeEvents[e] == "Color Add")
                    {
                        barColor = 0;
                        bar.material = barMats[0];
                    }

                        timeEvents.RemoveAt(e);
                    timeEventsTimes.RemoveAt(e);
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
        timeRatio = 1;
        barControl.gameObject.transform.localScale = new Vector3(1f, 0.01f, 0.01f);
        onTimerReset();
        yield return null;
    }

    void handleMain()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, mainButton.transform);
        mainButton.AddInteractionPunch();
        if (!_lightsOn || _isSolved || _isResetting) { return; }
        Debug.LogFormat("[Nomai #{0}] Main button pressed.", _moduleId);
        AddAction(_isSixth ? 6 : planetPatterns[planetsOrder[5]],6,false);
        if (colorActionsMain[_isSixth ? 6 : planetsOrder[5]] == 1)
        {
            if (barColor == 0)
            {
                timeEvents.Add("Color Add");
                timeEventsTimes.Add((float)timeRatio);
            }
            else
            {
                timeEvents.Add("Color Change");
                timeEventsTimes.Add((float)timeRatio);
            }
            ChangeColor(1);
        }
        _isDeact = _isDeact || checkDeact();
        if (_isDeact) { Debug.LogFormat("[Nomai #{0}] Careful! Looping mechanism disabled.", _moduleId); }
    }

    void handleLight()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, lightButton.transform);
        lightButton.AddInteractionPunch();
        if (!_lightsOn || _isSolved || _isResetting) { return; }
        Debug.LogFormat("[Nomai #{0}] Status light pressed.", _moduleId);
        AddAction(_isSixth ? 6 : planetPatterns[planetsOrder[5]], 7, false);
        if(colorActionsLight[_isSixth ? 6 : planetsOrder[5]] == 1)
        {
            if (barColor == 0)
            {
                timeEvents.Add("Color Add");
                timeEventsTimes.Add((float)timeRatio);
            }
            else
            {
                timeEvents.Add("Color Change");
                timeEventsTimes.Add((float)timeRatio);
            }
            ChangeColor(1);
        }
        _isDeact = _isDeact || checkDeact();
        if (_isDeact) { Debug.LogFormat("[Nomai #{0}] Careful! Looping mechanism disabled.", _moduleId); }
    }

    void handlePress(int id)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, buttons[id].transform);
        buttons[id].AddInteractionPunch();
        if (!_lightsOn || _isSolved || _isResetting) { return; }
        Debug.LogFormat("[Nomai #{0}] Planet {1} pressed.", _moduleId, id);

        bool struck = false;
        bool wasSixth = _isSixth;

        if (!_isSixth) { struck = planetSwap(id, true); }
        else { onFakeStrike(); struck |= true; }
        if (!struck)
        { 
            AddAction(wasSixth ? 7 : planetPatterns[planetsOrder[5]], id, struck);

            _isDeact = _isDeact || checkDeact();
            if (_isDeact)
            {
                    Debug.LogFormat("[Nomai #{0}] Careful! Looping mechanism disabled.", _moduleId);
            }
        }
        else
        {
            if (deactMethod == 9 && previous3[0].AtId == 5 && previous3[0].PressedId == 6) { case9 = true; }
            if (case9) { _willDeact = true; }
            case9 = false;

            if (deactMethod == 1)
            {
                try
                {
                    if (case1.AtId == (_isSixth ? 7 : planetPatterns[planetsOrder[5]]) && case1.ToId == (id <= 5 ? planetPatterns[planetsOrder[id]] : -1))
                    {
                        _willDeact = true;
                    }
                }
                catch { }
                finally { }
                case1 = new Action(_isSixth ? 7 : planetPatterns[planetsOrder[5]], -1, true, id <= 5 ? planetPatterns[planetsOrder[id]] : -1);
            }

            if ((id <= 5 ? planetPatterns[planetsOrder[id]] : -1) == 0 && deactMethod == 5)
            {
                _willDeact = true;
            }
        }
    }

    void onTimerEnd()
    { 
        _isResetting = true;
        StopCoroutine("timer");
        StartCoroutine("reset");

        if (_isSixth && barColor == goalColor)
        {
            onFakeSolve();
            return;
        }

        onFakeStrike();
        case9 = false;
    }

    void onTimerReset()
    {
        planetsOrder = new int[] { 0, 1, 2, 3, 4, 5 };
        reRender();

        _isSixth = false;
        _isResetting = false;
        if (_willDeact) { _isDeact = true; Debug.LogFormat("[Nomai #{0}] Time looping mechanism deactivated.", _moduleId); }
        barColor = 0;
        bar.material = barMats[0];
        StopCoroutine("reset");
        StartCoroutine("timer");

        previous3 = new Action[3];

        Debug.LogFormat("[Nomai #{0}] Timer reset, you can now interact with the module again.", _moduleId);

        //Debug.LogFormat("Order:{0}{1}{2}{3}{4}{5}", planetsOrder[0], planetsOrder[1], planetsOrder[2], planetsOrder[3], planetsOrder[4], planetsOrder[5]);
        //Debug.LogFormat("IsSolved:{0} IsSixth:{1}", _isSolved, _isSixth);
    }

    void onFakeStrike()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, Module.transform);
        FakeStatusLight.FlashStrikeFixed(currentStatusLightState);
        if (_isDeact)
        {
            FakeStatusLight.HandleStrike();
            Debug.LogFormat("[Nomai #{0}] Real strike! Module will regenerate.", _moduleId);
            StopCoroutine("timer");
            StopCoroutine("reset");
            _isSolved = false;
            _isResetting = false;
            _isSixth = false;
            _isDeact = false;
            _willDeact = false;
            timeEvents = new ArrayList();
            timeEventsTimes = new ArrayList();
            previous3 = new Action[3];
            colorActionsMain = new int[7];
            colorActionsLight = new int[7];
            case1 = null;
            Init();
            reRender();
            StartCoroutine("timer");
            return;
        }
        _isResetting = true;
        Debug.LogFormat("[Nomai #{0}] Fake strike! Module is not interactable until next loop.", _moduleId);
    }

    void onFakeSolve()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, Module.transform);
        if (_isDeact && barColor == goalColor)
        {
            _isSolved = true;
            Debug.LogFormat("[Nomai #{0}] Real solve! Congratulations!", _moduleId);
            FakeStatusLight.HandlePass();
            StopAllCoroutines();
            return;
        }

        currentStatusLightState = StatusLightState.Green;
        timeEvents.Add("FakeSolve");
        timeEventsTimes.Add((float)timeRatio);
        FakeStatusLight.SetPass();
        _isResetting = true;

        Debug.LogFormat("[Nomai #{0}] Fake solve! Module is not interactable until next loop.", _moduleId);
    }

    bool planetSwap(int id, bool normal)
    {
        if (planetActions[planetsOrder[5]][planetsOrder[id]] == -1)
        {
            Debug.LogFormat("[Nomai #{0}] Fatal error, regenerating module.", _moduleId);
            StopCoroutine("timer");
            StopCoroutine("reset");
            _isSolved = false;
            _isResetting = true;
            _isSixth = false;
            _isDeact = false;
            _willDeact = false;
            timeEvents = new ArrayList();
            timeEventsTimes = new ArrayList();
            previous3 = new Action[3];
            colorActionsMain = new int[7];
            colorActionsLight = new int[7];
            case1 = null;
            Init();
            reRender();
            StartCoroutine("reset");
            return false;
        }

        if( planetActions[planetsOrder[5]][planetsOrder[id]] == 0 && normal)
        {
            onFakeStrike();
            return true;
        }

        if(planetActions[planetsOrder[5]][planetsOrder[id]] == 1 || !normal)
        {
            int tmp = planetsOrder[5];
            planetsOrder[5] = planetsOrder[id];
            planetsOrder[id] = tmp;
            reRender();

            if (normal)
            {
                timeEvents.Add("Swap " + id);
                timeEventsTimes.Add((float)timeRatio);
            }

            Debug.LogFormat("[Nomai #{0}] Swapping planets {1} and 5(main).", _moduleId, id);
        }

        if (planetActions[planetsOrder[5]][planetsOrder[id]] == 2 && normal)
        {
            mainButtonRenderer.material = buttonMats[7];
            _isSixth = true;

            if (normal)
            {
                timeEvents.Add("Swap 6");
                timeEventsTimes.Add((float)timeRatio);
            }

            Debug.LogFormat("[Nomai #{0}] Reached location six!.", _moduleId);
        }
        return false;
    }

    void reRender()
    {
        for (int x = 0; x < buttonRenderers.Length + 1; x++)
        {
            if(x == 5) { mainButtonRenderer.material = buttonMats[planetPatterns[planetsOrder[5]]]; }
            else { buttonRenderers[x].material = buttonMats[planetPatterns[planetsOrder[x]]]; }
        }
        bar.material = barMats[barColor];
    }

    bool checkDeact()
    {
        try
        {
            switch (deactMethod)
            {
                case 1:
                    //Strike the same way you did the previous loop. (It will affect the next loop.) (It will not trigger from time.)
                    //Implemented elsewhere
                    break;
                case 2:
                    //Navigate to this planet immediately after navigating away from it.
                    //Working
                    return previous3[1].ToId == 1 && previous3[0].AtId == 1;
                case 3:
                    //Interact with the sixth location.
                    //Working
                    return _isSixth && previous3[0].PressedId == 6;
                case 4:
                    //Interact with any other planet.
                    //Working
                    return previous3[0].AtId != 2 && previous3[0].PressedId == 6;
                case 5:
                    //Navigate to the sun. (It will affect the next loop.)
                    //Implemented elsewhere
                    //Working
                    break;
                case 6:
                    //Interact with this planet twice before any other interactions.
                    //Working
                    return previous3[0].PressedId == 6 && previous3[1].PressedId == 6 && previous3[2] == null;
                case 7:
                    //Navigate from any other planet to this planet.
                    //Working
                    return previous3[0].ToId == 4;
                case 8:
                    //Interact with any planet immediately after having interacted with the status light twice.
                    //Working
                    return previous3[0].PressedId == 6 && previous3[1].PressedId == 7 && previous3[2].PressedId == 7;
                case 9:
                    //Earn a strike immediately after interacting with this planet. (It will affect the next loop.) (It will not trigger from time.)
                    //Implemented elsewhere
                    //Working
                    break;
                case 10:
                    //Interact with any other planet, then immediately travel to this planet.
                    //Working
                    return planetPatterns[planetsOrder[5]] == 5 && previous3[1].PressedId == 6;
                case 11:
                    //Interact with the status light while at the sixth location.
                    //Working
                    return previous3[0].PressedId == 7 && _isSixth;
                case 12:
                    //Interact with the status light while on any other planet.
                    //Working
                    return previous3[0].PressedId == 7 && previous3[0].AtId != 6;
            }
        }
        catch { }
        finally { }
        return false;
    }

    void AddAction(int fromId, int buttonId, bool struck)
    {
        previous3[2] = previous3[1];
        previous3[1] = previous3[0];
        previous3[0] = new Action(fromId, buttonId, struck, buttonId <= 5 ? planetPatterns[planetsOrder[buttonId]] : -1);
    }

    void ChangeColor(int dir)
    {
        if (dir == 1) { barColor += 2; }
        barColor -= 1;
        if (barColor == 4) { barColor = 1; }
        if (barColor == 0) { barColor = 3; }
        if (barColor < 0 || barColor > 4) { barColor = 1; }
        bar.material = barMats[barColor];
        Debug.LogFormat("[Nomai #{0}] Timer color changed to {1}.", _moduleId, COLORS[barColor]);
    }

    void addColorsRandom(int disabled)
    {
        int count = 0;
        for (int i = 0; i < 7; i++)
        {
            if (i == disabled) { colorActionsLight[i] = 0; colorActionsMain[i] = 0; continue; }
            colorActionsLight[i] = Random.Range(0, 5) == 0 ? 1 : 0;
            colorActionsMain[i] = Random.Range(0, 5) == 0 ? 1 : 0;
            count += colorActionsLight[i] + colorActionsMain[i];
            if (count > 7) { break; }
        }
        while (count < 3)
        {
            int i = Random.Range(0, 7);
            if (i != disabled)
            {
                int choice = Random.Range(0, 2);
                if (choice == 1 && colorActionsLight[i] == 0) { colorActionsLight[i] = 1; count++; }
                else if (colorActionsMain[i] == 0) { colorActionsMain[i] = 1; count++; }
            }
        }
    }
}
