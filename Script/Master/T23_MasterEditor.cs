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

        GUILayout.Box("Group #" + master.groupID.ToString(), T23_EditorUtility.HeadlineStyle(true));

        GUILayout.Space(10);

        SerializedProperty broadcastProp = serializedObject.FindProperty("broadcastTitles");
        if (broadcastReorderableList == null)
        {
            broadcastReorderableList = new ReorderableList(serializedObject, broadcastProp);
            broadcastReorderableList.draggable = true;
            broadcastReorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Broadcast");
            broadcastReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(rect, master.broadcastTitles[index]);
            };
            broadcastReorderableList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
            {
                var menu = new GenericMenu();
                var assetList = T23_EditorUtility.GetProgramAssets("Broadcast");
                foreach (var pair in assetList)
                {
                    menu.AddItem(new GUIContent(pair.Key), false, () =>
                    {
                        master.SetBroadcast(pair.Value);
                    });
                }
                menu.DropDown(buttonRect);
            };
            broadcastReorderableList.onChangedCallback = ChangeBroadcast;
        }
        broadcastReorderableList.DoLayoutList();

        /*
        EditorGUILayout.BeginHorizontal();
        setBroadcast = (UdonSharpProgramAsset)EditorGUILayout.ObjectField("New Broadcast", setBroadcast, typeof(UdonSharpProgramAsset), false);
        if (GUILayout.Button("Set"))
        {
            EditorApplication.delayCall += () => SetBroadcast();
        }
        EditorGUILayout.EndHorizontal();
        */

        GUILayout.Space(10);

        SerializedProperty triggerProp = serializedObject.FindProperty("triggerTitles");
        if (triggerReorderableList == null)
        {
            triggerReorderableList = new ReorderableList(serializedObject, triggerProp);
            triggerReorderableList.draggable = true;
            triggerReorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Trigger");
            triggerReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(rect, master.triggerTitles[index]);
            };
            triggerReorderableList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
            {
                var menu = new GenericMenu();
                var assetList = T23_EditorUtility.GetProgramAssets("Trigger");
                foreach (var pair in assetList)
                {
                    menu.AddItem(new GUIContent(pair.Key), false, () =>
                    {
                        master.AddTrigger(pair.Value);
                    });
                }
                menu.DropDown(buttonRect);
            };
            triggerReorderableList.onChangedCallback = ChangeTrigger;
        }
        triggerReorderableList.DoLayoutList();

        /*
        EditorGUILayout.BeginHorizontal();
        addTrigger = (UdonSharpProgramAsset)EditorGUILayout.ObjectField("New Trigger", addTrigger, typeof(UdonSharpProgramAsset), false);
        if (GUILayout.Button("Add"))
        {
            AddTrigger();
        }
        EditorGUILayout.EndHorizontal();
        */

        GUILayout.Space(10);

        SerializedProperty actionProp = serializedObject.FindProperty("actionTitles");
        if (actionReorderableList == null)
        {
            actionReorderableList = new ReorderableList(serializedObject, actionProp);
            actionReorderableList.draggable = true;
            actionReorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Action");
            actionReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(rect, master.actionTitles[index]);
            };
            actionReorderableList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
            {
                var menu = new GenericMenu();
                var assetList = T23_EditorUtility.GetProgramAssets("Action");
                foreach (var pair in assetList)
                {
                    menu.AddItem(new GUIContent(pair.Key), false, () =>
                    {
                        master.AddAction(pair.Value);
                    });
                }
                menu.DropDown(buttonRect);
            };
            actionReorderableList.onChangedCallback = ChangeAction;
        }
        actionReorderableList.DoLayoutList();

        /*
        EditorGUILayout.BeginHorizontal();
        addAction = (UdonSharpProgramAsset)EditorGUILayout.ObjectField("New Action", addAction, typeof(UdonSharpProgramAsset), false);
        if (GUILayout.Button("Add"))
        {
            AddAction();
        }
        EditorGUILayout.EndHorizontal();
        */

        GUILayout.Space(10);

        EditorGUI.BeginChangeCheck();
        EditorGUI.BeginDisabledGroup(master.hasObjectSync);
        serializedObject.FindProperty("reliable").boolValue = EditorGUILayout.Toggle("SyncMethod Manual", master.reliable);
        EditorGUI.EndDisabledGroup();
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            master.OrderComponents();
            serializedObject.Update();
        }
        if (master.hasObjectSync)
        {
            EditorGUILayout.HelpBox("VRC_ObjectSync が存在するため、Synchronize Method は Continuous に設定されています。", MessageType.Info);
        }

        if (master.shouldMoveComponents)
        {
            Object prefab = PrefabUtility.GetCorrespondingObjectFromSource(master.gameObject);
            string msg = "";
            if (prefab == null)
            {
                if (master.MoveComponents())
                {
                    master.shouldMoveComponents = false;
                }
                else
                {
                    msg = "コンポーネントが正しく整列できません。";
                }
            }
            else
            {
                msg = "コンポーネントがPrefab内にある場合、整列できません。";
            }
            if (msg != "")
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(msg, MessageType.Warning);
                if (GUILayout.Button("Clear"))
                {
                    master.shouldMoveComponents = false;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

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
