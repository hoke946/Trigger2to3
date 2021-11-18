
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using System.Collections.Generic;
using UdonSharpEditor;
#endif

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class T23_CommonBuffer : UdonSharpBehaviour
{
    public T23_BroadcastGlobal[] broadcasts;

    [UdonSynced(UdonSyncMode.None)]
    private bool syncReady;

    [UdonSynced(UdonSyncMode.None)]
    private int[] broadcastIdx = new int[0];

    private bool synced = false;
    private int buffering_count = 0;
    
    [UdonSynced(UdonSyncMode.None)]
    private int seed;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_CommonBuffer))]
    internal class T23_CommonBufferEditor : Editor
    {
        T23_CommonBuffer body;

        SerializedProperty prop;

        void OnEnable()
        {
            body = target as T23_CommonBuffer;
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            if (!UdonSharpEditorUtility.IsProxyBehaviour(body))
            {
                UdonSharpGUI.DrawConvertToUdonBehaviourButton(body);
                return;
            }

            serializedObject.Update();

            T23_EditorUtility.ShowTitle("Option");
            GUILayout.Box("CommonBuffer", T23_EditorUtility.HeadlineStyle());

            UdonSharpProgramAsset programAsset = UdonSharpEditorUtility.GetUdonSharpProgramAsset((UdonSharpBehaviour)target);
            UdonSharpGUI.DrawCompileErrorTextArea(programAsset);

            if (GUILayout.Button("Set Broadcasts"))
            {
                body.broadcasts = T23_EditorUtility.TakeCommonBuffersRelate(body);
            }

            prop = serializedObject.FindProperty("broadcasts");
            EditorGUILayout.PropertyField(prop);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    void Start()
    {
        if (Networking.IsOwner(gameObject))
        {
            seed = Random.Range(0, 1000000000);
            syncReady = true;
            RequestSerialization();
        }
    }

    void Update()
    {
        if (!synced && syncReady)
        {
            if (broadcasts == null) { return; }

            for (int i = 0; i < broadcastIdx.Length; i++)
            {
                if (broadcastIdx[i] >= broadcasts.Length)
                {
                    return;
                }
            }
            if (buffering_count < broadcastIdx.Length)
            {
                var broadcast = broadcasts[broadcastIdx[buffering_count]];
                if (!broadcast.gameObject.activeSelf)
                {
                    broadcast.gameObject.SetActive(true);
                    return;
                }
                broadcast.UnconditionalFire();

                buffering_count++;
                return;
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
            bool contains = false;
            for (int i = 0; i < broadcasts.Length; i++)
            {
                if (broadcasts[i] == broadcast)
                {
                    contains = true;
                    break;
                }
            }
            if (!contains)
            {
                broadcasts = AddBroadcastGlobalArray(broadcasts, broadcast);
            }
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
                    int exist = FindValueIntArray(broadcastIdx, bidx, 0);
                    if (exist != -1)
                    {
                        broadcastIdx = RemoveIntArray(broadcastIdx, exist);
                    }
                }

                broadcastIdx = AddIntArray(broadcastIdx, bidx);
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

    private int[] AddIntArray(int[] array, int value)
    {
        int[] new_array = new int[array.Length + 1];
        array.CopyTo(new_array, 0);
        new_array[new_array.Length - 1] = value;
        return new_array;
    }

    public int FindValueIntArray(int[] array, int value, int start)
    {
        for (int i = start; i < array.Length; i++)
        {
            if (array[i] == value)
            {
                return i;
            }
        }
        return -1;
    }

    private int[] RemoveIntArray(int[] array, int index)
    {
        int[] new_array = new int[array.Length - 1];
        for (int i = 0; i < index; i++)
        {
            new_array[i] = array[i];
        }
        for (int i = index; i < new_array.Length; i++)
        {
            new_array[i] = array[i + 1];
        }
        return new_array;
    }

    private T23_BroadcastGlobal[] AddBroadcastGlobalArray(T23_BroadcastGlobal[] array, T23_BroadcastGlobal value)
    {
        T23_BroadcastGlobal[] new_array = new T23_BroadcastGlobal[array.Length + 1];
        array.CopyTo(new_array, 0);
        new_array[new_array.Length - 1] = value;
        return new_array;
    }

    public int GetSeed(T23_BroadcastGlobal broadcast)
    {
        for (int i = 0; i < broadcasts.Length; i++)
        {
            if (broadcasts[i] == broadcast)
            {
                return seed + i;
            }
        }
        return seed;
    }
}
