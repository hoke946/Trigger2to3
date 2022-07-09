
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
#endif

public class T23_AudioPlay : UdonSharpBehaviour
{
    public int groupID;
    public int priority;
    public string title;
    public const bool isAction = true;

    public AudioSource[] recievers;

    public bool toggle;

    [Tooltip("if not toggle")]
    public bool operation = true;

    [Range(0, 1)]
    public float randomAvg;

    private float randomMin = 0;
    private float randomMax = 0;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_AudioPlay))]
    internal class T23_AudioPlayEditor : Editor
    {
        T23_AudioPlay body;
        T23_Master master;

        SerializedProperty prop;

        private ReorderableList recieverReorderableList;

        public enum ToggleOperation
        {
            Play,
            Stop,
            Toggle
        }
        private ToggleOperation operation;

        void OnEnable()
        {
            body = target as T23_AudioPlay;

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

            SerializedProperty recieverProp = serializedObject.FindProperty("recievers");
            if (recieverReorderableList == null)
            {
                recieverReorderableList = new ReorderableList(serializedObject, recieverProp);
                recieverReorderableList.draggable = true;
                recieverReorderableList.displayAdd = true;
                recieverReorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Recievers");
                recieverReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    body.recievers[index] = (AudioSource)EditorGUI.ObjectField(rect, body.recievers[index], typeof(AudioSource), true);
                };
            }
            recieverReorderableList.DoLayoutList();

            EditorGUI.BeginChangeCheck();
            operation = (ToggleOperation)EditorGUILayout.EnumPopup("Operation", GetOperation());
            if (EditorGUI.EndChangeCheck())
            {
                SelectOperation();
            }

            if (!master || master.randomize)
            {
                prop = serializedObject.FindProperty("randomAvg");
                EditorGUILayout.PropertyField(prop);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SelectOperation()
        {
            switch (operation)
            {
                case ToggleOperation.Play:
                    prop = serializedObject.FindProperty("toggle");
                    prop.boolValue = false;
                    prop = serializedObject.FindProperty("operation");
                    prop.boolValue = true;
                    break;
                case ToggleOperation.Stop:
                    prop = serializedObject.FindProperty("toggle");
                    prop.boolValue = false;
                    prop = serializedObject.FindProperty("operation");
                    prop.boolValue = false;
                    break;
                case ToggleOperation.Toggle:
                    prop = serializedObject.FindProperty("toggle");
                    prop.boolValue = true;
                    break;
            }
        }

        private ToggleOperation GetOperation()
        {
            if (body.toggle)
            {
                return ToggleOperation.Toggle;
            }
            else
            {
                if (body.operation)
                {
                    return ToggleOperation.Play;
                }
                else
                {
                    return ToggleOperation.Stop;
                }
            }
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
            return;
        }

        for (int i = 0; i < recievers.Length; i++)
        {
            if (recievers[i])
            {
                Execute(recievers[i]);
            }
        }
    }

    private void Execute(AudioSource target)
    {
        if (toggle)
        {
            if (target.isPlaying)
            {
                target.Stop();
            }
            else
            {
                target.Play();
            }
        }
        else
        {
            if (operation)
            {
                target.Play();
            }
            else
            {
                target.Stop();
            }
        }
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
}
