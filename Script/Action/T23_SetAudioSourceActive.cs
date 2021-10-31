﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
#endif

public class T23_SetAudioSourceActive : UdonSharpBehaviour
{
    public int groupID;
    public int priority;
    public string title;
    public const bool isAction = true;

    [SerializeField]
    private AudioSource[] recievers;

    [SerializeField]
    private bool toggle;

    [SerializeField]
    [Tooltip("if not toggle")]
    private bool operation = true;

    [SerializeField]
    private bool takeOwnership;

    private bool executing = false;
    private bool[] executed;
    private float waitTimer;

    [SerializeField, Range(0, 1)]
    private float randomAvg;

    private float randomMin = 0;
    private float randomMax = 0;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;
    
#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_SetAudioSourceActive))]
    internal class T23_SetAudioSourceActiveEditor : Editor
    {
        T23_SetAudioSourceActive body;
        T23_Master master;

        SerializedProperty prop;

        private ReorderableList recieverReorderableList;

        public enum ToggleOperation
        {
            True,
            False,
            Toggle
        }
        private ToggleOperation operation;

        void OnEnable()
        {
            body = target as T23_SetAudioSourceActive;

            master = T23_Master.GetMaster(body, body.groupID, 2, true, body.title);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            if (!T23_EditorUtility.GuideJoinMaster(master, body, body.groupID, 2))
            {
                return;
            }

            serializedObject.Update();

            T23_EditorUtility.ShowTitle("Action");

            UdonSharpProgramAsset programAsset = UdonSharpEditorUtility.GetUdonSharpProgramAsset((UdonSharpBehaviour)target);
            UdonSharpGUI.DrawCompileErrorTextArea(programAsset);

            if (master)
            {
                GUILayout.Box("[#" + body.groupID.ToString() + "] " + body.title, T23_EditorUtility.HeadlineStyle());
                T23_EditorUtility.ShowSwapButton(master, body.title);
                body.priority = master.actionTitles.IndexOf(body.title);
            }
            else
            {
                body.groupID = EditorGUILayout.IntField("Group ID", body.groupID);
                body.priority = EditorGUILayout.IntField("Priority", body.priority);
            }

            SerializedProperty recieverProp = serializedObject.FindProperty("recievers");
            if (recieverReorderableList == null)
            {
                recieverReorderableList = new ReorderableList(serializedObject, recieverProp);
                recieverReorderableList.draggable = true;
                recieverReorderableList.displayAdd = true;
                recieverReorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Recievers");
                recieverReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    body.recievers[index] = (AudioSource)EditorGUI.ObjectField(rect, body.recievers[index], typeof(AudioSource), true);
                };
            }
            recieverReorderableList.DoLayoutList();

            EditorGUI.BeginChangeCheck();
            operation = (ToggleOperation)EditorGUILayout.EnumPopup("Operation", GetOperation());
            if (EditorGUI.EndChangeCheck())
            {
                SelectOperation();
            }

            prop = serializedObject.FindProperty("takeOwnership");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("randomAvg");
            EditorGUILayout.PropertyField(prop);

            serializedObject.ApplyModifiedProperties();
        }

        private void SelectOperation()
        {
            switch (operation)
            {
                case ToggleOperation.True:
                    prop = serializedObject.FindProperty("toggle");
                    prop.boolValue = false;
                    prop = serializedObject.FindProperty("operation");
                    prop.boolValue = true;
                    break;
                case ToggleOperation.False:
                    prop = serializedObject.FindProperty("toggle");
                    prop.boolValue = false;
                    prop = serializedObject.FindProperty("operation");
                    prop.boolValue = false;
                    break;
                case ToggleOperation.Toggle:
                    prop = serializedObject.FindProperty("toggle");
                    prop.boolValue = true;
                    break;
            }
        }

        private ToggleOperation GetOperation()
        {
            if (body.toggle)
            {
                return ToggleOperation.Toggle;
            }
            else
            {
                if (body.operation)
                {
                    return ToggleOperation.True;
                }
                else
                {
                    return ToggleOperation.False;
                }
            }
        }
    }
#endif

    void Start()
    {
        T23_BroadcastLocal[] broadcastLocals = GetComponents<T23_BroadcastLocal>();
        for (int i = 0; i < broadcastLocals.Length; i++)
        {
            if (broadcastLocals[i].groupID == groupID)
            {
                broadcastLocal = broadcastLocals[i];
                break;
            }
        }

        if (broadcastLocal)
        {
            broadcastLocal.AddActions(this, priority);

            if (broadcastLocal.randomize)
            {
                randomMin = broadcastLocal.randomTotal;
                broadcastLocal.randomTotal += randomAvg;
                randomMax = broadcastLocal.randomTotal;
            }
        }
        else
        {
            T23_BroadcastGlobal[] broadcastGlobals = GetComponents<T23_BroadcastGlobal>();
            for (int i = 0; i < broadcastGlobals.Length; i++)
            {
                if (broadcastGlobals[i].groupID == groupID)
                {
                    broadcastGlobal = broadcastGlobals[i];
                    break;
                }
            }

            if (broadcastGlobal)
            {
                broadcastGlobal.AddActions(this, priority);

                if (broadcastGlobal.randomize)
                {
                    randomMin = broadcastGlobal.randomTotal;
                    broadcastGlobal.randomTotal += randomAvg;
                    randomMax = broadcastGlobal.randomTotal;
                }
            }
        }

#if UNITY_EDITOR
        // local simulation
        takeOwnership = false;
#endif

        this.enabled = false;
    }

    void Update()
    {
        if (executing)
        {
            bool failure = false;
            for (int i = 0; i < recievers.Length; i++)
            {
                if (recievers[i])
                {
                    if (!executed[i])
                    {
                        if (Networking.IsOwner(recievers[i].gameObject))
                        {
                            Execute(recievers[i]);
                            executed[i] = true;
                        }
                        else
                        {
                            failure = true;
                        }
                    }
                }
            }

            if (!failure)
            {
                executing = false;
                this.enabled = false;
                Finish();
            }

            waitTimer += Time.deltaTime;
            if (waitTimer > 5)
            {
                executing = false;
                this.enabled = false;
                Finish();
            }
        }
    }

    public void Action()
    {
        if (!RandomJudgement())
        {
            Finish();
            return;
        }

        for (int i = 0; i < recievers.Length; i++)
        {
            if (recievers[i])
            {
                if (takeOwnership)
                {
                    Networking.SetOwner(Networking.LocalPlayer, recievers[i].gameObject);
                    executing = true;
                    this.enabled = true;
                    executed = new bool[recievers.Length];
                    waitTimer = 0;
                }
                else
                {
                    Execute(recievers[i]);
                }
            }
        }

        if (!takeOwnership)
        {
            Finish();
        }
    }

    private void Execute(AudioSource target)
    {
        if (toggle)
        {
            if (target)
            {
                target.enabled = !target.enabled;
            }
        }
        else
        {
            if (target)
            {
                target.enabled = operation;
            }
        }
    }

    private bool RandomJudgement()
    {
        if (broadcastLocal)
        {
            if (!broadcastLocal.randomize || (broadcastLocal.randomValue >= randomMin && broadcastLocal.randomValue < randomMax))
            {
                return true;
            }
        }
        else if (broadcastGlobal)
        {
            if (!broadcastGlobal.randomize || (broadcastGlobal.randomValue >= randomMin && broadcastGlobal.randomValue < randomMax))
            {
                return true;
            }
        }

        return false;
    }

    private void Finish()
    {
        if (broadcastLocal)
        {
            broadcastLocal.NextAction();
        }
        else if (broadcastGlobal)
        {
            broadcastGlobal.NextAction();
        }
    }
}