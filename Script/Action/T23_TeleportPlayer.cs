
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
#endif

public class T23_TeleportPlayer : UdonSharpBehaviour
{
    public int groupID;
    public int priority;
    public string title;
    public const bool isAction = true;

    public bool byValue;

    public Transform teleportLocation;

    public Vector3 teleportPosition;
    public T23_PropertyBox positionPropertyBox;
    public bool positionUsePropertyBox;

    public Vector3 teleportRotation;
    public T23_PropertyBox rotationPropertyBox;
    public bool rotationUsePropertyBox;

    public VRC_SceneDescriptor.SpawnOrientation teleportOrientation;

    public bool lerpOnRemote;

    [Range(0, 1)]
    public float randomAvg;

    private float randomMin = 0;
    private float randomMax = 0;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_TeleportPlayer))]
    internal class T23_TeleportPlayerEditor : Editor
    {
        T23_TeleportPlayer body;
        T23_Master master;

        SerializedProperty prop;

        void OnEnable()
        {
            body = target as T23_TeleportPlayer;

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

            prop = serializedObject.FindProperty("byValue");
            EditorGUILayout.PropertyField(prop);
            if (body.byValue)
            {
                T23_EditorUtility.PropertyBoxField(serializedObject, "teleportPosition", "positionPropertyBox", "positionUsePropertyBox");
                T23_EditorUtility.PropertyBoxField(serializedObject, "teleportRotation", "rotationPropertyBox", "rotationUsePropertyBox");
            }
            else
            {
                prop = serializedObject.FindProperty("teleportLocation");
                EditorGUILayout.PropertyField(prop);
            }
            prop = serializedObject.FindProperty("teleportOrientation");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("lerpOnRemote");
            EditorGUILayout.PropertyField(prop);
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

        if (byValue)
        {
            if (positionUsePropertyBox && positionPropertyBox)
            {
                teleportPosition = positionPropertyBox.value_v3;
            }
            if (rotationUsePropertyBox && rotationPropertyBox)
            {
                teleportRotation = rotationPropertyBox.value_v3;
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

        if (byValue)
        {
            Networking.LocalPlayer.TeleportTo(teleportPosition, Quaternion.Euler(teleportRotation), teleportOrientation, lerpOnRemote);
        }
        else
        {
            Networking.LocalPlayer.TeleportTo(teleportLocation.position, teleportLocation.rotation, teleportOrientation, lerpOnRemote);
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
}
