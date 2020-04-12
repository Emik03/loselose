using UnityEngine;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Rnd = UnityEngine.Random;

public class LoseLose : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Info;
    public KMBombModule Module;
    public KMBossModule Boss;
    public KMSelectable Object;
    public TextMesh Text;

    static bool _playSound = true;
    private bool _lightsOn = false, _started = false, _isSolved = false, _solvable = false;
    private static int _moduleIdCounter = 1;
    private int _moduleId = 0, _stage = 0;
    static private string[] _ignore = { "Win/Win", "Lose/Lose", "Forget The Colors", "14", "Bamboozling Time Keeper", "Brainf---", "Forget Enigma", "Forget Everything", "Forget It Not", "Forget Me Not", "Forget Me Later", "Forget Perspective", "Forget Them All", "Forget This", "Forget Us Not", "Organization", "Purgatory", "Simon Forgets", "Simon's Stages", "Souvenir", "Tallordered Keys", "The Time Keeper", "The Troll", "The Very Annoying Button", "Timing Is Everything", "Turn The Key", "Ultimate Custom Night", "Übermodule" };
    readonly static private string[] _blacklist = { "Win/Win", "Lose/Lose", "WinWin", "LoseLose", "Twitch", "Bomb", "Module", "KTPlayerController", "Timer", "Mission", "Component_Highlight", "Camera", "TestHarness", "Audio Sources", "PlaySoundHandler", "MusicManager", "IDNumber" };
    private StringBuilder _builder = new StringBuilder();

    /// <summary>
    /// Strikes the user if they solve another module before activation.
    /// </summary>
    private void FixedUpdate()
    {
        //strike if module has been solved while the module is inactive
        if (!_started && _stage < Info.GetSolvedModuleNames().Where(a => !_ignore.Contains(a)).Count())
        {
            Debug.LogFormat("[Lose/Lose #{0}] Strike! The module was not activated, yet you solved {1}.", _moduleId, Info.GetSolvableModuleNames().Last());
            _stage++;
            Module.HandleStrike();
        }
    }

    /// <summary>
    /// Deletes a random instance from the hierarchy.
    /// </summary>
    private IEnumerator Delete()
    {
        //activate
        Debug.LogFormat("[Lose/Lose #{0}] Lose/Lose has been activated! Be careful what you wish for.", _moduleId);
        Object.AddInteractionPunch(32767);
        Text.text = "";
        Text.color = new Color32(255, 0, 0, 255);
        _started = true;
        _stage = 0;

        while (true)
        {
            //if all solvable modules are solved
            if (Application.isEditor && _stage == Info.GetSolvableModuleNames().Where(a => !_ignore.Contains(a)).Count())
            {
                _solvable = true;
                Text.text = "I'M  DONE  DESTROYING  THE  BOMB!";
                Text.fontSize = 800 / Text.text.Length;
                yield break;
            }

            //if a module has been solved
            if (_stage < Info.GetSolvedModuleNames().Where(a => !_ignore.Contains(a)).Count() && !_isSolved)
            {
                _stage++;

                //get array of every instance
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                int rng = Rnd.Range(0, allObjects.Length);

                //if current instance is accessible
                if (allObjects[rng] != null)
                {
                    //if any items found in preserve array match the name
                    for (int i = 0; i < _blacklist.Length; i++)
                    {
                        if (allObjects[rng].name.Contains(_blacklist[i]))
                        {
                            Text.text = allObjects[rng].name.ToUpper();
                            Text.fontSize = 750 / allObjects[rng].name.Length;
                            Text.color = new Color32(255, 255, 255, 255);
                            break;
                        }

                        //final index
                        if (i == _blacklist.Length - 1)
                        {
                            //actual payload
                            Debug.LogFormat("[Lose/Lose #{0}] {1}", _moduleId, allObjects[rng].name);
                            Text.text = allObjects[rng].name.ToUpper();
                            Text.fontSize = 750 / allObjects[rng].name.Length;
                            Text.color = new Color32(255, 0, 0, 255);
                            _builder.Append(allObjects[rng].name + ", ");
                            Destroy(allObjects[rng]);
                        }
                    }

                }

                else
                {
                    Text.text = allObjects[rng].name.ToUpper();
                    Text.fontSize = 750 / allObjects[rng].name.Length;
                    Text.color = new Color32(255, 255, 255, 255);
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Code that runs when bomb is loading.
    /// </summary>
    private void Start()
    {
        //make text invisible
        Module.OnActivate += Activate;
        _moduleId = _moduleIdCounter++;
        Text.transform.localScale = new Vector3(0, 0, 0);
    }

    /// <summary>
    /// Lights get turned on.
    /// </summary>
    void Activate()
    {
        //make text visible
        Text.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        if (_playSound)
        {
            Audio.PlaySoundAtTransform("soundLoseLose", Module.transform);
            _playSound = false;
        }

        _lightsOn = true;
        Init();
    }

    /// <summary>
    /// Creates new arrows and logs answer.
    /// </summary>
    private void Init()
    {
        //set ignore list from bossmodulehandler
        string[] ignoredModules = Boss.GetIgnoredModules(Module, _ignore);
        if (ignoredModules != null)
            _ignore = ignoredModules;

        //if module is selected, start payload
        if (!_isSolved && _lightsOn)
            GetComponent<KMSelectable>().OnInteract += OnInteract;
    }

    private bool OnInteract()
    {
        //solve the module if it's completed
        if (_solvable)
            Solve();

        //debug
        else if (Application.isEditor)
            _stage--;

        //start the module
        if (!_started)
            StartCoroutine("Delete");

        return false;
    }

    private void Solve()
    {
        Debug.LogFormat("[Lose/Lose #{0}] Defused! Module Solved!", _moduleId);
        Text.text = "";

        //if anything was destroyed, log all of it
        if (_builder.Length > 2)
        {
            _builder.Remove(_builder.Length - 2, 2);
            Debug.LogFormat("[Lose/Lose #{0}] I have deleted: {1}", _moduleId, _builder);
        }

        //if nothing was destroyed, i.e. modules consisted of only ignore array
        else
            Debug.LogFormat("[Lose/Lose #{0}] I have deleted nothing.", _moduleId);

        _isSolved = true;
        Audio.PlaySoundAtTransform("soundLoseLose", Module.transform);
        Object.AddInteractionPunch(32767);
        Module.HandlePass();
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} accept fate (Accept the fact that this module could end up destroying/softlocking your current bomb, or solve the bomb if it says LoseLose on-screen.)";
#pragma warning restore 414

    /// <summary>
    /// TwitchPlays Compatibility, detects every chat message and clicks buttons accordingly.
    /// </summary>
    /// <param name="command">The twitch command made by the user.</param>
    IEnumerator ProcessTwitchCommand(string command)
    {
        //if command is formatted correctly
        if (Regex.IsMatch(command, @"^\s*accept fate\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;

            //if it should solve
            if (_solvable)
                Solve();

            //start payload
            else
                StartCoroutine("Delete");
        }
    }

    /// <summary>
    /// Force the module to be solved in TwitchPlays
    /// </summary>
    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        Debug.LogFormat("[Lose/Lose #{0}] Forced to be solved, initiating...", _moduleId);
        Solve();
    }
}