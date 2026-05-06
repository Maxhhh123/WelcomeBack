// SceneStateData.cs
// 场景状态数据容器
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ObjectStateData
{
    public string objectName;
    public string objectTag;
    public int objectInstanceId;
    
    // 变换数据
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    
    // 状态
    public bool isActive;
    
    // Rigidbody数据（如果有）
    public bool hasRigidbody;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public bool isKinematic;
}

[Serializable]
public class SceneStateData
{
    public string recordTime;
    public List<ObjectStateData> objectStates = new List<ObjectStateData>();
    
    // 可以扩展：玩家数据、游戏进度等
    public string playerTag = "Player";
    public Vector3 playerPosition;
    public Quaternion playerRotation;
}
