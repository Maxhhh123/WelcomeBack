using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;

/// <summary>
/// 语音识别 UI 控制器
/// 通过 Button 控制语音识别的开始和停止，并显示状态
/// </summary>
public class VoiceRecognitionUI : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("控制语音识别的按钮")]
    public Button voiceButton;
    
    [Tooltip("按钮上的图标（可选）")]
    public Image buttonIcon;
    
    [Tooltip("按钮上的文本标签（可选）")]
    public TextMeshProUGUI buttonText;
    
    [Tooltip("显示当前状态的文本（可选）")]
    public TextMeshProUGUI statusText;
    
    [Header("图标设置")]
    [Tooltip("未录音时的图标")]
    public Sprite iconIdle;
    
    [Tooltip("录音中的图标")]
    public Sprite iconRecording;
    
    [Header("颜色设置")]
    [Tooltip("未录音时按钮的颜色")]
    public Color colorIdle = Color.white;
    
    [Tooltip("录音中按钮的颜色")]
    public Color colorRecording = Color.red;
    
    [Header("文本设置")]
    [Tooltip("未录音时的文本")]
    public string textIdle = "按住说话";
    
    [Tooltip("录音中的文本")]
    public string textRecording = "松开结束";
    
    [Header("模块引用")]
    [Tooltip("语音转文字模块")]
    public SpeechToText sttModule;
    
    [Tooltip("对话流程管理器")]
    public AIConversationFlow conversationFlow;
    
    // 当前状态
    private bool isRecording = false;
    private string lastTranscription = "";
    private bool isProcessing = false;

    void Start()
    {
        // 自动查找组件（如果没有手动分配）
        if (voiceButton == null)
        {
            voiceButton = GetComponent<Button>();
        }
        
        if (sttModule == null && conversationFlow != null)
        {
            sttModule = conversationFlow.sttModule;
        }
        
        // 验证必要组件
        if (voiceButton == null)
        {
            Debug.LogError("❌ 未找到 Button 组件！请确保脚本挂载在 Button 对象上");
            return;
        }
        
        // 绑定按钮点击事件
        voiceButton.onClick.AddListener(OnVoiceButtonClicked);
        
        // 订阅语音识别完成事件
        if (sttModule != null)
        {
            sttModule.OnTranscriptionComplete += OnTranscriptionComplete;
            
            // 订阅错误事件
            sttModule.OnTranscriptionError += OnTranscriptionError;
        }
        
        // 初始化 UI 状态
        UpdateUIState();
        
        Debug.Log("✅ 语音识别 UI 控制器已初始化");
    }

    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    private async void OnVoiceButtonClicked()
    {
        if (isProcessing)
        {
            Debug.LogWarning("⚠️ 正在处理中，请稍后再试");
            return;
        }
        
        if (isRecording)
        {
            // 停止录音
            StopRecording();
        }
        else
        {
            // 开始录音
            await StartRecording();
        }
    }

    /// <summary>
    /// 开始录音
    /// </summary>
    private async Task StartRecording()
    {
        if (sttModule == null)
        {
            Debug.LogError("❌ SpeechToText 模块未分配！");
            return;
        }
        
        if (isRecording)
        {
            Debug.LogWarning("⚠️ 已经在录音中...");
            return;
        }
        
        Debug.Log("🎤 开始录音...");
        
        // 更新状态
        isRecording = true;
        isProcessing = true;
        lastTranscription = "";
        
        // 更新 UI
        UpdateUIState();
        UpdateStatus("正在录音...");
        
        // 调用语音识别
        await sttModule.StartRecognitionAsync();
    }

    /// <summary>
    /// 停止录音
    /// </summary>
    private void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("⚠️ 当前未在录音");
            return;
        }
        
        Debug.Log("⏹️ 停止录音...");
        
        // 更新状态
        isRecording = false;
        
        // 更新 UI
        UpdateUIState();
        UpdateStatus("识别中...");
        
        // 停止语音识别
        if (sttModule != null)
        {
            sttModule.StopRecognition();
        }
    }

    /// <summary>
    /// 语音识别完成回调
    /// </summary>
    private void OnTranscriptionComplete(string transcription)
    {
        isProcessing = false;
        lastTranscription = transcription;
        
        Debug.Log($"✅ 识别完成：{transcription}");
        
        // 更新状态文本
        if (!string.IsNullOrEmpty(transcription))
        {
            UpdateStatus($"识别结果：{transcription}");
        }
        else
        {
            UpdateStatus("未识别到内容");
        }
        
        // 延迟清除状态文本
        Invoke(nameof(ClearStatus), 3f);
    }

    /// <summary>
    /// 语音识别错误回调
    /// </summary>
    private void OnTranscriptionError(string error)
    {
        isProcessing = false;
        
        Debug.LogError($"❌ 识别错误：{error}");
        UpdateStatus($"识别失败：{error}");
        
        // 延迟清除状态文本
        Invoke(nameof(ClearStatus), 3f);
    }

    /// <summary>
    /// 更新 UI 状态（图标、颜色、文本）
    /// </summary>
    private void UpdateUIState()
    {
        if (isRecording)
        {
            // 录音中状态
            if (buttonIcon != null && iconRecording != null)
            {
                buttonIcon.sprite = iconRecording;
            }
            
            if (buttonText != null)
            {
                buttonText.text = textRecording;
            }
            
            voiceButton.image.color = colorRecording;
        }
        else
        {
            // 空闲状态
            if (buttonIcon != null && iconIdle != null)
            {
                buttonIcon.sprite = iconIdle;
            }
            
            if (buttonText != null)
            {
                buttonText.text = textIdle;
            }
            
            voiceButton.image.color = colorIdle;
        }
    }

    /// <summary>
    /// 更新状态文本
    /// </summary>
    private void UpdateStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
        else
        {
            Debug.Log($"【状态】{status}");
        }
    }

    /// <summary>
    /// 清除状态文本
    /// </summary>
    private void ClearStatus()
    {
        if (statusText != null)
        {
            statusText.text = "";
        }
    }

    void OnDestroy()
    {
        // 清理事件监听
        if (voiceButton != null)
        {
            voiceButton.onClick.RemoveListener(OnVoiceButtonClicked);
        }
        
        if (sttModule != null)
        {
            sttModule.OnTranscriptionComplete -= OnTranscriptionComplete;
            sttModule.OnTranscriptionError -= OnTranscriptionError;
        }
    }

    void OnDisable()
    {
        // 禁用时如果正在录音，停止录音
        if (isRecording && sttModule != null)
        {
            sttModule.StopRecognition();
            isRecording = false;
            UpdateUIState();
        }
    }
}
