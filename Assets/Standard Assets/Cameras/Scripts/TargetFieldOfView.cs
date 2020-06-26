using System;
using UnityEngine;


namespace UnityStandardAssets.Cameras
{
    public class TargetFieldOfView : AbstractTargetFollower
    {
        // 这个脚本用于与LookatTarget协同工作，简而言之就是能够放大视野，避免车辆开远了以后图像过小的问题
        // 不过没有在LookatTarget中找到调用这个方法的地方，只能通过手动勾选脚本启用
        // This script is primarily designed to be used with the "LookAtTarget" script to enable a
        // CCTV style camera looking at a target to also adjust its field of view (zoom) to fit the
        // target (so that it zooms in as the target becomes further away).
        // When used with a follow cam, it will automatically use the same target.

        [SerializeField] private float m_FovAdjustTime = 1;             // the time taken to adjust the current FOV to the desired target FOV amount.
        [SerializeField] private float m_ZoomAmountMultiplier = 2;      // a multiplier for the FOV amount. The default of 2 makes the field of view twice as wide as required to fit the target.
        [SerializeField] private bool m_IncludeEffectsInSize = false;   // changing this only takes effect on startup, or when new target is assigned.

        private float m_BoundSize;
        private float m_FovAdjustVelocity;
        private Camera m_Cam;
        private Transform m_LastTarget;

        // Use this for initialization
        protected override void Start()
        {
            base.Start();

            // 获取最大的Bound
            m_BoundSize = MaxBoundsExtent(m_Target, m_IncludeEffectsInSize);

            // get a reference to the actual camera component:
            m_Cam = GetComponentInChildren<Camera>();
        }


        protected override void FollowTarget(float deltaTime)
        {
            // 根据最大bounds平滑计算视野
            // calculate the correct field of view to fit the bounds size at the current distance
            float dist = (m_Target.position - transform.position).magnitude;
            float requiredFOV = Mathf.Atan2(m_BoundSize, dist)*Mathf.Rad2Deg*m_ZoomAmountMultiplier;

            m_Cam.fieldOfView = Mathf.SmoothDamp(m_Cam.fieldOfView, requiredFOV, ref m_FovAdjustVelocity, m_FovAdjustTime);
        }

        // 设置目标
        public override void SetTarget(Transform newTransform)
        {
            base.SetTarget(newTransform);
            m_BoundSize = MaxBoundsExtent(newTransform, m_IncludeEffectsInSize);
        }


        public static float MaxBoundsExtent(Transform obj, bool includeEffects)
        {
            // 获得目标最大的边界并返回，表示摄像机的最大视野
            // 这里设计了includeEffects参数用于表示是否包括特效，但未被使用
            // 所以这里一律不包括粒子效果
            // get the maximum bounds extent of object, including all child renderers,
            // but excluding particles and trails, for FOV zooming effect.

            // 获取对象的所有renderer
            var renderers = obj.GetComponentsInChildren<Renderer>();

            Bounds bounds = new Bounds();
            bool initBounds = false;

            // 遍历所有的renderer，使bounds不断生长，也就是取所有bounds中的最大值
            foreach (Renderer r in renderers)
            {
                // 不包括线渲染器和粒子渲染器
                if (!((r is TrailRenderer) || (r is ParticleSystemRenderer)))
                {
                    if (!initBounds)
                    {
                        initBounds = true;
                        bounds = r.bounds;  // 对于第一个遇到的bound就不生长了
                    }
                    else
                    {
                        bounds.Encapsulate(r.bounds);   // 生长
                    }
                }
            }
            // 选择三个轴中最大的一个
            float max = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            return max;
        }
    }
}
