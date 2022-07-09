
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UdonSharpEditor;
using System.Collections.Generic;
#endif

public class T23_PropertyBox : UdonSharpBehaviour
{
    public int valueType;

    public bool value_b;
    public int value_i;
    public float value_f;
    public Vector3 value_v3;
    public string value_s;

    public int trackType;

    public GameObject targetObject;

    public int targetPlayer;

    public UdonSharpBehaviour targetTrigger;

    public Object targetComponent;

    public int index;

    public string spot;

    public string spotDetail;

    public bool positionTracking;

    public bool updateEveryFrame;

    private System.DateTime startTime;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(T23_PropertyBox))]
    internal class T23_PropertyBoxEditor : Editor
    {
        T23_PropertyBox body;

        SerializedProperty prop;

        public enum ValueType
        {
            Bool = 0,
            Int = 1,
            Float = 2,
            Vector3 = 3,
            String = 4
        }

        public enum TrackType
        {
            None = 0,
            Player = 1,
            GameObject = 2,
            World = 3,
            UI = 4,
            AnimatorParameter = 5,
            Controller = 6,
        }

        public enum TargetPlayer
        {
            Local = 0,
            ObjectOwner = 1,
            TriggeredPlayer = 2,
            ByIndex = 3
        }

        private string[] PlayerSpot_b = { "IsUserInVR", "IsPlayerGrounded", "IsMaster", "IsInstanceOwner", "IsGameObjectOwner" };
        private string[] PlayerSpot_v3 = { "Position", "Rotation", "HeadPosition", "HeadRotation", "RightHandPosition", "RightHandRotation", "LeftHandPosition", "LeftHandRotation", "Velocity" };
        private string[] PlayerSpot_s = { "DisplayName" };

        private string[] ObjectSpot_b = { "IsActive" };
        private string[] ObjectSpot_v3 = { "Position", "Rotation", "LocalPosition", "LocalRotation", "Velocity", "AngularVelocity" };

        private string[] WorldSpot_if = { "PlayerCount", "Year", "Month", "Day", "DayOfWeek", "Hour", "Minute", "Second", "JoinHours", "JoinMinutes", "JoinSeconds" };

        private string[] ControllerSpot_f = { "RightIndexTrigger", "LeftIndexTrigger", "RightGrip", "LeftGrip", "MoveHorizo​​ntal", "MoveVertical", "LookHorizontal", "LookVertical" };

        private string[] SpotDetail_v3_f = { "X", "Y", "Z", "Magnitude" };
        private string[] SpotDetail_s = { "Nomal", "OneLetter" };

        void OnEnable()
        {
            body = target as T23_PropertyBox;
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            if (!UdonSharpEditorUtility.IsProxyBehaviour(body))
            {
                UdonSharpGUI.DrawConvertToUdonBehaviourButton(body);
                return;
            }

            T23_EditorUtility.ShowTitle("Option");
            GUILayout.Box("PropertyBox", T23_EditorUtility.HeadlineStyle());

            UdonSharpProgramAsset programAsset = UdonSharpEditorUtility.GetUdonSharpProgramAsset((UdonSharpBehaviour)target);
            UdonSharpGUI.DrawCompileErrorTextArea(programAsset);

            serializedObject.Update();

            serializedObject.FindProperty("valueType").intValue = (int)(ValueType)EditorGUILayout.EnumPopup("Value Type", (ValueType)body.valueType);
            if (body.valueType == 0)
            {
                serializedObject.FindProperty("value_b").boolValue = EditorGUILayout.Toggle("Value", body.value_b);
            }
            else if (body.valueType == 1)
            {
                serializedObject.FindProperty("value_i").intValue = EditorGUILayout.IntField("Value", body.value_i);
            }
            else if (body.valueType == 2)
            {
                serializedObject.FindProperty("value_f").floatValue = EditorGUILayout.FloatField("Value", body.value_f);
            }
            else if (body.valueType == 3)
            {
                serializedObject.FindProperty("value_v3").vector3Value = EditorGUILayout.Vector3Field("Value", body.value_v3);
            }
            else if (body.valueType == 4)
            {
                serializedObject.FindProperty("value_s").stringValue = EditorGUILayout.TextField("Value", body.value_s);
            }

            UdonSharpGUI.DrawUILine(Color.gray);

            serializedObject.FindProperty("trackType").intValue = (int)(TrackType)EditorGUILayout.EnumPopup("Track Type", (TrackType)body.trackType);
            if (body.trackType != 0)
            {
                List<string> spotList = new List<string>();
                if (body.trackType == 1)
                {
                    serializedObject.FindProperty("targetPlayer").intValue = (int)(TargetPlayer)EditorGUILayout.EnumPopup("Target Player", (TargetPlayer)body.targetPlayer);

                    if (body.targetPlayer == 1 || body.targetPlayer == 2)
                    {
                        prop = serializedObject.FindProperty("targetObject");
                        EditorGUILayout.PropertyField(prop);
                    }

                    if (body.targetPlayer == 2)
                    {
                        List<string> triggerList = new List<string>();
                        List<UdonSharpBehaviour> usharpList = new List<UdonSharpBehaviour>();
                        if (body.targetObject)
                        {
                            var udons = body.targetObject.GetComponents<UdonBehaviour>();
                            foreach (var udon in udons)
                            {
                                UdonSharpBehaviour usharp = UdonSharpEditorUtility.FindProxyBehaviour(udon);
                                if (usharp)
                                {
                                    var groupIDField = usharp.GetProgramVariable("groupID") as int?;
                                    var titleField = usharp.GetProgramVariable("title") as string;
                                    var playerTriggerField = usharp.GetProgramVariable("playerTrigger") as bool?;
                                    if (groupIDField != null && titleField != null && playerTriggerField == true)
                                    {
                                        if (titleField != "")
                                        {
                                            usharpList.Add(usharp);
                                            triggerList.Add($"[#{groupIDField}] {titleField}");
                                        }
                                    }
                                }
                            }
                        }
                        var triggerIndex = EditorGUILayout.Popup("Target Trigger", usharpList.IndexOf(body.targetTrigger), triggerList.ToArray());
                        serializedObject.FindProperty("targetTrigger").objectReferenceValue = triggerIndex >= 0 ? usharpList[triggerIndex] : null;
                    }

                    if (body.targetPlayer == 3)
                    {
                        prop = serializedObject.FindProperty("index");
                        EditorGUILayout.PropertyField(prop);
                    }

                    if (body.valueType == 0)
                    {
                        spotList.AddRange(PlayerSpot_b);
                    }
                    else if (body.valueType == 2 || body.valueType == 3)
                    {
                        spotList.AddRange(PlayerSpot_v3);
                    }
                    else if (body.valueType == 4)
                    {
                        spotList.AddRange(PlayerSpot_s);
                    }
                }

                if (body.trackType == 2)
                {
                    prop = serializedObject.FindProperty("targetObject");
                    EditorGUILayout.PropertyField(prop);
                    if (body.valueType == 0)
                    {
                        spotList.AddRange(ObjectSpot_b);
                    }
                    else if (body.valueType == 2 || body.valueType == 3)
                    {
                        spotList.AddRange(ObjectSpot_v3);
                    }
                }

                if (body.trackType == 3)
                {
                    if (body.valueType == 1 || body.valueType == 2)
                    {
                        spotList.AddRange(WorldSpot_if);
                    }
                }

                if (body.trackType == 4)
                {
                    prop = serializedObject.FindProperty("targetObject");
                    EditorGUILayout.PropertyField(prop);
                    if (body.targetObject)
                    {
                        body.targetComponent = null;
                        serializedObject.FindProperty("spot").stringValue = "";
                        List<System.Type> UITypes = new List<System.Type>();
                        if (body.valueType == 0)
                        {
                            UITypes.Add(typeof(Toggle));
                        }
                        if (body.valueType == 1)
                        {
                            UITypes.Add(typeof(Text));
                            UITypes.Add(typeof(InputField));
                            UITypes.Add(typeof(Dropdown));
                        }
                        if (body.valueType == 2)
                        {
                            UITypes.Add(typeof(Slider));
                            UITypes.Add(typeof(Scrollbar));
                            UITypes.Add(typeof(Text));
                            UITypes.Add(typeof(InputField));
                            UITypes.Add(typeof(Toggle));
                            UITypes.Add(typeof(Dropdown));
                        }
                        if (body.valueType == 4)
                        {
                            UITypes.Add(typeof(Text));
                            UITypes.Add(typeof(InputField));
                        }
                        foreach (var type in UITypes)
                        {
                            body.targetComponent = body.targetObject.GetComponent(type);
                            if (body.targetComponent != null)
                            {
                                serializedObject.FindProperty("spot").stringValue = type.Name;
                                break;
                            }
                        }
                        if (body.targetComponent == null)
                        {
                            EditorGUILayout.HelpBox($"{(ValueType)body.valueType} で取得可能な UI コンポーネントがありません。", MessageType.Error);
                        }
                        else
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            prop = serializedObject.FindProperty("targetComponent");
                            EditorGUILayout.PropertyField(prop);
                            EditorGUI.EndDisabledGroup();
                        }
                    }
                }

                if (body.trackType == 5)
                {
                    prop = serializedObject.FindProperty("targetComponent");
                    prop.objectReferenceValue = EditorGUILayout.ObjectField("Animator", prop.objectReferenceValue, typeof(Animator), true);
                    if (prop.objectReferenceValue != null)
                    {
                        Animator animator = prop.objectReferenceValue as Animator;
                        if (animator)
                        {
                            animator.Update(0);
                            for (int i = 0; i < animator.parameters.Length; i++)
                            {
                                var paramType = animator.GetParameter(i).type;
                                if (body.valueType == 0 && paramType == AnimatorControllerParameterType.Bool)
                                {
                                    spotList.Add(animator.GetParameter(i).name);
                                }
                                if (body.valueType == 1 && paramType == AnimatorControllerParameterType.Int)
                                {
                                    spotList.Add(animator.GetParameter(i).name);
                                }
                                if (body.valueType == 2 && paramType == AnimatorControllerParameterType.Float)
                                {
                                    spotList.Add(animator.GetParameter(i).name);
                                }
                            }
                        }
                    }
                }

                if (body.trackType == 6)
                {
                    if (body.valueType == 2)
                    {
                        spotList.AddRange(ControllerSpot_f);
                    }
                }

                if (spotList.Count > 0)
                {
                    var spotIndex = EditorGUILayout.Popup("Spot", spotList.IndexOf(body.spot), spotList.ToArray());
                    serializedObject.FindProperty("spot").stringValue = spotIndex >= 0 ? spotList[spotIndex] : "";
                }

                if (body.valueType == 2)
                {
                    List<string> changeable = new List<string>();
                    changeable.AddRange(PlayerSpot_v3);
                    changeable.AddRange(ObjectSpot_v3);
                    if (changeable.Contains(body.spot))
                    {
                        List<string> detailList = new List<string>(SpotDetail_v3_f);
                        var detailIndex = EditorGUILayout.Popup("Spot Detail", detailList.IndexOf(body.spotDetail), SpotDetail_v3_f);
                        serializedObject.FindProperty("spotDetail").stringValue = detailIndex >= 0 ? SpotDetail_v3_f[detailIndex] : "";
                    }
                }

                if (body.valueType == 4)
                {
                    List<string> detailList = new List<string>(SpotDetail_s);
                    var detailIndex = EditorGUILayout.Popup("Spot Detail", detailList.IndexOf(body.spotDetail), SpotDetail_s);
                    serializedObject.FindProperty("spotDetail").stringValue = detailIndex >= 0 ? SpotDetail_s[detailIndex] : "";
                    if (serializedObject.FindProperty("spotDetail").stringValue == "OneLetter")
                    {
                        prop = serializedObject.FindProperty("index");
                        EditorGUILayout.PropertyField(prop);
                    }
                }

                if (body.spot.Contains("Position"))
                {
                    prop = serializedObject.FindProperty("positionTracking");
                    EditorGUILayout.PropertyField(prop);
                }
                else
                {
                    serializedObject.FindProperty("positionTracking").boolValue = false;
                }

                bool constAlways = body.trackType == 6 && body.valueType == 2;
                EditorGUI.BeginDisabledGroup(constAlways);
                prop = serializedObject.FindProperty("updateEveryFrame");
                EditorGUILayout.PropertyField(prop);
                EditorGUI.EndDisabledGroup();
                if (constAlways) { serializedObject.FindProperty("updateEveryFrame").boolValue = true; }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    void Start()
    {
        startTime = System.DateTime.Now;
    }

    void Update()
    {
        if (updateEveryFrame)
        {
            UpdateTrackValue();
        }
    }

    public void UpdateTrackValue()
    {
        if (trackType == 1)
        {
            VRCPlayerApi player = null;
            switch (targetPlayer)
            {
                case 0:
                    player = Networking.LocalPlayer;
                    break;
                case 1:
                    player = Networking.GetOwner(gameObject);
                    break;
                case 2:
                    if (targetTrigger)
                    {
                        var playerField = (VRCPlayerApi)targetTrigger.GetProgramVariable("triggeredPlayer");
                        player = playerField;
                    }
                    break;
                case 3:
                    VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()]; ;
                    VRCPlayerApi.GetPlayers(players);
                    if (index < players.Length)
                    {
                        player = players[index];
                    }
                    else
                    {
                        player = Networking.LocalPlayer;
                    }
                    break;
            }
            if (player == null || !player.IsValid()) { return; }

            switch (spot)
            {
                case "IsUserInVR":
                    value_b = player.IsUserInVR();
                    break;
                case "IsPlayerGrounded":
                    value_b = player.IsPlayerGrounded();
                    break;
                case "IsMaster":
                    value_b = player.isMaster;
                    break;
                case "IsInstanceOwner":
                    value_b = player.isInstanceOwner;
                    break;
                case "IsGameObjectOwner":
                    value_b = player.IsOwner(gameObject);
                    break;
                case "Position":
                    value_v3 = player.GetPosition();
                    if (positionTracking)
                    {
                        transform.position = player.GetPosition();
                        transform.rotation = player.GetRotation();
                    }
                    break;
                case "Rotation":
                    value_v3 = player.GetRotation().eulerAngles;
                    break;
                case "HeadPosition":
                    value_v3 = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                    if (positionTracking)
                    {
                        transform.position = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                        transform.rotation = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                    }
                    break;
                case "HeadRotation":
                    value_v3 = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation.eulerAngles;
                    break;
                case "RightHandPosition":
                    value_v3 = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                    if (positionTracking)
                    {
                        transform.position = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                        transform.rotation = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                    }
                    break;
                case "RightHandRotation":
                    value_v3 = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation.eulerAngles;
                    break;
                case "LeftHandPosition":
                    value_v3 = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                    if (positionTracking)
                    {
                        transform.position = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                        transform.rotation = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
                    }
                    break;
                case "LeftHandRotation":
                    value_v3 = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation.eulerAngles;
                    break;
                case "Velocity":
                    value_v3 = player.GetVelocity();
                    break;
                case "DisplayName":
                    value_s = player.displayName;
                    break;
            }
        }

        if (trackType == 2)
        {
            if (!targetObject) { return; }

            switch (spot)
            {
                case "IsActive":
                    value_b = targetObject.activeSelf;
                    break;
                case "Position":
                    value_v3 = targetObject.transform.position;
                    if (positionTracking)
                    {
                        transform.position = targetObject.transform.position;
                        transform.rotation = targetObject.transform.rotation;
                    }
                    break;
                case "Rotation":
                    value_v3 = targetObject.transform.rotation.eulerAngles;
                    break;
                case "LocalPosition":
                    value_v3 = targetObject.transform.localPosition;
                    if (positionTracking)
                    {
                        transform.position = targetObject.transform.position;
                        transform.rotation = targetObject.transform.rotation;
                    }
                    break;
                case "LocalRotation":
                    value_v3 = targetObject.transform.localRotation.eulerAngles;
                    break;
                case "Velocity":
                    value_v3 = targetObject.GetComponent<Rigidbody>().velocity;
                    break;
                case "AngularVelocity":
                    value_v3 = targetObject.GetComponent<Rigidbody>().angularVelocity;
                    break;
            }
        }

        if (trackType == 3)
        {
            switch (spot)
            {
                case "PlayerCount":
                    value_i = VRCPlayerApi.GetPlayerCount();
                    value_f = value_i;
                    break;
                case "Year":
                    value_i = System.DateTime.Now.Year;
                    value_f = value_i;
                    break;
                case "Month":
                    value_i = System.DateTime.Now.Month;
                    value_f = value_i;
                    break;
                case "Day":
                    value_i = System.DateTime.Now.Day;
                    value_f = value_i;
                    break;
                case "DayOfWeek":
                    value_i = (int)System.DateTime.Now.DayOfWeek;
                    value_f = value_i;
                    break;
                case "Hour":
                    value_i = System.DateTime.Now.Hour;
                    value_f = value_i;
                    break;
                case "Minute":
                    value_i = System.DateTime.Now.Minute;
                    value_f = value_i;
                    break;
                case "Second":
                    value_i = System.DateTime.Now.Second;
                    value_f = value_i;
                    break;
                case "JoinHours":
                    value_f = (float)(System.DateTime.Now - startTime).TotalHours;
                    value_i = (int)value_f;
                    break;
                case "JoinMinutes":
                    value_f = (float)(System.DateTime.Now - startTime).TotalMinutes;
                    value_i = (int)value_f;
                    break;
                case "JoinSeconds":
                    value_f = (float)(System.DateTime.Now - startTime).TotalSeconds;
                    value_i = (int)value_f;
                    break;
            }
        }

        if (trackType == 4)
        {
            if (targetComponent)
            {
                if (valueType == 0)
                {
                    if (spot == "Toggle")
                    {
                        var toggle = (Toggle)targetComponent;
                        value_b = toggle.isOn;
                    }
                }
                if (valueType == 1)
                {
                    if (spot == "Text")
                    {
                        var text = (Text)targetComponent;
                        int.TryParse(text.text, out value_i);
                    }
                    if (spot == "InputField")
                    {
                        var inputField = (InputField)targetComponent;
                        int.TryParse(inputField.text, out value_i);
                    }
                    if (spot == "Dropdown")
                    {
                        var dropdown = (Dropdown)targetComponent;
                        value_i = dropdown.value;
                    }
                }
                if (valueType == 2)
                {
                    if (spot == "Slider")
                    {
                        var slider = (Slider)targetComponent;
                        value_f = slider.value;
                    }
                    if (spot == "Scrollbar")
                    {
                        var scrollbar = (Scrollbar)targetComponent;
                        value_f = scrollbar.value;
                    }
                    if (spot == "Text")
                    {
                        var text = (Text)targetComponent;
                        float.TryParse(text.text, out value_f);
                    }
                    if (spot == "InputField")
                    {
                        var inputField = (InputField)targetComponent;
                        float.TryParse(inputField.text, out value_f);
                    }
                    if (spot == "Dropdown")
                    {
                        var dropdown = (Dropdown)targetComponent;
                        value_f = dropdown.value;
                    }
                }
                if (valueType == 4)
                {
                    if (spot == "Text")
                    {
                        var text = (Text)targetComponent;
                        value_s = text.text;
                    }
                    if (spot == "InputField")
                    {
                        var inputField = (InputField)targetComponent;
                        value_s = inputField.text;
                    }
                }
            }
        }

        if (trackType == 5)
        {
            if (targetComponent != null && spot != "")
            {
                Animator animator = (Animator)targetComponent;
                if (valueType == 0)
                {
                    value_b = animator.GetBool(spot);
                }
                if (valueType == 1)
                {
                    value_i = animator.GetInteger(spot);
                }
                if (valueType == 2)
                {
                    value_f = animator.GetFloat(spot);
                }
            }
        }

        if (trackType == 6)
        {
            switch (spot)
            {
                case "RightIndexTrigger":
                    value_f = Input.GetAxis("Oculus_CrossPlatform_SecondaryIndexTrigger");
                    break;
                case "LeftIndexTrigger":
                    value_f = Input.GetAxis("Oculus_CrossPlatform_PrimaryIndexTrigger");
                    break;
                case "RightGrip":
                    value_f = Input.GetAxis("Oculus_CrossPlatform_SecondaryHandTrigger");
                    break;
                case "LeftGrip":
                    value_f = Input.GetAxis("Oculus_CrossPlatform_PrimaryHandTrigger");
                    break;
            }
        }

        UpdateSubValue();
    }

    public void UpdateSubValue()
    {
        switch (spotDetail)
        {
            case "X":
                value_f = value_v3.x;
                break;
            case "Y":
                value_f = value_v3.y;
                break;
            case "Z":
                value_f = value_v3.z;
                break;
            case "Magnitude":
                value_f = value_v3.magnitude;
                break;
            case "OneLetter":
                if (index < value_s.Length) { value_s = value_s.Substring(index, 1); }
                else { value_s = ""; }
                break;
        }

        switch (valueType)
        {
            case 0:
                value_s = value_b.ToString();
                break;
            case 1:
                value_s = value_i.ToString();
                break;
            case 2:
                value_s = value_f.ToString();
                break;
            case 3:
                value_s = value_v3.ToString();
                break;
        }
    }

    public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
    {
        if (trackType == 4 && spot == "MoveHorizontal")
        {
            value_f = value;
        }
    }

    public override void InputMoveVertical(float value, UdonInputEventArgs args)
    {
        if (trackType == 4 && spot == "MoveVertical")
        {
            value_f = value;
        }
    }

    public override void InputLookHorizontal(float value, UdonInputEventArgs args)
    {
        if (trackType == 4 && spot == "LookHorizontal")
        {
            value_f = value;
        }
    }

    public override void InputLookVertical(float value, UdonInputEventArgs args)
    {
        if (trackType == 4 && spot == "LookVertical")
        {
            value_f = value;
        }
    }
}
