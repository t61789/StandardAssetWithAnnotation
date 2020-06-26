using System;
using UnityEngine;


namespace UnityStandardAssets.Cameras
{
    public abstract class PivotBasedCameraRig : AbstractTargetFollower
    {
        // 这个类没干太多的事情，仅仅是获取锚点物体和摄像机
        // This script is designed to be placed on the root object of a camera rig,
        // comprising 3 gameobjects, each parented to the next:

        // 场景中的物体结构，CameraRig是脚本挂载的对象，Camera是真正的摄像机
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
