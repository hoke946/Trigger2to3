
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
#endif

public class T23_OnExitCollider : UdonSharpBehaviour
{
    public int groupID;
    public string title;
    public const bool isTrigger = true;

    [SerializeField]
    private bool triggerIndividuals = true;

    [SerializeField]
    private LayerMask layers = 0;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

    private Collision enterCollider;
    private VRCPlayerApi enterPlayer;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_OnExitCollider))]
    internal class T23_OnExitColliderEditor : Editor
    {
        T23_OnExitCollider body;
        T23_Master master;

        SerializedProperty prop;

        void OnEnable()
        {
            body = target as T23_OnExitCollider;

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

    private void OnCollisionEnter(Collision other)
    {
        if ((layers.value & 1 << other.gameObject.layer) == 0) { return; }

        if (!triggerIndividuals)
        {
            if (enterCollider != null) { return; }
            if (enterPlayer != null) { return; }
        }

        enterCollider = other;
    }

    private void OnCollisionExit(Collision other)
    {
        if (enterCollider == null || enterCollider != null && enterCollider != other) { return; }

        Trigger();

        enterCollider = null;
    }

    public override void OnPlayerCollisionEnter(VRCPlayerApi player)
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
            if (enterCollider != null) { return; }
            if (enterPlayer != null) { return; }
        }

        enterPlayer = player;
    }

    public override void OnPlayerCollisionExit(VRCPlayerApi player)
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
