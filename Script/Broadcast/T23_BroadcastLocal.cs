
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
#endif

public class T23_BroadcastLocal : UdonSharpBehaviour
{
    public int groupID;
    public string title;
    public const bool isBroadcast = true;

    [SerializeField]
    private float delayInSeconds;

    public bool randomize;

    private UdonSharpBehaviour[] actions;
    private int[] priorities;

    //private bool synced = false;
    private bool fired = false;
    private float timer = 0;
    private int actionIndex = 0;

    [HideInInspector]
    public VRCPlayerApi triggeredPlayer;

    [HideInInspector]
    public float randomTotal;

    [HideInInspector]
    public float randomValue;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_BroadcastLocal))]
    internal class T23_BroadcastLocalEditor : Editor
    {
        T23_BroadcastLocal body;
        T23_Master master;

        SerializedProperty prop;

        void OnEnable()
        {
            body = target as T23_BroadcastLocal;

            master = T23_Master.GetMaster(body, body.groupID, 0, true, body.title);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            if (!T23_EditorUtility.GuideJoinMaster(master, body, body.groupID, 0))
            {
                return;
            }

            serializedObject.Update();

            T23_EditorUtility.ShowTitle("Broadcast");

            if (master)
            {
                GUILayout.Box("[#" + body.groupID.ToString() + "] " + body.title, T23_EditorUtility.HeadlineStyle());
            }
            else
            {
                body.groupID = EditorGUILayout.IntField("Group ID", body.groupID);
            }

            prop = serializedObject.FindProperty("delayInSeconds");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("randomize");
            EditorGUILayout.PropertyField(prop);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    void Start()
    {
        this.enabled = false;
    }

    public void Trigger()
    {
        triggeredPlayer = Networking.LocalPlayer;
        Trigger_internal();
    }

    public void AnyPlayerTrigger(VRCPlayerApi player)
    {
        triggeredPlayer = player;
        Trigger_internal();
    }

    public void Trigger_internal()
    {
        if (delayInSeconds > 0)
        {
            fired = true;
            this.enabled = true;
            timer = 0;
        }
        else
        {
            Fire(false);
        }
    }

    void Update()
    {
        if (fired)
        {
            timer += Time.deltaTime;
            if (timer > delayInSeconds)
            {
                Fire(false);
                fired = false;
                this.enabled = false;
            }
        }
    }

    private void Fire(bool local)
    {
        if (actions == null) { return; }

        if (randomize && randomTotal > 0)
        {
            randomValue = Random.Range(0, Mathf.Max(1, randomTotal));
        }

        actionIndex = 0;
        if (actionIndex < actions.Length)
        {
            actions[actionIndex].SendCustomEvent("Action");
        }
    }

    public void NextAction()
    {
        actionIndex++;
        if (actionIndex < actions.Length)
        {
            actions[actionIndex].SendCustomEvent("Action");
        }
    }

    public void AddActions(UdonSharpBehaviour actionTarget, int priority)
    {
        if (actions == null)
        {
            actions = new UdonSharpBehaviour[1];
            actions[0] = actionTarget;
            priorities = new int[1];
            priorities[0] = priority;
        }
        else
        {
            int i = 0;
            while (i < actions.Length)
            {
                if (priorities[i] > priority)
                {
                    break;
                }
                i++;
            }
            actions = AddUdonSharpBehaviourArray(actions, actionTarget, i);
            priorities = AddIntArray(priorities, priority, i);
        }
    }

    private UdonSharpBehaviour[] AddUdonSharpBehaviourArray(UdonSharpBehaviour[] array, UdonSharpBehaviour value, int index)
    {
        UdonSharpBehaviour[] new_array = new UdonSharpBehaviour[array.Length + 1];
        array.CopyTo(new_array, 0);
        for (int i = 0; i < index; i++)
        {
            new_array[i] = array[i];
        }
        new_array[index] = value;
        for (int i = index + 1; i < new_array.Length; i++)
        {
            new_array[i] = array[i - 1];
        }
        return new_array;
    }

    private int[] AddIntArray(int[] array, int value, int index)
    {
        int[] new_array = new int[array.Length + 1];
        array.CopyTo(new_array, 0);
        for (int i = 0; i < index; i++)
        {
            new_array[i] = array[i];
        }
        new_array[index] = value;
        for (int i = index + 1; i < new_array.Length; i++)
        {
            new_array[i] = array[i - 1];
        }
        return new_array;
    }
}
