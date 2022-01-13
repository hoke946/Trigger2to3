
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UdonSharpEditor;
#endif

public class T23_BroadcastGlobal : UdonSharpBehaviour
{
    public int groupID;
    public string title;
    public const bool isBroadcast = true;

    [SerializeField]
    private NetworkEventTarget sendTarget;

    [SerializeField]
    [Tooltip("0:Always\n1:Master\n2:Owner")]
    private int useablePlayer;

    [SerializeField]
    [Tooltip("0:Unbuffered\n1:BufferOne\n2:Everytime")]
    private int bufferType;

    [SerializeField]
    private float delayInSeconds;

    public bool randomize;

    public T23_CommonBuffer commonBuffer;

    private UdonSharpBehaviour[] actions;
    private int[] priorities;

    [UdonSynced(UdonSyncMode.None)]
    private bool syncReady;

    [UdonSynced(UdonSyncMode.None)]
    private int bufferTimes;

    private bool synced = false;
    private bool synced2 = false;
    private bool fired = false;
    private float timer = 0;
    private int actionCount = 0;
    private int buffering_count = 0;
    private int missing_count = 0;

    [HideInInspector]
    public float randomTotal;

    [HideInInspector]
    public float randomValue = 0;

    [HideInInspector]
    [UdonSynced(UdonSyncMode.None)]
    public int seed;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_BroadcastGlobal))]
    internal class T23_BroadcastGlobalEditor : Editor
    {
        T23_BroadcastGlobal body;
        T23_Master master;

        SerializedProperty prop;

        public enum UsablePlayer
        {
            Always = 0,
            Master = 1,
            Owner = 2
        }

        public enum BufferType
        {
            Unbuffered = 0,
            BufferOne = 1,
            Everytime = 2
        }

        void OnEnable()
        {
            body = target as T23_BroadcastGlobal;
            
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
                master.randomize = body.randomize;
            }
            else
            {
                body.groupID = EditorGUILayout.IntField("Group ID", body.groupID);
            }

            if (body.groupID > 9 || body.groupID < 0)
            {
                EditorGUILayout.HelpBox("BroadcastGlobal は Group #0 ～ #9 の間でしか使用できません。", MessageType.Error);
            }

            prop = serializedObject.FindProperty("sendTarget");
            EditorGUILayout.PropertyField(prop);
            serializedObject.FindProperty("useablePlayer").intValue = (int)(UsablePlayer)EditorGUILayout.EnumPopup("Usable Player", (UsablePlayer)body.useablePlayer);
            serializedObject.FindProperty("bufferType").intValue = (int)(BufferType)EditorGUILayout.EnumPopup("Buffer Type", (BufferType)body.bufferType);
            prop = serializedObject.FindProperty("delayInSeconds");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("randomize");
            EditorGUILayout.PropertyField(prop);

            EditorGUI.BeginChangeCheck();
            prop = serializedObject.FindProperty("commonBuffer");
            EditorGUILayout.PropertyField(prop);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                T23_EditorUtility.UpdateAllCommonBuffersRelate();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    void Start()
    {
        if (commonBuffer)
        {
            commonBuffer.LinkBroadcast(this);
        }
        else
        {
            if (Networking.IsOwner(gameObject))
            {
                bufferTimes = 0;
                seed = Random.Range(0, 1000000000);
                syncReady = true;
                RequestSerialization();
            }
            else
            {
                SendCustomNetworkEvent(NetworkEventTarget.Owner, "RequestFirstSync" + groupID.ToString());
            }
        }
    }

    public void Trigger()
    {
        if (useablePlayer == 1 && !Networking.IsMaster) { return; }
        if (useablePlayer == 2 && !Networking.IsOwner(gameObject)) { return; }

        fired = true;
        timer = 0;
    }

    void Update()
    {
        if (synced) { synced2 = true; }

        if (!synced && syncReady)
        {
            if (!commonBuffer)
            {
                if (buffering_count < bufferTimes)
                {
                    if (!UnconditionalFire()) { return; }
                    buffering_count++;
                    return;
                }
                SetSynced();
            }
        }

        if (fired)
        {
            timer += Time.deltaTime;
            if (timer > delayInSeconds)
            {
                SendNetworkFire();
            }
        }
    }

    public void SetSynced()
    {
        synced = true;
    }

    private void SendNetworkFire()
    {
        if (actions == null)
        {
            fired = false;
            return;
        }

#if UNITY_EDITOR
        // local simulation
        Fire();
#else
        SendCustomNetworkEvent(NetworkEventTarget.Owner, "OwnerProcess" + groupID.ToString());
        SendCustomNetworkEvent(sendTarget, "RecieveNetworkFire" + groupID.ToString());
#endif

        fired = false;
        return;
    }

    public void Fire()
    {
        if (!synced2) { return; }   // 初期同期直後は待ちタスクが流れてくる場合があるので１フレーム待つ
        UnconditionalFire();
    }

    public bool UnconditionalFire()
    {
        if (actions == null)
        {
            missing_count++;
            if (missing_count > 3)
            {
                actions = new UdonSharpBehaviour[0];
            }
            return false;
        }

        actionCount++;
        if (randomize && randomTotal > 0)
        {
            Random.InitState(GetSeed() + actionCount);
            randomValue = Random.Range(0, Mathf.Max(1, randomTotal));
        }

        for (int i = 0; i < actions.Length; i++)
        {
            actions[i].SendCustomEvent("Action");
        }
        return true;
    }

    /*
    private T23_BroadcastGlobal GetCorrectBroadcast(int id)
    {
        var bgs = GetComponents<T23_BroadcastGlobal>();
        foreach (var bg in bgs)
        {
            if (bg.groupID == id)
            {
                return bg;
            }
        }
        return null;
    }
    */

    public void RecieveNetworkFire0()
    {
        if (groupID == 0) { RecieveNetworkFire(); }
    }

    public void RecieveNetworkFire1()
    {
        if (groupID == 1) { RecieveNetworkFire(); }
    }

    public void RecieveNetworkFire2()
    {
        if (groupID == 2) { RecieveNetworkFire(); }
    }

    public void RecieveNetworkFire3()
    {
        if (groupID == 3) { RecieveNetworkFire(); }
    }

    public void RecieveNetworkFire4()
    {
        if (groupID == 4) { RecieveNetworkFire(); }
    }

    public void RecieveNetworkFire5()
    {
        if (groupID == 5) { RecieveNetworkFire(); }
    }

    public void RecieveNetworkFire6()
    {
        if (groupID == 6) { RecieveNetworkFire(); }
    }

    public void RecieveNetworkFire7()
    {
        if (groupID == 7) { RecieveNetworkFire(); }
    }

    public void RecieveNetworkFire8()
    {
        if (groupID == 8) { RecieveNetworkFire(); }
    }

    public void RecieveNetworkFire9()
    {
        if (groupID == 9) { RecieveNetworkFire(); }
    }

    public void RecieveNetworkFire()
    {
        if (Networking.IsOwner(gameObject)) { return; }
        Fire();
    }

    public void OwnerProcess0()
    {
        if (groupID == 0) { OwnerProcess(); }
    }

    public void OwnerProcess1()
    {
        if (groupID == 1) { OwnerProcess(); }
    }

    public void OwnerProcess2()
    {
        if (groupID == 2) { OwnerProcess(); }
    }

    public void OwnerProcess3()
    {
        if (groupID == 3) { OwnerProcess(); }
    }

    public void OwnerProcess4()
    {
        if (groupID == 4) { OwnerProcess(); }
    }

    public void OwnerProcess5()
    {
        if (groupID == 5) { OwnerProcess(); }
    }

    public void OwnerProcess6()
    {
        if (groupID == 6) { OwnerProcess(); }
    }

    public void OwnerProcess7()
    {
        if (groupID == 7) { OwnerProcess(); }
    }

    public void OwnerProcess8()
    {
        if (groupID == 8) { OwnerProcess(); }
    }

    public void OwnerProcess9()
    {
        if (groupID == 9) { OwnerProcess(); }
    }

    public void OwnerProcess()
    {
        if (commonBuffer)
        {
            Networking.SetOwner(Networking.LocalPlayer, commonBuffer.gameObject);
            commonBuffer.EntryBuffer(this, bufferType);
        }
        else
        {
            if (bufferType == 1)
            {
                if (bufferTimes == 0)
                {
                    bufferTimes = 1;
                    RequestSerialization();
                }
            }
            else if (bufferType == 2)
            {
                bufferTimes++;
                RequestSerialization();
            }
        }
        Fire();
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

    public bool IsSyncReady()
    {
        return syncReady;
    }

    public int GetSeed()
    {
        if (commonBuffer)
        {
            return commonBuffer.GetSeed(this);
        }
        else
        {
            return seed;
        }
    }
}
