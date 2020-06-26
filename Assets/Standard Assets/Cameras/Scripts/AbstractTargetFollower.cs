using System;
using UnityEngine;

#pragma warning disable 649
namespace UnityStandardAssets.Cameras
{
    public abstract class AbstractTargetFollower : MonoBehaviour
    {
        // 三种更新方式 Update/FixedUpdate/LateUpdate
        public enum UpdateType // The available methods of updating are:
        {
            FixedUpdate, // Update in FixedUpdate (for tracking rigidbodies).
            LateUpdate, // Update in LateUpdate. (for tracking objects that are moved in Update)
            ManualUpdate, // user must call to update camera
        }

        [SerializeField] protected Transform m_Target;            // The target object to follow
        [SerializeField] private bool m_AutoTargetPlayer = true;  // Whether the rig should automatically target the player.
        [SerializeField] private UpdateType m_UpdateType;         // stores the selected update type

        protected Rigidbody targetRigidbody;


        protected virtual void Start()
        {
            // 如果启用了了自动寻找玩家功能，就自动寻找Tag为Player的物体作为目标
            // if auto targeting is used, find the object tagged "Player"
            // any class inheriting from this should call base.Start() to perform this action!
            if (m_AutoTargetPlayer)
            {
                FindAndTargetPlayer();
            }

            if (m_Target == null) return;
            targetRigidbody = m_Target.GetComponent<Rigidbody>();
        }


        private void FixedUpdate()
        {
            // 在目标有刚体组件或者不是运动学模式时调用
            // we update from here if updatetype is set to Fixed, or in auto mode,
            // if the target has a rigidbody, and isn't kinematic.

            // 若启用了自动寻找玩家功能，在目标为null或是非活动时自动寻找玩家
            if (m_AutoTargetPlayer && (m_Target == null || !m_Target.gameObject.activeSelf))
            {
                FindAndTargetPlayer();
            }
            if (m_UpdateType == UpdateType.FixedUpdate)
            {
                FollowTarget(Time.deltaTime);
            }
        }


        private void LateUpdate()
        {
            // 在目标没有刚体组件或是运动学模式时调用
            // we update from here if updatetype is set to Late, or in auto mode,
            // if the target does not have a rigidbody, or - does have a rigidbody but is set to kinematic.
            if (m_AutoTargetPlayer && (m_Target == null || !m_Target.gameObject.activeSelf))
            {
                FindAndTargetPlayer();
            }
            if (m_UpdateType == UpdateType.LateUpdate)
            {
                FollowTarget(Time.deltaTime);
            }
        }

        public void ManualUpdate()
        {
            // 同LateUpdate，但这不是Unity定义的消息，不知道什么时候可以调用，或者只是写错了？应该是Update()
            // we update from here if updatetype is set to Late, or in auto mode,
            // if the target does not have a rigidbody, or - does have a rigidbody but is set to kinematic.
            if (m_AutoTargetPlayer && (m_Target == null || !m_Target.gameObject.activeSelf))
            {
                FindAndTargetPlayer();
            }
            if (m_UpdateType == UpdateType.ManualUpdate)
            {
                FollowTarget(Time.deltaTime);
            }
        }

        // 如何跟随目标，交给子类重写
        protected abstract void FollowTarget(float deltaTime);


        public void FindAndTargetPlayer()
        {
            // 寻找Tag为Player的物体并设为目标
            // auto target an object tagged player, if no target has been assigned
            var targetObj = GameObject.FindGameObjectWithTag("Player");
            if (targetObj)
            {
                SetTarget(targetObj.transform);
            }
        }

        // 设置目标
        public virtual void SetTarget(Transform newTransform)
        {
            m_Target = newTransform;
        }


        public Transform Target
        {
            get { return m_Target; }
        }
    }
}
