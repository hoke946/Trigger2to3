﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.Components;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
#endif

public class T23_ReturnObjectPoolAll : UdonSharpBehaviour
{
    public int groupID;
    public int priority;
    public string title;
    public const bool isAction = true;

    [SerializeField]
    private VRCObjectPool objectPool;

    private bool executing = false;
    private bool executed = false;
    private float waitTimer;

    [SerializeField, Range(0, 1)]
    private float randomAvg;

    private float randomMin = 0;
    private float randomMax = 0;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_ReturnObjectPoolAll))]
    internal class T23_ReturnObjectPoolAllEditor : Editor
    {
        T23_ReturnObjectPoolAll body;
        T23_Master master;

        private ReorderableList recieverReorderableList;

        void OnEnable()
        {
            body = target as T23_ReturnObjectPoolAll;

            master = T23_Master.GetMaster(body, body.groupID, 2, true, body.title);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            if (master == null)
            {
                T23_EditorUtility.GuideJoinMaster(body, body.groupID, 2);
            }

            serializedObject.Update();

            T23_EditorUtility.ShowTitle("Action");

            if (master)
            {
                GUILayout.Box("[#" + body.groupID.ToString() + "] " + body.title, new GUIStyle() { fontSize = 14, alignment = TextAnchor.MiddleCenter });
                T23_EditorUtility.ShowSwapButton(master, body.title);
                body.priority = master.actionTitles.IndexOf(body.title);
            }
            else
            {
                body.groupID = EditorGUILayout.IntField("Group ID", body.groupID);
                body.priority = EditorGUILayout.IntField("Priority", body.priority);
            }

            body.objectPool = (VRCObjectPool)EditorGUILayout.ObjectField("Object Pool", body.objectPool, typeof(VRCObjectPool), true);
            EditorGUILayout.HelpBox("無条件でObject PoolのOwnershipを取得します。", MessageType.Info);

            body.randomAvg = EditorGUILayout.Slider("Random Avg", body.randomAvg, 0, 1);

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

    void Update()
    {
        if (executing)
        {
            bool failure = false;
            if (!executed)
            {
                if (Networking.IsOwner(objectPool.gameObject))
                {
                    Execute();
                    executed = true;
                }
                else
                {
                    failure = true;
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
        if (!objectPool || !RandomJudgement())
        {
            Finish();
            return;
        }

        Networking.SetOwner(Networking.LocalPlayer, objectPool.gameObject);
        executing = true;
        this.enabled = true;
        executed = false;
        waitTimer = 0;
    }

    private void Execute()
    {
        foreach(GameObject obj in objectPool.Pool)
        {
            objectPool.Return(obj);
        }

        Finish();
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
