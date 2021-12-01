
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
using UdonSharpEditor;
using System.Collections.Generic;
#endif

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class T23_AudioBank : UdonSharpBehaviour
{
    [SerializeField]
    private AudioSource source;

    [SerializeField]
    private int playbackOrder;

    [SerializeField]
    private int playbackStyle;

    [SerializeField]
    private bool repeat;

    [SerializeField, Range(-3, 3)]
    private float minPitchRange = 1;

    [SerializeField, Range(-3, 3)]
    private float maxPitchRange = 1;

    [SerializeField]
    private GameObject onPlay;
    [SerializeField]
    private string onPlayCustomName;

    [SerializeField]
    private GameObject onStop;
    [SerializeField]
    private string onStopCustomName;

    [SerializeField]
    private GameObject onChange;
    [SerializeField]
    private string onChangeCustomName;

    [SerializeField]
    private AudioClip[] clips;

    [HideInInspector]
    public int[] order;

    [HideInInspector]
    public int currentIndex = 0;
    private bool isPlaying = false;
    private int seed = 0;
    private int actionCount = 0;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_AudioBank))]
    internal class T23_AudioBankEditor : Editor
    {
        T23_AudioBank body;

        SerializedProperty prop;

        private ReorderableList recieverReorderableList;

        public enum Order
        {
            InOrder = 0,
            InOrderReversing = 1,
            Shuffle = 2,
            Random = 3
        }

        public enum Style
        {
            OneShot = 0,
            Continuous = 1
        }

        void OnEnable()
        {
            body = target as T23_AudioBank;
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

            GUILayout.Box("AudioBank", T23_EditorUtility.HeadlineStyle());

            prop = serializedObject.FindProperty("source");
            EditorGUILayout.PropertyField(prop);
            serializedObject.FindProperty("playbackOrder").intValue = (int)(Order)EditorGUILayout.EnumPopup("Playback Order", (Order)body.playbackOrder);
            serializedObject.FindProperty("playbackStyle").intValue = (int)(Style)EditorGUILayout.EnumPopup("Playback Style", (Style)body.playbackStyle);
            prop = serializedObject.FindProperty("repeat");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("minPitchRange");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("maxPitchRange");
            EditorGUILayout.PropertyField(prop);

            EditorGUILayout.BeginHorizontal();
            prop = serializedObject.FindProperty("onPlay");
            EditorGUILayout.PropertyField(prop);
            List<string> onPlayCustomNameList = new List<string>();
            if (body.onPlay)
            {
                onPlayCustomNameList = GetCustomNameList(body.onPlay);
            }
            if (onPlayCustomNameList.Count > 0)
            {
                var index = EditorGUILayout.Popup(onPlayCustomNameList.IndexOf(body.onPlayCustomName), onPlayCustomNameList.ToArray());
                serializedObject.FindProperty("onPlayCustomName").stringValue = index >= 0 ? onPlayCustomNameList[index] : "";
            }
            else
            {
                prop = serializedObject.FindProperty("onPlayCustomName");
                EditorGUILayout.PropertyField(prop);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            prop = serializedObject.FindProperty("onStop");
            EditorGUILayout.PropertyField(prop);
            List<string> onStopCustomNameList = new List<string>();
            if (body.onStop)
            {
                onStopCustomNameList = GetCustomNameList(body.onStop);
            }
            if (onStopCustomNameList.Count > 0)
            {
                var index = EditorGUILayout.Popup(onStopCustomNameList.IndexOf(body.onStopCustomName), onStopCustomNameList.ToArray());
                serializedObject.FindProperty("onStopCustomName").stringValue = index >= 0 ? onStopCustomNameList[index] : "";
            }
            else
            {
                prop = serializedObject.FindProperty("onStopCustomName");
                EditorGUILayout.PropertyField(prop);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            prop = serializedObject.FindProperty("onChange");
            EditorGUILayout.PropertyField(prop);
            List<string> onChangeCustomNameList = new List<string>();
            if (body.onChange)
            {
                onChangeCustomNameList = GetCustomNameList(body.onChange);
            }
            if (onChangeCustomNameList.Count > 0)
            {
                var index = EditorGUILayout.Popup(onChangeCustomNameList.IndexOf(body.onChangeCustomName), onChangeCustomNameList.ToArray());
                serializedObject.FindProperty("onChangeCustomName").stringValue = index >= 0 ? onChangeCustomNameList[index] : "";
            }
            else
            {
                prop = serializedObject.FindProperty("onChangeCustomName");
                EditorGUILayout.PropertyField(prop);
            }
            EditorGUILayout.EndHorizontal();

            SerializedProperty recieverProp = serializedObject.FindProperty("clips");
            if (recieverReorderableList == null)
            {
                recieverReorderableList = new ReorderableList(serializedObject, recieverProp);
                recieverReorderableList.draggable = true;
                recieverReorderableList.displayAdd = true;
                recieverReorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Clips");
                recieverReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    body.clips[index] = (AudioClip)EditorGUI.ObjectField(rect, body.clips[index], typeof(AudioClip), false);
                };
            }
            recieverReorderableList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private List<string> GetCustomNameList(GameObject targetObject)
        {
            List<string> list = new List<string>();
            var udons = targetObject.GetComponents<UdonBehaviour>();
            foreach (var udon in udons)
            {
                UdonSharpBehaviour usharp = UdonSharpEditorUtility.FindProxyBehaviour(udon);
                if (usharp && usharp.GetUdonSharpComponent<T23_CustomTrigger>())
                {
                    var nameField = usharp.GetProgramVariable("Name") as string;
                    if (nameField != null)
                    {
                        if (nameField != "" && !list.Contains(nameField))
                        {
                            list.Add(nameField);
                        }
                    }
                }
            }
            return list;
        }
    }
#endif

    void Start()
    {
        seed = Random.Range(0, 1000000000);
        currentIndex = -1;
        if (playbackOrder == 1)
        {
            order = new int[clips.Length];
            for (int i = 0; i < order.Length; i++)
            {
                order[i] = order.Length - 1 - i;
            }
        }
        else if (playbackOrder == 2)
        {
            Shuffle();
        }
        else
        {
            order = new int[clips.Length];
            for (int i = 0; i < order.Length; i++)
            {
                order[i] = i;
            }
        }
    }

    public void SetInitialSeed(int _seed)
    {
        seed = _seed;
    }

    public void Play(int index)
    {
        if (clips.Length <= index) { return; }
        currentIndex = index;
        Play_inner();
        
        SendOnPlayTrigger();
    }

    private void Play_inner()
    {
        if (!source) { return; }

        actionCount++;
        if (source.isPlaying) { source.Stop(); }
        source.clip = clips[order[currentIndex]];
        if (minPitchRange >= maxPitchRange)
        {
            source.pitch = minPitchRange;
        }
        else
        {
            Random.InitState(seed + actionCount);
            source.pitch = Random.Range(minPitchRange, maxPitchRange);
        }
        source.Play();
        isPlaying = true;
    }

    public void PlayNext()
    {
        PlayNext_inner();
        SendOnPlayTrigger();
    }

    private void PlayNext_inner()
    {
        if (playbackOrder == 3)
        {
            Random.InitState(seed + actionCount);
            currentIndex = Random.Range(0, clips.Length);
        }
        else
        {
            if (currentIndex == clips.Length - 1)
            {
                if (repeat)
                {
                    currentIndex = 0;
                }
                else
                {
                    Stop();
                    return;
                }
            }
            else
            {
                currentIndex++;
            }
        }
        Play_inner();
    }

    public void Stop()
    {
        if (!source) { return; }

        if (source.isPlaying)
        {
            source.Stop();
            isPlaying = false;
            SendOnStopTrigger();
        }
    }

    void Update()
    {
        if (!source) { return; }
        if (isPlaying && !source.isPlaying)
        {
            if (playbackStyle == 0)
            {
                isPlaying = false;
                SendOnStopTrigger();
            }
            else
            {
                PlayNext_inner();
                SendOnChangeTrigger();
            }
        }
    }

    public void Shuffle()
    {
        int[] lottery = new int[clips.Length];
        for (int i = 0; i < lottery.Length; i++)
        {
            lottery[i] = i;
        }
        order = new int[clips.Length];
        actionCount++;
        Random.InitState(seed + actionCount);
        Debug.Log(seed + actionCount);
        for (int i = 0; i < lottery.Length; i++)
        {
            int result = -1;
            while (result == -1)
            {
                int idx = Random.Range(0, lottery.Length);
                result = lottery[idx];
                lottery[idx] = -1;
            }
            order[i] = result;
        }
    }

    private void SendOnPlayTrigger()
    {
        if (onPlay && onPlayCustomName != "")
        {
            T23_CustomTrigger[] customTriggers = onPlay.GetComponents<T23_CustomTrigger>();
            for (int i = 0; i < customTriggers.Length; i++)
            {
                if (customTriggers[i].Name == onPlayCustomName)
                {
                    customTriggers[i].Trigger();
                }
            }
        }
    }

    private void SendOnStopTrigger()
    {
        if (onStop && onStopCustomName != "")
        {
            T23_CustomTrigger[] customTriggers = onStop.GetComponents<T23_CustomTrigger>();
            for (int i = 0; i < customTriggers.Length; i++)
            {
                if (customTriggers[i].Name == onStopCustomName)
                {
                    customTriggers[i].Trigger();
                }
            }
        }
    }

    private void SendOnChangeTrigger()
    {
        if (onChange && onChangeCustomName != "")
        {
            T23_CustomTrigger[] customTriggers = onChange.GetComponents<T23_CustomTrigger>();
            for (int i = 0; i < customTriggers.Length; i++)
            {
                if (customTriggers[i].Name == onChangeCustomName)
                {
                    customTriggers[i].Trigger();
                }
            }
        }
    }
}
