// SceneStateRecorder.cs
// 场景状态记录器 - 单例模式
using System.Collections.Generic;
using UnityEngine;

public class SceneStateRecorder : MonoBehaviour
{
    [Header("记录设置")]
    [Tooltip("要记录的物体标签（留空则记录所有可记录物体）")]
    public List<string> recordableTags = new List<string>();
    
    [Tooltip("是否记录Rigidbody物理状态")]
    public bool recordPhysics = true;
    
    [Tooltip("最大记录数量（循环覆盖）")]
    public int maxRecordCount = 5;
    
    [Header("调试")]
    public bool showDebugLog = true;

    // 状态记录队列
    private Queue<SceneStateData> stateHistory = new Queue<SceneStateData>();
    
    // 单例实例
    public static SceneStateRecorder Instance { get; private set; }

    // 回溯完成事件
    public static event System.Action OnRewindCompleted;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 设置默认记录标签
        if (recordableTags.Count == 0)
        {
            recordableTags.Add("Player");
            recordableTags.Add("Grabbable");
            recordableTags.Add("Movable");
        }
    }

    /// <summary>
    /// 记录当前场景状态
    /// </summary>
    public void RecordSceneState()
    {
        SceneStateData stateData = new SceneStateData();
        stateData.recordTime = System.DateTime.Now.ToString("HH:mm:ss.fff");
        
        // 查找所有可记录的物体
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // 跳过不需要记录的对象
            if (!ShouldRecordObject(obj)) continue;
            
            ObjectStateData objectState = CaptureObjectState(obj);
            if (objectState != null)
            {
                stateData.objectStates.Add(objectState);
                
                // 单独记录玩家位置
                if (obj.CompareTag("Player"))
                {
                    stateData.playerPosition = obj.transform.position;
                    stateData.playerRotation = obj.transform.rotation;
                }
            }
        }
        
        // 添加到历史记录
        stateHistory.Enqueue(stateData);
        
        // 超过最大数量时移除最旧的记录
        if (stateHistory.Count > maxRecordCount)
        {
            stateHistory.Dequeue();
        }
        
        if (showDebugLog)
        {
            Debug.Log($"[场景记录] 已记录 {stateData.objectStates.Count} 个物体 | 时间: {stateData.recordTime} | 历史记录数: {stateHistory.Count}");
        }
    }

    /// <summary>
    /// 回溯到最近记录的状态
    /// </summary>
    public void RewindToLastState()
    {
        if (stateHistory.Count == 0)
        {
            Debug.LogWarning("[场景回溯] 没有可回溯的记录！");
            return;
        }
        
        // 获取最近的记录（不移除，可以重复回溯）
        SceneStateData stateToRestore = stateHistory.Peek();
        
        RestoreSceneState(stateToRestore);

        // 触发回溯完成事件
        OnRewindCompleted?.Invoke();
        
        if (showDebugLog)
        {
            Debug.Log($"[场景回溯] 已回溯到记录时间: {stateToRestore.recordTime} | 可用记录数: {stateHistory.Count}");
        }
    }

    /// <summary>
    /// 删除最近的一次记录（如果需要消耗回溯次数）
    /// </summary>
    public void ConsumeLastRecord()
    {
        if (stateHistory.Count > 0)
        {
            stateHistory.Dequeue();
            Debug.Log("[场景记录] 已消耗一次记录");
        }
    }

    /// <summary>
    /// 回溯到指定的记录
    /// </summary>
    public void RewindToState(SceneStateData stateData)
    {
        if (stateData == null) return;
        
        RestoreSceneState(stateData);
        
        // 清空该记录之后的所有记录
        while (stateHistory.Count > 0 && stateHistory.Peek() != stateData)
        {
            stateHistory.Dequeue();
        }
    }

    /// <summary>
    /// 恢复场景状态
    /// </summary>
    private void RestoreSceneState(SceneStateData stateData)
    {
        Dictionary<int, GameObject> objectMap = BuildObjectInstanceMap();
        HashSet<int> restoredObjectIds = new HashSet<int>();
        
        // 第一步：恢复记录在数据中的物体
        foreach (ObjectStateData objectState in stateData.objectStates)
        {
            // 尝试通过InstanceID找到原对象
            if (objectMap.TryGetValue(objectState.objectInstanceId, out GameObject obj))
            {
                ApplyObjectState(obj, objectState);
                restoredObjectIds.Add(obj.GetInstanceID());
            }
            else
            {
                // 如果找不到，尝试通过名称查找
                GameObject foundObj = GameObject.Find(objectState.objectName);
                if (foundObj != null)
                {
                    ApplyObjectState(foundObj, objectState);
                    restoredObjectIds.Add(foundObj.GetInstanceID());
                }
            }
        }
        
        // 第二步：禁用那些不在记录中但带有记录标签的物体
        // （这些是在记录之后才创建/激活的物体）
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            // 跳过已经恢复的物体
            if (restoredObjectIds.Contains(obj.GetInstanceID())) continue;
            
            // 只处理带有记录标签的物体
            if (ShouldRecordObject(obj))
            {
                // 这个物体在记录时不存在（或不应该存在），禁用它
                if (obj.activeSelf)
                {
                    obj.SetActive(false);
                    if (showDebugLog)
                    {
                        Debug.Log($"[场景回溯] 禁用记录后创建的物体: {obj.name}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 捕获单个物体的状态
    /// </summary>
    private ObjectStateData CaptureObjectState(GameObject obj)
    {
        ObjectStateData data = new ObjectStateData
        {
            objectName = obj.name,
            objectTag = obj.tag,
            objectInstanceId = obj.GetInstanceID(),
            position = obj.transform.position,
            rotation = obj.transform.rotation,
            scale = obj.transform.localScale,
            isActive = obj.activeSelf
        };

        // 记录Rigidbody状态
        if (recordPhysics)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                data.hasRigidbody = true;
                data.velocity = rb.velocity;
                data.angularVelocity = rb.angularVelocity;
                data.isKinematic = rb.isKinematic;
            }
        }

        return data;
    }

    /// <summary>
    /// 应用状态到物体
    /// </summary>
    private void ApplyObjectState(GameObject obj, ObjectStateData stateData)
    {
        if (obj == null) return;

        // 应用变换
        obj.transform.position = stateData.position;
        obj.transform.rotation = stateData.rotation;
        obj.transform.localScale = stateData.scale;
        obj.SetActive(stateData.isActive);

        // 应用物理状态
        if (recordPhysics && stateData.hasRigidbody)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = stateData.velocity;
                rb.angularVelocity = stateData.angularVelocity;
                rb.isKinematic = stateData.isKinematic;
            }
        }
    }

    /// <summary>
    /// 判断是否应该记录该物体
    /// </summary>
    private bool ShouldRecordObject(GameObject obj)
    {
        // 不记录场景摄像机、灯光等系统对象
        if (obj.hideFlags != HideFlags.None) return false;
        
        // 不记录没有Transform的对象（理论上不会发生）
        if (obj.transform == null) return false;
        
        // 如果设置了特定标签，只记录这些标签的物体
        if (recordableTags.Count > 0)
        {
            foreach (string tag in recordableTags)
            {
                if (obj.CompareTag(tag)) return true;
            }
            return false;
        }
        
        // 默认记录所有非静态、非UI的物体
        return !obj.isStatic && obj.layer != LayerMask.NameToLayer("UI");
    }

    /// <summary>
    /// 构建物体实例ID映射表
    /// </summary>
    private Dictionary<int, GameObject> BuildObjectInstanceMap()
    {
        Dictionary<int, GameObject> map = new Dictionary<int, GameObject>();
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            int instanceId = obj.GetInstanceID();
            if (!map.ContainsKey(instanceId))
            {
                map.Add(instanceId, obj);
            }
        }
        
        return map;
    }

    /// <summary>
    /// 清空所有记录
    /// </summary>
    public void ClearAllRecords()
    {
        stateHistory.Clear();
        Debug.Log("[场景记录] 已清空所有记录");
    }

    /// <summary>
    /// 获取当前记录数量
    /// </summary>
    public int GetRecordCount()
    {
        return stateHistory.Count;
    }

    /// <summary>
    /// 检查是否有可回溯的记录
    /// </summary>
    public bool HasRecords()
    {
        return stateHistory.Count > 0;
    }
}
