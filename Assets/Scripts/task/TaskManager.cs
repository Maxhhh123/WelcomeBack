
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro; // 如果使用TextMeshPro

public class TaskManager : MonoBehaviour
{
    [Header("任务配置")]
    public TaskType currentTask = TaskType.None;
    public TextMeshProUGUI taskDisplayText; // UI上的任务文字
    
    [Header("任务完成条件参数")]
    public Light targetLight; // 需要控制的灯泡
    public float targetIntensity = 6f;// 目标亮度
    
    [Header("UI 控制（第三个任务用）")]
    public LightUIController lightUIController; // 新增：灯光 UI 控制器
    
    [Header("事件系统")]
    public UnityEvent onTaskCompleted; // 通用任务完成事件
    public UnityEvent onAllTasksCompleted; // 所有任务完成事件
    
    // 任务状态字典（可选，用于更复杂的状态管理）
    private Dictionary<TaskType, bool> taskCompletionStatus = new Dictionary<TaskType, bool>();
    
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        InitializeTasks();
    }
    
    void InitializeTasks()
    {
        // 初始化任务状态
        foreach (TaskType task in System.Enum.GetValues(typeof(TaskType)))
        {
            taskCompletionStatus[task] = false;
        }
        
        // 设置初始任务
        currentTask = TaskType.StartGame;
    }
    
    // 推进到下一个任务
    public void AdvanceToNextTask(TaskType completedTask)
    {
        // 验证当前完成的任务是否正确
        if (completedTask != currentTask)
        {
            Debug.LogWarning($"任务顺序错误：期望 {currentTask}，但完成了 {completedTask}");
            return;
        }
        
        // 标记当前任务为已完成
        taskCompletionStatus[currentTask] = true;
        
        // 触发任务完成事件
        onTaskCompleted?.Invoke();
        
        // 切换到下一个任务
        switch (currentTask)
        {
            case TaskType.StartGame:
                currentTask = TaskType.OpenDoor;
                UpdateTaskUI("任务1：请打开前方的门");
                break;
                
            case TaskType.OpenDoor:
                currentTask = TaskType.PlaceRouter;
                UpdateTaskUI("任务2：打开背包，将路由器放置到客厅的指定位置，通过它将所有物联网设备接入互联网并彼此通信。");
                break;
                
            case TaskType.PlaceRouter:
                currentTask = TaskType.AdjustLight;
                UpdateTaskUI($"任务3：打开手机，将灯泡亮度调到 {targetIntensity * 100}%");
                break;
                
            case TaskType.AdjustLight:
                currentTask = TaskType.PlaceSpeaker;
                UpdateTaskUI("任务4：将小爱音箱安装到茶几上");
                break;
                
            case TaskType.PlaceSpeaker:
                currentTask = TaskType.VoiceControl;
                UpdateTaskUI("任务5：请对小爱音箱说“打开电视”");
                break;
                
            case TaskType.VoiceControl:
                // 所有任务完成
                currentTask = TaskType.None;
                UpdateTaskUI("恭喜你！所有任务完成！");
                onAllTasksCompleted?.Invoke();
                
                // 10 秒后隐藏任务文本
                Invoke(nameof(HideTaskText), 10f);
                break;
        }
    }
    
    // 更新 UI 显示
    void UpdateTaskUI(string taskText)
    {
        taskDisplayText.gameObject.SetActive(true);
        if (taskDisplayText != null)
        {
            taskDisplayText.text = taskText;
        }
        
        // 可选：播放任务更新的提示音
        Debug.Log($"任务更新：{taskText}");
    }
    
    // 隐藏任务文本
    void HideTaskText()
    {
        if (taskDisplayText != null)
        {
            taskDisplayText.gameObject.SetActive(false);
        }
    }
    
    // 播放语音（由触发器调用）
    public void PlayVoiceClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // 检查当前任务是否完成（用于条件性任务）
    public bool IsTaskCompleted(TaskType task)
    {
        return taskCompletionStatus.ContainsKey(task) && taskCompletionStatus[task];
    }
}