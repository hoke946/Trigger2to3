
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
#endif

public class T23_OnPlayerJoined : UdonSharpBehaviour
{
    public int groupID;
    public string title;
    public const bool isTrigger = true;

    [SerializeField]
    private bool excludeLocal;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

    private bool onJoined = false;
    private bool firstFlame = true;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_OnPlayerJoined))]
    internal class T23_OnPlayerJoinedEditor : Editor
    {
        T23_OnPlayerJoined body;
        T23_Master master;

        void OnEnable()
        {
            body = target as T23_OnPlayerJoined;

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

            body.excludeLocal = EditorGUILayout.Toggle("Exclude Local", body.excludeLocal);

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

        onJoined = true;
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (excludeLocal && player == Networking.LocalPlayer) { return; }

        onJoined = true;
    }

    void Update()
    {
        if (firstFlame)
        {
            firstFlame = false;
            return;
        }

        if (onJoined)
        {
            Trigger();
            onJoined = false;
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
