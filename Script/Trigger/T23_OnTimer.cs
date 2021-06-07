
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
#endif

public class T23_OnTimer : UdonSharpBehaviour
{
    public int groupID;
    public string title;
    public const bool isTrigger = true;

    [SerializeField]
    private bool repeat;

    [SerializeField]
    private bool resetOnEnable;

    [SerializeField]
    private float lowPeriodTime;

    [SerializeField]
    private float highPeriodTime;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

    private float timer;
    private float nextPeriodTime;
    private bool finished;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_OnTimer))]
    internal class T23_OnTimerEditor : Editor
    {
        T23_OnTimer body;
        T23_Master master;

        void OnEnable()
        {
            body = target as T23_OnTimer;

            master = T23_Master.GetMaster(body, body.groupID, 1, true, body.title);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            if (master == null)
            {
                T23_EditorUtility.GuideJoinMaster(body, body.groupID, 1);
            }

            serializedObject.Update();

            T23_EditorUtility.ShowTitle("Trigger");

            if (master)
            {
                GUILayout.Box("[#" + body.groupID.ToString() + "] " + body.title, new GUIStyle() { fontSize = 14, alignment = TextAnchor.MiddleCenter });
            }
            else
            {
                body.groupID = EditorGUILayout.IntField("Group ID", body.groupID);
            }

            body.repeat = EditorGUILayout.Toggle("Repeat", body.repeat);
            body.resetOnEnable = EditorGUILayout.Toggle("Reset OnEnable", body.resetOnEnable);
            body.lowPeriodTime = EditorGUILayout.FloatField("Low Period Time", body.lowPeriodTime);
            body.highPeriodTime = EditorGUILayout.FloatField("High Period Time", body.highPeriodTime);

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

        if (!broadcastLocal)
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
        }

        ResetTime();
    }

    void Update()
    {
        if (finished) { return; }

        timer += Time.deltaTime;

        if (timer > nextPeriodTime)
        {
            Trigger();

            if (repeat)
            {
                ResetTime();
            }
            else
            {
                finished = true;
            }
        }
    }

    private void ResetTime()
    {
        finished = false;
        timer = 0;
        if (highPeriodTime > lowPeriodTime)
        {
            nextPeriodTime = Random.Range(lowPeriodTime, highPeriodTime);
        }
        else
        {
            nextPeriodTime = lowPeriodTime;
        }
    }

    private void OnEnable()
    {
        if (resetOnEnable)
        {
            ResetTime();
        }
    }

    private void Trigger()
    {
        if (broadcastLocal)
        {
            broadcastLocal.Trigger();
        }
        else if (broadcastGlobal)
        {
            broadcastGlobal.Trigger();
        }
    }
}
