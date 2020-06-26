using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof (CarController))]
    public class CarUserControl : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use


        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
        }


        private void FixedUpdate()
        {
            // pass the input to the car!

            float h = CrossPlatformInputManager.GetAxis("Horizontal");  // 本章重点
            float v = CrossPlatformInputManager.GetAxis("Vertical");

#if !MOBILE_INPUT
            float handbrake = CrossPlatformInputManager.GetAxis("Jump");
            // m_Car是CarController类型的变量，也就是当前车辆的核心控制脚本
            // 他的Move方法定义为public void Move(float steering, float accel, float footbrake, float handbrake)
            // 作用是通过几个变量的输入改变车辆速度，具体实现我放在下章分析，这章只考虑如何获取输入
            m_Car.Move(h, v, v, handbrake);
#else
            m_Car.Move(h, v, v, 0f);
#endif
        }
    }
}
