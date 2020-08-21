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
    public Material[] buttonMats;
    public KMSelectable lightButton;
    public KMSelectable mainButton;
    public Material mainButtonMat;
    public GameObject barControl;
    public MeshRenderer bar;
    public KMSelectable centerButton;

    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;

    //Action modes are: -1: Error 0: Strike 1: Normal 2: Loacation six

    private string[] PATTERNS = { "YellowSpots", "BlueWaves", "GrayCraters", "GreenOcean", "GreenSquiggles", "PurpleTriangles", "RedStripes", "PurpleSpiral"};
    private int[] planetPatterns = new int[6]; 

    void Start () {
        int[] ordered = { 1, 2, 3, 4, 5, 6 };
        for (int i = 5; i > 0; i--)
        {
            int j = Random.Range(0, i);
            int tmp = ordered[i];
            ordered[i] = ordered[j];
            ordered[j] = tmp;
        }
        
        int rng = Random.Range(0, 4);
        ordered[rng] = 0;
        planetPatterns = ordered;

        int[] template = { -1, -1, -1, -1, -1, 0 };

        _moduleId = _moduleIdCounter++;
        Module.OnActivate += Activate;
        Debug.LogFormat("[Nomai #{0}] Initialised with planet order (Reading order): #0: {1} #1: {2} #3: {3} #2: {4} #4: {5} #5: {6}", _moduleId, PATTERNS[ordered[0]], PATTERNS[ordered[1]], PATTERNS[ordered[3]], PATTERNS[ordered[2]], PATTERNS[ordered[4]], PATTERNS[ordered[5]]);
    }
	
    void Activate ()
    {

    }
}
