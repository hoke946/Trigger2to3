﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
#endif

public class T23_SetPlayerSpeed : UdonSharpBehaviour
{
    public int groupID;
    public int priority;
    public string title;
    public const bool isAction = true;

    [SerializeField]
    private float walkSpeed = 2;
    [SerializeField]
    private T23_PropertyBox propertyBox_walk;
    [SerializeField]
    private bool usePropertyBox_walk;

    [SerializeField]
    private float runSpeed = 4;
    [SerializeField]
    private T23_PropertyBox propertyBox_run;
    [SerializeField]
    private bool usePropertyBox_run;

    [SerializeField]
    private float strafeSpeed = 2;
    [SerializeField]
    private T23_PropertyBox propertyBox_strafe;
    [SerializeField]
    private bool usePropertyBox_strafe;

    [SerializeField, Range(0, 1)]
    private float randomAvg;

    private float randomMin = 0;
    private float randomMax = 0;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_SetPlayerSpeed))]
    internal class T23_SetPlayerSpeedEditor : Editor
    {
        T23_SetPlayerSpeed body;
        T23_Master master;

        SerializedProperty prop;

        void OnEnable()
        {
            body = target as T23_SetPlayerSpeed;

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

            T23_EditorUtility.PropertyBoxField(serializedObject, "walkSpeed", "propertyBox_walk", "usePropertyBox_walk");
            T23_EditorUtility.PropertyBoxField(serializedObject, "runSpeed", "propertyBox_run", "usePropertyBox_run");
            T23_EditorUtility.PropertyBoxField(serializedObject, "strafeSpeed", "propertyBox_strafe", "usePropertyBox_strafe");
            if (!master || master.randomize)
            {
                prop = serializedObject.FindProperty("randomAvg");
                EditorGUILayout.PropertyField(prop);
            }

            serializedObject.ApplyModifiedProperties();
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

        this.enabled = false;
    }

    public void Action()
    {
        if (!RandomJudgement())
        {
            return;
        }

        if (usePropertyBox_walk && propertyBox_walk)
        {
            walkSpeed = propertyBox_walk.value_f;
        }
        if (usePropertyBox_run && propertyBox_run)
        {
            runSpeed = propertyBox_run.value_f;
        }
        if (usePropertyBox_strafe && propertyBox_strafe)
        {
            strafeSpeed = propertyBox_strafe.value_f;
        }
        Networking.LocalPlayer.SetWalkSpeed(walkSpeed);
        Networking.LocalPlayer.SetRunSpeed(runSpeed);
        Networking.LocalPlayer.SetStrafeSpeed(strafeSpeed);
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
}
