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
        // 每次启用时设置文本为当前视角名
        text.text = objects[m_CurrentActiveObject].name;
    }


    public void NextCamera()
    {
        // 循环切换下一个视角，其实用模length的方式更简单
        int nextactiveobject = m_CurrentActiveObject + 1 >= objects.Length ? 0 : m_CurrentActiveObject + 1;

        // 将除了当前选择视角之外的摄像机设为非活动
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i].SetActive(i == nextactiveobject);
        }

        // 设置当前摄像机
        m_CurrentActiveObject = nextactiveobject;
        // 设置文本名
        text.text = objects[m_CurrentActiveObject].name;
    }
}
