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

    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;

    void Start () {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += Activate;
        Debug.LogFormat("[Nomai #{0}] Initialised.", _moduleId);
    }
	
    void Activate ()
    {

    }
}
