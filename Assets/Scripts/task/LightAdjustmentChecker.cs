﻿// LightAdjustmentChecker.cs
using UnityEngine;
using UnityEngine.UI;

public class LightAdjustmentChecker : MonoBehaviour
{
    [Header("灯光配置")]
    public TaskType thisTask = TaskType.AdjustLight;
    public Light targetLight; // 您的灯泡控制脚本
    public float targetIntensity = 2.4f; // 目标亮度
    public float tolerance = 0.1f; // 允许误差
    
    [Header("UI控制（可选）")]
    public Slider brightnessSlider; // 亮度调节滑块
    
    private TaskManager taskManager;
    private bool taskCompleted = false;
    
    void Start()
    {
        taskManager = FindObjectOfType<TaskManager>();
        
        // 如果使用Slider，订阅其事件
        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }
    }
    
    void Update()
    {
        if (taskCompleted) return;
        
        // 检查当前任务
        if (taskManager != null && taskManager.currentTask == thisTask)
        {
            // 获取当前亮度（根据您的LightController实现）
            float currentBrightness = GetCurrentBrightness();
            
            // 检查是否达到目标
            if (Mathf.Abs(currentBrightness - targetIntensity) <= tolerance)
            {
                CompleteTask();
            }
        }
    }
    
    float GetCurrentBrightness()
    {
        if (targetLight != null)
        {
            return targetLight.intensity;
        }
        
        return 0f;
    }
    
    void OnBrightnessChanged(float value)
    {
        // 如果通过Slider控制，可以在这里触发检查
        // 但Update中已经持续检查，所以不是必须的
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