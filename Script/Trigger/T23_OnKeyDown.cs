
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
#endif

public class T23_OnKeyDown : UdonSharpBehaviour
{
    public int groupID;
    public string title;
    public const bool isTrigger = false;

    public KeyCode key;

    public string keyFree;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_OnKeyDown))]
    internal class T23_OnKeyDownEditor : Editor
    {
        T23_OnKeyDown body;
        T23_Master master;

        SerializedProperty prop;

        void OnEnable()
        {
            body = target as T23_OnKeyDown;

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

            prop = serializedObject.FindProperty("key");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("keyFree");
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
        if (keyFree == "")
        {
            if (Input.GetKeyDown(key))
            {
                Trigger();
            }
        }
        else
        {
            if (Input.GetKeyDown(keyFree))
            {
                Trigger();
            }
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
