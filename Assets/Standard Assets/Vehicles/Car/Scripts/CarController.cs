using System;
using UnityEngine;

#pragma warning disable 649
namespace UnityStandardAssets.Vehicles.Car
{
    internal enum CarDriveType
    {
        FrontWheelDrive,
        RearWheelDrive,
        FourWheelDrive
    }

    internal enum SpeedType
    {
        MPH,
        KPH
    }

    public class CarController : MonoBehaviour
    {
        [SerializeField] private CarDriveType m_CarDriveType = CarDriveType.FourWheelDrive;
        [SerializeField] private WheelCollider[] m_WheelColliders = new WheelCollider[4];
        [SerializeField] private GameObject[] m_WheelMeshes = new GameObject[4];
        [SerializeField] private WheelEffects[] m_WheelEffects = new WheelEffects[4];
        [SerializeField] private Vector3 m_CentreOfMassOffset;
        [SerializeField] private float m_MaximumSteerAngle;
        [Range(0, 1)] [SerializeField] private float m_SteerHelper; // 0 is raw physics , 1 the car will grip in the direction it is facing
        [Range(0, 1)] [SerializeField] private float m_TractionControl; // 0 is no traction control, 1 is full interference
        [SerializeField] private float m_FullTorqueOverAllWheels;
        [SerializeField] private float m_ReverseTorque;
        [SerializeField] private float m_MaxHandbrakeTorque;
        [SerializeField] private float m_Downforce = 100f;
        [SerializeField] private SpeedType m_SpeedType;
        [SerializeField] private float m_Topspeed = 200;
        [SerializeField] private static int NoOfGears = 5;
        [SerializeField] private float m_RevRangeBoundary = 1f;
        [SerializeField] private float m_SlipLimit;
        [SerializeField] private float m_BrakeTorque;

        private Quaternion[] m_WheelMeshLocalRotations;
        private Vector3 m_Prevpos, m_Pos;
        private float m_SteerAngle;
        private int m_GearNum;
        private float m_GearFactor;
        private float m_OldRotation;
        private float m_CurrentTorque;
        private Rigidbody m_Rigidbody;
        private const float k_ReversingThreshold = 0.01f;

        public bool Skidding { get; private set; }
        public float BrakeInput { get; private set; }
        public float CurrentSteerAngle { get { return m_SteerAngle; } }
        public float CurrentSpeed { get { return m_Rigidbody.velocity.magnitude * 2.23693629f; } }
        public float MaxSpeed { get { return m_Topspeed; } }
        public float Revs { get; private set; }
        public float AccelInput { get; private set; }

        // Use this for initialization
        private void Start()
        {
            m_WheelMeshLocalRotations = new Quaternion[4];
            for (int i = 0; i < 4; i++)
            {
                m_WheelMeshLocalRotations[i] = m_WheelMeshes[i].transform.localRotation;    // 初始化轮胎的旋转角度
            }
            m_WheelColliders[0].attachedRigidbody.centerOfMass = m_CentreOfMassOffset;  // 设置wheelCollider的车辆重心

            m_MaxHandbrakeTorque = float.MaxValue;  // 手刹的扭矩

            m_Rigidbody = GetComponent<Rigidbody>();    // 缓存rigidBody
            m_CurrentTorque = m_FullTorqueOverAllWheels - (m_TractionControl * m_FullTorqueOverAllWheels);
        }


        private void GearChanging()
        {
            // 根据齿轮总数和当前挡位计算当前挡位的速度上下限，超过上限就升档，低于下限就降档
            float f = Mathf.Abs(CurrentSpeed / MaxSpeed);
            float upgearlimit = (1 / (float)NoOfGears) * (m_GearNum + 1);
            float downgearlimit = (1 / (float)NoOfGears) * m_GearNum;

            if (m_GearNum > 0 && f < downgearlimit)
            {
                m_GearNum--;
            }

            if (f > upgearlimit && (m_GearNum < (NoOfGears - 1)))
            {
                m_GearNum++;
            }
        }


        // simple function to add a curved bias towards 1 for a value in the 0-1 range
        private static float CurveFactor(float factor)
        {
            return 1 - (1 - factor) * (1 - factor);
        }


        // unclamped version of Lerp, to allow value to exceed the from-to range
        private static float ULerp(float from, float to, float value)
        {
            return (1.0f - value) * from + value * to;
        }


        private void CalculateGearFactor()
        {
            float f = (1 / (float)NoOfGears);
            // 平滑计算齿轮因子
            // gear factor is a normalised representation of the current speed within the current gear's range of speeds.
            // We smooth towards the 'target' gear factor, so that revs don't instantly snap up or down when changing gear.
            var targetGearFactor = Mathf.InverseLerp(f * m_GearNum, f * (m_GearNum + 1), Mathf.Abs(CurrentSpeed / MaxSpeed));
            m_GearFactor = Mathf.Lerp(m_GearFactor, targetGearFactor, Time.deltaTime * 5f);
        }


        private void CalculateRevs()
        {
            // 平滑计算引擎转速，用于改变声音，不用于力的计算
            // calculate engine revs (for display / sound)
            // (this is done in retrospect - revs are not used in force/power calculations)
            CalculateGearFactor();
            var gearNumFactor = m_GearNum / (float)NoOfGears;
            var revsRangeMin = ULerp(0f, m_RevRangeBoundary, CurveFactor(gearNumFactor));
            var revsRangeMax = ULerp(m_RevRangeBoundary, 1f, gearNumFactor);
            Revs = ULerp(revsRangeMin, revsRangeMax, m_GearFactor);
        }


        public void Move(float steering, float accel, float footbrake, float handbrake)
        {
            // WheelCollider和轮胎的Mesh是两个东西
            // 所以碰撞盒的Steer变化，也就是轮胎转向时，Mesh是不会发生转向的
            // 所以需要再这里根据碰撞盒的转向数据重新设置Mesh的转向
            // 使得外观上看起来轮胎旋转了一样
            for (var i = 0; i < 4; i++)
            {
                Quaternion quat;
                Vector3 position;
                m_WheelColliders[i].GetWorldPose(out position, out quat);   // 获取碰撞盒的世界坐标和世界转向
                m_WheelMeshes[i].transform.position = position; // 设置Mesh的转向和位置
                m_WheelMeshes[i].transform.rotation = quat;
            }

            // Steer直译为舵，也就是方向盘
            // steering为[-1,1]上的值，而在WheelCollider上读取时需要将这个值映射到[-m_MaximumSteerAngle,m_MaximumSteerAngle]上
            // 也就是以角度的形式来描述轮胎的转向程度，并且旋转的角度最大值为m_MaximumSteerAngle

            // 在这里的一系列Clamp是在将输入数据调整到可接受的范围内
            // clamp input values
            steering = Mathf.Clamp(steering, -1, 1);    // 舵输入
            AccelInput = accel = Mathf.Clamp(accel, 0, 1);  // 加速输入
            BrakeInput = footbrake = -1 * Mathf.Clamp(footbrake, -1, 0);  // 倒车输入
            handbrake = Mathf.Clamp(handbrake, 0, 1);   // 手刹，暂时不知道输入来自哪里

            // 这里就是上面所说的将值[-1,1]映射到角度上
            // Set the steer on the front wheels.
            // Assuming that wheels 0 and 1 are the front wheels.
            m_SteerAngle = steering * m_MaximumSteerAngle;
            m_WheelColliders[0].steerAngle = m_SteerAngle;
            m_WheelColliders[1].steerAngle = m_SteerAngle;


            SteerHelper();  // 通过车体转向调整速度方向
            ApplyDrive(accel, footbrake);   // 通过不同的操作模式以及踩脚刹的程度，调整每个轮胎上的扭矩
            CapSpeed(); // 限制最大速度

            // 设置手刹
            // Set the handbrake.
            // Assuming that wheels 2 and 3 are the rear wheels.
            if (handbrake > 0f)
            {
                // 拉了手刹就设置制动扭矩
                var hbTorque = handbrake * m_MaxHandbrakeTorque;
                m_WheelColliders[2].brakeTorque = hbTorque;
                m_WheelColliders[3].brakeTorque = hbTorque;
            }


            CalculateRevs();    // 计算引擎转速
            GearChanging(); // 自动挡

            AddDownForce(); // 向车身施加向下的力
            CheckForWheelSpin();    // 检查轮胎滑动情况，并以此播放声音、粒子、轮胎印
            TractionControl();  // 根据滑动情况控制扭矩输出
        }


        private void CapSpeed()
        {
            // 限制最大速度，超过的话根据不同的单位换算到最大速度
            float speed = m_Rigidbody.velocity.magnitude;
            switch (m_SpeedType)
            {
                case SpeedType.MPH:

                    speed *= 2.23693629f;
                    if (speed > m_Topspeed)
                        m_Rigidbody.velocity = (m_Topspeed / 2.23693629f) * m_Rigidbody.velocity.normalized;
                    break;

                case SpeedType.KPH:
                    speed *= 3.6f;
                    if (speed > m_Topspeed)
                        m_Rigidbody.velocity = (m_Topspeed / 3.6f) * m_Rigidbody.velocity.normalized;
                    break;
            }
        }


        private void ApplyDrive(float accel, float footbrake)
        {
            // 根据驾驶模式(四驱\前驱\后驱)调整发动机的扭矩
            float thrustTorque;
            switch (m_CarDriveType)
            {
                case CarDriveType.FourWheelDrive:   // 四驱
                    thrustTorque = accel * (m_CurrentTorque / 4f);  // 当前的总扭矩除以4再乘上加速度
                    for (int i = 0; i < 4; i++)
                    {
                        m_WheelColliders[i].motorTorque = thrustTorque; // 扭矩分配到每个轮胎上
                    }
                    break;
                // 同四驱，只是分配到特定的轮胎上
                case CarDriveType.FrontWheelDrive:
                    thrustTorque = accel * (m_CurrentTorque / 2f);
                    m_WheelColliders[0].motorTorque = m_WheelColliders[1].motorTorque = thrustTorque;
                    break;

                case CarDriveType.RearWheelDrive:
                    thrustTorque = accel * (m_CurrentTorque / 2f);
                    m_WheelColliders[2].motorTorque = m_WheelColliders[3].motorTorque = thrustTorque;
                    break;

            }

            for (int i = 0; i < 4; i++)
            {
                // 如果当前速度大于5，且车头的朝向与速度朝向之间的夹角小于50，则设置制动扭矩为最大制动扭矩乘以脚刹因子[0,1]
                if (CurrentSpeed > 5 && Vector3.Angle(transform.forward, m_Rigidbody.velocity) < 50f)
                {
                    m_WheelColliders[i].brakeTorque = m_BrakeTorque * footbrake;
                }
                else if (footbrake > 0)
                {
                    // 否则踩了脚刹的话，发动机扭矩就为-m_ReverseTorque乘以脚刹因子？
                    m_WheelColliders[i].brakeTorque = 0f;
                    m_WheelColliders[i].motorTorque = -m_ReverseTorque * footbrake;
                }
            }
        }


        private void SteerHelper()
        {
            // 如果有一个轮子不在地上，也就是接触的法向量为(0,0,0)，车体就不转向了
            for (int i = 0; i < 4; i++)
            {
                WheelHit wheelhit;
                m_WheelColliders[i].GetGroundHit(out wheelhit);
                if (wheelhit.normal == Vector3.zero)
                    return; // wheels arent on the ground so dont realign the rigidbody velocity
            }

            // 若是转过的角度大于10度就可能发生万向节死锁？不知道这个10的阈值时什么意思
            // this if is needed to avoid gimbal lock problems that will make the car suddenly shift direction
            if (Mathf.Abs(m_OldRotation - transform.eulerAngles.y) < 10f)
            {
                // 接下来这几行是利用车体的转向调整速度的方向
                // 调整的程度与m_SteerHelper有关，值越大，速度转向的能力越强
                // 可以理解为抓地力越强，转向越快，而抓地力很弱的话，车体已经转向，速度却没转过来，进入了漂移的状态
                var turnadjust = (transform.eulerAngles.y - m_OldRotation) * m_SteerHelper;
                Quaternion velRotation = Quaternion.AngleAxis(turnadjust, Vector3.up);
                m_Rigidbody.velocity = velRotation * m_Rigidbody.velocity;
            }
            m_OldRotation = transform.eulerAngles.y;
        }


        // this is used to add more grip in relation to speed
        private void AddDownForce()
        {
            // 向车身施加向下的力，速度越快力越大
            m_WheelColliders[0].attachedRigidbody.AddForce(-transform.up * m_Downforce *
                                                         m_WheelColliders[0].attachedRigidbody.velocity.magnitude);
        }

        // 检测轮胎的旋转情况
        // 干三件事：1.释放粒子；2.播放轮胎滑行的声音；3.在地上留下轮胎印
        // checks if the wheels are spinning and is so does three things
        // 1) emits particles
        // 2) plays tiure skidding sounds
        // 3) leaves skidmarks on the ground
        // these effects are controlled through the WheelEffects class
        private void CheckForWheelSpin()
        {
            // 检测每个轮子
            // loop through all wheels
            for (int i = 0; i < 4; i++)
            {
                // 获取轮子的触地情况
                WheelHit wheelHit;
                m_WheelColliders[i].GetGroundHit(out wheelHit);

                // 如果轮胎的加速滑动或是减速滑动超过给定的阈值
                // is the tire slipping above the given threshhold
                if (Mathf.Abs(wheelHit.forwardSlip) >= m_SlipLimit || Mathf.Abs(wheelHit.sidewaysSlip) >= m_SlipLimit)
                {
                    // 播放轮胎烟雾，粒子效果等脚本下章分析
                    m_WheelEffects[i].EmitTyreSmoke();

                    // 避免有多个轮胎同时播放声音，如果有轮胎播放了，这个轮胎就不播放了
                    // avoiding all four tires screeching at the same time
                    // if they do it can lead to some strange audio artefacts
                    if (!AnySkidSoundPlaying())
                    {
                        m_WheelEffects[i].PlayAudio();
                    }
                    continue;
                }

                // 没在滑动了就停止播放声音和结束轮胎印
                // if it wasnt slipping stop all the audio
                if (m_WheelEffects[i].PlayingAudio)
                {
                    m_WheelEffects[i].StopAudio();
                }
                // end the trail generation
                m_WheelEffects[i].EndSkidTrail();
            }
        }

        // 车子轮胎转太快了就减少给予轮胎的能量
        // crude traction control that reduces the power to wheel if the car is wheel spinning too much
        private void TractionControl()
        {
            WheelHit wheelHit;
            switch (m_CarDriveType)
            {
                case CarDriveType.FourWheelDrive:
                    // loop through all wheels
                    for (int i = 0; i < 4; i++)
                    {
                        m_WheelColliders[i].GetGroundHit(out wheelHit);

                        AdjustTorque(wheelHit.forwardSlip);
                    }
                    break;

                case CarDriveType.RearWheelDrive:
                    m_WheelColliders[2].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);

                    m_WheelColliders[3].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);
                    break;

                case CarDriveType.FrontWheelDrive:
                    m_WheelColliders[0].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);

                    m_WheelColliders[1].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);
                    break;
            }
        }


        private void AdjustTorque(float forwardSlip)
        {
            // 轮胎滑动超过了限制值就减少发动机扭矩
            if (forwardSlip >= m_SlipLimit && m_CurrentTorque >= 0)
            {
                m_CurrentTorque -= 10 * m_TractionControl;
            }
            else
            {
                // 增加扭矩，并且避免超过最大值
                m_CurrentTorque += 10 * m_TractionControl;
                if (m_CurrentTorque > m_FullTorqueOverAllWheels)
                {
                    m_CurrentTorque = m_FullTorqueOverAllWheels;
                }
            }
        }

        // 判断是否有轮胎再播放声音
        private bool AnySkidSoundPlaying()
        {
            for (int i = 0; i < 4; i++)
            {
                if (m_WheelEffects[i].PlayingAudio)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
