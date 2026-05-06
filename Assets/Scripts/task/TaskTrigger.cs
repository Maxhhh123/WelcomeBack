using UnityEngine;

public class TaskTrigger : MonoBehaviour
{
    [Header("触发器配置")]
    public TaskType triggerTask; // 这个触发器负责哪个任务
    public AudioClip startVoice; // 触发时播放的语音
    public bool destroyAfterTrigger = true; // 触发后是否销毁
    
    private TaskManager taskManager;
    private bool hasTriggered = false;
    
    void Start()
    {
        taskManager = FindObjectOfType<TaskManager>();
        if (taskManager == null)
        {
            Debug.LogError("场景中找不到TaskManager！");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // 确保只有 XR Origin 或 Player 触发
        if (!IsXRPlayer(other))
            return;
            
        if (hasTriggered) return;
        
        // 检查当前任务是否匹配
        if (taskManager != null && taskManager.currentTask == triggerTask)
        {
            // 播放语音
            if (startVoice != null)
            {
                taskManager.PlayVoiceClip(startVoice);
            }

            taskManager.AdvanceToNextTask(triggerTask);
   
            
            hasTriggered = true;
            
            // 可选：触发后是否销毁碰撞体
            if (destroyAfterTrigger)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 检查碰撞体是否是 XR 玩家
    /// </summary>
    private bool IsXRPlayer(Collider other)
    {
        // 方法 1：检查 Tag（如果定义了的话）
        if (other.CompareTag("Player") )
        {
            return true;
        }
        
        // 方法 3：检查是否有 CharacterController 组件
        if (other.GetComponentInParent<CharacterController>() != null)
        {
            return true;
        }
        
        // 方法 4：检查名称是否包含 "XR" 或 "Player"
        string name = other.gameObject.name.ToUpper();
        if (name.Contains("XR") || name.Contains("PLAYER") || name.Contains("ORIGIN"))
        {
            return true;
        }
        
        return false;
    }
}