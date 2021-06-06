#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UdonSharp;
using System;

public class T23_EditorUtility : Editor
{
    public static void ShowTitle(string title)
    {
        Color backColor = Color.white;
        switch (title)
        {
            case "Master":
                backColor = Color.red;
                break;
            case "Broadcast":
                backColor = Color.green;
                break;
            case "Trigger":
                backColor = Color.yellow;
                break;
            case "Action":
                backColor = Color.cyan;
                break;
        }

        Color oldBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = backColor;
        GUIStyle titleStyle = new GUIStyle(EditorStyles.textField);
        titleStyle.normal.textColor = Color.white;
        titleStyle.fontStyle = FontStyle.BoldAndItalic;
        EditorGUILayout.TextField(">>> Trigger2to3 " + title, titleStyle);
        GUI.backgroundColor = oldBackgroundColor;
    }


    public static void ShowSwapButton(T23_Master master, string currentTitle)
    {
        List<string> titles = master.actionTitles;

        int c = titles.IndexOf(currentTitle);
        if (c == -1) { return; }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.currentViewWidth - 100);
        EditorGUI.BeginDisabledGroup(c == 0);
        if (GUILayout.Button("↑"))
        {
            string swapTitle = titles[c - 1];
            titles[c - 1] = currentTitle;
            titles[c] = swapTitle;
            master.OrderComponents(true);
        }
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(c == titles.Count - 1);
        if (GUILayout.Button("↓"))
        {
            string swapTitle = titles[c + 1];
            titles[c + 1] = currentTitle;
            titles[c] = swapTitle;
            master.OrderComponents(true);
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    public static void GuideJoinMaster(UdonSharpBehaviour body, int gid, int category)
    {
        EditorGUILayout.HelpBox("Master に組み込まれていません。 このままでも動作しますが、次のボタンで Master に組み込むことができます。", MessageType.Info);
        if (GUILayout.Button("Join Master"))
        {
            EditorApplication.delayCall += () => T23_Master.JoinMaster(body, gid, category);
        }
    }

    public static LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        List<string> layers = new List<string>();
        List<int> layerNumbers = new List<int>();

        for (var i = 0; i < 32; ++i)
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName))
            {
                layers.Add(layerName);
                layerNumbers.Add(i);
            }
        }

        int maskWithoutEmpty = 0;
        for (var i = 0; i < layerNumbers.Count; ++i)
        {
            if (0 < ((1 << layerNumbers[i]) & layerMask.value))
                maskWithoutEmpty |= 1 << i;
        }

        maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers.ToArray());
        int mask = 0;
        for (var i = 0; i < layerNumbers.Count; ++i)
        {
            if (0 < (maskWithoutEmpty & (1 << i)))
                mask |= 1 << layerNumbers[i];
        }
        layerMask.value = mask;

        return layerMask;
    }
}
#endif
