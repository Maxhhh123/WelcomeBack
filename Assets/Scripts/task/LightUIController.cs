// LightUIController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LightUIController : MonoBehaviour
{
    [Header("灯光控制")]
    public LightController lightController; // 灯光控制器引用
    public Light targetLight; // 目标灯泡
    
    [Header("UI 组件")]
    public Toggle lightToggle; // 开关 Toggle
    public TextMeshProUGUI toggleText; // Toggle 旁边的文字（显示"开"/"关"）
    public GameObject lightObject; // 灯泡 GameObject（用于控制显示/隐藏）
    
    [Header("亮度滑块")]
    public Slider brightnessSlider; // 亮度 Slider
    public TextMeshProUGUI intensityText; // 显示当前亮度数值的 TextMeshPro
    
    [Header("任务配置")]
    public float targetIntensity = 6f; // 目标亮度值
    public float tolerance = 0.5f; // 允许的误差范围
    public float minBrightness = 0.1f; // 最小亮度阈值（低于此值认为灯是关的）
    
    private bool isLightOn = false; // 灯的开关状态
    private bool taskCompleted = false; // 任务是否已完成
    private TaskManager taskManager; // 任务管理器引用
    
    void Start()
    {
        // 获取 TaskManager 引用
        taskManager = FindObjectOfType<TaskManager>();
        
        // 验证必要组件
        if (lightController == null)
        {
            Debug.LogError("请分配 LightController 引用！");
        }
        
        if (targetLight == null && lightController != null)
        {
            targetLight = lightController.targetLight;
        }
        
        // 初始化 UI 状态
        InitializeUI();
        
        // 设置事件监听
        SetupEventListeners();
    }
    
    void InitializeUI()
    {
        // 初始时灯是关闭的
        isLightOn = false;
        
        // 隐藏灯泡（如果提供了 GameObject）
        if (lightObject != null)
        {
            lightObject.SetActive(false);
        }
        
        // 确保 Toggle 初始为关闭状态
        if (lightToggle != null)
        {
            lightToggle.isOn = false;
        }
        
        // 更新 Toggle 文字
        UpdateToggleText();
        
        // 初始化 Slider（如果提供了）
        if (brightnessSlider != null)
        {
            brightnessSlider.value = 0f; // 初始亮度为 0
            UpdateIntensityText(0f);
        }
        
        // 确保灯初始是关闭的
        if (lightController != null)
        {
            lightController.TurnOff();
        }
    }
    
    void SetupEventListeners()
    {
        // Toggle 值改变事件
        if (lightToggle != null)
        {
            lightToggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
        
        // Slider 值改变事件
        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }
    
    // Toggle 值改变时的处理
    public void OnToggleValueChanged(bool isOn)
    {
        isLightOn = isOn;
        
        // 更新 Toggle 文字
        UpdateToggleText();
        
        // 控制灯泡显示/隐藏
        if (lightObject != null)
        {
            lightObject.SetActive(isOn);
        }
        
        // 如果 Toggle 打开，但 Slider 值为 0，给一个默认亮度
        if (isOn && brightnessSlider != null && brightnessSlider.value <= 0f)
        {
            brightnessSlider.value = 0.5f; // 默认 50% 亮度
        }
        
        // 更新灯光
        UpdateLight();
        
        // 检查任务完成条件
        CheckTaskCompletion();
    }
    
    // Slider 值改变时的处理
    public void OnSliderValueChanged(float value)
    {
        // 更新强度文字显示
        UpdateIntensityText(value);
        
        // 如果灯是开着的，更新灯光亮度
        if (isLightOn)
        {
            UpdateLight();
        }
        
        // 检查任务完成条件
        CheckTaskCompletion();
    }
    
    // 更新灯光状态
    void UpdateLight()
    {
        if (lightController == null) return;
        
        if (isLightOn && brightnessSlider != null)
        {
            // 根据 Slider 值设置亮度（归一化到 0-1）
            float normalizedBrightness = brightnessSlider.value / brightnessSlider.maxValue;
            lightController.SetBrightness(normalizedBrightness);
        }
        else
        {
            // 灯关闭
            lightController.TurnOff();
        }
    }
    
    // 更新 Toggle 文字
    void UpdateToggleText()
    {
        if (toggleText != null)
        {
            toggleText.text = isLightOn ? "开" : "关";
        }
    }
    
    // 更新强度文字显示
    void UpdateIntensityText(float sliderValue)
    {
        if (intensityText == null || brightnessSlider == null) return;
        
        // 计算实际亮度值（假设 Slider 的 maxValue 对应 maxIntensity）
        float actualIntensity = (sliderValue / brightnessSlider.maxValue) * lightController.maxIntensity;
        
        // 显示亮度值（保留一位小数）
        intensityText.text = actualIntensity.ToString("F1");
    }
    
    // 检查任务是否完成
    void CheckTaskCompletion()
    {
        // 如果任务已经完成，就不再检查
        if (taskCompleted) return;
        
        // 确保当前任务是 AdjustLight
        if (taskManager == null || taskManager.currentTask != TaskType.AdjustLight) return;
        
        // 获取当前实际亮度
        float currentIntensity = GetCurrentLightIntensity();
        
        // 检查是否达到目标亮度
        if (Mathf.Abs(currentIntensity - targetIntensity) <= tolerance)
        {
            CompleteTask();
        }
    }
    
    // 获取当前灯光的实际亮度
    float GetCurrentLightIntensity()
    {
        if (targetLight != null && isLightOn)
        {
            return targetLight.intensity;
        }
        return 0f;
    }
    
    // 完成任务
    void CompleteTask()
    {
        taskCompleted = true;
        Debug.Log($"任务完成！灯泡亮度已调整到目标值 {targetIntensity}");
        
        if (taskManager != null)
        {
            taskManager.AdvanceToNextTask(TaskType.AdjustLight);
        }
    }
    
    // 清理事件监听
    void OnDestroy()
    {
        if (lightToggle != null)
        {
            lightToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
        
        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }
}
