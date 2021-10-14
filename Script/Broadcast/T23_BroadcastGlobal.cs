
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
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

    [SerializeField]
    private T23_CommonBuffer commonBuffer;

    private UdonSharpBehaviour[] actions;
    private int[] priorities;

    [UdonSynced(UdonSyncMode.None)]
    private bool syncReady;

    [UdonSynced(UdonSyncMode.None)]
    private int bufferTimes;
    private int bufferTimes_local;
    private bool sync_bufferTimes = false;

    private bool synced = false;
    private bool synced2 = false;
    private bool fired = false;
    private float timer = 0;
    private int actionCount = 0;
    private int cbOwnerTrigger = 0;
    private int firstSyncRequests = 0;
    private int actionIndex = 0;

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
            prop = serializedObject.FindProperty("commonBuffer");
            EditorGUILayout.PropertyField(prop);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    void Start()
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

        if (commonBuffer)
        {
            commonBuffer.LinkBroadcast(this);
        }
    }

    public void RequestFirstSync0()
    {
        //var bg = GetCorrectBroadcast(0);
        //if (bg) { bg.RequestFirstSync(); }
        if (groupID == 0) { RequestFirstSync(); }
    }

    public void RequestFirstSync1()
    {
        if (groupID == 1) { RequestFirstSync(); }
    }

    public void RequestFirstSync2()
    {
        if (groupID == 2) { RequestFirstSync(); }
    }

    public void RequestFirstSync3()
    {
        if (groupID == 3) { RequestFirstSync(); }
    }

    public void RequestFirstSync4()
    {
        if (groupID == 4) { RequestFirstSync(); }
    }

    public void RequestFirstSync5()
    {
        if (groupID == 5) { RequestFirstSync(); }
    }

    public void RequestFirstSync6()
    {
        if (groupID == 6) { RequestFirstSync(); }
    }

    public void RequestFirstSync7()
    {
        if (groupID == 7) { RequestFirstSync(); }
    }

    public void RequestFirstSync8()
    {
        if (groupID == 8) { RequestFirstSync(); }
    }

    public void RequestFirstSync9()
    {
        if (groupID == 9) { RequestFirstSync(); }
    }

    public void RequestFirstSync()
    {
        firstSyncRequests++;
        ActivitySwitching();
    }

    public void Trigger()
    {
        if (useablePlayer == 1 && !Networking.IsMaster) { return; }
        if (useablePlayer == 2 && !Networking.IsOwner(gameObject)) { return; }

        fired = true;
        ActivitySwitching();
        timer = 0;
    }

    void Update()
    {
        if (synced) { synced2 = true; }

        if (!synced && syncReady)
        {
            if (!commonBuffer)
            {
                for (int t = 0; t < bufferTimes; t++)
                {
                    UnconditionalFire();
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

        if (cbOwnerTrigger > 0)
        {
            if (Networking.IsOwner(commonBuffer.gameObject))
            {
                if (randomize && randomTotal > 0)
                {
                    randomValue = Random.Range(0, Mathf.Max(1, randomTotal));
                }
                commonBuffer.EntryBuffer(this, bufferType);
            }
            cbOwnerTrigger--;
        }

        if (sync_bufferTimes)
        {
            if (bufferTimes_local != bufferTimes)
            {
                sync_bufferTimes = false;
                bufferTimes_local = bufferTimes;
            }
        }

        ActivitySwitching();
    }

    public void SetSynced()
    {
        synced = true;
        bufferTimes_local = bufferTimes;
        if (!Networking.IsOwner(gameObject))
        {
            SendCustomNetworkEvent(NetworkEventTarget.Owner, "ResponceFirstSynced" + groupID.ToString());
        }
    }

    public void ResponceFirstSynced0()
    {
        if (groupID == 0) { ResponceFirstSynced(); }
    }

    public void ResponceFirstSynced1()
    {
        if (groupID == 1) { ResponceFirstSynced(); }
    }

    public void ResponceFirstSynced2()
    {
        if (groupID == 2) { ResponceFirstSynced(); }
    }

    public void ResponceFirstSynced3()
    {
        if (groupID == 3) { ResponceFirstSynced(); }
    }

    public void ResponceFirstSynced4()
    {
        if (groupID == 4) { ResponceFirstSynced(); }
    }

    public void ResponceFirstSynced5()
    {
        if (groupID == 5) { ResponceFirstSynced(); }
    }

    public void ResponceFirstSynced6()
    {
        if (groupID == 6) { ResponceFirstSynced(); }
    }

    public void ResponceFirstSynced7()
    {
        if (groupID == 7) { ResponceFirstSynced(); }
    }

    public void ResponceFirstSynced8()
    {
        if (groupID == 8) { ResponceFirstSynced(); }
    }

    public void ResponceFirstSynced9()
    {
        if (groupID == 9) { ResponceFirstSynced(); }
    }

    public void ResponceFirstSynced()
    {
        firstSyncRequests--;
        ActivitySwitching();
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
        SendCustomNetworkEvent(sendTarget, "RecieveNetworkFire" + groupID.ToString());

        SendCustomNetworkEvent(NetworkEventTarget.Owner, "OwnerProcess" + groupID.ToString());
#endif

        fired = false;
        return;
    }

    public void Fire()
    {
        if (!synced2) { return; }   // 初期同期直後は待ちタスクが流れてくる場合があるので１フレーム待つ
        UnconditionalFire();
    }

    public void UnconditionalFire()
    {
        actionCount++;
        if (randomize && randomTotal > 0)
        {
            Random.InitState(seed + actionCount);
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
        if (groupID == 0) { Fire(); }
    }

    public void RecieveNetworkFire1()
    {
        if (groupID == 1) { Fire(); }
    }

    public void RecieveNetworkFire2()
    {
        if (groupID == 2) { Fire(); }
    }

    public void RecieveNetworkFire3()
    {
        if (groupID == 3) { Fire(); }
    }

    public void RecieveNetworkFire4()
    {
        if (groupID == 4) { Fire(); }
    }

    public void RecieveNetworkFire5()
    {
        if (groupID == 5) { Fire(); }
    }

    public void RecieveNetworkFire6()
    {
        if (groupID == 6) { Fire(); }
    }

    public void RecieveNetworkFire7()
    {
        if (groupID == 7) { Fire(); }
    }

    public void RecieveNetworkFire8()
    {
        if (groupID == 8) { Fire(); }
    }

    public void RecieveNetworkFire9()
    {
        if (groupID == 9) { Fire(); }
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
            cbOwnerTrigger++;
            ActivitySwitching();
        }
        else
        {
            if (bufferType == 1)
            {
                if (bufferTimes == 0)
                {
                    bufferTimes = 1;
                    SendSyncAll();
                }
            }
            else if (bufferType == 2)
            {
                bufferTimes++;
                SendSyncAll();
            }
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

    public bool IsSyncReady()
    {
        return syncReady;
    }

    private void ActivitySwitching()
    {
        if (!synced2 || fired || cbOwnerTrigger > 0 || firstSyncRequests > 0 || sync_bufferTimes)
        {
            this.enabled = true;
        }
        else
        {
            this.enabled = false;
        }
    }

    private void SendSyncAll()
    {
        RequestSerialization();
        bufferTimes_local = bufferTimes;
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RecieveSyncAll));
    }

    public void RecieveSyncAll0()
    {
        if (groupID == 0) { RecieveSyncAll(); }
    }

    public void RecieveSyncAll1()
    {
        if (groupID == 1) { RecieveSyncAll(); }
    }

    public void RecieveSyncAll2()
    {
        if (groupID == 2) { RecieveSyncAll(); }
    }

    public void RecieveSyncAll3()
    {
        if (groupID == 3) { RecieveSyncAll(); }
    }

    public void RecieveSyncAll4()
    {
        if (groupID == 4) { RecieveSyncAll(); }
    }

    public void RecieveSyncAll5()
    {
        if (groupID == 5) { RecieveSyncAll(); }
    }

    public void RecieveSyncAll6()
    {
        if (groupID == 6) { RecieveSyncAll(); }
    }

    public void RecieveSyncAll7()
    {
        if (groupID == 7) { RecieveSyncAll(); }
    }

    public void RecieveSyncAll8()
    {
        if (groupID == 8) { RecieveSyncAll(); }
    }

    public void RecieveSyncAll9()
    {
        if (groupID == 9) { RecieveSyncAll(); }
    }

    public void RecieveSyncAll()
    {
        this.enabled = true;
        sync_bufferTimes = true;
    }
}
