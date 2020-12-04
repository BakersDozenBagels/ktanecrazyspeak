using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;

public class InterCubeFunct : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMSelectable ModuleSelectable;
    public GameObject ModuleHighlight;
    public Transform Position;
    public GameObject TestObject;
    public GameObject TestObject2;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _isMain = false;

    sealed class InterCubeBombInfo
    {
        public List<InterCubeFunct> Modules = new List<InterCubeFunct>();
        public List<Transform> Positions = new List<Transform>();
        public bool Chosen = false;
        public int[] Links;
    }

    private static readonly Dictionary<string, InterCubeBombInfo> _infos = new Dictionary<string, InterCubeBombInfo>();
    private InterCubeBombInfo _info;

	// Use this for initialization
	void Start () {
        _moduleId = _moduleIdCounter++;

        var serialNumber = Bomb.GetSerialNumber();
        if (!_infos.ContainsKey(serialNumber))
            _infos[serialNumber] = new InterCubeBombInfo();
        _info = _infos[serialNumber];
        _info.Modules.Add(this);

        Bomb.OnBombExploded += delegate { _infos.Clear(); };
        Bomb.OnBombSolved += delegate
        {
            // This check is necessary because this delegate gets called even if another bomb in the same room got solved instead of this one
            if (Bomb.GetSolvedModuleNames().Count == Bomb.GetSolvableModuleNames().Count)
                _infos.Remove(serialNumber);
        };

        StartCoroutine(Initialize(serialNumber));
    }

    private IEnumerator Initialize(string serialNumber)
    {
        yield return null;

        _info.Positions.Add(Position);

        if (!_info.Chosen)
        {
            _info.Chosen = true;
            _isMain = true;
        }

        StartCoroutine(Initialize2());
    }

    private IEnumerator Initialize2()
    {
        yield return null;

        if (_isMain)
        {
            _info.Links = new int[_info.Modules.Count - 1];
            for (int i = 1; i <= _info.Links.Length; i++)
            {
                _info.Links[i - 1] = i;
            }

            Debug.Log(_info.Links.Join());

            Shuffle<int>(_info.Links);

            Debug.Log(_info.Links.Join());

            for (int i = 1; i < _info.Modules.Count; i++)
            {
                GameObject Sphere = Instantiate(TestObject);
                Sphere.transform.SetPositionAndRotation(_info.Positions[i].position, _info.Positions[i].rotation);
                Sphere.transform.SetParent(Module.transform);
                Sphere.transform.localScale = new Vector3(1f, 1f, 1f);
                KMSelectable script = Sphere.GetComponentInChildren<KMSelectable>();

                KMSelectable[] outarr = new KMSelectable[_info.Modules[i].ModuleSelectable.Children.Length + 1];
                _info.Modules[i].ModuleSelectable.Children.CopyTo(outarr, 0);
                outarr[_info.Modules[i].ModuleSelectable.Children.Length] = script;

                _info.Modules[i].ModuleSelectable.Children = outarr;
                script.Parent = _info.Modules[i].ModuleSelectable;
                _info.Modules[i].ModuleSelectable.UpdateChildren();

                int j = _info.Links[i-1];
                Sphere.GetComponentInChildren<KMSelectable>().OnInteract += delegate () { onSimp(j); return false; };
            }

            GameObject Sphere2 = Instantiate(TestObject2);
            Sphere2.transform.SetPositionAndRotation(_info.Positions[0].position, _info.Positions[0].rotation);
            Sphere2.transform.SetParent(Module.transform);
            Sphere2.transform.localScale = new Vector3(1f, 1f, 1f);
            KMSelectable script2 = Sphere2.GetComponentInChildren<KMSelectable>();

            KMSelectable[] outarr2 = new KMSelectable[ModuleSelectable.Children.Length + 1];
            ModuleSelectable.Children.CopyTo(outarr2, 0);
            outarr2[ModuleSelectable.Children.Length] = script2;

            ModuleSelectable.Children = outarr2;
            script2.Parent = ModuleSelectable;
            ModuleSelectable.UpdateChildren();

            Sphere2.GetComponentInChildren<KMSelectable>().OnInteract += delegate () { onSimp(0); return false; };
        }
    }

    private void onSimp(int i)
    {
        if (i != 0)
        {
            _info.Modules[i].onSimped();
        }
        else
        {
            int a = 0;
            List<int> b = new List<int>();
            List<int> c = new List<int>();
            for (int x = 0; x < _info.Links.Length; x++) 
            {
                b.Add(_info.Links[x]);
            }
            
            for(int x = 1; x <= b.Count; x++)
            {
                int tmp = x;
                tmp = b[tmp - 1];
                while (tmp != x)
                {
                    if (c.Contains(tmp)) { break; }
                    tmp = b[tmp - 1];
                }
                if (!c.Contains(tmp))
                {
                    c.Add(tmp);
                    a++;
                }
            }

            a %= 100;

            Regex regex = new Regex(a.ToString());
            if (regex.Match(Bomb.GetFormattedTime()).Success)
            {
                onSimped();
            }
            else
            {
                Module.HandleStrike();
            }
        }
    }

    public void onSimped()
    {
        Module.HandlePass();
    }

    static System.Random _random = new System.Random();

    static void Shuffle<T>(T[] array)
    {
        int n = array.Length;
        for (int i = 0; i < (n - 1); i++)
        {
            // Use Next on random instance with an argument.
            // ... The argument is an exclusive bound.
            //     So we will not go past the end of the array.
            int r = i + _random.Next(n - i);
            T t = array[r];
            array[r] = array[i];
            array[i] = t;
        }
    }
}