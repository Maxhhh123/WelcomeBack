// VoiceControlChecker.cs
using UnityEngine;

public class VoiceControlChecker : MonoBehaviour
{
    [Header("电视配置")]
    public TaskType thisTask = TaskType.VoiceControl;
    public TVController tvController; // 电视控制器
    
    [Header("检测参数")]
    public GameObject tvModel; // 电视模型物体（用于检测激活状态）
    
    private TaskManager taskManager;
    private bool taskCompleted = false;
    private bool wasTVHidden = true; // 记录电视之前的状态（默认隐藏）
    
    void Start()
    {
        taskManager = FindObjectOfType<TaskManager>();
        
        // 如果没分配 tvModel，尝试从 tvController 获取
        if (tvModel == null && tvController != null)
        {
            tvModel = tvController.tvModel;
        }
        
        // 初始化电视状态
        if (tvModel != null)
        {
            wasTVHidden = !tvModel.activeSelf;
        }
    }
    
    void Update()
    {
        if (taskCompleted) return;
        
        // 检查当前任务
        if (taskManager != null && taskManager.currentTask == thisTask)
        {
            CheckTVStateChanged();
        }
    }
    
    void CheckTVStateChanged()
    {
        if (tvModel == null) return;
        
        bool isTVCurrentlyVisible = tvModel.activeSelf;
        
        // 检测电视是否从隐藏变为显示
        if (wasTVHidden && isTVCurrentlyVisible)
        {
            CompleteTask();
        }
        
        // 更新状态记录
        wasTVHidden = !isTVCurrentlyVisible;
    }
    
    void CompleteTask()
    {
        taskCompleted = true;
        Debug.Log("✅ 语音控制任务完成：电视已打开");
        
        if (taskManager != null)
        {
            taskManager.AdvanceToNextTask(thisTask);
        }
    }
}
