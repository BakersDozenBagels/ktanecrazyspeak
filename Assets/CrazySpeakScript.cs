using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;
using System;
using System.Linq;
using System.Text.RegularExpressions;

public class CrazySpeakScript : MonoBehaviour {
    public KMNeedyModule Module;
    public KMAudio Audio;
    public TextMesh Text;
    public KMBombInfo Info;

    private static KeywordRecognizer mic;
    private static bool micInit = false;
    private static int micOn = 0;
    private bool state = false;
    private Dictionary<string, Tuple> actions = new Dictionary<string, Tuple>();
    private int lookingFor = -1;

    private static int modCounter = 0;
    private int _id = -1;

    private class Tuple
    {
        public string str;
        public int i;

        public Tuple(int iIn, string strIn)
        {
            str = strIn;
            i = iIn;
        }
    }

	void Start () {
        actions.Add("we have souvenir", new Tuple(1, "We have Souvenir."));
        actions.Add("we have divided squares", new Tuple(2, "We have Divided Squares."));
        actions.Add("we have forget me not", new Tuple(3, "We have Forget\nMe Not."));
        actions.Add("we have crazy talk", new Tuple(4, "We have Crazy Talk."));
        actions.Add("we have crazy talk with a kilo", new Tuple(5, "We have Crazy Talk\nwith a Kilo."));
        actions.Add("we have crazy talk with a charlie", new Tuple(6, "We have Crazy Talk\nwith a Charlie."));
        actions.Add("we have crazy speak", new Tuple(7, "We have Crazy Speak."));
        actions.Add("we got a strike", new Tuple(8, "We got a strike."));
        actions.Add("forget me not stage seven is an eight", new Tuple(9, "Forget Me Not stage\nseven is an eight."));
        actions.Add("that solved", new Tuple(10, "That solved."));
        actions.Add("i didn't see that souvenir", new Tuple(11, "I didn't see that\nSouvenir."));
        actions.Add("i didn't see that divided squares", new Tuple(12, "I didn't see that\nDivided Squares."));
        actions.Add("i didn't see that forget me not", new Tuple(13, "I didn't see that\nForget Me Not."));
        actions.Add("we have simpleton", new Tuple(14, "We have Simpleton."));
        actions.Add("we have misery squares", new Tuple(15, "We have Misery Squares."));
        actions.Add("my needy went off", new Tuple(15, "My needy went off."));
        actions.Add("we blew up", new Tuple(17, "We blew up."));
        actions.Add("it just changed", new Tuple(18, "It just changed."));
        actions.Add("what number was that", new Tuple(19, "What number was that?"));
        actions.Add("oh no", new Tuple(20, "Oh no!"));
        actions.Add("i died", new Tuple(21, "I died."));
        actions.Add("i struck", new Tuple(22, "I struck."));
        actions.Add("one five four nine in that order", new Tuple(23, "1, 5, 4, 9\nin that order."));
        actions.Add("five six four nine in that order", new Tuple(24, "5, 6, 4, 9\nin that order."));
        actions.Add("one seven seven nine in that order", new Tuple(25, "1, 7, 7, 9\nin that order."));
        actions.Add("two two four eight in that order", new Tuple(26, "2, 2, 4, 8\nin that order."));
        actions.Add("is this souvenir", new Tuple(27, "Is this Souvenir?"));
        actions.Add("is this divided squares", new Tuple(28, "Is this Divided Squares?"));
        actions.Add("is this forget me not", new Tuple(29, "Is this Forget\nMe Not?"));
        actions.Add("we have a swan", new Tuple(30, "We have a Swan."));
        actions.Add("i didn't see that swan", new Tuple(31, "I didn't see that Swan."));
        actions.Add("is this the swan", new Tuple(32, "Is this the Swan?"));
        actions.Add("which colored squares is this", new Tuple(33, "Which Colored Squares\nis this?"));
        actions.Add("which colored switches is this", new Tuple(34, "Which Colored Switches\nis this?"));
        actions.Add("which ordered keys is this", new Tuple(35, "Which Ordered Keys\nis this?"));
        actions.Add("my memory is a three", new Tuple(36, "My Memory is a 3."));
        actions.Add("my who's on first is blank", new Tuple(37, "My Who's on First\nis blank."));
        actions.Add("my morse code is a dot dash", new Tuple(38, "My Morse Code\nis a dot dash."));
        actions.Add("my wires are red and blue", new Tuple(39, "My Wires are\nred and blue."));
        actions.Add("my button says hold", new Tuple(40, "My Button\nsays hold."));
        actions.Add("my keypad has a star", new Tuple(41, "My Keypad has\na star."));
        actions.Add("my maze is bravo four", new Tuple(42, "My Maze is\nbravo-4."));
        actions.Add("my simon says is red", new Tuple(42, "My Simon Says is red."));
        actions.Add("are you there", new Tuple(43, "Are you there?"));

        if (!micInit) { mic = new KeywordRecognizer(actions.Keys.ToArray(), ConfidenceLevel.Low); micInit = true; StartCoroutine(ResetMic()); }

        mic.OnPhraseRecognized += Heard;

        Module.OnNeedyActivation += Activate;
        Module.OnNeedyDeactivation += Deactivate;
        Module.OnTimerExpired += Strike;

        _id = modCounter++;
    }

    private IEnumerator ResetMic()
    {
        yield return null;
        micInit = false;
    }

    private void Deactivate()
    {
        if (state)
        {
            micOn--;
            if (micOn == 0) mic.Stop();
            state = false;
        }
        Debug.LogFormat("[Crazy Speak #{0}] Needy deactivated.", _id);
    }

    private void Activate()
    {
        if (!state)
        {
            if (micOn == 0) mic.Start();
            micOn++;
            state = true;
        }
        Text.text = actions.PickRandom().Key;
        lookingFor = actions[Text.text].i;
        Text.text = actions[Text.text].str;
        Debug.LogFormat("[Crazy Speak #{0}] Needy activated. The phrase is: \"{1}\"", _id, Text.text);
    }

    private void Heard(PhraseRecognizedEventArgs speech)
    {
        Debug.LogFormat("<Crazy Speak #{0}> Heard \"{1}\".", _id, speech.text);
        if (actions.Select(x => speech.text.ToLowerInvariant().RegexMatch(x.Key) ? x.Value.i : 0).Where(x => x == lookingFor).FirstOrDefault() != 0)
        {
            Module.HandlePass();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            if (state)
            {
                micOn--;
                if (micOn == 0) mic.Stop();
                state = false;
            }
            Text.text = "";
            Debug.LogFormat("[Crazy Speak #{0}] Needy solved.", _id);
        }
    }

    private void Strike()
    {
        Module.HandleStrike();
        if (state)
        {
            micOn--;
            if (micOn == 0) mic.Stop();
            state = false;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
        Text.text = "";
        Debug.LogFormat("[Crazy Speak #{0}] Needy struck.", _id);
    }
}