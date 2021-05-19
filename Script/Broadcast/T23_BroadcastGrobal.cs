
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class T23_BroadcastGrobal : UdonSharpBehaviour
{
    public int groupID;

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

    private bool synced = false;
    private bool fired = false;
    private float timer = 0;
    private int actionCount = 0;
    private int cbOwnerTrigger = 0;
    private int actionIndex = 0;

    [HideInInspector]
    public float randomTotal;

    [HideInInspector]
    public float randomValue = 0;

    [HideInInspector]
    [UdonSynced(UdonSyncMode.None)]
    public int seed;

    void Start()
    {
        if (Networking.IsOwner(gameObject))
        {
            bufferTimes = 0;
            seed = Random.Range(0, 1000000000);
            syncReady = true;
        }

        if (commonBuffer)
        {
            commonBuffer.LinkBroadcast(this);
        }
    }

    public void Trigger()
    {
        if (useablePlayer == 1 && !Networking.IsMaster) { return; }
        if (useablePlayer == 2 && !Networking.IsOwner(gameObject)) { return; }

        fired = true;
        this.enabled = true;
        timer = 0;
    }

    void Update()
    {
        if (!synced && syncReady)
        {
            if (!commonBuffer)
            {
                for (int t = 0; t < bufferTimes; t++)
                {
                    Fire();
                }
            }
            synced = true;
            this.enabled = false;
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
            if (cbOwnerTrigger == 0)
            {
                this.enabled = false;
            }
        }
    }

    private void SendNetworkFire()
    {
        if (actions == null)
        {
            fired = false;
            this.enabled = false;
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
        this.enabled = false;
        return;
    }

    public void Fire()
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

    public void RecieveNetworkFire0()
    {
        if (groupID != 0) { return; }
        Fire();
    }

    public void RecieveNetworkFire1()
    {
        if (groupID != 1) { return; }
        Fire();
    }

    public void RecieveNetworkFire2()
    {
        if (groupID != 2) { return; }
        Fire();
    }

    public void RecieveNetworkFire3()
    {
        if (groupID != 3) { return; }
        Fire();
    }

    public void RecieveNetworkFire4()
    {
        if (groupID != 4) { return; }
        Fire();
    }

    public void RecieveNetworkFire5()
    {
        if (groupID != 5) { return; }
        Fire();
    }

    public void RecieveNetworkFire6()
    {
        if (groupID != 6) { return; }
        Fire();
    }

    public void RecieveNetworkFire7()
    {
        if (groupID != 7) { return; }
        Fire();
    }

    public void RecieveNetworkFire8()
    {
        if (groupID != 8) { return; }
        Fire();
    }

    public void RecieveNetworkFire9()
    {
        if (groupID != 9) { return; }
        Fire();
    }

    public void OwnerProcess0()
    {
        if (groupID != 0) { return; }
        OwnerProcess();
    }

    public void OwnerProcess1()
    {
        if (groupID != 1) { return; }
        OwnerProcess();
    }

    public void OwnerProcess2()
    {
        if (groupID != 2) { return; }
        OwnerProcess();
    }

    public void OwnerProcess3()
    {
        if (groupID != 3) { return; }
        OwnerProcess();
    }

    public void OwnerProcess4()
    {
        if (groupID != 4) { return; }
        OwnerProcess();
    }

    public void OwnerProcess5()
    {
        if (groupID != 5) { return; }
        OwnerProcess();
    }

    public void OwnerProcess6()
    {
        if (groupID != 6) { return; }
        OwnerProcess();
    }

    public void OwnerProcess7()
    {
        if (groupID != 7) { return; }
        OwnerProcess();
    }

    public void OwnerProcess8()
    {
        if (groupID != 8) { return; }
        OwnerProcess();
    }

    public void OwnerProcess9()
    {
        if (groupID != 9) { return; }
        OwnerProcess();
    }

    public void OwnerProcess()
    {
        if (commonBuffer)
        {
            Networking.SetOwner(Networking.LocalPlayer, commonBuffer.gameObject);
            cbOwnerTrigger++;
            this.enabled = true;
        }
        else
        {
            if (bufferType == 1)
            {
                bufferTimes = 1;
            }
            else if (bufferType == 2)
            {
                bufferTimes++;
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
}
