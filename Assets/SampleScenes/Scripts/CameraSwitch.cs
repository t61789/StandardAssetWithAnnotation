using System;
using UnityEngine;
using UnityEngine.UI;

public class CameraSwitch : MonoBehaviour
{
    public GameObject[] objects;
    public Text text;

    private int m_CurrentActiveObject;


    private void OnEnable()
    {
        // ÿ������ʱ�����ı�Ϊ��ǰ�ӽ���
        text.text = objects[m_CurrentActiveObject].name;
    }


    public void NextCamera()
    {
        // ѭ���л���һ���ӽǣ���ʵ��ģlength�ķ�ʽ����
        int nextactiveobject = m_CurrentActiveObject + 1 >= objects.Length ? 0 : m_CurrentActiveObject + 1;

        // �����˵�ǰѡ���ӽ�֮����������Ϊ�ǻ
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i].SetActive(i == nextactiveobject);
        }

        // ���õ�ǰ�����
        m_CurrentActiveObject = nextactiveobject;
        // �����ı���
        text.text = objects[m_CurrentActiveObject].name;
    }
}
