
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

public class T23_ActiveCustomTrigger : UdonSharpBehaviour
{
    public int groupID;
    public int priority;
    public string title;
    public const bool isAction = true;

    [SerializeField]
    private GameObject[] recievers;

    [SerializeField]
    private string Name;

    [SerializeField]
    private bool takeOwnership;

    private bool executing = false;
    private bool[] executed;
    private float waitTimer;

    [SerializeField, Range(0, 1)]
    private float randomAvg;

    private float randomMin = 0;
    private float randomMax = 0;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_ActiveCustomTrigger))]
    internal class T23_ActiveCustomTriggerEditor : Editor
    {
        T23_ActiveCustomTrigger body;
        T23_Master master;

        SerializedProperty prop;

        private ReorderableList recieverReorderableList;

        void OnEnable()
        {
            body = target as T23_ActiveCustomTrigger;

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
                    body.recievers[index] = (GameObject)EditorGUI.ObjectField(rect, body.recievers[index], typeof(GameObject), true);
                };
            }
            recieverReorderableList.DoLayoutList();

            List<string> customNameList = new List<string>();
            if (body.recievers != null)
            {
                foreach (var go in body.recievers)
                {
                    if (go)
                    {
                        customNameList.AddRange(GetCustomNameList(go));
                    }
                }
            }
            if (customNameList.Count > 0)
            {
                var index = EditorGUILayout.Popup("Name", customNameList.IndexOf(body.Name), customNameList.ToArray());
                serializedObject.FindProperty("Name").stringValue = index >= 0 ? customNameList[index] : "";
            }
            else
            {
                serializedObject.FindProperty("Name").stringValue = "";
            }
            prop = serializedObject.FindProperty("takeOwnership");
            EditorGUILayout.PropertyField(prop);
            prop = serializedObject.FindProperty("randomAvg");
            EditorGUILayout.PropertyField(prop);

            serializedObject.ApplyModifiedProperties();
        }

        private List<string> GetCustomNameList(GameObject targetObject)
        {
            List<string> list = new List<string>();
            var udons = targetObject.GetComponents<UdonBehaviour>();
            foreach (var udon in udons)
            {
                UdonSharpBehaviour usharp = UdonSharpEditorUtility.FindProxyBehaviour(udon);
                if (usharp.GetUdonSharpComponent<T23_CustomTrigger>())
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

#if UNITY_EDITOR
        // local simulation
        takeOwnership = false;
#endif
        
        this.enabled = false;
    }

    void Update()
    {
        if (executing)
        {
            bool failure = false;
            for (int i = 0; i < recievers.Length; i++)
            {
                if (recievers[i])
                {
                    if (!executed[i])
                    {
                        if (Networking.IsOwner(recievers[i]))
                        {
                            Execute(recievers[i]);
                            executed[i] = true;
                        }
                        else
                        {
                            failure = true;
                        }
                    }
                }
            }

            if (!failure)
            {
                executing = false;
                this.enabled = false;
                Finish();
            }

            waitTimer += Time.deltaTime;
            if (waitTimer > 5)
            {
                executing = false;
                this.enabled = false;
                Finish();
            }
        }
    }

    public void Action()
    {
        if (!RandomJudgement())
        {
            Finish();
            return;
        }

        for (int i = 0; i < recievers.Length; i++)
        {
            if (recievers[i])
            {
                if (takeOwnership)
                {
                    Networking.SetOwner(Networking.LocalPlayer, recievers[i]);
                    executing = true;
                    this.enabled = true;
                    executed = new bool[recievers.Length];
                    waitTimer = 0;
                }
                else
                {
                    Execute(recievers[i]);
                }
            }
        }

        if (!takeOwnership)
        {
            Finish();
        }
    }

    private void Execute(GameObject target)
    {
        T23_CustomTrigger[] customTriggers = target.GetComponents<T23_CustomTrigger>();
        for (int i = 0; i < customTriggers.Length; i++)
        {
            if (customTriggers[i].Name == Name)
            {
                customTriggers[i].Trigger();
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
