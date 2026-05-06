// DoorTaskChecker.cs
using UnityEngine;

public class DoorTaskChecker : MonoBehaviour
{
    [Header("门配置")]
    public TaskType thisTask = TaskType.OpenDoor;
    public float openAngle = 90f; // 门打开的角度阈值
    public bool requirePlayerPass = true; // 是否需要玩家通过
    
    private TaskManager taskManager;
    private bool taskCompleted = false;
    private HingeJoint doorHinge;
    
    void Start()
    {
        taskManager = FindObjectOfType<TaskManager>();
        doorHinge = GetComponent<HingeJoint>();
    }
    
    void Update()
    {
        if (taskCompleted) return;
        
        // 检查当前任务是否是开门任务
        if (taskManager != null && taskManager.currentTask == thisTask)
        {
            // 检查门是否打开到足够角度
            float currentAngle = GetDoorAngle();
            if (Mathf.Abs(currentAngle) >= openAngle)
            {
                CompleteTask();
            }
        }
    }
    
    float GetDoorAngle()
    {
        // 这里根据您的门实现方式调整
        // 如果是使用铰链关节
        if (doorHinge != null)
        {
            return doorHinge.angle;
        }
        
        // 如果是简单旋转门
        return transform.localEulerAngles.y;
    }
    
    void CompleteTask()
    {
        taskCompleted = true;
        if (taskManager != null)
        {
            taskManager.AdvanceToNextTask(thisTask);
        }
    }
}