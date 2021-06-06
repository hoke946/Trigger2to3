#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UdonSharp;

[CustomEditor(typeof(T23_Master))]
internal class T23_MasterEditor : Editor
{
    private ReorderableList broadcastReorderableList;
    private ReorderableList triggerReorderableList;
    private ReorderableList actionReorderableList;

    private UdonSharpProgramAsset setBroadcast;
    private UdonSharpProgramAsset addTrigger;
    private UdonSharpProgramAsset addAction;

    T23_Master master;

    void OnEnable()
    {
        master = target as T23_Master;
        master.SetupGroup();
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        master.CheckComponents();

        serializedObject.Update();

        T23_EditorUtility.ShowTitle("Master");

        GUILayout.Box("Group #" + master.groupID.ToString(), new GUIStyle() { fontSize = 20, alignment = TextAnchor.MiddleCenter });

        GUILayout.Space(10);

        SerializedProperty broadcastProp = serializedObject.FindProperty("broadcastTitles");
        if (broadcastReorderableList == null)
        {
            broadcastReorderableList = new ReorderableList(serializedObject, broadcastProp);
            broadcastReorderableList.draggable = true;
            broadcastReorderableList.onCanAddCallback += list => false;
            broadcastReorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Broadcast");
            broadcastReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(rect, master.broadcastTitles[index]);
            };
            broadcastReorderableList.onChangedCallback = ChangeBroadcast;
        }
        broadcastReorderableList.DoLayoutList();

        EditorGUILayout.BeginHorizontal();
        setBroadcast = (UdonSharpProgramAsset)EditorGUILayout.ObjectField("New Broadcast", setBroadcast, typeof(UdonSharpProgramAsset), false);
        if (GUILayout.Button("Set"))
        {
            EditorApplication.delayCall += () => SetBroadcast();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        SerializedProperty triggerProp = serializedObject.FindProperty("triggerTitles");
        if (triggerReorderableList == null)
        {
            triggerReorderableList = new ReorderableList(serializedObject, triggerProp);
            triggerReorderableList.draggable = true;
            triggerReorderableList.onCanAddCallback += list => false;
            triggerReorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Trigger");
            triggerReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(rect, master.triggerTitles[index]);
            };
            triggerReorderableList.onChangedCallback = ChangeTrigger;
        }
        triggerReorderableList.DoLayoutList();

        EditorGUILayout.BeginHorizontal();
        addTrigger = (UdonSharpProgramAsset)EditorGUILayout.ObjectField("New Trigger", addTrigger, typeof(UdonSharpProgramAsset), false);
        if (GUILayout.Button("Add"))
        {
            AddTrigger();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        SerializedProperty actionProp = serializedObject.FindProperty("actionTitles");
        if (actionReorderableList == null)
        {
            actionReorderableList = new ReorderableList(serializedObject, actionProp);
            actionReorderableList.draggable = true;
            actionReorderableList.onCanAddCallback += list => false;
            actionReorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Action");
            actionReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(rect, master.actionTitles[index]);
            };
            actionReorderableList.onChangedCallback = ChangeAction;
        }
        actionReorderableList.DoLayoutList();

        EditorGUILayout.BeginHorizontal();
        addAction = (UdonSharpProgramAsset)EditorGUILayout.ObjectField("New Action", addAction, typeof(UdonSharpProgramAsset), false);
        if (GUILayout.Button("Add"))
        {
            AddAction();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.HelpBox("現在、Manual Sync は未対応。 Synchronize Method は Continuous に設定されます。", MessageType.Info);
        /*
        EditorGUI.BeginDisabledGroup(master.hasObjectSync);
        master.reliable = EditorGUILayout.Toggle("SyncMethod Manual", master.reliable);
        EditorGUI.EndDisabledGroup();
        if (master.hasObjectSync)
        {
            EditorGUILayout.HelpBox("VRC_ObjectSync が存在するため、Synchronize Method は Continuous に設定されています。", MessageType.Info);
        }
        */

        serializedObject.ApplyModifiedProperties();

    }

    private void ChangeBroadcast(ReorderableList list)
    {
        EditorApplication.delayCall += () => master.ChangeBroadcast();
    }

    private void SetBroadcast()
    {
        if (setBroadcast)
        {
            if (setBroadcast.GetClass().GetField("isBroadcast") != null)
            {
                master.SetBroadcast(setBroadcast);
                setBroadcast = null;
            }
        }
    }

    private void ChangeTrigger(ReorderableList list)
    {
        EditorApplication.delayCall += () => master.ChangeTrigger();
    }

    private void AddTrigger()
    {
        if (addTrigger)
        {
            if (addTrigger.GetClass().GetField("isTrigger") != null)
            {
                master.AddTrigger(addTrigger);
                addTrigger = null;
            }
        }
    }

    private void ChangeAction(ReorderableList list)
    {
        EditorApplication.delayCall += () => master.ChangeAction();
    }

    private void AddAction()
    {
        if (addAction)
        {
            if (addAction.GetClass().GetField("isAction") != null)
            {
                master.AddAction(addAction);
                addAction = null;
            }
        }
    }
}
#endif
