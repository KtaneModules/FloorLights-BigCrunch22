using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class FloorLightsScript : MonoBehaviour
{
	public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
	public KMSelectable[] Lighting;
	public KMSelectable Showtime;
	
	public AudioSource ThematicMusic;
	public TextMesh ShowTime;
	public AudioClip[] SFX;
	public KMBossModule Boss;
	public Renderer[] TilingLighting;
	public Material[] FloorColor;
	public Material DefaultColor;
	public Renderer[] TheBulbs;
	public Material[] BulbColors;
	
	private string[] IgnoredModules;
	long RoundNumber = 0, ActualStage = 0, MaxStage;
	bool Playable = false, Striked = false;
	Coroutine Mackerel, Waiter, March;
	
	int CounterOfCheck = 0;
	
	int[] TheLB = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
	int[] TheLB2 = {0, 0, 0, 0, 0, 0, 0, 0};
	
	int[][] NumberBasing = new int[10][]{
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
	};
	
	int[][] Guideline = new int[10][]{
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
		new int[10] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
	};
	
	int ForwardRewind = -1;
	List<int[]> CorrectNumberPlacement = new List<int[]>();
	
	//Logging
	static int moduleIdCounter = 1;
    int moduleId;
    private bool ModuleSolved;
	
	void Awake()
	{
		moduleId = moduleIdCounter++;
		for (int q = 0; q < Lighting.Count(); q++)
		{
			int Button = q;
			Lighting[Button].OnInteract += delegate
			{
				TilingToggle(Button);
				return false;
			};
		}
		Showtime.OnInteract += delegate(){Waiter = StartCoroutine(WaitForTime()); return false;};
		Showtime.OnInteractEnded += delegate(){if (Waiter != null) {StopCoroutine(Waiter);} ShowtimeReal();};
	}
	
	void Start()
	{
		if (IgnoredModules == null)
            IgnoredModules = Boss.GetIgnoredModules("Floor Lights", new string[]{
				    "14",
					"42",
					"501",
					"A>N<D",
					"Bamboozling Time Keeper",
					"Brainf---",
					"Busy Beaver",
					"Don't Touch Anything",
				    "Floor Lights",
					"Forget Any Color",
					"Forget Enigma",
					"Forget Everything",
					"Forget It Not",
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
					"The Troll",
					"The Twin",
					"The Very Annoying Button",
					"Timing is Everything",
					"Turn The Key",
					"Ultimate Custom Night",
					"Übermodule",
					"Whiteout"
            });
			Module.OnActivate += StartingNumber;
	}
	
	IEnumerator WaitForTime()
	{
		Audio.PlaySoundAtTransform(SFX[1].name, transform);
		Showtime.AddInteractionPunch(.2f);
		if (Playable && Striked && ShowTime.text != "REWIND")
		{
			int Number = 0;
			while (Number != 2)
			{
				yield return new WaitForSecondsRealtime(1f);
				Number++;
			}
			ShowTime.text = "REWIND";
			for (int x = 0; x < 100; x++)
			{
				TilingLighting[x].material = DefaultColor;
			}
			for (int z = 0; z < 8; z++)
			{
				TheBulbs[z].material = BulbColors[0];
			}
			ForwardRewind = -1;
		}
	}
	
	void ShowtimeReal()
	{
		if (ShowTime.text != "REWIND")
		{
			if (Playable)
			{
				Playable = false;
				Debug.LogFormat("[Floor Lights #{0}] You submitted these toggles:", moduleId);
				for (int y = 0; y < 10; y++)
				{
					string Bardock = "";
					for (int z = 0; z < 10; z++)
					{
						Bardock += Guideline[y][z].ToString();
					}
					Debug.LogFormat("[Floor Lights #{0}] {1}", moduleId, Bardock);
				}
				Debug.LogFormat("[Floor Lights #{0}] ----------------------------------------------------------", moduleId);
				
				ShowTime.text = "";
				for (int x = 0; x < 100; x++)
				{
					if (Guideline[x / 10][x % 10] == NumberBasing[x / 10][x % 10])
					{
						CounterOfCheck++;
					}
				}
				
				if (CounterOfCheck == 100)
				{
					StartCoroutine(BulbCycle());
					StartCoroutine(MechaCelebration());
					
				}
				
				else
				{
					TheLB2 = new int[] {0, 0, 0, 0, 0, 0, 0, 0};
					for (int z = 0; z < 8; z++)
					{
						TheBulbs[z].material = BulbColors[0];
					}
					StartCoroutine(BadShow());
				}
			}
		}
		
		else
		{
			if (Playable)
			{
				if (March != null)
				{
					StopCoroutine(March);
				}
				ForwardRewind++;
				ThematicMusic.Stop();
				March = StartCoroutine(ForwardMarch());
			}
		}
	}
	
	IEnumerator ForwardMarch()
	{
		ThematicMusic.clip = SFX[2];
		ThematicMusic.Play();
		if (ForwardRewind < MaxStage)
		{
			while (ThematicMusic.isPlaying)
			{
				int[] Alea = Enumerable.Range(0,100).ToArray().Shuffle();
				
				for (int a = 0; a < 100; a++)
				{
					if (a < 8)
					{
						TilingLighting[Alea[a]].material = FloorColor[UnityEngine.Random.Range(0,3)];
					}
					
					else
					{
						TilingLighting[Alea[a]].material = DefaultColor;
					}
				}
				yield return new WaitForSecondsRealtime(0.2f);
			}
			
			for (int b = 0; b < 100; b++)
			{
				if (CorrectNumberPlacement[ForwardRewind][b] > -1)
				{
					TilingLighting[b].material = FloorColor[CorrectNumberPlacement[ForwardRewind][b]];
				}
				
				else
				{
					TilingLighting[b].material = DefaultColor;
				}
			}
		}
		
		else
		{
			Playable = false;
			Striked = false;
			while (ThematicMusic.isPlaying)
			{
				int[] Alea = Enumerable.Range(0,100).ToArray().Shuffle();
				
				for (int a = 0; a < 100; a++)
				{
					if (a < 8)
					{
						TilingLighting[Alea[a]].material = FloorColor[UnityEngine.Random.Range(0,3)];
					}
					
					else
					{
						TilingLighting[Alea[a]].material = DefaultColor;
					}
				}
				yield return new WaitForSecondsRealtime(0.2f);
			}
			for (int c = 0; c < 100; c++)
			{
				TilingLighting[c].material = Guideline[c / 10][c % 10] == 1 ? FloorColor[3] : DefaultColor;
			}
			Playable = true;
			ShowTime.text = "SHOW TIME!";
		}
	}
	
	IEnumerator BadShow()
	{
		Striked = true;
		Audio.PlaySoundAtTransform(SFX[3].name, transform);
		Debug.LogFormat("[Floor Lights #{0}] That was incorrect. Module strikes.", moduleId);
		Debug.LogFormat("[Floor Lights #{0}] The amount of correct toggles submitted: {1}", moduleId, CounterOfCheck.ToString());
		Debug.LogFormat("[Floor Lights #{0}] ----------------------------------------------------------", moduleId);
		for (int x = 0; x < 100; x++)
		{
			TilingLighting[x].material = FloorColor[0];
		}
		yield return new WaitForSecondsRealtime(0.5f);
		for (int a = 0; a < 100; a++)
		{
			TilingLighting[a].material = BulbColors[0];
		}
		yield return new WaitForSecondsRealtime(0.5f);
		int[] CrossMark = {0, 1, 8, 9, 10, 11, 18, 19, 80, 81, 88, 89, 90, 91, 98, 99, 22, 23, 26, 27, 32, 33, 36, 37, 62, 63, 66, 67, 72, 73, 76, 77, 44, 45, 54, 55};
		for (int b = 0; b < CrossMark.Length; b++)
		{
			TilingLighting[CrossMark[b]].material = FloorColor[0];
		}
		ShowTime.text = "BAD SHOW";
		yield return new WaitForSecondsRealtime(1f);
		for (int c = 0; c < 100; c++)
		{
			TilingLighting[c].material = Guideline[c / 10][c % 10] == 1 ? FloorColor[3] : DefaultColor;
		}
		ShowTime.text = "SHOW TIME!";
		Module.HandleStrike();
		for (int i = 0; i < CounterOfCheck; i++)
		{
			TheLB2[0]++;
			for (int y = 0; y < 8; y++)
			{
				if (TheLB2[y] > 1)
				{
					TheLB2[y] = TheLB2[y]-2;
					TheLB2[y+1]++;
				}
			}
		}
		
		for (int z = 0; z < 8; z++)
		{
			if (TheLB2[z] == 1)
			{
				TheBulbs[z].material = BulbColors[2];
			}
			
			else
			{
				TheBulbs[z].material = BulbColors[0];
			}
		}
		CounterOfCheck = 0;
		Playable = true;
	}
	
	IEnumerator BulbCycle()
	{
		int[] Genecode = new int[57];
		for (int x = 0; x < Genecode.Length; x++)
		{
			Genecode[x] = x % 3;
		}
		
		while (true)
		{
			for (int a = 0; a < Genecode.Length; a++)
			{
				Genecode[a] = (Genecode[a] + 1) % 3;
				switch (Genecode[a])
				{
					case 0:
						TheBulbs[a].material = FloorColor[0];
						break;
					case 1:
						TheBulbs[a].material = BulbColors[1];
						break;
					case 2:
						TheBulbs[a].material = FloorColor[2];
						break;
					default:
						break;
				}
			}
			yield return new WaitForSecondsRealtime(0.2f);
		}
	}
	
	IEnumerator MechaCelebration()
	{
		Debug.LogFormat("[Floor Lights #{0}] That was correct. Module solves.", moduleId);
		Debug.LogFormat("[Floor Lights #{0}] The amount of correct toggles submitted: 100", moduleId);
		Debug.LogFormat("[Floor Lights #{0}] ----------------------------------------------------------", moduleId);
		int[] TL = {0, 1, 2, 3, 4, 10, 11, 12, 13, 14, 20, 21, 22, 23, 24, 30, 31, 32, 33, 34, 40, 41, 42, 43, 44};
		int[] TR = {5, 6, 7, 8, 9, 15, 16, 17, 18, 19, 25, 26, 27, 28, 29, 35, 36, 37, 38, 39, 45, 46, 47, 48, 49};
		int[] BL = {50, 51, 52, 53, 54, 60, 61, 62, 63, 64, 70, 71, 72, 73, 74, 80, 81, 82, 83, 84, 90, 91, 92, 93, 94};
		int[] BR = {55, 56, 57, 58, 59, 65, 66, 67, 68, 69, 75, 76, 77, 78, 79, 85, 86, 87, 88, 89, 95, 96, 97, 98, 99};
		
		Audio.PlaySoundAtTransform(SFX[0].name, transform);
		for (int a = 0; a < TL.Length; a++)
		{
			TilingLighting[TL[a]].material = FloorColor[1];
		}
		yield return new WaitForSecondsRealtime(0.22f);
		for (int b = 0; b < BR.Length; b++)
		{
			TilingLighting[BR[b]].material = FloorColor[1];
		}
		yield return new WaitForSecondsRealtime(0.22f);
		for (int c = 0; c < TR.Length; c++)
		{
			TilingLighting[TR[c]].material = FloorColor[1];
		}
		yield return new WaitForSecondsRealtime(0.22f);
		for (int d = 0; d < BL.Length; d++)
		{
			TilingLighting[BL[d]].material = FloorColor[1];
		}
		yield return new WaitForSecondsRealtime(0.46f);
		Module.HandlePass();
		ShowTime.text = MaxStage == 0 ? "PLAY TIME" : "GOOD SHOW";
		int[] Darken = {5, 4, 15, 14, 25, 24, 35, 34, 45, 44, 55, 54, 85, 84, 95, 94};
		for (int i = 0; i < Darken.Length; i++)
		{
			TilingLighting[Darken[i]].material = BulbColors[0];
		}
		
		yield return new WaitForSecondsRealtime(1.75f);
		int[] Genecode = new int[100];
		for (int x = 0; x < Genecode.Length; x++)
		{
			Genecode[x] = x % 4;
			TilingLighting[x].material = DefaultColor;
		}
		
		yield return new WaitForSecondsRealtime(0.05f);
		while (true)
		{
			for (int a = 0; a < Genecode.Length; a++)
			{
				Genecode[a] = ((Genecode[a] - 1) + 4) % 4;
				switch (Genecode[a])
				{
					case 0:
						TilingLighting[a].material = FloorColor[0];
						break;
					case 1:
						TilingLighting[a].material = FloorColor[3];
						break;
					case 2:
						TilingLighting[a].material = FloorColor[1];
						break;
					case 3:
						TilingLighting[a].material = FloorColor[2];
						break;
					default:
						break;
				}
			}
			yield return new WaitForSecondsRealtime(0.3f);
		}
	}
	
	void StartingNumber()
	{
		MaxStage = Bomb.GetSolvableModuleNames().Where(a => !IgnoredModules.Contains(a)).Count();
		Mackerel = StartCoroutine(Advancement());
	}
	
	void Update()
	{
		if (ActualStage < Bomb.GetSolvedModuleNames().Where(a => !IgnoredModules.Contains(a)).Count() && !ModuleSolved)
        {
            ActualStage++;
			if (Mackerel != null)
			{
				StopCoroutine(Mackerel);
			}
			ThematicMusic.Stop();
			Mackerel = StartCoroutine(Advancement());
        }
		
	}
	
	void TilingToggle (int Button)
	{
		Lighting[Button].AddInteractionPunch(.2f);
		Audio.PlaySoundAtTransform(SFX[1].name, transform);
		if (Playable && ShowTime.text != "REWIND")
		{
			Guideline[Button / 10][Button % 10] = (Guideline[Button / 10][Button % 10] + 1) % 2;
			TilingLighting[Button].material = Guideline[Button / 10][Button % 10] == 1 ? FloorColor[3] : DefaultColor;
		}
	}
	
	IEnumerator Advancement()
	{
		RoundNumber++;
		if (ActualStage == MaxStage || RoundNumber == 144115188075855871)
		{
			if (MaxStage == 0)
			{
				Playable = false;
				StartCoroutine(BulbCycle());
				StartCoroutine(MechaCelebration());
				ShowTime.text = "";
				Debug.LogFormat("[Floor Lights #{0}] Unable to generate stages. Play time is in effect.", moduleId);
			}
			
			else
			{
				for (int z = 0; z < TheLB.Count(); z++)
				{
					TheBulbs[z].material = BulbColors[0];
				}
				
				Debug.LogFormat("[Floor Lights #{0}] Correct toggles to submit:", moduleId);
				for (int y = 0; y < 10; y++)
				{
					string Bardock = "";
					for (int z = 0; z < 10; z++)
					{
						Bardock += NumberBasing[y][z].ToString();
					}
					Debug.LogFormat("[Floor Lights #{0}] {1}", moduleId, Bardock);
				}
				Debug.LogFormat("[Floor Lights #{0}] ----------------------------------------------------------", moduleId);
				
				ThematicMusic.clip = SFX[2];
				ThematicMusic.Play();
				while (ThematicMusic.isPlaying)
				{
					int[] Alea = Enumerable.Range(0,100).ToArray().Shuffle();			
					for (int a = 0; a < 100; a++)
					{
						if (a < 8)
						{
							TilingLighting[Alea[a]].material = FloorColor[UnityEngine.Random.Range(0,3)];
						}
						
						else
						{
							TilingLighting[Alea[a]].material = DefaultColor;
						}
					}
					yield return new WaitForSecondsRealtime(0.2f);
				}
				
				for (int x = 0; x < 100; x++)
				{
					TilingLighting[x].material = DefaultColor;
				}
				Playable = true;
			}
		}
		
		else if (RoundNumber == 144115188075855871)
		{
			for (int z = 0; z < TheLB.Count(); z++)
			{
				TheBulbs[z].material = BulbColors[0];
			}
			
			Debug.LogFormat("[Floor Lights #{0}] Correct toggles to submit:", moduleId);
			for (int y = 0; y < 10; y++)
			{
				string Bardock = "";
				for (int z = 0; z < 10; z++)
				{
					Bardock += NumberBasing[y][z].ToString();
				}
				Debug.LogFormat("[Floor Lights #{0}] {1}", moduleId, Bardock);
			}
			Debug.LogFormat("[Floor Lights #{0}] ----------------------------------------------------------", moduleId);
			
			ThematicMusic.clip = SFX[2];
			ThematicMusic.Play();
			while (ThematicMusic.isPlaying)
			{
				int[] Alea = Enumerable.Range(0,100).ToArray().Shuffle();
				for (int a = 0; a < 100; a++)
				{
					if (a < 8)
					{
						TilingLighting[Alea[a]].material = FloorColor[UnityEngine.Random.Range(0,3)];
					}
					
					else
					{
						TilingLighting[Alea[a]].material = DefaultColor;
					}
				}
			}
			
			for (int x = 0; x < 100; x++)
			{
				TilingLighting[x].material = DefaultColor;
			}
			
			Playable = true;
		}
		
		else
		{
			int[] Mocha = new int[100];
			int[] Secha = Enumerable.Range(0,100).ToArray().Shuffle();
			TheLB[0]++;
			List<int> Callous = new List<int>();
			for (int e = 0; e < 8; e++)
			{
				Callous.Add(Secha[e]);
			}
			Callous.Sort();
			string ColorLights = "";
			
			for (int y = 0; y < TheLB.Count(); y++)
			{
				if (TheLB[y] > 1)
				{
					TheLB[y] = TheLB[y]-2;
					TheLB[y+1]++;
				}
			}
			
			for (int z = 0; z < TheLB.Count(); z++)
			{
					TheBulbs[z].material = BulbColors[TheLB[z]];
			}
			
			int Monty = 0;
			Debug.LogFormat("[Floor Lights #{0}] Tile patterns for Stage {1}:", moduleId, (ActualStage + 1).ToString());
			for (int x = 0; x < 100; x++)
			{
				if (Monty != 8 && x == Callous[Monty])
				{
					int Heckel = UnityEngine.Random.Range(0,3);
					Mocha[x] = Heckel;
					Monty++;
					if (Heckel == 0)
					{
						ColorLights += "R";
						if (x / 10 == 0)
						{
							if (x == 0)
							{
								NumberBasing[0][0] = (NumberBasing[0][0] + 1) % 2;
								NumberBasing[0][1] = (NumberBasing[0][1] + 1) % 2;
								NumberBasing[1][0] = (NumberBasing[1][0] + 1) % 2;
							}
							
							else if (x == 9)
							{
								NumberBasing[0][9] = (NumberBasing[0][9] + 1) % 2;
								NumberBasing[0][8] = (NumberBasing[0][8] + 1) % 2;
								NumberBasing[1][9] = (NumberBasing[1][9] + 1) % 2;
							}
							
							else
							{
								NumberBasing[0][x % 10] = (NumberBasing[0][x % 10] + 1) % 2;
								NumberBasing[0][(x % 10) + 1] = (NumberBasing[0][(x % 10) + 1] + 1) % 2;
								NumberBasing[0][(x % 10) - 1] = (NumberBasing[0][(x % 10) - 1] + 1) % 2;
								NumberBasing[1][x % 10] = (NumberBasing[1][x % 10] + 1) % 2;
							}
						}
						
						else if (x / 10 == 9)
						{
							if (x == 90)
							{
								NumberBasing[9][0] = (NumberBasing[9][0] + 1) % 2;
								NumberBasing[9][1] = (NumberBasing[9][1] + 1) % 2;
								NumberBasing[8][0] = (NumberBasing[8][0] + 1) % 2;
							}
							
							else if (x == 99)
							{
								NumberBasing[9][9] = (NumberBasing[9][9] + 1) % 2;
								NumberBasing[9][8] = (NumberBasing[9][8] + 1) % 2;
								NumberBasing[8][9] = (NumberBasing[8][9] + 1) % 2;
							}
						
							else
							{
								NumberBasing[9][x % 10] = (NumberBasing[9][x % 10] + 1) % 2;
								NumberBasing[9][(x % 10) + 1] = (NumberBasing[9][(x % 10) + 1] + 1) % 2;
								NumberBasing[9][(x % 10) - 1] = (NumberBasing[9][(x % 10) - 1] + 1) % 2;
								NumberBasing[8][x % 10] = (NumberBasing[8][x % 10] + 1) % 2;
							}
						}
						
						else if (x % 10 == 0)
						{
							if (x == 0)
							{
								NumberBasing[0][0] = (NumberBasing[0][0] + 1) % 2;
								NumberBasing[0][1] = (NumberBasing[0][1] + 1) % 2;
								NumberBasing[1][0] = (NumberBasing[1][0] + 1) % 2;
							}
							
							else if (x == 90)
							{
								NumberBasing[9][0] = (NumberBasing[9][0] + 1) % 2;
								NumberBasing[9][1] = (NumberBasing[9][1] + 1) % 2;
								NumberBasing[8][0] = (NumberBasing[8][0] + 1) % 2;
							}
							
							else
							{
								NumberBasing[x / 10][0] = (NumberBasing[x / 10][0] + 1) % 2;
								NumberBasing[(x / 10) + 1][0] = (NumberBasing[(x / 10) + 1][0] + 1) % 2;
								NumberBasing[(x / 10) - 1][0] = (NumberBasing[(x / 10) - 1][0] + 1) % 2;
								NumberBasing[x / 10][1] = (NumberBasing[x / 10][1] + 1) % 2;
							}
						}
						
						else if (x % 10 == 9)
						{
							if (x == 9)
							{
								NumberBasing[0][9] = (NumberBasing[0][9] + 1) % 2;
								NumberBasing[0][8] = (NumberBasing[0][8] + 1) % 2;
								NumberBasing[1][9] = (NumberBasing[1][9] + 1) % 2;
							}
							
							else if (x == 99)
							{
								NumberBasing[9][9] = (NumberBasing[9][9] + 1) % 2;
								NumberBasing[9][8] = (NumberBasing[9][8] + 1) % 2;
								NumberBasing[8][9] = (NumberBasing[8][9] + 1) % 2;
							}
							
							else
							{
								NumberBasing[x / 10][9] = (NumberBasing[x / 10][9] + 1) % 2;
								NumberBasing[(x / 10) + 1][9] = (NumberBasing[(x / 10) + 1][9] + 1) % 2;
								NumberBasing[(x / 10) - 1][9] = (NumberBasing[(x / 10) - 1][9] + 1) % 2;
								NumberBasing[x / 10][8] = (NumberBasing[x / 10][8] + 1) % 2;
							}
						}
						
						else
						{
							NumberBasing[x / 10][x % 10] = (NumberBasing[x / 10][x % 10] + 1) % 2;
							NumberBasing[x / 10][(x % 10) + 1] = (NumberBasing[x / 10][(x % 10) + 1] + 1) % 2;
							NumberBasing[x / 10][(x % 10) - 1] = (NumberBasing[x / 10][(x % 10) - 1] + 1) % 2;
							NumberBasing[(x / 10) + 1][x % 10] = (NumberBasing[(x / 10) + 1][x % 10] + 1) % 2;
							NumberBasing[(x / 10) - 1][x % 10] = (NumberBasing[(x / 10) - 1][x % 10] + 1) % 2;
						}
					}
					
					else if (Heckel == 1)
					{
						ColorLights += "G";
						if (x / 10 == 0)
						{
							if (x == 0)
							{
								NumberBasing[0][0] = (NumberBasing[0][0] + 1) % 2;
								NumberBasing[1][1] = (NumberBasing[1][1] + 1) % 2;
							}
							
							else if (x == 9)
							{
								NumberBasing[0][9] = (NumberBasing[0][9] + 1) % 2;
								NumberBasing[1][8] = (NumberBasing[1][8] + 1) % 2;
							}
							
							else
							{
								NumberBasing[0][x % 10] = (NumberBasing[0][x % 10] + 1) % 2;
								NumberBasing[1][(x % 10) + 1] = (NumberBasing[1][(x % 10) + 1] + 1) % 2;
								NumberBasing[1][(x % 10) - 1] = (NumberBasing[1][(x % 10) - 1] + 1) % 2;
							}
						}
						
						else if (x / 10 == 9)
						{
							if (x == 90)
							{
								NumberBasing[9][0] = (NumberBasing[9][0] + 1) % 2;
								NumberBasing[8][1] = (NumberBasing[8][1] + 1) % 2;
							}
							
							else if (x == 99)
							{
								NumberBasing[9][9] = (NumberBasing[9][9] + 1) % 2;
								NumberBasing[8][8] = (NumberBasing[8][8] + 1) % 2;
							}
							
							else
							{
								NumberBasing[9][x % 10] = (NumberBasing[9][x % 10] + 1) % 2;
								NumberBasing[8][(x % 10) + 1] = (NumberBasing[8][(x % 10) + 1] + 1) % 2;
								NumberBasing[8][(x % 10) - 1] = (NumberBasing[8][(x % 10) - 1] + 1) % 2;
							}
						}
						
						else if (x % 10 == 0)
						{
							if (x == 0)
							{
								NumberBasing[0][0] = (NumberBasing[0][0] + 1) % 2;
								NumberBasing[1][1] = (NumberBasing[1][1] + 1) % 2;
							}
							
							else if (x == 90)
							{
								NumberBasing[9][0] = (NumberBasing[9][0] + 1) % 2;
								NumberBasing[8][1] = (NumberBasing[8][1] + 1) % 2;
							}
							
							else
							{
								NumberBasing[x / 10][0] = (NumberBasing[x / 10][0] + 1) % 2;
								NumberBasing[(x / 10) + 1][1] = (NumberBasing[(x / 10) + 1][1] + 1) % 2;
								NumberBasing[(x / 10) - 1][1] = (NumberBasing[(x / 10) - 1][1] + 1) % 2;
							}
						}
						
						else if (x % 10 == 9)
						{
							if (x == 9)
							{
								NumberBasing[0][9] = (NumberBasing[0][9] + 1) % 2;
								NumberBasing[1][8] = (NumberBasing[1][8] + 1) % 2;
							}
							
							else if (x == 99)
							{
								NumberBasing[9][9] = (NumberBasing[9][9] + 1) % 2;
								NumberBasing[8][8] = (NumberBasing[8][8] + 1) % 2;
							}
							
							else
							{
								NumberBasing[x / 10][9] = (NumberBasing[x / 10][9] + 1) % 2;
								NumberBasing[(x / 10) + 1][8] = (NumberBasing[(x / 10) + 1][8] + 1) % 2;
								NumberBasing[(x / 10) - 1][8] = (NumberBasing[(x / 10) - 1][8] + 1) % 2;
							}
						}
						
						else
						{
							NumberBasing[x / 10][x % 10] = (NumberBasing[x / 10][x % 10] + 1) % 2;
							NumberBasing[(x / 10) + 1][(x % 10) + 1] = (NumberBasing[(x / 10) + 1][(x % 10) + 1] + 1) % 2;
							NumberBasing[(x / 10) - 1][(x % 10) - 1] = (NumberBasing[(x / 10) - 1][(x % 10) - 1] + 1) % 2;
							NumberBasing[(x / 10) + 1][(x % 10) - 1] = (NumberBasing[(x / 10) + 1][(x % 10) - 1] + 1) % 2;
							NumberBasing[(x / 10) - 1][(x % 10) + 1] = (NumberBasing[(x / 10) - 1][(x % 10) + 1] + 1) % 2;
						}
					}
					
					else
					{
						ColorLights += "B";
						if (x / 10 == 0)
						{
							if (x == 0)
							{
								NumberBasing[0][0] = (NumberBasing[0][0] + 1) % 2;
								NumberBasing[0][1] = (NumberBasing[0][1] + 1) % 2;
								NumberBasing[1][0] = (NumberBasing[1][0] + 1) % 2;
								NumberBasing[1][1] = (NumberBasing[1][1] + 1) % 2;
							}
							
							else if (x == 9)
							{
								NumberBasing[0][9] = (NumberBasing[0][9] + 1) % 2;
								NumberBasing[0][8] = (NumberBasing[0][8] + 1) % 2;
								NumberBasing[1][9] = (NumberBasing[1][9] + 1) % 2;
								NumberBasing[1][8] = (NumberBasing[1][8] + 1) % 2;
							}
							
							else
							{
								NumberBasing[0][x % 10] = (NumberBasing[0][x % 10] + 1) % 2;
								NumberBasing[0][(x % 10) + 1] = (NumberBasing[0][(x % 10) + 1] + 1) % 2;
								NumberBasing[0][(x % 10) - 1] = (NumberBasing[0][(x % 10) - 1] + 1) % 2;
								NumberBasing[1][(x % 10) + 1] = (NumberBasing[1][(x % 10) + 1] + 1) % 2;
								NumberBasing[1][(x % 10) - 1] = (NumberBasing[1][(x % 10) - 1] + 1) % 2;
								NumberBasing[1][x % 10] = (NumberBasing[1][x % 10] + 1) % 2;
							}
						}
						
						else if (x / 10 == 9)
						{
							if (x == 90)
							{
								NumberBasing[9][0] = (NumberBasing[9][0] + 1) % 2;
								NumberBasing[9][1] = (NumberBasing[9][1] + 1) % 2;
								NumberBasing[8][0] = (NumberBasing[8][0] + 1) % 2;
								NumberBasing[8][1] = (NumberBasing[8][1] + 1) % 2;
							}
							
							else if (x == 99)
							{
								NumberBasing[9][9] = (NumberBasing[9][9] + 1) % 2;
								NumberBasing[9][8] = (NumberBasing[9][8] + 1) % 2;
								NumberBasing[8][9] = (NumberBasing[8][9] + 1) % 2;
								NumberBasing[8][8] = (NumberBasing[8][8] + 1) % 2;
							}
							
							else
							{
								NumberBasing[9][x % 10] = (NumberBasing[9][x % 10] + 1) % 2;
								NumberBasing[9][(x % 10) + 1] = (NumberBasing[9][(x % 10) + 1] + 1) % 2;
								NumberBasing[9][(x % 10) - 1] = (NumberBasing[9][(x % 10) - 1] + 1) % 2;
								NumberBasing[8][(x % 10) + 1] = (NumberBasing[8][(x % 10) + 1] + 1) % 2;
								NumberBasing[8][(x % 10) - 1] = (NumberBasing[8][(x % 10) - 1] + 1) % 2;
								NumberBasing[8][x % 10] = (NumberBasing[8][x % 10] + 1) % 2;
							}
						}
						
						else if (x % 10 == 0)
						{
							if (x == 0)
							{
								NumberBasing[0][0] = (NumberBasing[0][0] + 1) % 2;
								NumberBasing[0][1] = (NumberBasing[0][1] + 1) % 2;
								NumberBasing[1][0] = (NumberBasing[1][0] + 1) % 2;
								NumberBasing[1][1] = (NumberBasing[1][1] + 1) % 2;
							}
							
							else if (x == 90)
							{
								NumberBasing[9][0] = (NumberBasing[9][0] + 1) % 2;
								NumberBasing[9][1] = (NumberBasing[9][1] + 1) % 2;
								NumberBasing[8][0] = (NumberBasing[8][0] + 1) % 2;
								NumberBasing[8][1] = (NumberBasing[8][1] + 1) % 2;
							}
							
							else
							{
								NumberBasing[x / 10][0] = (NumberBasing[x / 10][0] + 1) % 2;
								NumberBasing[(x / 10) + 1][0] = (NumberBasing[(x / 10) + 1][0] + 1) % 2;
								NumberBasing[(x / 10) - 1][0] = (NumberBasing[(x / 10) - 1][0] + 1) % 2;
								NumberBasing[(x / 10) + 1][1] = (NumberBasing[(x / 10) + 1][1] + 1) % 2;
								NumberBasing[(x / 10) - 1][1] = (NumberBasing[(x / 10) - 1][1] + 1) % 2;
								NumberBasing[x / 10][1] = (NumberBasing[x / 10][1] + 1) % 2;
							}
						}
						
						else if (x % 10 == 9)
						{
							if (x == 9)
							{
								NumberBasing[0][9] = (NumberBasing[0][9] + 1) % 2;
								NumberBasing[0][8] = (NumberBasing[0][8] + 1) % 2;
								NumberBasing[1][9] = (NumberBasing[1][9] + 1) % 2;
								NumberBasing[1][8] = (NumberBasing[1][8] + 1) % 2;
							}
							
							else if (x == 99)
							{
								NumberBasing[9][9] = (NumberBasing[9][9] + 1) % 2;
								NumberBasing[9][8] = (NumberBasing[9][8] + 1) % 2;
								NumberBasing[8][9] = (NumberBasing[8][9] + 1) % 2;
								NumberBasing[8][8] = (NumberBasing[8][8] + 1) % 2;
							}
							
							else
							{
								NumberBasing[x / 10][9] = (NumberBasing[x / 10][9] + 1) % 2;
								NumberBasing[(x / 10) + 1][9] = (NumberBasing[(x / 10) + 1][9] + 1) % 2;
								NumberBasing[(x / 10) - 1][9] = (NumberBasing[(x / 10) - 1][9] + 1) % 2;
								NumberBasing[(x / 10) + 1][8] = (NumberBasing[(x / 10) + 1][8] + 1) % 2;
								NumberBasing[(x / 10) - 1][8] = (NumberBasing[(x / 10) - 1][8] + 1) % 2;
								NumberBasing[x / 10][8] = (NumberBasing[x / 10][8] + 1) % 2;
							}
						}
						
						else
						{
							NumberBasing[x / 10][x % 10] = (NumberBasing[x / 10][x % 10] + 1) % 2;
							NumberBasing[x / 10][(x % 10) + 1] = (NumberBasing[x / 10][(x % 10) + 1] + 1) % 2;
							NumberBasing[x / 10][(x % 10) - 1] = (NumberBasing[x / 10][(x % 10) - 1] + 1) % 2;
							NumberBasing[(x / 10) + 1][x % 10] = (NumberBasing[(x / 10) + 1][x % 10] + 1) % 2;
							NumberBasing[(x / 10) - 1][x % 10] = (NumberBasing[(x / 10) - 1][x % 10] + 1) % 2;
							NumberBasing[(x / 10) + 1][(x % 10) + 1] = (NumberBasing[(x / 10) + 1][(x % 10) + 1] + 1) % 2;
							NumberBasing[(x / 10) - 1][(x % 10) - 1] = (NumberBasing[(x / 10) - 1][(x % 10) - 1] + 1) % 2;
							NumberBasing[(x / 10) + 1][(x % 10) - 1] = (NumberBasing[(x / 10) + 1][(x % 10) - 1] + 1) % 2;
							NumberBasing[(x / 10) - 1][(x % 10) + 1] = (NumberBasing[(x / 10) - 1][(x % 10) + 1] + 1) % 2;
						}
					}
				}
				
				else
				{
					ColorLights += "*";
					Mocha[x] = -1;
				}
				
				if (x % 10 == 9)
				{
					Debug.LogFormat("[Floor Lights #{0}] {1}", moduleId, ColorLights);
					ColorLights = "";
				}
			}
			
			CorrectNumberPlacement.Add(Mocha);
			
			Debug.LogFormat("[Floor Lights #{0}] Correct toggle patterns for this stage:", moduleId);
			for (int y = 0; y < 10; y++)
			{
				string Bardock = "";
				for (int z = 0; z < 10; z++)
				{
					Bardock += NumberBasing[y][z].ToString();
				}
				Debug.LogFormat("[Floor Lights #{0}] {1}", moduleId, Bardock);
			}
			Debug.LogFormat("[Floor Lights #{0}] ----------------------------------------------------------", moduleId);
			
			ThematicMusic.clip = SFX[2];
			ThematicMusic.Play();
			while (ThematicMusic.isPlaying)
			{
				int[] Alea = Enumerable.Range(0,100).ToArray().Shuffle();
				for (int a = 0; a < 100; a++)
				{
					if (a < 8)
					{
						TilingLighting[Alea[a]].material = FloorColor[UnityEngine.Random.Range(0,3)];
					}
					
					else
					{
						TilingLighting[Alea[a]].material = DefaultColor;
					}
				}
				yield return new WaitForSecondsRealtime(0.2f);
			}
			
			for (int b = 0; b < 100; b++)
			{
				if (Mocha[b] > -1)
				{
					TilingLighting[b].material = FloorColor[Mocha[b]];
				}
				
				else
				{
					TilingLighting[b].material = DefaultColor;
				}
			}
		}
	}
	
	//twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"To press a certain tile(s) on the platform, use the command !{0} tile [1-100] (Multiple tiles can be pressed using the command) | To submit the answer given on the module, use the command !{0} submit | To perform a rewind (if you are able to), use the command !{0} rewind | To move forward on a stage during rewind, use the command !{0} advance";
    #pragma warning restore 414
	
	 IEnumerator ProcessTwitchCommand(string command)
    {
		string[] parameters = command.Split(' ');
		if (!Playable)
		{
			yield return "sendtochaterror You can not interact with the module currently. The command was not processed.";
			yield break;
		}
		
		if (Regex.IsMatch(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			if (ShowTime.text == "REWIND")
			{
				yield return "sendtochaterror You are currently on rewind. The command was not processed.";
				yield break;
			}
			yield return "solve";
			yield return "strike";
			Showtime.OnInteract();
			yield return new WaitForSecondsRealtime(0.1f);
			Showtime.OnInteractEnded();
		}
		
		if (Regex.IsMatch(command, @"^\s*rewind\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			if (!Striked)
			{
				yield return "sendtochaterror You are unable to rewind currently. The command was not processed.";
				yield break;
			}
			
			if (ShowTime.text == "REWIND")
			{
				yield return "sendtochaterror You are currently on rewind. The command was not processed.";
				yield break;
			}
			Showtime.OnInteract();
			yield return new WaitForSecondsRealtime(2.5f);
			Showtime.OnInteractEnded();
		}
		
		if (Regex.IsMatch(command, @"^\s*advance\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			if (ShowTime.text != "REWIND")
			{
				yield return "sendtochaterror You are currently not on rewind. The command was not processed.";
				yield break;
			}
			Showtime.OnInteract();
			yield return new WaitForSecondsRealtime(0.1f);
			Showtime.OnInteractEnded();
		}
		
		if (Regex.IsMatch(parameters[0], @"^\s*tile\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			if (parameters.Length < 2)
			{
				yield return "sendtochaterror Parameter length invalid. The command was not processed.";
				yield break;
			}
			
			for (int x = 1; x < parameters.Length; x++)
			{
				yield return "trycancel The command to perform the action was cancelled due to a cancel request.";
				if (ShowTime.text == "REWIND")
				{
					yield return "sendtochaterror You are currently on rewind. The command was halted.";
					yield break;
				}
				
				int Ham;
				if (!Int32.TryParse(parameters[x], out Ham))
				{
					yield return "sendtochaterror Tile position being sent contains is invalid. The command was halted.";
					yield break;
				}
			
				if (Int32.Parse(parameters[x]) < 1 || Int32.Parse(parameters[x]) > 100)
				{
					yield return "sendtochaterror Tile position being is not between [1-100]. The command was halted.";
					yield break;
				}
				Lighting[Int32.Parse(parameters[x]) - 1].OnInteract();
				yield return new WaitForSecondsRealtime(0.05f);
			}
		}
		
	}
}
