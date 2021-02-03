
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

    private UdonSharpBehaviour[] actions;

    [UdonSynced(UdonSyncMode.None)]
    private bool syncReady;

    [UdonSynced(UdonSyncMode.None)]
    private int bufferTimes;

    private bool synced = false;
    private bool fired = false;
    private float timer = 0;

    [HideInInspector]
    public float randomTotal;

    [HideInInspector]
    public float randomValue;

    void Start()
    {
        if (Networking.IsOwner(gameObject))
        {
            syncReady = true;
            bufferTimes = 0;
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
        if (!synced && syncReady)
        {
            for (int t = 0; t < bufferTimes; t++)
            {
                Fire();
            }
            synced = true;
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

        if (bufferType == 2 || (bufferType == 1 && bufferTimes == 0))
        {
            SendCustomNetworkEvent(NetworkEventTarget.Owner, "AddBufferTimes" + groupID.ToString());
        }
#endif

        fired = false;
        return;
    }

    private void Fire()
    {
        if (randomize && randomTotal > 0)
        {
            randomValue = Random.Range(0, Mathf.Max(1, randomTotal));
        }

        for (int i = 0; i < actions.Length; i++)
        {
            actions[i].SendCustomEvent("Action");
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

    public void AddBufferTimes0()
    {
        if (groupID != 0) { return; }
        bufferTimes++;
    }

    public void AddBufferTimes1()
    {
        if (groupID != 1) { return; }
        bufferTimes++;
    }

    public void AddBufferTimes2()
    {
        if (groupID != 2) { return; }
        bufferTimes++;
    }

    public void AddBufferTimes3()
    {
        if (groupID != 3) { return; }
        bufferTimes++;
    }

    public void AddBufferTimes4()
    {
        if (groupID != 4) { return; }
        bufferTimes++;
    }

    public void AddBufferTimes5()
    {
        if (groupID != 5) { return; }
        bufferTimes++;
    }

    public void AddBufferTimes6()
    {
        if (groupID != 6) { return; }
        bufferTimes++;
    }

    public void AddBufferTimes7()
    {
        if (groupID != 7) { return; }
        bufferTimes++;
    }

    public void AddBufferTimes8()
    {
        if (groupID != 8) { return; }
        bufferTimes++;
    }

    public void AddBufferTimes9()
    {
        if (groupID != 9) { return; }
        bufferTimes++;
    }

    public void AddActions(UdonSharpBehaviour actionTarget)
    {
        if (actions == null)
        {
            actions = new UdonSharpBehaviour[1];
            actions[0] = actionTarget;
        }
        else
        {
            actions = AddUdonSharpBehaviourArray(actions, actionTarget);
        }
    }

    private UdonSharpBehaviour[] AddUdonSharpBehaviourArray(UdonSharpBehaviour[] array, UdonSharpBehaviour value)
    {
        UdonSharpBehaviour[] new_array = new UdonSharpBehaviour[array.Length + 1];
        array.CopyTo(new_array, 0);
        new_array[new_array.Length - 1] = value;
        return new_array;
    }
}
