
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
//[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class T23_CommonBuffer : UdonSharpBehaviour
{
    private T23_BroadcastGlobal[] broadcasts;

    [UdonSynced(UdonSyncMode.None)]
    private bool syncReady;

    [UdonSynced(UdonSyncMode.None)]
    private string broadcastIdxChars;
    private string broadcastIdxChars_local;
    private bool sync_broadcastIdxChars = false;

    private bool synced = false;
    private int firstSyncRequests = 0;

    void Start()
    {
        if (Networking.IsOwner(gameObject))
        {
            syncReady = true;
            RequestSerialization();
        }
        else
        {
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(RequestFirstSync));
        }
    }

    public void RequestFirstSync()
    {
        firstSyncRequests++;
        ActivitySwitching();
    }

    public void ResponceFirstSynced()
    {
        firstSyncRequests--;
        ActivitySwitching();
    }

    void Update()
    {
        if (!synced && syncReady)
        {
            if (broadcasts == null) { return; }

            int[] broadcastIdx = CharsToIntArray(broadcastIdxChars);

            foreach (var broadcast in broadcasts)
            {
                if (!broadcast.IsSyncReady())
                {
                    return;
                }
            }
            for (int i = 0; i < broadcastIdx.Length; i++)
            {
                if (broadcastIdx[i] >= broadcasts.Length)
                {
                    return;
                }
            }
            for (int i = 0; i < broadcastIdx.Length; i++)
            {
                broadcasts[broadcastIdx[i]].UnconditionalFire();
            }
            foreach (var broadcast in broadcasts)
            {
                broadcast.SetSynced();
            }
            synced = true;
            if (!Networking.IsOwner(gameObject))
            {
                broadcastIdxChars_local = broadcastIdxChars;
                SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(ResponceFirstSynced));
            }
        }

        if (sync_broadcastIdxChars)
        {
            if (broadcastIdxChars_local != broadcastIdxChars)
            {
                sync_broadcastIdxChars = false;
                broadcastIdxChars_local = broadcastIdxChars;
            }
        }

        ActivitySwitching();
    }

    public void LinkBroadcast(T23_BroadcastGlobal broadcast)
    {
        if (broadcasts == null)
        {
            broadcasts = new T23_BroadcastGlobal[1];
            broadcasts[0] = broadcast;
        }
        else
        {
            broadcasts = AddBroadcastGlobalArray(broadcasts, broadcast);
        }
        if (synced)
        {
            broadcast.SetSynced();
        }
    }

    public void EntryBuffer(T23_BroadcastGlobal broadcast, int bufferType)
    {
        if (bufferType == 0) { return; }

        for (int bidx = 0; bidx < broadcasts.Length; bidx++)
        {
            if (broadcast == broadcasts[bidx])
            {
                if (bufferType == 1)
                {
                    broadcastIdxChars = broadcastIdxChars.Replace(((char)bidx).ToString(), "");
                }

                broadcastIdxChars += ((char)bidx).ToString();
            }
        }
        SendSyncAll();
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Display));
    }

    private int[] CharsToIntArray(string charsStr)
    {
        char[] chars = charsStr.ToCharArray();
        int[] res = new int[chars.Length];
        for (int i = 0; i < chars.Length; i++)
        {
            res[i] = chars[i];
        }
        return res;
    }

    private string IntArrayToChars(int[] array)
    {
        string res = "";
        for (int i = 0; i < array.Length; i++)
        {
            res += ((char)i).ToString();
        }
        return res;
    }

    private T23_BroadcastGlobal[] AddBroadcastGlobalArray(T23_BroadcastGlobal[] array, T23_BroadcastGlobal value)
    {
        T23_BroadcastGlobal[] new_array = new T23_BroadcastGlobal[array.Length + 1];
        array.CopyTo(new_array, 0);
        new_array[new_array.Length - 1] = value;
        return new_array;
    }

    private void ActivitySwitching()
    {
        if (!synced || firstSyncRequests > 0 || sync_broadcastIdxChars)
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
        broadcastIdxChars_local = broadcastIdxChars;
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RecieveSyncAll));
    }

    public void RecieveSyncAll()
    {
        this.enabled = true;
        sync_broadcastIdxChars = true;
    }
}
