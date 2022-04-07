using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class FloorLightsScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMBossModule Boss;
    public KMSelectable[] SquareSels;
    public GameObject[] SquareObjs;
    public Material[] SquareMats;
    public KMSelectable ShowTimeSel;
    public TextMesh[] ScreenText;
    public AudioSource StageAdvanceAudio;
    public TextMesh[] PartyText;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;
    private string[] _ignoredModules;

    private sealed class Cell : IEquatable<Cell>
    {
        public int coord;
        public int color;

        public Cell(int coord, int color)
        {
            this.coord = coord;
            this.color = color;
        }

        public bool Equals(Cell other)
        {
            return other != null && other.coord == coord && other.color == color;
        }
    }

    private List<Cell[]> _stageInfo = new List<Cell[]>();
    private const int _cellCount = 3; // Adjust this to change tiles per stage.
    private int _stageCount;
    private int _currentStage = -1;
    private int _currentSolves;
    private bool _readyToAdvance;
    private bool _submissionPhase;
    private bool _pseudoSubmissionPhase;
    private bool[] _inputSquares = new bool[36];
    private bool[] _solutionSquares = new bool[36];
    private bool _stageRecovery;
    private bool _hasStruck;
    private bool _isAnimating;
    private float _holdTime;
    private bool _trueModuleSolved;
    private Coroutine _holdTimer;
    private Coroutine _flashStage;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < SquareSels.Length; i++)
            SquareSels[i].OnInteract += SquarePress(i);
        Module.OnActivate += Activate;
        ShowTimeSel.OnInteract += ShowTimePress;
        ShowTimeSel.OnInteractEnded += ShowTimeRelease;
        StartCoroutine(Init());
    }

    private KMSelectable.OnInteractHandler SquarePress(int btn)
    {
        return delegate ()
        {
            Audio.PlaySoundAtTransform("Douga", SquareSels[btn].transform);
            SquareSels[btn].AddInteractionPunch(0.25f);
            if (_moduleSolved || !_submissionPhase || _isAnimating || !_pseudoSubmissionPhase)
                return false;
            _inputSquares[btn] = !_inputSquares[btn];
            SquareObjs[btn].GetComponent<MeshRenderer>().material = _inputSquares[btn] ? SquareMats[3] : SquareMats[4];
            return false;
        };
    }

    private bool ShowTimePress()
    {
        Audio.PlaySoundAtTransform("Douga", ShowTimeSel.transform);
        if (_moduleSolved)
            return false;
        ShowTimeSel.AddInteractionPunch(0.5f);
        _holdTimer = StartCoroutine(HoldTimer());
        return false;
    }

    private void ShowTimeRelease()
    {
        if (_moduleSolved || _isAnimating)
            return;
        if (_holdTimer != null)
            StopCoroutine(_holdTimer);
        if (_stageRecovery)
        {
            Advance();
            return;
        }
        if (_holdTime < 2f || !_hasStruck)
        {
            if (_submissionPhase)
            {
                Debug.LogFormat("[Floor Lights #{0}] Submitted {1}.", _moduleId, _inputSquares.Select(i => i ? "#" : "*").Join(""));
                int count = 0;
                for (int i = 0; i < 36; i++)
                    if (_inputSquares[i] == _solutionSquares[i])
                        count++;
                if (count == 36)
                {
                    _moduleSolved = true;
                    Debug.LogFormat("[Floor Lights #{0}] 36 out of 36 cells were correct. Module solved.", _moduleId);
                    StartCoroutine(SolveAnimation());
                }
                else
                {
                    _isAnimating = true;
                    _hasStruck = true;
                    Debug.LogFormat("[Floor Lights #{0}] {1} out of 36 cells were correct. Strike.", _moduleId, count);
                    StartCoroutine(StrikeAnimation(count));
                }
            }
        }
        else
        {
            _stageRecovery = true;
            ScreenText[0].text = "REWIND";
            _pseudoSubmissionPhase = false;
            _submissionPhase = false;
            _currentStage = -1;
            for (int i = 0; i < 36; i++)
                _inputSquares[i] = false;
            Advance();
        }
    }

    private IEnumerator HoldTimer()
    {
        _holdTime = 0f;
        while (true)
        {
            yield return null;
            _holdTime += Time.deltaTime;
        }
    }

    private void Activate()
    {
        _readyToAdvance = true;
    }

    private IEnumerator Init()
    {
        yield return null;
        if (_ignoredModules == null)
            _ignoredModules = GetComponent<KMBossModule>().GetIgnoredModules("Floor Lights", new string[] {
                "14",
                "42",
                "501",
                "A>N<D",
                "Bamboozling Time Keeper",
                "Black Arrows",
                "Brainf---",
                "The Board Walk",
                "Busy Beaver",
                "Don't Touch Anything",
                "Doomsday Button",
                "Duck Konundrum",
                "Floor Lights",
                "Forget Any Color",
                "Forget Enigma",
                "Forget Everything",
                "Forget Infinity",
                "Forget It Not",
                "Forget Maze Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Perspective",
                "Forget The Colors",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Iconic",
                "Keypad Directionality",
                "Kugelblitz",
                "Multitask",
                "OmegaDestroyer",
                "OmegaForget",
                "Organization",
                "Password Destroyer",
                "Purgatory",
                "RPS Judging",
                "Security Council",
                "Shoddy Chess",
                "Simon Forgets",
                "Simon's Stages",
                "Souvenir",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn the Key",
                "The Twin",
                "Twister",
                "Übermodule",
                "Ultimate Custom Night",
                "The Very Annoying Button",
                "Whiteout",
                "Zener Cards"
            });
        _stageCount = BombInfo.GetSolvableModuleNames().Count(i => !_ignoredModules.Contains(i));
        for (int i = 0; i < _stageCount; i++)
            _stageInfo.Add(MakeCells());
        for (int i = 0; i < _stageCount; i++)
        {
            var str = new List<string>();
            for (int j = 0; j < _cellCount; j++)
                str.Add("RGB"[_stageInfo[i][j].color] + " at " + GetCoord(_stageInfo[i][j].coord));
            Debug.LogFormat("[Floor Lights #{0}] Stage {1}: Generated {2}", _moduleId, i + 1, str.Join(", "));
            ToggleCells(_stageInfo[i]);
            Debug.LogFormat("[Floor Lights #{0}] Grid: {1}", _moduleId, _solutionSquares.Select(j => j ? "#" : "*").Join(""));
        }
        Debug.LogFormat("[Floor Lights #{0}] All stages generated.", _moduleId);
    }

    private Cell[] MakeCells()
    {
        var pos = Enumerable.Range(0, 36).ToArray().Shuffle().Take(_cellCount).ToArray();
        var cellArr = new Cell[_cellCount];
        for (int i = 0; i < cellArr.Length; i++)
            cellArr[i] = new Cell(pos[i], Rnd.Range(0, 3));
        return cellArr;
    }

    private void Update()
    {
        if (_stageRecovery || !_readyToAdvance)
            return;
        _currentSolves = BombInfo.GetSolvedModuleNames().Count(i => !_ignoredModules.Contains(i));
        if (_currentStage == _currentSolves)
            return;
        if (_currentStage <= _stageCount)
            Advance();
    }

    private void Advance()
    {
        _currentStage++;
        if (_currentStage != _stageCount)
            ScreenText[1].text = "stage " + (_currentStage + 1).ToString();
        else
            ScreenText[1].text = "";
        StageAdvanceAudio.Stop();
        if (_flashStage != null)
            StopCoroutine(_flashStage);
        _flashStage = StartCoroutine(FlashStage());
    }

    private IEnumerator FlashStage()
    {
        if (!_stageRecovery)
            _readyToAdvance = false;
        if (_currentStage == _stageCount)
        {
            _pseudoSubmissionPhase = true;
            _stageRecovery = false;
        }
        StageAdvanceAudio.Play();
        for (int i = 0; i < 8; i++)
        {
            var tempCells = MakeCells();
            ShowCells(tempCells);
            yield return new WaitForSeconds(0.25f);
        }
        if (_currentStage != _stageCount)
        {
            ShowCells(_stageInfo[_currentStage]);
            yield return new WaitForSeconds(3f);
            _readyToAdvance = true;
        }
        else
        {
            for (int i = 0; i < 36; i++)
                SquareObjs[i].GetComponent<MeshRenderer>().material = SquareMats[4];
            _submissionPhase = true;
            ScreenText[0].text = "SHOW TIME!";
        }
    }

    private string GetCoord(int num)
    {
        return "ABCDEF".Substring(num % 6, 1) + "123456".Substring(num / 6, 1);
    }

    private void ShowCells(Cell[] cells)
    {
        for (int i = 0; i < 36; i++)
        {
            if (!cells.Select(j => j.coord).Contains(i))
                SquareObjs[i].GetComponent<MeshRenderer>().material = SquareMats[4];
            else
                SquareObjs[i].GetComponent<MeshRenderer>().material = SquareMats[cells[Array.IndexOf(cells.Select(j => j.coord).ToArray(), i)].color];
        }
    }

    private void ToggleCells(Cell[] cells)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            var adjs = GetAdjacents(cells[i].coord, cells[i].color);
            for (int j = 0; j < adjs.Count; j++)
                _solutionSquares[adjs[j]] = !_solutionSquares[adjs[j]];
        }
    }

    private List<int> GetAdjacents(int num, int col)
    {
        var list = new List<int>();
        list.Add(num);
        if (col == 0 || col == 2)
        {
            if (num % 6 != 0)
                list.Add(num - 1);
            if (num % 6 != 5)
                list.Add(num + 1);
            if (num / 6 != 0)
                list.Add(num - 6);
            if (num / 6 != 5)
                list.Add(num + 6);
        }
        if (col == 1 || col == 2)
        {
            if (num % 6 != 0 && num / 6 != 0)
                list.Add(num - 7);
            if (num % 6 != 0 && num / 6 != 5)
                list.Add(num + 5);
            if (num % 6 != 5 && num / 6 != 0)
                list.Add(num - 5);
            if (num % 6 != 5 && num / 6 != 5)
                list.Add(num + 7);
        }
        return list;
    }

    private IEnumerator SolveAnimation()
    {
        Audio.PlaySoundAtTransform("Floor Victory", transform);
        var TL = new int[] { 0, 1, 2, 6, 7, 8, 12, 13, 14 };
        var TR = new int[] { 3, 4, 5, 9, 10, 11, 15, 16, 17 };
        var BL = new int[] { 18, 19, 20, 24, 25, 26, 30, 31, 32 };
        var BR = new int[] { 21, 22, 23, 27, 28, 29, 33, 34, 35 };
        var solvePic = new int[] { 7, 10, 19, 22, 26, 27 };
        for (int i = 0; i < TL.Length; i++)
            SquareObjs[TL[i]].GetComponent<MeshRenderer>().material = SquareMats[1];
        yield return new WaitForSeconds(0.22f);
        for (int i = 0; i < TR.Length; i++)
            SquareObjs[TR[i]].GetComponent<MeshRenderer>().material = SquareMats[1];
        yield return new WaitForSeconds(0.22f);
        for (int i = 0; i < BL.Length; i++)
            SquareObjs[BL[i]].GetComponent<MeshRenderer>().material = SquareMats[1];
        yield return new WaitForSeconds(0.22f);
        for (int i = 0; i < BR.Length; i++)
            SquareObjs[BR[i]].GetComponent<MeshRenderer>().material = SquareMats[1];
        yield return new WaitForSeconds(0.46f);
        ScreenText[0].text = "GOOD SHOW";
        ScreenText[1].text = "";
        _trueModuleSolved = true;
        Module.HandlePass();
        for (int i = 0; i < PartyText.Length; i++)
            PartyText[i].gameObject.SetActive(true);
        for (int i = 0; i < solvePic.Length; i++)
            SquareObjs[solvePic[i]].GetComponent<MeshRenderer>().material = SquareMats[5];
        yield return new WaitForSeconds(1.75f);
        int ix = 0;
        var rndShuff = Enumerable.Range(0, 4).ToArray().Shuffle();
        var txtColors = new Color32[3] { new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255), new Color32(255, 255, 255, 255) };
        while (true)
        {
            ix++;
            for (int r = 0; r < 6; r++)
                for (int c = 0; c < 6; c++)
                    SquareObjs[r * 6 + c].GetComponent<MeshRenderer>().material = SquareMats[rndShuff[(ix + r + c) % 4]];
            for (int t = 0; t < 5; t++)
                PartyText[t].color = txtColors[(t + ix) % 3];
            yield return new WaitForSeconds(0.3f);
        }
    }

    private IEnumerator StrikeAnimation(int count)
    {
        var x = new int[] { 0, 5, 7, 10, 14, 15, 20, 21, 25, 28, 30, 35 };
        ScreenText[0].text = "BAD SHOW";
        Audio.PlaySoundAtTransform("Incorrect", transform);
        for (int i = 0; i < 36; i++)
            SquareObjs[i].GetComponent<MeshRenderer>().material = SquareMats[0];
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < 36; i++)
            SquareObjs[i].GetComponent<MeshRenderer>().material = SquareMats[5];
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < 36; i++)
            SquareObjs[i].GetComponent<MeshRenderer>().material = SquareMats[x.Contains(i) ? 0 : 5];
        yield return new WaitForSeconds(1f);
        Module.HandleStrike();
        for (int i = 0; i < 36; i++)
            SquareObjs[i].GetComponent<MeshRenderer>().material = SquareMats[_inputSquares[i] ? 3 : 4];
        ScreenText[0].text = "SHOW TIME!";
        ScreenText[1].text = count.ToString() + " of 36";
        _isAnimating = false;
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} set ##..###.....# [set whole grid; # = yellow, . = black. Must be of length 36.] | !{0} submit [Submits the input.]\n!{0} rewind [Enters stage recovery.] | !{0} advance [Goes to next stage of stage recovery.] | !{0} skip [Skips stage recovery, goes back to submission phase.]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        m = Regex.Match(command, @"^\s*(?:set)\s+([\.#]{36})\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            if (!_submissionPhase)
            {
                yield return "sendtochaterror You are not in submission phase. You may not press any cells.";
                yield break;
            }
            yield return null;
            var arr = m.Groups[1].Value.Select(i => i == '#' ? true : false).ToArray();
            for (int i = 0; i < 36; i++)
            {
                if (_inputSquares[i] != arr[i])
                {
                    SquareSels[i].OnInteract();
                    yield return new WaitForSeconds(0.05f);
                }
            }
            yield break;
        }
        m = Regex.Match(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            if (_stageRecovery)
            {
                yield return "sendtochaterror You are currently in stage recovery!";
                yield break;
            }
            if (_currentStage != _stageCount)
            {
                yield return "sendtochaterror Not all stages have been shown yet?";
                yield break;
            }
            yield return null;
            yield return "strike";
            yield return "solve";
            ShowTimeSel.OnInteract();
            yield return new WaitForSeconds(0.1f);
            ShowTimeSel.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
        }
        m = Regex.Match(command, @"^\s*rewind\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            if (!_hasStruck)
            {
                yield return "sendtochaterror You have not struck yet, and cannot rewind to enter stage recovery!";
                yield break;
            }
            if (_stageRecovery)
            {
                yield return "sendtochaterror You are already in stage recovery!";
                yield break;
            }
            yield return null;
            ShowTimeSel.OnInteract();
            yield return new WaitForSeconds(2.5f);
            ShowTimeSel.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
            yield break;
        }
        m = Regex.Match(command, @"^\s*advance\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            if (!_stageRecovery)
            {
                yield return "sendtochaterror You are not in stage recovery! You may not advance.";
                yield break;
            }
            yield return null;
            yield return "strike";
            yield return "solve";
            ShowTimeSel.OnInteract();
            yield return new WaitForSeconds(0.1f);
            ShowTimeSel.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
        }
        m = Regex.Match(command, @"^\s*skip\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
        {
            if (!_stageRecovery)
            {
                yield return "sendtochaterror You are not in stage recovery! You may not skip to input.";
                yield break;
            }
            while (_currentStage != _stageCount && !_pseudoSubmissionPhase)
            {
                ShowTimeSel.OnInteract();
                yield return new WaitForSeconds(0.05f);
                ShowTimeSel.OnInteractEnded();
                yield return new WaitForSeconds(0.05f);
            }
        }
        yield break;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (_stageRecovery && _currentStage != _stageCount && !_pseudoSubmissionPhase)
        {
            ShowTimeSel.OnInteract();
            yield return new WaitForSeconds(0.05f);
            ShowTimeSel.OnInteractEnded();
            yield return new WaitForSeconds(0.05f);
        }
        while (_currentStage != _stageCount || !_submissionPhase)
        {
            yield return true;
        }
        for (int i = 0; i < 36; i++)
        {
            if (_inputSquares[i] != _solutionSquares[i])
            {
                SquareSels[i].OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
        }
        ShowTimeSel.OnInteract();
        yield return new WaitForSeconds(0.1f);
        ShowTimeSel.OnInteractEnded();
        yield return new WaitForSeconds(0.1f);
        while (!_trueModuleSolved)
            yield return true;
    }
}
