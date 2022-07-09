
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.Components;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEditorInternal;
#endif

public class T23_SetPropertyBox : UdonSharpBehaviour
{
    public int groupID;
    public int priority;
    public string title;
    public const bool isAction = true;

    public T23_PropertyBox propertyBox;

    public int calcOperator;

    public bool value_bool;
    public int value_int;
    public float value_float;
    public Vector3 value_Vector3;
    public string value_string;
    public T23_PropertyBox valuePropertyBox;
    public bool usePropertyBox;

    [Range(0, 1)]
    public float randomAvg;

    private float randomMin = 0;
    private float randomMax = 0;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGlobal broadcastGlobal;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_SetPropertyBox))]
    internal class T23_SetPropertyBoxEditor : Editor
    {
        T23_SetPropertyBox body;
        T23_Master master;

        SerializedProperty prop;

        private ReorderableList recieverReorderableList;

        private string[] CalcOperator_a = { "Update", "Substitute (=)", "Add (+)", "Subtract (-)", "Multiple (*)", "Divide" };
        private string[] CalcOperator_b = { "Update", "Substitute (=)" };

        void OnEnable()
        {
            body = target as T23_SetPropertyBox;

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

            prop = serializedObject.FindProperty("propertyBox");
            EditorGUILayout.PropertyField(prop);
            if (body.propertyBox)
            {
                serializedObject.FindProperty("calcOperator").intValue = EditorGUILayout.Popup("Operator", body.calcOperator, (body.propertyBox.valueType == 1 || body.propertyBox.valueType == 2 || body.propertyBox.valueType == 3) ? CalcOperator_a : CalcOperator_b);
                if (body.calcOperator != 0)
                {
                    if (body.propertyBox.valueType == 0)
                    {
                        T23_EditorUtility.PropertyBoxField(serializedObject, "value_bool", "valuePropertyBox", "usePropertyBox", () => serializedObject.FindProperty("value_bool").boolValue = EditorGUILayout.Toggle("Value_bool", body.value_bool));
                    }
                    else if (body.propertyBox.valueType == 1)
                    {
                        T23_EditorUtility.PropertyBoxField(serializedObject, "value_int", "valuePropertyBox", "usePropertyBox", () => serializedObject.FindProperty("value_int").intValue = EditorGUILayout.IntField("Value_int", body.value_int));
                    }
                    else if (body.propertyBox.valueType == 2 || (body.propertyBox.valueType == 3 && (body.calcOperator == 4 || body.calcOperator == 5)))
                    {
                        T23_EditorUtility.PropertyBoxField(serializedObject, "value_float", "valuePropertyBox", "usePropertyBox", () => serializedObject.FindProperty("value_float").floatValue = EditorGUILayout.FloatField("Value_float", body.value_float));
                    }
                    else if (body.propertyBox.valueType == 3)
                    {
                        T23_EditorUtility.PropertyBoxField(serializedObject, "value_Vector3", "valuePropertyBox", "usePropertyBox", () => serializedObject.FindProperty("value_Vector3").vector3Value = EditorGUILayout.Vector3Field("Value_Vector3", body.value_Vector3));
                    }
                    else if (body.propertyBox.valueType == 4)
                    {
                        T23_EditorUtility.PropertyBoxField(serializedObject, "value_string", "valuePropertyBox", "usePropertyBox", () => serializedObject.FindProperty("value_string").stringValue = EditorGUILayout.TextField("Value_string", body.value_string));
                    }
                }
            }

            if (!master || master.randomize)
            {
                prop = serializedObject.FindProperty("randomAvg");
                EditorGUILayout.PropertyField(prop);
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
        if (!propertyBox) { return; }

        if (usePropertyBox && valuePropertyBox)
        {
            value_bool = valuePropertyBox.value_b;
            value_int = valuePropertyBox.value_i;
            value_float = valuePropertyBox.value_f;
            value_Vector3 = valuePropertyBox.value_v3;
            value_string = valuePropertyBox.value_s;
        }
        if (calcOperator == 0)
        {
            propertyBox.UpdateTrackValue();
        }
        if (calcOperator == 1)
        {
            if (propertyBox.valueType == 0) { propertyBox.value_b = value_bool; }
            if (propertyBox.valueType == 1) { propertyBox.value_i = value_int; }
            if (propertyBox.valueType == 2) { propertyBox.value_f = value_float; }
            if (propertyBox.valueType == 3) { propertyBox.value_v3 = value_Vector3; }
            if (propertyBox.valueType == 4) { propertyBox.value_s = value_string; }
        }
        if (calcOperator == 2)
        {
            if (propertyBox.valueType == 1) { propertyBox.value_i += value_int; }
            if (propertyBox.valueType == 2) { propertyBox.value_f += value_float; }
            if (propertyBox.valueType == 3) { propertyBox.value_v3 += value_Vector3; }
        }
        if (calcOperator == 3)
        {
            if (propertyBox.valueType == 1) { propertyBox.value_i -= value_int; }
            if (propertyBox.valueType == 2) { propertyBox.value_f -= value_float; }
            if (propertyBox.valueType == 3) { propertyBox.value_v3 -= value_Vector3; }
        }
        if (calcOperator == 4)
        {
            if (propertyBox.valueType == 1) { propertyBox.value_i *= value_int; }
            if (propertyBox.valueType == 2) { propertyBox.value_f *= value_float; }
            if (propertyBox.valueType == 3) { propertyBox.value_v3 *= value_float; }
        }
        if (calcOperator == 5)
        {
            if (propertyBox.valueType == 1) { propertyBox.value_i /= value_int; }
            if (propertyBox.valueType == 2) { propertyBox.value_f /= value_float; }
            if (propertyBox.valueType == 3) { propertyBox.value_v3 /= value_float; }
        }
        propertyBox.UpdateSubValue();
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
