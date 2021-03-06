﻿using UnityEngine;
using System.Collections;

public class TimerDothAppear : MonoBehaviour {
	public GUIStyle timerGUI;
    public bool Countdown {get; set;}

	private float minutes = 20;
	private float seconds = 0;
	private float milliseconds = 0;

	void OnGUI(){
        if(Countdown){
        	if(minutes < 0){
                Countdown = false;
                Application.LoadLevel("WinLose");
            }

            if(milliseconds <= 0){
        		if(seconds <= 0){
        			minutes--;
        			seconds = 59;
        		}
        		else if(seconds >= 0){
        			seconds--;
        		}
                milliseconds = 100;
    		}
    		milliseconds -= Time.deltaTime * 60;
            string timerTime = string.Format("{0}:{1}:{2}", minutes, seconds, (int)milliseconds);
            GUI.Label(new Rect(Screen.width-500, 10, 300, 20), "Signal Loss Countdown: \n             " + timerTime, timerGUI);
        }
	}
}
