using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityStandardAssets.CrossPlatformInput
{
	public class AxisTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		// ˵�Ǳ���Ƴɳɶ�ʹ�õİ�ť��һ���̶�ֵ-1��һ���̶�ֵ1������ǰ����ť�ͺ��˰�ť���о�ûɶ��Ҫ
		// designed to work in a pair with another axis touch button
		// (typically with one having -1 and one having 1 axisValues)
		public string axisName = "Horizontal"; // The name of the axis // ���id
		public float axisValue = 1; // The axis that the value has // �����º�����Ĺ̶�ֵ
		public float responseSpeed = 3; // The speed at which the axis touch button responds // ���Ź̶�ֵ�ƶ����ٶ�
		public float returnToCentreSpeed = 3; // The speed at which the button will return to its centre // ˵�Ƿ���0���ٶȣ���û�б�����

		AxisTouchButton m_PairedWith; // Which button this one is paired with // һ�԰�ť�е���һ��
		CrossPlatformInputManager.VirtualAxis m_Axis; // A reference to the virtual axis as it is in the cross platform input // ��������Ϣ����

		void OnEnable()
		{
			// δ��ע��ʹ����洢�ಢע��
			if (!CrossPlatformInputManager.AxisExists(axisName))
			{
				// if the axis doesnt exist create a new one in cross platform input
				m_Axis = new CrossPlatformInputManager.VirtualAxis(axisName);
				CrossPlatformInputManager.RegisterVirtualAxis(m_Axis);
			}
			else
			{
                // �ѱ�ע���ֱ�ӻ�ȡ
				m_Axis = CrossPlatformInputManager.VirtualAxisReference(axisName);
			}
			FindPairedButton(); // ������һ����ť
		}

		void FindPairedButton()
		{
			// Ѱ����id��ͬ����һ����ť
			// find the other button witch which this button should be paired
			// (it should have the same axisName)
			var otherAxisButtons = FindObjectsOfType(typeof(AxisTouchButton)) as AxisTouchButton[];

			// ��ȡ����AxisTouchButton���󣬱���������id��ͬ�İ�ť
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

		// ʹ��ǰֵ���Ź̶�ֵ�ƶ�
		public void OnPointerDown(PointerEventData data)
		{
			if (m_PairedWith == null)
			{
				FindPairedButton();
			}
			// update the axis and record that the button has been pressed this frame
			m_Axis.Update(Mathf.MoveTowards(m_Axis.GetValue, axisValue, responseSpeed * Time.deltaTime));
		}

		// ʹ��ǰֵ����0�ƶ�
		public void OnPointerUp(PointerEventData data)
		{
			m_Axis.Update(Mathf.MoveTowards(m_Axis.GetValue, 0, responseSpeed * Time.deltaTime));
		}
	}
}