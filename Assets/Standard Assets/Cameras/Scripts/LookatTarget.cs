using System;
using UnityEngine;

#pragma warning disable 649
namespace UnityStandardAssets.Cameras
{
    public class LookatTarget : AbstractTargetFollower
    {
        // 一个简单的脚本，让一个物体看向另一个物体，但有着可选的旋转限制
        // A simple script to make one object look at another,
        // but with optional constraints which operate relative to
        // this gameobject's initial rotation.

        // 只围着X轴和Y轴旋转
        // Only rotates around local X and Y.

        // 在本地坐标下工作，所以如果这个物体是另一个移动的物体的子物体，他的本地旋转限制依然能够正常工作。
        // 就像在车内望向车窗外面，或者一艘移动的飞船上的有旋转限制的炮塔
        // Works in local coordinates, so if this object is parented
        // to another moving gameobject, its local constraints will
        // operate correctly
        // (Think: looking out the side window of a car, or a gun turret
        // on a moving spaceship with a limited angular range)

        // 如果想要没有限制的话，把旋转距离设置得大于360度
        // to have no constraints on an axis, set the rotationRange greater than 360.

        [SerializeField] private Vector2 m_RotationRange;
        [SerializeField] private float m_FollowSpeed = 1;

        private Vector3 m_FollowAngles;
        private Quaternion m_OriginalRotation;

        protected Vector3 m_FollowVelocity;


        // 初始化
        // Use this for initialization
        protected override void Start()
        {
            base.Start();
            m_OriginalRotation = transform.localRotation;
        }

        // 重写父类的方法，编写跟随逻辑
        protected override void FollowTarget(float deltaTime)
        {
            // 将旋转初始化
            // we make initial calculations from the original local rotation
            transform.localRotation = m_OriginalRotation;

            // 先处理Y轴的旋转
            // tackle rotation around Y first
            Vector3 localTarget = transform.InverseTransformPoint(m_Target.position);   // 将目标坐标映射到本地坐标
            float yAngle = Mathf.Atan2(localTarget.x, localTarget.z)*Mathf.Rad2Deg; // 得到y轴上的旋转角度

            yAngle = Mathf.Clamp(yAngle, -m_RotationRange.y*0.5f, m_RotationRange.y*0.5f);  // 限制旋转角度
            transform.localRotation = m_OriginalRotation*Quaternion.Euler(0, yAngle, 0);    // 赋值

            // 再处理X轴的旋转
            // then recalculate new local target position for rotation around X
            localTarget = transform.InverseTransformPoint(m_Target.position);
            float xAngle = Mathf.Atan2(localTarget.y, localTarget.z)*Mathf.Rad2Deg;
            xAngle = Mathf.Clamp(xAngle, -m_RotationRange.x*0.5f, m_RotationRange.x*0.5f);  // 同y轴的计算方法
            // 根据目标角度增量来计算目标角度
            var targetAngles = new Vector3(m_FollowAngles.x + Mathf.DeltaAngle(m_FollowAngles.x, xAngle),
                                           m_FollowAngles.y + Mathf.DeltaAngle(m_FollowAngles.y, yAngle));

            // 平滑跟踪
            // smoothly interpolate the current angles to the target angles
            m_FollowAngles = Vector3.SmoothDamp(m_FollowAngles, targetAngles, ref m_FollowVelocity, m_FollowSpeed);

            // 赋值
            // and update the gameobject itself
            transform.localRotation = m_OriginalRotation*Quaternion.Euler(-m_FollowAngles.x, m_FollowAngles.y, 0);
        }
    }
}
