﻿using Parse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : Singleton<GameManager> {
    private string teamID;
    private int readyTeamsCount = 0;
    private static float POLL_INTERVAL = 1f;

    private static float X_ANCHOR = -6.0f;
    private static float Y_ANCHOR = 4.0f;
    private static float Z_ANCHOR = 1.0f;
    private static float X_ROW_DIFF = 2.0f;
    private static float Y_COL_DIFF = -1.5f;
    private static float X_OFFSET = X_ROW_DIFF/2;
    private static int BUTTONS_PER_ROW = 4;

    private static string TEAM = "Team";
    private static string DISABLED = "Disabled";
    private static string SELECTED = "Selected";
    private static string AVAILABLE = "";

    private static string STATUS_START = "Waiting for ";
    private static string STATUS_END = " teams to sign in...";

	public static int TEAM_COUNT = 16;
    public static int DISABLED_LAYER = 9;
    public static int ENABLED_LAYER = 0;

    public GameObject buttonPrefab;

    private Dictionary<string, GameObject> buttons = new Dictionary<string, GameObject>(TEAM_COUNT);
    private Dictionary<string, Team> teams = new Dictionary<string, Team>(TEAM_COUNT);
    private TeamButton currentSelection;
    private DateTime? lastUpdatedTime;

    private bool waitForTeams = true;
    private bool isGuiOn = true;
    public GUIText statusOfTeams;
	
    protected GameManager(){}
		
	void Start () {
		DontDestroyOnLoad(this);
        StartCoroutine("InitButtons");
        StartCoroutine("CheckForUpdatesThenStartGame");
    }

    void Update () {}

    private IEnumerator InitButtons(){
        GameObject go = new GameObject("StatusOfTeams");
        go.transform.position = new Vector3(0.65f, 0.75f, 0);
        statusOfTeams = (GUIText) go.AddComponent(typeof(GUIText));
        statusOfTeams.text = GetStatusOfTeamsText();
        statusOfTeams.fontSize = 16;
        var query = new ParseQuery<Team>().FindAsync();
        while(!query.IsCompleted) yield return null;
        IEnumerable<Team> allTeams = query.Result;
        int i = 0;
        foreach(Team team in allTeams){
            if(i >= TEAM_COUNT){
                break;
            }
            teams.Add(team.ObjectId, team);
            AddButton(team, i);
            i++;
            lastUpdatedTime = ParseUtil.GetLatestTime(team, lastUpdatedTime);
        }
    }
    
    private IEnumerator CheckForUpdatesThenStartGame(){
        yield return null;
        while(waitForTeams){
            yield return new WaitForSeconds(POLL_INTERVAL);
            var query = new ParseQuery<Team>().WhereGreaterThan("updatedAt", lastUpdatedTime).FindAsync();
            while(!query.IsCompleted) yield return null;
            IEnumerable<Team> updatedTeams = query.Result;
            foreach(Team team in updatedTeams){
                GameObject button;
                buttons.TryGetValue(team.ObjectId, out button);
                if(team.IsSignedIn){    
                    ++readyTeamsCount;
                    SetStatus(team.ObjectId, DISABLED);
                }
                else{
                    --readyTeamsCount;
                    SetStatus(team.ObjectId, AVAILABLE);
                }
                lastUpdatedTime = ParseUtil.GetLatestTime(team, lastUpdatedTime);
            }
            if(readyTeamsCount == TEAM_COUNT){
                waitForTeams = false;
            }
        }
        Application.LoadLevel("PS_MainMapScene");
    }

	private void AddButton(Team team, int index){
        int row = index / BUTTONS_PER_ROW;
        float x = (index % BUTTONS_PER_ROW) * X_ROW_DIFF + ((row % 2) * X_OFFSET);
        float y = row * Y_COL_DIFF;
        Vector3 position = new Vector3(x + X_ANCHOR, y + Y_ANCHOR, Z_ANCHOR);
        GameObject go = (GameObject) Instantiate(buttonPrefab, position, Quaternion.identity);
        go.renderer.material.mainTexture = GetButtonTexture(team, AVAILABLE);
        go.renderer.material.shader = Shader.Find("Unlit/Transparent");
		TeamButton button = (TeamButton) go.GetComponent<TeamButton>();
        string id = team.ObjectId;
        button.Id = id;
		buttons.Add(id, go);
		if(team.IsSignedIn){
            ++readyTeamsCount;
            SetStatus(id, DISABLED);
		}
	}
    
    private Texture GetButtonTexture(Team team, string modifier) {
        string teamButtonName = "Buttons/" + TEAM + team.Name + modifier;
        return Resources.Load(teamButtonName, typeof(Texture)) as Texture;
    }

	public void UpdateSelection(TeamButton newSelection){
		if(newSelection == null){
			return;
		}
		if(currentSelection != null){
            SetStatus(currentSelection.Id, AVAILABLE);
		}
		if(currentSelection == newSelection){
            //unselecting
			currentSelection = null;
			return;
		}
		currentSelection = newSelection;
        SetStatus(currentSelection.Id, SELECTED);
	}
    
    private void SetStatus(string id, string status){
        GameObject go;
        buttons.TryGetValue(id, out go);
        Team team = GetTeam(id);
        go.renderer.material.mainTexture = GetButtonTexture(team, status);
        if(DISABLED.Equals(status)){
            go.layer = DISABLED_LAYER;
        }
        else {
            go.layer = ENABLED_LAYER;
        }
        statusOfTeams.text = GetStatusOfTeamsText();
    }

    private string GetStatusOfTeamsText(){
        return STATUS_START + (TEAM_COUNT - readyTeamsCount) + STATUS_END;
    }

    public Team GetTeam(string id){
        Team team;
        teams.TryGetValue(id, out team);
        return team;
    }

    public void Gui(bool state){
        isGuiOn = state;
    }

    public bool IsGuiOn(){
        return isGuiOn;
    }
}
