#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;
using System;
using System.IO;

public class T23_EditorUtility : Editor
{
    public static void ShowTitle(string title)
    {
        Color backColor = Color.white;
        Color textColor = Color.white;
        switch (title)
        {
            case "Master":
                backColor = Color.red;
                textColor = new Color(0.7f, 0.7f, 0.7f);
                break;
            case "Broadcast":
                backColor = Color.green;
                textColor = new Color(0.5f, 0.5f, 0.5f);
                break;
            case "Trigger":
                backColor = Color.yellow;
                textColor = new Color(0.5f, 0.5f, 0.5f);
                break;
            case "Action":
                backColor = Color.cyan;
                textColor = new Color(0.5f, 0.5f, 0.5f);
                break;
            case "Option":
                backColor = Color.white;
                textColor = new Color(0.5f, 0.5f, 0.5f);
                break;
        }

        Color oldBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = backColor;
        GUIStyle titleStyle = new GUIStyle(EditorStyles.textField);
        titleStyle.normal.textColor = textColor;
        titleStyle.fontStyle = FontStyle.BoldAndItalic;
        EditorGUILayout.TextField(">>> Trigger2to3 " + title, titleStyle);
        GUI.backgroundColor = oldBackgroundColor;
    }

    public static GUIStyle HeadlineStyle(bool isMaster = false)
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = isMaster ? 20 : 14;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(0.5f, 0.5f, 0);
        return style;
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

    public static bool GuideJoinMaster(T23_Master master, UdonSharpBehaviour body, int gid, int category)
    {
        if (!UdonSharpEditorUtility.IsProxyBehaviour(body))
        {
            UdonSharpGUI.DrawConvertToUdonBehaviourButton(body);
            return false;
        }

        if (master == null)
        {
            EditorGUILayout.HelpBox("Master に組み込まれていません。 このままでも動作しますが、次のボタンで Master に組み込むことができます。", MessageType.Info);
            if (GUILayout.Button("Join Master"))
            {
                EditorApplication.delayCall += () => T23_Master.JoinMaster(body, gid, category);
            }
        }
        return true;
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

    public static Dictionary<string, UdonSharpProgramAsset> GetProgramAssets(string categoryName)
    {
        Dictionary<string, UdonSharpProgramAsset> assetList = new Dictionary<string, UdonSharpProgramAsset>();
        string path = "Assets/Trigger2to3/ProgramAsset/" + categoryName;
        string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        List<string> filelist = new List<string>(files);
        filelist.Sort();
        foreach (var file in filelist)
        {
            if (file.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase)) { continue; }
            UdonSharpProgramAsset asset = (UdonSharpProgramAsset)AssetDatabase.LoadAssetAtPath(file, typeof(UdonSharpProgramAsset));
            string key = asset.GetClass().Name.Replace("T23_", "");
            assetList.Add(key, asset);
        }
        return assetList;
    }

    public static T23_BroadcastGlobal[] TakeCommonBuffersRelate(T23_CommonBuffer commonBuffer)
    {
        List<T23_BroadcastGlobal> broadcastGlobals = new List<T23_BroadcastGlobal>();
        var rootObjs = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        if (rootObjs.Length > 0)
        {
            foreach (var rootObj in rootObjs)
            {
                var udons = rootObj.GetComponentsInChildren<UdonBehaviour>(true);
                foreach (var udon in udons)
                {
                    UdonSharpBehaviour usharp = UdonSharpEditorUtility.FindProxyBehaviour(udon);
                    var broadcast = usharp.GetUdonSharpComponent<T23_BroadcastGlobal>();
                    if (broadcast)
                    {
                        var field = usharp.GetProgramVariable("commonBuffer") as T23_CommonBuffer;
                        if (field == commonBuffer)
                        {
                            broadcastGlobals.Add(broadcast);
                        }
                    }
                }
            }
        }
        return broadcastGlobals.ToArray();
    }
}
#endif
