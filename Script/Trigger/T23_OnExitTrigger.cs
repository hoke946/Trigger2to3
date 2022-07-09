
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
#endif

public class T23_OnExitTrigger : UdonSharpBehaviour
{
    public int groupID;
    public string title;
    public const bool isTrigger = true;

    public bool triggerIndividuals = true;

    public LayerMask layers = 0;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

    private Collider enterCollider;
    private VRCPlayerApi enterPlayer;

    [HideInInspector]
    public VRCPlayerApi triggeredPlayer = Networking.LocalPlayer;
    [HideInInspector]
    public bool playerTrigger = true;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_OnExitTrigger))]
    internal class T23_OnExitTriggerEditor : Editor
    {
        T23_OnExitTrigger body;
        T23_Master master;

        SerializedProperty prop;

        void OnEnable()
        {
            body = target as T23_OnExitTrigger;

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

            prop = serializedObject.FindProperty("triggerIndividuals");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("layers");
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

    private void OnTriggerEnter(Collider other)
    {
        if ((layers.value & 1 << other.gameObject.layer) == 0) { return; }

        if (!triggerIndividuals)
        {
            if (enterCollider) { return; }
            if (enterPlayer != null) { return; }
        }

        enterCollider = other;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!enterCollider || enterCollider && enterCollider != other) { return; }

        Trigger();

        enterCollider = null;
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player == Networking.LocalPlayer)
        {
            if ((layers.value & 1 << LayerMask.NameToLayer("PlayerLocal")) == 0) { return; }
        }
        else
        {
            if ((layers.value & 1 << LayerMask.NameToLayer("Player")) == 0) { return; }
        }

        if (!triggerIndividuals)
        {
            if (enterCollider) { return; }
            if (enterPlayer != null) { return; }
        }

        enterPlayer = player;
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (enterPlayer == null || enterPlayer != null && enterPlayer != player) { return; }

        AnyPlayerTrigger(player);

        enterPlayer = null;
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
