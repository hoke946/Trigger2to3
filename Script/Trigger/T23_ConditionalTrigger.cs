
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UdonSharpEditor;
#endif

public class T23_ConditionalTrigger : UdonSharpBehaviour
{
    public int groupID;
    public string title;
    public const bool isTrigger = true;

    [SerializeField]
    private bool passive;

    [SerializeField]
    private bool allowContinuity;

    [SerializeField] private T23_PropertyBox basePropertyBox;

    [SerializeField]
    private int compOperator;

    [SerializeField]
    private int compParameterType;

    [SerializeField] private T23_PropertyBox compPropertyBox;
    [SerializeField] private bool comp_b;
    [SerializeField] private int comp_i;
    [SerializeField] private float comp_f;
    [SerializeField] private Vector3 comp_v3;
    [SerializeField] private string comp_s;

    private object value;
    private object before = null;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

    private bool trigger_on;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_ConditionalTrigger))]
    internal class T23_ConditionalTriggerEditor : Editor
    {
        T23_ConditionalTrigger body;
        T23_Master master;

        SerializedProperty prop;

        public enum CompParameterType
        {
            Constant = 0,
            PropertyBox = 1,
            DifferenceFromBefore = 2
        }

        private string[] CompOperator_a = { "Equal (=)", "Not Equal (!=)", "Greater (>)", "Less (<)", "Greater or Equal (>=)", "Less or Equal (<=)" };
        private string[] CompOperator_b = { "Equal (=)", "Not Equal (!=)" };

        void OnEnable()
        {
            body = target as T23_ConditionalTrigger;

            master = T23_Master.GetMaster(body, body.groupID, 1, true, body.title);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            if (!T23_EditorUtility.GuideJoinMaster(master, body, body.groupID, 1))
            {
                return;
            }

            UdonSharpProgramAsset programAsset = UdonSharpEditorUtility.GetUdonSharpProgramAsset((UdonSharpBehaviour)target);
            UdonSharpGUI.DrawCompileErrorTextArea(programAsset);

            serializedObject.Update();

            T23_EditorUtility.ShowTitle("Trigger");

            if (master)
            {
                GUILayout.Box("[#" + body.groupID.ToString() + "] " + body.title, T23_EditorUtility.HeadlineStyle());
            }
            else
            {
                body.groupID = EditorGUILayout.IntField("Group ID", body.groupID);
            }

            prop = serializedObject.FindProperty("passive");
            EditorGUILayout.PropertyField(prop);
            if (!body.passive)
            {
                prop = serializedObject.FindProperty("allowContinuity");
                EditorGUILayout.PropertyField(prop);
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Base", EditorStyles.boldLabel);
            serializedObject.FindProperty("basePropertyBox").objectReferenceValue = EditorGUILayout.ObjectField("PropertyBox", body.basePropertyBox, typeof(T23_PropertyBox), true);

            if (body.basePropertyBox)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Comparison", EditorStyles.boldLabel);
                serializedObject.FindProperty("compOperator").intValue = EditorGUILayout.Popup("Operator", body.compOperator, (body.basePropertyBox.valueType == 1 || body.basePropertyBox.valueType == 2) ? CompOperator_a : CompOperator_b);
                serializedObject.FindProperty("compParameterType").intValue = (int)(CompParameterType)EditorGUILayout.EnumPopup("Parameter Type", (CompParameterType)body.compParameterType);
                if (body.compParameterType == 0)
                {
                    switch (body.basePropertyBox.valueType)
                    {
                        case 0:
                            serializedObject.FindProperty("comp_b").boolValue = EditorGUILayout.Toggle("Value", body.comp_b);
                            break;
                        case 1:
                            serializedObject.FindProperty("comp_i").intValue = EditorGUILayout.IntField("Value", body.comp_i);
                            break;
                        case 2:
                            serializedObject.FindProperty("comp_f").floatValue = EditorGUILayout.FloatField("Value", body.comp_f);
                            break;
                        case 4:
                            serializedObject.FindProperty("comp_s").stringValue = EditorGUILayout.TextField("Value", body.comp_s);
                            break;
                    }
                }
                if (body.compParameterType == 1)
                {
                    serializedObject.FindProperty("compPropertyBox").objectReferenceValue = EditorGUILayout.ObjectField("PropertyBox", body.compPropertyBox, typeof(T23_PropertyBox), true);
                    if (body.compPropertyBox)
                    {
                        if (body.compPropertyBox.valueType != body.basePropertyBox.valueType)
                        {
                            EditorGUILayout.HelpBox("PropertyBox の ValueType が不適合です", MessageType.Error);
                        }
                    }
                }
            }

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

        if (!broadcastLocal)
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
        }
    }

    void Update()
    {
        if (!passive)
        {
            Judgement();
        }
    }

    public void Judgement()
    {
        if (basePropertyBox)
        {
            if (basePropertyBox.valueType == 0) { value = basePropertyBox.value_b; }
            if (basePropertyBox.valueType == 1) { value = basePropertyBox.value_i; }
            if (basePropertyBox.valueType == 2) { value = basePropertyBox.value_f; }
            if (basePropertyBox.valueType == 3) { value = basePropertyBox.value_v3; }
            if (basePropertyBox.valueType == 4) { value = basePropertyBox.value_s; }
        }
        if (compParameterType == 1)
        {
            if (compPropertyBox)
            {
                if (basePropertyBox.valueType == 0) { comp_b = compPropertyBox.value_b; }
                if (basePropertyBox.valueType == 1) { comp_i = compPropertyBox.value_i; }
                if (basePropertyBox.valueType == 2) { comp_f = compPropertyBox.value_f; }
                if (basePropertyBox.valueType == 3) { comp_v3 = compPropertyBox.value_v3; }
                if (basePropertyBox.valueType == 4) { comp_s = compPropertyBox.value_s; }
            }
        }
        if (compParameterType == 2)
        {
            if (before != null)
            {
                if (basePropertyBox.valueType == 0) { comp_b = (bool)before; }
                if (basePropertyBox.valueType == 1) { comp_i = (int)before; }
                if (basePropertyBox.valueType == 2) { comp_f = (float)before; }
                if (basePropertyBox.valueType == 3) { comp_v3 = (Vector3)before; }
                if (basePropertyBox.valueType == 4) { comp_s = (string)before; }
            }
        }

        bool on = false;
        if (compOperator == 0)
        {
            if (basePropertyBox.valueType == 0) { on = (bool)value == comp_b; }
            if (basePropertyBox.valueType == 1) { on = (int)value == comp_i; }
            if (basePropertyBox.valueType == 2) { on = (float)value == comp_f; }
            if (basePropertyBox.valueType == 3) { on = (Vector3)value == comp_v3; }
            if (basePropertyBox.valueType == 4) { on = (string)value == comp_s; }
        }
        if (compOperator == 1)
        {
            if (basePropertyBox.valueType == 0) { on = (bool)value != comp_b; }
            if (basePropertyBox.valueType == 1) { on = (int)value != comp_i; }
            if (basePropertyBox.valueType == 2) { on = (float)value != comp_f; }
            if (basePropertyBox.valueType == 3) { on = (Vector3)value != comp_v3; }
            if (basePropertyBox.valueType == 4) { on = (string)value != comp_s; }
        }
        if (compOperator == 2)
        {
            if (basePropertyBox.valueType == 1) { on = (int)value > comp_i; }
            if (basePropertyBox.valueType == 2) { on = (float)value > comp_f; }
        }
        if (compOperator == 3)
        {
            if (basePropertyBox.valueType == 1) { on = (int)value < comp_i;}
            if (basePropertyBox.valueType == 2) { on = (float)value < comp_f; }
        }
        if (compOperator == 4)
        {
            if (basePropertyBox.valueType == 1) { on = (int)value >= comp_i; }
            if (basePropertyBox.valueType == 2) { on = (float)value >= comp_f; }
        }
        if (compOperator == 5)
        {
            if (basePropertyBox.valueType == 1) { on = (int)value <= comp_i; }
            if (basePropertyBox.valueType == 2) { on = (float)value <= comp_f; }
        }

        if (compParameterType == 2)
        {
            if (before == null) { on = true; }
            before = value;
        }

        if (on)
        {
            if (!trigger_on)
            {
                Trigger();
                if (!passive && !allowContinuity) { trigger_on = true; }
            }
        }
        else
        {
            trigger_on = false;
        }
    }

    public void Trigger()
    {
        if (broadcastLocal)
        {
            broadcastLocal.Trigger();
        }
        else if (broadcastGlobal)
        {
            broadcastGlobal.Trigger();
        }
    }
}
