﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
#endif

public class T23_AddAngularVelocity : UdonSharpBehaviour
{
    public int groupID;
    public int priority;
    public string title;
    public const bool isAction = true;

    [SerializeField]
    private Rigidbody[] recievers;

    [SerializeField]
    private Vector3 angularVelocity;

    [SerializeField]
    private bool useWorldSpace;

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
    [CustomEditor(typeof(T23_AddAngularVelocity))]
    internal class T23_AddAngularVelocityEditor : Editor
    {
        T23_AddAngularVelocity body;
        T23_Master master;

        private ReorderableList recieverReorderableList;

        void OnEnable()
        {
            body = target as T23_AddAngularVelocity;

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
                    body.recievers[index] = (Rigidbody)EditorGUI.ObjectField(rect, body.recievers[index], typeof(Rigidbody), true);
                };
            }
            recieverReorderableList.DoLayoutList();

            body.angularVelocity = EditorGUILayout.Vector3Field("Angular Velocity", body.angularVelocity);
            body.useWorldSpace = EditorGUILayout.Toggle("Use World Space", body.useWorldSpace);

            body.takeOwnership = EditorGUILayout.Toggle("Take Ownership", body.takeOwnership);
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
                            waitTimer = 0;
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

    private void Execute(Rigidbody target)
    {
        if (useWorldSpace)
        {
            target.angularVelocity += angularVelocity;
        }
        else
        {
            target.angularVelocity += target.rotation * angularVelocity;
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
