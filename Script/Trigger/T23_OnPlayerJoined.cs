
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

    public bool excludeLocal = true;

    public bool excludePostJoinng = true;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

    [HideInInspector]
    public VRCPlayerApi triggeredPlayer = Networking.LocalPlayer;
    [HideInInspector]
    public bool playerTrigger = true;

    private int frameCount = 100;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_OnPlayerJoined))]
    internal class T23_OnPlayerJoinedEditor : Editor
    {
        T23_OnPlayerJoined body;
        T23_Master master;

        SerializedProperty prop;

        void OnEnable()
        {
            body = target as T23_OnPlayerJoined;

            master = T23_Master.GetMaster(body, body.groupID, 1, true, body.title);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            if (!T23_EditorUtility.GuideJoinMaster(master, body, body.groupID, 1))
            {
                return;
            }

            serializedObject.Update();

            T23_EditorUtility.ShowTitle("Trigger");

            if (master)
            {
                GUILayout.Box("[#" + body.groupID.ToString() + "] " + body.title, T23_EditorUtility.HeadlineStyle());
            }
            else
            {
                body.groupID = EditorGUILayout.IntField("Group ID", body.groupID);
            }

            prop = serializedObject.FindProperty("excludeLocal");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("excludePostJoinng");
            EditorGUILayout.PropertyField(prop);

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
    }

    void Update()
    {
        if (frameCount > 0)
        {
            frameCount--;
        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (player == Networking.LocalPlayer)
        {
            frameCount = 5;
            if (excludeLocal) { return; }
        }
        if (excludePostJoinng && frameCount > 0) { return; }

        AnyPlayerTrigger(player);
    }

    private void AnyPlayerTrigger(VRCPlayerApi player)
    {
        triggeredPlayer = player;
        if (broadcastLocal)
        {
            broadcastLocal.AnyPlayerTrigger(player);
        }
        else if (broadcastGlobal)
        {
            broadcastGlobal.Trigger();
        }
    }
}
