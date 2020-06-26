using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityStandardAssets.CrossPlatformInput
{
    // helps with managing tilt input on mobile devices
    public class TiltInput : MonoBehaviour
    {
        // options for the various orientations
        // 用于计算角度
        public enum AxisOptions
        {
            ForwardAxis,
            SidewaysAxis,
        }


        [Serializable]
        public class AxisMapping
        {
            public enum MappingType
            {
                NamedAxis,      // 直接控制轴
                MousePositionX, // 虚拟光标x轴方向
                MousePositionY, // 虚拟光标y轴方向
                MousePositionZ  // 虚拟光标z轴方向
            };


            public MappingType type;    // 映射方式
            public string axisName;     // 轴的id
        }


        public AxisMapping mapping; // 基础设置
        public AxisOptions tiltAroundAxis = AxisOptions.ForwardAxis;    // 用于计算角度
        public float fullTiltAngle = 25;    // 手机倾斜角度的限制
        public float centreAngleOffset = 0; // 倾斜的初始角度偏移值


        private CrossPlatformInputManager.VirtualAxis m_SteerAxis;  // 对应的轴


        private void OnEnable()
        {
            // 初始化自己的数据储存单元并且注册，若是虚拟光标控制就无需存储和注册
            if (mapping.type == AxisMapping.MappingType.NamedAxis)
            {
                m_SteerAxis = new CrossPlatformInputManager.VirtualAxis(mapping.axisName);
                CrossPlatformInputManager.RegisterVirtualAxis(m_SteerAxis);
            }
        }


        private void Update()
        {
            float angle = 0;
            if (Input.acceleration != Vector3.zero)
            {
                switch (tiltAroundAxis)
                {
                    // 将手机加速矢量映射成角度
                    case AxisOptions.ForwardAxis:   // 手机左右摆动
                        angle = Mathf.Atan2(Input.acceleration.x, -Input.acceleration.y)*Mathf.Rad2Deg +
                                centreAngleOffset;  
                        break;
                    case AxisOptions.SidewaysAxis:  // 手机前后摆动
                        angle = Mathf.Atan2(Input.acceleration.z, -Input.acceleration.y)*Mathf.Rad2Deg +
                                centreAngleOffset;
                        break;
                }
            }

            float axisValue = Mathf.InverseLerp(-fullTiltAngle, fullTiltAngle, angle)*2 - 1;    // 将角度映射到-1,1之间
            switch (mapping.type)
            {
                case AxisMapping.MappingType.NamedAxis:
                    m_SteerAxis.Update(axisValue);  // 更新角度
                    break;
                case AxisMapping.MappingType.MousePositionX:    // 更新虚拟光标轴坐标
                    CrossPlatformInputManager.SetVirtualMousePositionX(axisValue*Screen.width);
                    break;
                case AxisMapping.MappingType.MousePositionY:
                    CrossPlatformInputManager.SetVirtualMousePositionY(axisValue*Screen.width);
                    break;
                case AxisMapping.MappingType.MousePositionZ:
                    CrossPlatformInputManager.SetVirtualMousePositionZ(axisValue*Screen.width);
                    break;
            }
        }


        private void OnDisable()
        {
            m_SteerAxis.Remove();
        }
    }
}


namespace UnityStandardAssets.CrossPlatformInput.Inspector
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof (TiltInput.AxisMapping))]
    public class TiltInputAxisStylePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = position.x;
            float y = position.y;
            float inspectorWidth = position.width;

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var props = new[] {"type", "axisName"};
            var widths = new[] {.4f, .6f};
            if (property.FindPropertyRelative("type").enumValueIndex > 0)
            {
                // hide name if not a named axis
                props = new[] {"type"};
                widths = new[] {1f};
            }
            const float lineHeight = 18;
            for (int n = 0; n < props.Length; ++n)
            {
                float w = widths[n]*inspectorWidth;

                // Calculate rects
                Rect rect = new Rect(x, y, w, lineHeight);
                x += w;

                EditorGUI.PropertyField(rect, property.FindPropertyRelative(props[n]), GUIContent.none);
            }

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
#endif
}
