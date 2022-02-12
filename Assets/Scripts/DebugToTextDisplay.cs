using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class DebugToTextDisplay : MonoBehaviour
{
    public int maxLines = 8;
    private Queue<string> queue = new Queue<string>();
    private string currentText = "";
    private GUIStyle guiStyle = new GUIStyle();

    // private void Update()
    // {
    //     debugText.text = "Debug Log: " + currentText;
    // }

    void OnEnable()
    {
        Application.logMessageReceivedThreaded += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLog;
    }
    void OnGUI()
    {
        guiStyle.fontSize = 25;
        guiStyle.normal.textColor = Color.white;
        GUI.BeginGroup(new Rect(10,1500,800,500));
        GUI.Box(new Rect(0,0,800,500),$"Debug", guiStyle);
        GUI.Label(new Rect(10,25,300,30), currentText,guiStyle);

        GUI.EndGroup();
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Delete oldest message
        if (queue.Count >= maxLines) queue.Dequeue();

        queue.Enqueue(logString);

        var builder = new StringBuilder();
        foreach (string st in queue)
        {
            builder.Append(st).Append("\n");
        }

        currentText = builder.ToString();
    }
}