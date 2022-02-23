#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;
using System;
using System.IO;

public class T23_EditorUtility : Editor
{
    private static bool commonBufferUpdateTask = false;

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
            master.OrderComponents();
            master.shouldMoveComponents = true;
        }
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(c == titles.Count - 1);
        if (GUILayout.Button("↓"))
        {
            string swapTitle = titles[c + 1];
            titles[c + 1] = currentTitle;
            titles[c] = swapTitle;
            master.OrderComponents();
            master.shouldMoveComponents = true;
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

    public static List<T23_BroadcastGlobal> GetAllBroadcastGlobals()
    {
        List<T23_BroadcastGlobal> broadcastGlobals = new List<T23_BroadcastGlobal>();
        GameObject[] rootObjs = null;
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null)
        {
            rootObjs = new GameObject[1];
            rootObjs[0] = stage.prefabContentsRoot;
        }
        else
        {
            rootObjs = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        }
        if (rootObjs.Length > 0)
        {
            foreach (var rootObj in rootObjs)
            {
                var udons = rootObj.GetComponentsInChildren<UdonBehaviour>(true);
                foreach (var udon in udons)
                {
                    var proxy = UdonSharpEditorUtility.FindProxyBehaviour(udon);
                    if (proxy == null) { continue; }

                    var broadcast = proxy as T23_BroadcastGlobal;
                    if (broadcast == null) { continue; }

                    broadcastGlobals.Add(broadcast);
                }
            }
        }
        return broadcastGlobals;
    }

    public static List<T23_CommonBuffer> GetAllCommonBuffers()
    {
        var commonBuffers = new List<T23_CommonBuffer>();
        GameObject[] rootObjs = null;
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null)
        {
            rootObjs = new GameObject[1];
            rootObjs[0] = stage.prefabContentsRoot;
        }
        else
        {
            rootObjs = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        }
        if (rootObjs.Length > 0)
        {
            foreach (var rootObj in rootObjs)
            {
                var udons = rootObj.GetComponentsInChildren<UdonBehaviour>(true);
                foreach (var udon in udons)
                {
                    var proxy = UdonSharpEditorUtility.FindProxyBehaviour(udon);
                    if (proxy == null) { continue; }

                    var commonBuffer = proxy as T23_CommonBuffer;
                    if (commonBuffer == null) { continue; }

                    commonBuffers.Add(commonBuffer);
                }
            }
        }
        return commonBuffers;
    }

    public static T23_BroadcastGlobal[] TakeCommonBuffersRelate(T23_CommonBuffer commonBuffer)
    {
        List<T23_BroadcastGlobal> broadcastGlobals = new List<T23_BroadcastGlobal>();
        var allbroadcasts = GetAllBroadcastGlobals();
        foreach (var broadcast in allbroadcasts)
        {
            var field = broadcast.GetProgramVariable("commonBuffer") as T23_CommonBuffer;
            if (field != null && field.transform.GetHierarchyPath() == commonBuffer.transform.GetHierarchyPath())
            {
                broadcastGlobals.Add(broadcast);
            }
        }
        return broadcastGlobals.ToArray();
    }

    public static void UpdateAllCommonBuffersRelate()
    {
        if (!commonBufferUpdateTask)
        {
            commonBufferUpdateTask = true;
            EditorApplication.delayCall += () => UpdateAllCommonBuffersRelate_Delayed();
        }
    }

    private static void UpdateAllCommonBuffersRelate_Delayed()
    {
        var commonBuffers = GetAllCommonBuffers();
        foreach (var commonBuffer in commonBuffers)
        {
            commonBuffer.broadcasts = TakeCommonBuffersRelate(commonBuffer);
            UdonSharpEditorUtility.CopyProxyToUdon(commonBuffer);
        }
        commonBufferUpdateTask = false;
    }

    public static void JoinAllBufferingBroadcasts(T23_CommonBuffer commonBuffer)
    {
        var broadcasts = GetAllBroadcastGlobals(); ;
        foreach (var broadcast in broadcasts)
        {
            var commonBufferField = broadcast.GetProgramVariable("commonBuffer") as T23_CommonBuffer;
            var bufferTypeField = broadcast.GetProgramVariable("bufferType") as int?;
            if (commonBufferField == null && bufferTypeField != 0)
            {
                broadcast.commonBuffer = commonBuffer;
                UdonSharpEditorUtility.CopyProxyToUdon(broadcast);
            }
        }
        commonBuffer.broadcasts = TakeCommonBuffersRelate(commonBuffer);
        UdonSharpEditorUtility.CopyProxyToUdon(commonBuffer);
    }

    public static T23_CommonBuffer GetAutoJoinCommonBuffer(T23_BroadcastGlobal broadcast)
    {
        var commonBuffers = GetAllCommonBuffers();
        foreach (var commonBuffer in commonBuffers)
        {
            if (commonBuffer.autoJoin)
            {
                broadcast.commonBuffer = commonBuffer;
                return commonBuffer;
            }
        }
        return null;
    }

    public static void PropertyBoxField(SerializedObject serializedObject, string constFieldName, string propertyBoxFieldName, string switchFieldName, Action edit = null)
    {
        EditorGUILayout.BeginHorizontal();
        var switchField = serializedObject.FindProperty(switchFieldName);
        if (switchField.boolValue)
        {
            var propertyBoxField = serializedObject.FindProperty(propertyBoxFieldName);
            propertyBoxField.objectReferenceValue = EditorGUILayout.ObjectField(ToUnityFieldName(constFieldName), propertyBoxField.objectReferenceValue, typeof(T23_PropertyBox), true);
        }
        else
        {
            if (edit == null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(constFieldName));
            }
            else
            {
                edit();
            }
        }
        //EditorGUILayout.BeginHorizontal();
        //GUILayout.Space(EditorGUIUtility.currentViewWidth - 150);
        string buttonLabel = switchField.boolValue ? "to Constant" : "to PropertyBox";
        var buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 10;
        buttonStyle.stretchWidth = false;
        Color oldBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button(buttonLabel, buttonStyle))
        {
            switchField.boolValue = !switchField.boolValue;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
        GUI.backgroundColor = oldBackgroundColor;
        EditorGUILayout.EndHorizontal();
        if (switchField.boolValue)
        {
            var propertyBoxField = serializedObject.FindProperty(propertyBoxFieldName);
            if (propertyBoxField.objectReferenceValue)
            {
                var propertyBox = (T23_PropertyBox)propertyBoxField.objectReferenceValue;
                var constField = serializedObject.FindProperty(constFieldName);
                bool unsuitable = false;
                if (constField.propertyType != SerializedPropertyType.String)
                {
                    if (propertyBox.valueType == 0 && constField.propertyType != SerializedPropertyType.Boolean) { unsuitable = true; }
                    if (propertyBox.valueType == 1 && constField.propertyType != SerializedPropertyType.Integer) { unsuitable = true; }
                    if (propertyBox.valueType == 2 && constField.propertyType != SerializedPropertyType.Float) { unsuitable = true; }
                    if (propertyBox.valueType == 3 && constField.propertyType != SerializedPropertyType.Vector3) { unsuitable = true; }
                    if (propertyBox.valueType == 4 && constField.propertyType != SerializedPropertyType.String) { unsuitable = true; }
                }
                if (unsuitable)
                {
                    EditorGUILayout.HelpBox("PropertyBox の ValueType が不適合です", MessageType.Error);
                }
            }
        }
    }

    public static string ToUnityFieldName(string before)
    {
        var _array = before.ToCharArray();
        _array[0] = char.ToUpper(_array[0]);
        var _ins = new List<int>();
        for (int i = _array.Length - 1; i > 0; i--)
        {
            if (char.IsUpper(_array[i])) { _ins.Add(i); }
        }
        var _list = new List<char>(_array);
        for (int i = 0; i < _ins.Count; i++)
        {
            _list.Insert(_ins[i], ' ');
        }
        return new string(_list.ToArray());
    }
}
#endif
