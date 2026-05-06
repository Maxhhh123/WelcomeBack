
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 电视 UI 控制器
/// 通过 UI 界面控制电视的开关、音量和频道
/// </summary>
public class TVUIController : MonoBehaviour
{
    [Header("UI 组件引用")]
    
    [Tooltip("控制电视开关的 Toggle")]
    public Toggle powerToggle;
    
    public TextMeshProUGUI toggleText;
    
    [Tooltip("控制音量的 Slider")]
    public Slider volumeSlider;
    
    
    
    [Tooltip("显示音量数值的 Text")]
    public TextMeshProUGUI volumeText;
    
    [Tooltip("频道 + 按钮")]
    public Button channelPlusButton;
    
    [Tooltip("频道 - 按钮")]
    public Button channelMinusButton;
    
    [Tooltip("显示频道数值的 Text")]
    public TextMeshProUGUI channelText;
    
    [Header("电视控制器引用")]
    [Tooltip("TVController 组件")]
    public TVController tvController;
    
    // 当前频道索引（从 1 开始）
    private int currentChannelIndex = 1;

    void Start()
    {
        // 自动获取组件（如果 Inspector 中没设置）
        if (tvController == null)
        {
            tvController = FindObjectOfType<TVController>();
        }
        
        if (tvController == null)
        {
            Debug.LogError("❌ 未找到 TVController 组件！");
            return;
        }
        
        // 初始化 UI 状态
        InitializeUI();
        
        // 绑定 UI 事件
        SetupUIEvents();
    }

    /// <summary>
    /// 初始化 UI 状态
    /// </summary>
    void InitializeUI()
    {
        // 初始化 Toggle（默认关闭）
        if (powerToggle != null)
        {
            powerToggle.isOn = false;
        }
        
        // 初始化 Slider（默认音量 50%）
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = tvController.GetCurrentVolume();
        }
        
        // 初始化音量文本
        if (volumeText != null)
        {
            UpdateVolumeText(tvController.GetCurrentVolume());
        }
        
        // 初始化频道文本（默认频道 1）
        if (channelText != null)
        {
            currentChannelIndex = 1;
            UpdateChannelText();
        }
        
        // 初始化按钮状态（电视关闭时禁用频道按钮）
        UpdateButtonInteractable();
    }

    /// <summary>
    /// 绑定 UI 事件
    /// </summary>
    void SetupUIEvents()
    {
        // Toggle 事件 - 电视开关
        if (powerToggle != null)
        {
            powerToggle.onValueChanged.AddListener(OnPowerToggleChanged);
        }
        
        // Slider 事件 - 音量调节
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
        }
        
        // Button 事件 - 频道切换
        if (channelPlusButton != null)
        {
            channelPlusButton.onClick.AddListener(OnChannelPlusClicked);
        }
        
        if (channelMinusButton != null)
        {
            channelMinusButton.onClick.AddListener(OnChannelMinusClicked);
        }
    }

    #region UI 事件处理

    /// <summary>
    /// Toggle 状态改变（电视开关）
    /// </summary>
    void OnPowerToggleChanged(bool isOn)
    {
        if (tvController == null) return;
        
        if (isOn)
        {
            // 打开电视
            tvController.PowerOn();
            toggleText.text = "开" ;
            
            // 播放第一个频道
            if (tvController.channelMappings.Length > 0)
            {
                tvController.ChangeChannel(tvController.channelMappings[0].channelName);
                
            }
        }
        else
        {
            // 关闭电视
            tvController.PowerOff();
            toggleText.text = "关" ;
        }
        
        // 更新按钮可用状态
        UpdateButtonInteractable();
    }

    /// <summary>
    /// Slider 值改变（音量调节）
    /// </summary>
    void OnVolumeSliderChanged(float value)
    {
        if (tvController == null) return;
        
        // 通过 AudioSource 直接设置音量
        if (tvController.videoAudioSource != null)
        {
            tvController.videoAudioSource.volume = Mathf.Clamp01(value);
        }
        
        // 更新音量文本显示
        UpdateVolumeText(value);
    }

    /// <summary>
    /// 频道 + 按钮点击
    /// </summary>
    void OnChannelPlusClicked()
    {
        if (tvController == null) return;
        
        // 频道索引 +1
        currentChannelIndex++;
        
        // 循环处理（超过最大频道数回到第一个）
        if (currentChannelIndex > tvController.channelMappings.Length)
        {
            currentChannelIndex = 1;
        }
        
        // 切换到对应频道
        ChangeToChannelByIndex(currentChannelIndex);
    }

    /// <summary>
    /// 频道 - 按钮点击
    /// </summary>
    void OnChannelMinusClicked()
    {
        if (tvController == null) return;
        
        // 频道索引 -1
        currentChannelIndex--;
        
        // 循环处理（小于 1 跳到最后一个）
        if (currentChannelIndex < 1)
        {
            currentChannelIndex = tvController.channelMappings.Length;
        }
        
        // 切换到对应频道
        ChangeToChannelByIndex(currentChannelIndex);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 根据频道索引切换频道
    /// </summary>
    void ChangeToChannelByIndex(int index)
    {
        if (index < 1 || index > tvController.channelMappings.Length)
        {
            Debug.LogWarning($"❌ 频道索引超出范围：{index}");
            return;
        }
        
        // 获取对应频道的名称
        string channelName = tvController.channelMappings[index - 1].channelName;
        
        // 切换频道
        tvController.ChangeChannel(channelName);
        
        // 更新 UI 显示
        UpdateChannelText();
        
        Debug.Log($"📺 切换到频道 {index}: {channelName}");
    }

    /// <summary>
    /// 更新音量文本显示
    /// </summary>
    void UpdateVolumeText(float volume)
    {
        if (volumeText != null)
        {
            // 将 0-1 的范围转换为 0-100 的百分比
            int percentage = Mathf.RoundToInt(volume * 100);
            volumeText.text = $"{percentage}%";
        }
    }

    /// <summary>
    /// 更新频道文本显示
    /// </summary>
    void UpdateChannelText()
    {
        if (channelText != null && tvController.channelMappings.Length > 0)
        {
            // 显示格式："频道 X" 或直接显示数字
            channelText.text = currentChannelIndex.ToString();
        }
    }

    /// <summary>
    /// 更新按钮的可交互状态
    /// </summary>
    void UpdateButtonInteractable()
    {
        bool isTVOn = tvController.IsTVOn();
        
        // 电视关闭时，禁用音量和频道控制
        if (volumeSlider != null)
        {
            volumeSlider.interactable = isTVOn;
        }
        
        if (channelPlusButton != null)
        {
            channelPlusButton.interactable = isTVOn;
        }
        
        if (channelMinusButton != null)
        {
            channelMinusButton.interactable = isTVOn;
        }
    }

    #endregion

    #region 公开方法（供其他脚本调用）

    /// <summary>
    /// 从外部更新 UI 状态（例如语音控制后同步 UI）
    /// </summary>
    public void SyncUIWithTVState()
    {
        if (tvController == null) return;
        
        // 同步开关状态
        if (powerToggle != null)
        {
            powerToggle.onValueChanged.RemoveAllListeners();
            powerToggle.isOn = tvController.IsTVOn();
            SetupUIEvents(); // 重新绑定事件
        }
        
        // 同步音量
        if (volumeSlider != null && tvController.videoAudioSource != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.value = tvController.videoAudioSource.volume;
            UpdateVolumeText(tvController.videoAudioSource.volume);
            SetupUIEvents();
        }
        
        // 更新按钮状态
        UpdateButtonInteractable();
    }

    #endregion

    void OnDestroy()
    {
        // 清理事件监听（防止内存泄漏）
        if (powerToggle != null)
        {
            powerToggle.onValueChanged.RemoveListener(OnPowerToggleChanged);
        }
        
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
        }
        
        if (channelPlusButton != null)
        {
            channelPlusButton.onClick.RemoveListener(OnChannelPlusClicked);
        }
        
        if (channelMinusButton != null)
        {
            channelMinusButton.onClick.RemoveListener(OnChannelMinusClicked);
        }
    }
}