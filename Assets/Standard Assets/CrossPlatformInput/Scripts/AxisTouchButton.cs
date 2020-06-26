using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityStandardAssets.CrossPlatformInput
{
	public class AxisTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		// 说是被设计成成对使用的按钮，一个固定值-1，一个固定值1，就像前进按钮和后退按钮，感觉没啥必要
		// designed to work in a pair with another axis touch button
		// (typically with one having -1 and one having 1 axisValues)
		public string axisName = "Horizontal"; // The name of the axis // 轴的id
		public float axisValue = 1; // The axis that the value has // 被按下后输出的固定值
		public float responseSpeed = 3; // The speed at which the axis touch button responds // 朝着固定值移动的速度
		public float returnToCentreSpeed = 3; // The speed at which the button will return to its centre // 说是返回0的速度，但没有被调用

		AxisTouchButton m_PairedWith; // Which button this one is paired with // 一对按钮中的另一个
		CrossPlatformInputManager.VirtualAxis m_Axis; // A reference to the virtual axis as it is in the cross platform input // 储存轴信息的类

		void OnEnable()
		{
			// 未被注册就创建存储类并注册
			if (!CrossPlatformInputManager.AxisExists(axisName))
			{
				// if the axis doesnt exist create a new one in cross platform input
				m_Axis = new CrossPlatformInputManager.VirtualAxis(axisName);
				CrossPlatformInputManager.RegisterVirtualAxis(m_Axis);
			}
			else
			{
                // 已被注册就直接获取
				m_Axis = CrossPlatformInputManager.VirtualAxisReference(axisName);
			}
			FindPairedButton(); // 找另外一个按钮
		}

		void FindPairedButton()
		{
			// 寻找轴id相同的另一个按钮
			// find the other button witch which this button should be paired
			// (it should have the same axisName)
			var otherAxisButtons = FindObjectsOfType(typeof(AxisTouchButton)) as AxisTouchButton[];

			// 获取所有AxisTouchButton对象，遍历查找轴id相同的按钮
			if (otherAxisButtons != null)
			{
				for (int i = 0; i < otherAxisButtons.Length; i++)
				{
					if (otherAxisButtons[i].axisName == axisName && otherAxisButtons[i] != this)
					{
						m_PairedWith = otherAxisButtons[i];
					}
				}
			}
		}

		void OnDisable()
		{
			// The object is disabled so remove it from the cross platform input system
			m_Axis.Remove();
		}

		// 使当前值朝着固定值移动
		public void OnPointerDown(PointerEventData data)
		{
			if (m_PairedWith == null)
			{
				FindPairedButton();
			}
			// update the axis and record that the button has been pressed this frame
			m_Axis.Update(Mathf.MoveTowards(m_Axis.GetValue, axisValue, responseSpeed * Time.deltaTime));
		}

		// 使当前值朝着0移动
		public void OnPointerUp(PointerEventData data)
		{
			m_Axis.Update(Mathf.MoveTowards(m_Axis.GetValue, 0, responseSpeed * Time.deltaTime));
		}
	}
}