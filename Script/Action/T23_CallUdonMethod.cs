
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
#endif

public class T23_CallUdonMethod : UdonSharpBehaviour
{
    public int groupID;
    public int priority;
    public string title;
    public const bool isAction = true;

    [SerializeField]
    private UdonBehaviour udonBehaviour;
    
    [SerializeField]
    private string method;

    [SerializeField]
    private int ownershipControl;

    private bool executing = false;
    private bool executed;
    private float waitTimer;

    [SerializeField, Range(0, 1)]
    private float randomAvg;

    private float randomMin = 0;
    private float randomMax = 0;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_CallUdonMethod))]
    internal class T23_CallUdonMethodEditor : Editor
    {
        T23_CallUdonMethod body;
        T23_Master master;

        public enum OwnershipControl
        {
            None = 0,
            SendOwner = 1,
            TakeOwnership = 2
        }

        private ReorderableList recieverReorderableList;

        void OnEnable()
        {
            body = target as T23_CallUdonMethod;

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

            body.udonBehaviour = (UdonBehaviour)EditorGUILayout.ObjectField("UdonBehaviour", body.udonBehaviour, typeof(UdonBehaviour), true);
            body.method = EditorGUILayout.TextField("Method", body.method);

            body.ownershipControl = System.Convert.ToInt32(EditorGUILayout.EnumPopup("Ownership Control", (OwnershipControl)body.ownershipControl));
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
        ownershipControl = 0;
#endif

        this.enabled = false;
    }

    void Update()
    {
        if (executing)
        {
            bool failure = false;
            if (!executed)
            {
                if (Networking.IsOwner(udonBehaviour.gameObject))
                {
                    Execute();
                    executed = true;
                }
                else
                {
                    failure = true;
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

        if (ownershipControl == 2)
        {
            Networking.SetOwner(Networking.LocalPlayer, udonBehaviour.gameObject);
            executing = true;
            this.enabled = true;
            executed = false;
            waitTimer = 0;
        }
        else
        {
            Execute();
            Finish();
        }
    }

    private void Execute()
    {
        if (ownershipControl == 1)
        {
#if UNITY_EDITOR
            // local simulation
            udonBehaviour.SendCustomEvent(method);
#else
            udonBehaviour.SendCustomNetworkEvent(NetworkEventTarget.Owner, method);
#endif
        }
        else
        {
            udonBehaviour.SendCustomEvent(method);
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
