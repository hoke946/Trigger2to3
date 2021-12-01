
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
#endif

public class T23_TeleportObject : UdonSharpBehaviour
{
    public int groupID;
    public int priority;
    public string title;
    public const bool isAction = true;

    [SerializeField]
    private GameObject[] recievers;

    [SerializeField]
    private bool byValue;

    [SerializeField]
    private Transform teleportLocation;

    [SerializeField]
    private bool local;

    [SerializeField]
    private Vector3 teleportPosition;
    [SerializeField]
    private T23_PropertyBox positionPropertyBox;
    [SerializeField]
    private bool positionUsePropertyBox;

    [SerializeField]
    private Vector3 teleportRotation;
    [SerializeField]
    private T23_PropertyBox rotationPropertyBox;
    [SerializeField]
    private bool rotationUsePropertyBox;

    [SerializeField]
    private bool removeVelocity;

    [SerializeField]
    private bool takeOwnership;

    [SerializeField, Range(0, 1)]
    private float randomAvg;

    private float randomMin = 0;
    private float randomMax = 0;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_TeleportObject))]
    internal class T23_TeleportObjectEditor : Editor
    {
        T23_TeleportObject body;
        T23_Master master;

        SerializedProperty prop;

        private ReorderableList recieverReorderableList;

        void OnEnable()
        {
            body = target as T23_TeleportObject;

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
                    body.recievers[index] = (GameObject)EditorGUI.ObjectField(rect, body.recievers[index], typeof(GameObject), true);
                };
            }
            recieverReorderableList.DoLayoutList();

            prop = serializedObject.FindProperty("byValue");
            EditorGUILayout.PropertyField(prop);
            if (body.byValue)
            {
                prop = serializedObject.FindProperty("local");
                EditorGUILayout.PropertyField(prop);
                T23_EditorUtility.PropertyBoxField(serializedObject, "teleportPosition", "positionPropertyBox", "positionUsePropertyBox");
                T23_EditorUtility.PropertyBoxField(serializedObject, "teleportRotation", "rotationPropertyBox", "rotationUsePropertyBox");
            }
            else
            {
                prop = serializedObject.FindProperty("teleportLocation");
                EditorGUILayout.PropertyField(prop);
            }
            prop = serializedObject.FindProperty("removeVelocity");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("takeOwnership");
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

    public void Action()
    {
        if (!RandomJudgement())
        {
            return;
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
        for (int i = 0; i < recievers.Length; i++)
        {
            if (recievers[i])
            {
                if (takeOwnership)
                {
                    Networking.SetOwner(Networking.LocalPlayer, recievers[i]);
                }
                Execute(recievers[i]);
            }
        }
    }

    private void Execute(GameObject target)
    {
        if (byValue)
        {
            if (local)
            {
                target.transform.localPosition = teleportPosition;
                target.transform.localRotation = Quaternion.Euler(teleportRotation);
            }
            else
            {
                target.transform.position = teleportPosition;
                target.transform.rotation = Quaternion.Euler(teleportRotation);
            }
        }
        else
        {
            target.transform.position = teleportLocation.position;
            target.transform.rotation = teleportLocation.rotation;
        }

        if (removeVelocity)
        {
            var rigidBody = target.GetComponent<Rigidbody>();
            if (rigidBody)
            {
                rigidBody.velocity = Vector3.zero;
                rigidBody.angularVelocity = Vector3.zero;
            }
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
