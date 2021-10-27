
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
#endif

public class T23_SetVoiceParameters : UdonSharpBehaviour
{
    public int groupID;
    public int priority;
    public string title;
    public const bool isAction = true;

    [SerializeField]
    private bool triggeredPlayer = false;

    [SerializeField]
    private float distanceFar = 25;

    [SerializeField]
    private float distanceNear = 0;

    [SerializeField]
    private float gain = 15;

    [SerializeField]
    private bool lowpass = true;

    [SerializeField]
    private float volumetricRadius = 0;

    [SerializeField, Range(0, 1)]
    private float randomAvg;

    private float randomMin = 0;
    private float randomMax = 0;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_SetVoiceParameters))]
    internal class T23_SetVoiceParametersEditor : Editor
    {
        T23_SetVoiceParameters body;
        T23_Master master;

        public enum TargetPlayer
        {
            All = 0,
            TriggeredPlayer = 1
        }
        private TargetPlayer targetPlayer;

        SerializedProperty prop;

        void OnEnable()
        {
            body = target as T23_SetVoiceParameters;

            master = T23_Master.GetMaster(body, body.groupID, 2, true, body.title);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            if (!T23_EditorUtility.GuideJoinMaster(master, body, body.groupID, 2))
            {
                return;
            }

            serializedObject.Update();

            T23_EditorUtility.ShowTitle("Action");

            if (master)
            {
                GUILayout.Box("[#" + body.groupID.ToString() + "] " + body.title, T23_EditorUtility.HeadlineStyle());
                T23_EditorUtility.ShowSwapButton(master, body.title);
                body.priority = master.actionTitles.IndexOf(body.title);
            }
            else
            {
                body.groupID = EditorGUILayout.IntField("Group ID", body.groupID);
                body.priority = EditorGUILayout.IntField("Priority", body.priority);
            }

            prop = serializedObject.FindProperty("triggeredPlayer");
            prop.boolValue = (TargetPlayer)EditorGUILayout.EnumPopup("Target Player", (TargetPlayer)System.Convert.ToInt32(body.triggeredPlayer)) == TargetPlayer.TriggeredPlayer;
            if (body.triggeredPlayer) { EditorGUILayout.HelpBox("TriggeredPlayer は、BroadcastLocalでのみ有効です", MessageType.Info); }
            prop = serializedObject.FindProperty("distanceFar");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("distanceNear");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("gain");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("lowpass");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("volumetricRadius");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("randomAvg");
            EditorGUILayout.PropertyField(prop);
            
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    void Start()
    {
        T23_BroadcastLocal[] broadcastLocals = GetComponents<T23_BroadcastLocal>();
        for (int i = 0; i < broadcastLocals.Length; i++)
        {
            if (broadcastLocals[i].groupID == groupID)
            {
                broadcastLocal = broadcastLocals[i];
                break;
            }
        }

        if (broadcastLocal)
        {
            broadcastLocal.AddActions(this, priority);

            if (broadcastLocal.randomize)
            {
                randomMin = broadcastLocal.randomTotal;
                broadcastLocal.randomTotal += randomAvg;
                randomMax = broadcastLocal.randomTotal;
            }
        }
        else
        {
            T23_BroadcastGlobal[] broadcastGlobals = GetComponents<T23_BroadcastGlobal>();
            for (int i = 0; i < broadcastGlobals.Length; i++)
            {
                if (broadcastGlobals[i].groupID == groupID)
                {
                    broadcastGlobal = broadcastGlobals[i];
                    break;
                }
            }

            if (broadcastGlobal)
            {
                broadcastGlobal.AddActions(this, priority);

                if (broadcastGlobal.randomize)
                {
                    randomMin = broadcastGlobal.randomTotal;
                    broadcastGlobal.randomTotal += randomAvg;
                    randomMax = broadcastGlobal.randomTotal;
                }
            }
        }

        this.enabled = false;
    }

    public void Action()
    {
        if (!RandomJudgement())
        {
            Finish();
            return;
        }

        if (triggeredPlayer && broadcastLocal)
        {
            SetParameter(broadcastLocal.triggeredPlayer);
        }
        else
        {
            VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
            foreach (var player in players)
            {
                if (player == null) { continue; }
                SetParameter(player);
            }
        }

        Finish();
    }

    private void SetParameter(VRCPlayerApi player)
    {
        player.SetVoiceDistanceFar(distanceFar);
        player.SetVoiceDistanceNear(distanceNear);
        player.SetVoiceGain(gain);
        player.SetVoiceLowpass(lowpass);
        player.SetVoiceVolumetricRadius(volumetricRadius);
    }

    private bool RandomJudgement()
    {
        if (broadcastLocal)
        {
            if (!broadcastLocal.randomize || (broadcastLocal.randomValue >= randomMin && broadcastLocal.randomValue < randomMax))
            {
                return true;
            }
        }
        else if (broadcastGlobal)
        {
            if (!broadcastGlobal.randomize || (broadcastGlobal.randomValue >= randomMin && broadcastGlobal.randomValue < randomMax))
            {
                return true;
            }
        }

        return false;
    }

    private void Finish()
    {
        if (broadcastLocal)
        {
            broadcastLocal.NextAction();
        }
        else if (broadcastGlobal)
        {
            broadcastGlobal.NextAction();
        }
    }
}
