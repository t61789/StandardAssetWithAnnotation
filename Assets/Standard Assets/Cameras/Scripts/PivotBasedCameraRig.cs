using System;
using UnityEngine;


namespace UnityStandardAssets.Cameras
{
    public abstract class PivotBasedCameraRig : AbstractTargetFollower
    {
        // �����û��̫������飬�����ǻ�ȡê������������
        // This script is designed to be placed on the root object of a camera rig,
        // comprising 3 gameobjects, each parented to the next:

        // �����е�����ṹ��CameraRig�ǽű����صĶ���Camera�������������
        // 	Camera Rig
        // 		Pivot
        // 			Camera

        protected Transform m_Cam; // the transform of the camera
        protected Transform m_Pivot; // the point at which the camera pivots around
        protected Vector3 m_LastTargetPosition;


        protected virtual void Awake()
        {
            // find the camera in the object hierarchy
            m_Cam = GetComponentInChildren<Camera>().transform;
            m_Pivot = m_Cam.parent;
        }
    }
}
