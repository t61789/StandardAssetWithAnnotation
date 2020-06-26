using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Cameras
{
    public class FreeLookCam : PivotBasedCameraRig
    {
        // 自由相机，通过滑动屏幕中间的区域旋转相机，并且注视着目标
        // 需要放在有rigidbody的物体上
        // This script is designed to be placed on the root object of a camera rig,
        // comprising 3 gameobjects, each parented to the next:

        // 场景中的对象结构
        // 	Camera Rig
        // 		Pivot
        // 			Camera

        [SerializeField] private float m_MoveSpeed = 1f;                      // How fast the rig will move to keep up with the target's position.
        [Range(0f, 10f)] [SerializeField] private float m_TurnSpeed = 1.5f;   // How fast the rig will rotate from user input.
        [SerializeField] private float m_TurnSmoothing = 0.0f;                // How much smoothing to apply to the turn input, to reduce mouse-turn jerkiness
        [SerializeField] private float m_TiltMax = 75f;                       // The maximum value of the x axis rotation of the pivot.
        [SerializeField] private float m_TiltMin = 45f;                       // The minimum value of the x axis rotation of the pivot.
        [SerializeField] private bool m_LockCursor = false;                   // Whether the cursor should be hidden and locked.
        [SerializeField] private bool m_VerticalAutoReturn = false;           // set wether or not the vertical axis should auto return

        private float m_LookAngle;                    // The rig's y axis rotation.
        private float m_TiltAngle;                    // The pivot's x axis rotation.
        private const float k_LookDistance = 100f;    // How far in front of the pivot the character's look target is.
		private Vector3 m_PivotEulers;
		private Quaternion m_PivotTargetRot;
		private Quaternion m_TransformTargetRot;

        protected override void Awake()
        {
            base.Awake();
            // 设置是否将鼠标锁定在屏幕中间
            // Lock or unlock the cursor.
            Cursor.lockState = m_LockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            // 锁定了鼠标就不可见
            Cursor.visible = !m_LockCursor;
            // 记录锚点的欧拉角、旋转四元数
			m_PivotEulers = m_Pivot.rotation.eulerAngles;
	        m_PivotTargetRot = m_Pivot.transform.localRotation;
			m_TransformTargetRot = transform.localRotation;
        }


        protected void Update()
        {
            HandleRotationMovement();
            // 锁定鼠标
            if (m_LockCursor && Input.GetMouseButtonUp(0))
            {
                Cursor.lockState = m_LockCursor ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !m_LockCursor;
            }
        }


        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        protected override void FollowTarget(float deltaTime)
        {
            if (m_Target == null) return;
            // Move the rig towards target position.
            transform.position = Vector3.Lerp(transform.position, m_Target.position, deltaTime*m_MoveSpeed);
        }


        private void HandleRotationMovement()
        {
            // 处理相机旋转

            // 时间静止则不能旋转
			if(Time.timeScale < float.Epsilon)
			    return;

            // 读取输入
            // Read the user input
            var x = CrossPlatformInputManager.GetAxis("Mouse X");
            var y = CrossPlatformInputManager.GetAxis("Mouse Y");

            // 根据x轴输入调整视角的y轴旋转
            // Adjust the look angle by an amount proportional to the turn speed and horizontal input.
            m_LookAngle += x*m_TurnSpeed;

            // 赋值
            // Rotate the rig (the root object) around Y axis only:
            m_TransformTargetRot = Quaternion.Euler(0f, m_LookAngle, 0f);

            if (m_VerticalAutoReturn)
            {
                // 对于倾斜输入，我们需要根据使用鼠标还是触摸输入采取不同的行动
                // 在移动端上，垂直输入可以直接映射为倾斜值，所以它可以在观察输入释放后自动弹回
                // 我们必须测试它是否超过最大值或是小于0，因为我们想要它自动回到0，即便最大值和最小值不对称
                // For tilt input, we need to behave differently depending on whether we're using mouse or touch input:
                // on mobile, vertical input is directly mapped to tilt value, so it springs back automatically when the look input is released
                // we have to test whether above or below zero because we want to auto-return to zero even if min and max are not symmetrical.
                m_TiltAngle = y > 0 ? Mathf.Lerp(0, -m_TiltMin, y) : Mathf.Lerp(0, m_TiltMax, -y);
            }
            else
            {
                // 在使用鼠标的平台山，我们根据鼠标的y轴输入和转向速度调整当前角度
                // on platforms with a mouse, we adjust the current angle based on Y mouse input and turn speed
                m_TiltAngle -= y*m_TurnSpeed;
                // 保证角度在限制范围内
                // and make sure the new value is within the tilt range
                m_TiltAngle = Mathf.Clamp(m_TiltAngle, -m_TiltMin, m_TiltMax);
            }

            // 赋值
            // Tilt input around X is applied to the pivot (the child of this object)
			m_PivotTargetRot = Quaternion.Euler(m_TiltAngle, m_PivotEulers.y , m_PivotEulers.z);

            // 平滑赋值
			if (m_TurnSmoothing > 0)
			{
				m_Pivot.localRotation = Quaternion.Slerp(m_Pivot.localRotation, m_PivotTargetRot, m_TurnSmoothing * Time.deltaTime);
				transform.localRotation = Quaternion.Slerp(transform.localRotation, m_TransformTargetRot, m_TurnSmoothing * Time.deltaTime);
			}
			else
			{
                // 即时赋值
				m_Pivot.localRotation = m_PivotTargetRot;
				transform.localRotation = m_TransformTargetRot;
			}
        }
    }
}
