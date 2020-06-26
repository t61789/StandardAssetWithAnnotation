using System;
using System.Collections.Generic;
using UnityEngine;


namespace UnityStandardAssets.CrossPlatformInput
{
    // 这是一个抽象类，在CrossPlatformInputManager中可以看到它有两种实现，MobileInput和StandaloneInput
    // 提供三种输入数据，虚拟光标VirtualMousePosition，轴VirtualAxis，按键VirtualButton
    public abstract class VirtualInput
    {
        // 利用手机的倾斜来模拟光标，与正常的鼠标输入一样为Vector3
        public Vector3 virtualMousePosition { get; private set; }

        // 缓冲区，储存了输入的数据，作为输入控制器的类通过id填入数据，需要数据的类通过id获取数据
        // 输入控制器的作用是将原始数据转化成方便读取的格式，例如TiltInpt类将手机加速度转化为角度，再映射到[-1,1]，用于控制转向
        // 这个是轴数据的缓冲区
        // Dictionary to store the name relating to the virtual axes
        protected Dictionary<string, CrossPlatformInputManager.VirtualAxis> m_VirtualAxes =
            new Dictionary<string, CrossPlatformInputManager.VirtualAxis>();

        // 这个是按键数据的缓冲区
        // list of the axis and button names that have been flagged to always use a virtual axis or button
        protected Dictionary<string, CrossPlatformInputManager.VirtualButton> m_VirtualButtons =
            new Dictionary<string, CrossPlatformInputManager.VirtualButton>();
        protected List<string> m_AlwaysUseVirtual = new List<string>();
        
        // 判断是否有名为name的轴输入
        public bool AxisExists(string name)
        {
            return m_VirtualAxes.ContainsKey(name);
        }

        // 判断是否有名为name的按键输入
        public bool ButtonExists(string name)
        {
            return m_VirtualButtons.ContainsKey(name);
        }

        // 在使用之前需要注册，这个工作是由输入控制器完成的
        // 相当于声明变量，在上述缓冲区中开辟一个区域，才能以id进行写入和读出

        // 注册一个轴输入
        public void RegisterVirtualAxis(CrossPlatformInputManager.VirtualAxis axis)
        {
            // id不可重复
            // check if we already have an axis with that name and log and error if we do
            if (m_VirtualAxes.ContainsKey(axis.name))
            {
                Debug.LogError("There is already a virtual axis named " + axis.name + " registered.");
            }
            else
            {
                // 注册
                // add any new axes
                m_VirtualAxes.Add(axis.name, axis);

                // 是否匹配InputManager，暂时没有发现有什么用处
                // if we dont want to match with the input manager setting then revert to always using virtual
                if (!axis.matchWithInputManager)
                {
                    m_AlwaysUseVirtual.Add(axis.name);
                }
            }
        }

        // 注册一个按键输入
        public void RegisterVirtualButton(CrossPlatformInputManager.VirtualButton button)
        {
            // check if already have a buttin with that name and log an error if we do
            if (m_VirtualButtons.ContainsKey(button.name))
            {
                Debug.LogError("There is already a virtual button named " + button.name + " registered.");
            }
            else
            {
                // add any new buttons
                m_VirtualButtons.Add(button.name, button);

                // if we dont want to match to the input manager then always use a virtual axis
                if (!button.matchWithInputManager)
                {
                    m_AlwaysUseVirtual.Add(button.name);
                }
            }
        }

        // 取消一个轴输入
        public void UnRegisterVirtualAxis(string name)
        {
            // if we have an axis with that name then remove it from our dictionary of registered axes
            if (m_VirtualAxes.ContainsKey(name))
            {
                m_VirtualAxes.Remove(name);
            }
        }

        // 取消一个按键输入
        public void UnRegisterVirtualButton(string name)
        {
            // if we have a button with this name then remove it from our dictionary of registered buttons
            if (m_VirtualButtons.ContainsKey(name))
            {
                m_VirtualButtons.Remove(name);
            }
        }

        // 根据id获取输入，如果不存在就返回null
        // returns a reference to a named virtual axis if it exists otherwise null
        public CrossPlatformInputManager.VirtualAxis VirtualAxisReference(string name)
        {
            return m_VirtualAxes.ContainsKey(name) ? m_VirtualAxes[name] : null;
        }

        // 设置虚拟光标的x坐标
        public void SetVirtualMousePositionX(float f)
        {
            virtualMousePosition = new Vector3(f, virtualMousePosition.y, virtualMousePosition.z);
        }

        // 设置虚拟光标的y坐标
        public void SetVirtualMousePositionY(float f)
        {
            virtualMousePosition = new Vector3(virtualMousePosition.x, f, virtualMousePosition.z);
        }

        // 设置虚拟光标的z坐标
        public void SetVirtualMousePositionZ(float f)
        {
            virtualMousePosition = new Vector3(virtualMousePosition.x, virtualMousePosition.y, f);
        }

        // 以下方法通过子类来具体实现
        public abstract float GetAxis(string name, bool raw);
        
        public abstract bool GetButton(string name);
        public abstract bool GetButtonDown(string name);
        public abstract bool GetButtonUp(string name);

        public abstract void SetButtonDown(string name);
        public abstract void SetButtonUp(string name);
        public abstract void SetAxisPositive(string name);
        public abstract void SetAxisNegative(string name);
        public abstract void SetAxisZero(string name);
        public abstract void SetAxis(string name, float value);
        public abstract Vector3 MousePosition();
    }
}
