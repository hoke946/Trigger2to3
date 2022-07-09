
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Core.Config.Interfaces;
using VRC.Udon.Common;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
#endif

public class T23_InputGrab : UdonSharpBehaviour
{
    public int groupID;
    public string title;
    public const bool isTrigger = false;

    public bool inputValue = true;

    public int hand;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_InputGrab))]
    internal class T23_InputGrabEditor : Editor
    {
        T23_InputGrab body;
        T23_Master master;

        SerializedProperty prop;

        enum InputValue
        {
            Down = 1,
            Up = 0
        }

        enum HandType
        {
            Any = 0,
            Right = 1,
            Left = 2
        }

        void OnEnable()
        {
            body = target as T23_InputGrab;

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

            prop = serializedObject.FindProperty("inputValue");
            prop.boolValue = (InputValue)EditorGUILayout.EnumPopup("Value", (InputValue)System.Convert.ToInt32(body.inputValue)) == InputValue.Down;
            prop.intValue = (int)(HandType)EditorGUILayout.EnumPopup("Hand", (HandType)System.Convert.ToInt32(body.hand));

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

    public override void InputGrab(bool value, UdonInputEventArgs args)
    {
        if (value == inputValue)
        {
            if (hand == 0 || (hand == 1 && args.handType == HandType.RIGHT) || (hand == 2 && args.handType == HandType.LEFT))
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
