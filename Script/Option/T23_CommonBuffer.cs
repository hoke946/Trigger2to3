
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class T23_CommonBuffer : UdonSharpBehaviour
{
    private T23_BroadcastGrobal[] broadcasts;

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
        }
    }

    void Update()
    {
        if (!synced && syncReady)
        {
            int[] broadcastIdx = CharsToIntArray(broadcastIdxChars);

            for (int i = 0; i < broadcastIdx.Length; i++)
            {
                broadcasts[broadcastIdx[i]].Fire();
            }
            synced = true;
        }
    }

    public void LinkBroadcast(T23_BroadcastGrobal broadcast)
    {
        if (broadcasts == null)
        {
            broadcasts = new T23_BroadcastGrobal[1];
            broadcasts[0] = broadcast;
        }
        else
        {
            broadcasts = AddBroadcastGrobalArray(broadcasts, broadcast);
        }
    }

    public void EntryBuffer(T23_BroadcastGrobal broadcast, int bufferType)
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

    private T23_BroadcastGrobal[] AddBroadcastGrobalArray(T23_BroadcastGrobal[] array, T23_BroadcastGrobal value)
    {
        T23_BroadcastGrobal[] new_array = new T23_BroadcastGrobal[array.Length + 1];
        array.CopyTo(new_array, 0);
        new_array[new_array.Length - 1] = value;
        return new_array;
    }
}
