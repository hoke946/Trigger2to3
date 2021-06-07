﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

//[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class T23_CommonBuffer : UdonSharpBehaviour
{
    private T23_BroadcastGlobal[] broadcasts;

    [UdonSynced(UdonSyncMode.None)]
    private bool syncReady;

    [UdonSynced(UdonSyncMode.None)]
    private string broadcastIdxChars;

    private bool synced = false;

    void Start()
    {
        if (Networking.IsOwner(gameObject))
        {
            syncReady = true;
            RequestSerialization();
        }
    }

    void Update()
    {
        if (!synced && syncReady)
        {
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
        }
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
        RequestSerialization();
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
}
