using UnityEngine;
using System.Collections.Generic;

public class AIConversationFlow : MonoBehaviour
{
    [Header("模块引用")]
    public SpeechToText sttModule;
    public LLMManager llmModule;
    public TTSManager ttsManager;
    public CommandExecutor executor;

    [Header("对话设置")]
    [Tooltip("是否启用命令模式（自动检测并执行命令）")]
    public bool enableCommandMode = true;
    
    [Tooltip("系统提示词，用于设定 AI 角色")]
    public string systemPrompt = "你是一个智能音箱助手。当用户发出指令时（如打开电视、换台等），请在回复中使用特定格式：[CMD:命令类型，参数]。例如：'好的，马上打开电视[CMD:TV_ON]'或'正在切换到 CCTV-1[CMD:TV_CHANNEL,CCTV-1]'。如果没有命令，就正常聊天。";

    private bool isProcessing = false;
    private string lastUserMessage = "";

    void OnEnable()
    {
        sttModule.OnTranscriptionComplete += HandleUserSpoke;
        llmModule.OnLLMResponse += HandleAIResponse;
        llmModule.OnLLMError += HandleAIError;
        
        // 订阅语音识别错误事件
        sttModule.OnTranscriptionError += HandleTranscriptionError;
        
        // 设置系统提示词
        llmModule.SetSystemPrompt(systemPrompt);
    }

    void OnDisable()
    {
        sttModule.OnTranscriptionComplete -= HandleUserSpoke;
        llmModule.OnLLMResponse -= HandleAIResponse;
        llmModule.OnLLMError -= HandleAIError;
        sttModule.OnTranscriptionError -= HandleTranscriptionError;
    }

    void HandleUserSpoke(string userMessage)
    {
        if (string.IsNullOrEmpty(userMessage))
        {
            Debug.LogWarning("识别到的消息为空");
            return;
        }

        if (isProcessing)
        {
            Debug.LogWarning("正在处理上一个请求");
            return;
        }

        lastUserMessage = userMessage;
        isProcessing = true;

        Debug.Log($"用户说：{userMessage}");
        
        // 发送到大模型
        StartCoroutine(llmModule.SendToLLM(userMessage));
    }

    void HandleAIResponse(string aiResponse)
    {
        isProcessing = false;

        Debug.Log($"AI 回复：{aiResponse}");

        // 提取命令（如果有）
        if (enableCommandMode && TryExtractCommand(aiResponse, out string command, out string parameters))
        {
            Debug.Log($"检测到命令：{command}, 参数：{parameters}");
            
            // 先播放语音回复（去除命令标记的纯净文本）
            string cleanResponse = RemoveCommandTags(aiResponse);
            ttsManager.Speak(cleanResponse);
            
            // 执行命令
            executor.ExecuteCommand(command, parameters, lastUserMessage);
        }
        else
        {
            // 纯聊天模式
            ttsManager.Speak(aiResponse);
        }

        // 可选：自动开始下一轮识别
        // sttModule.StartRecognition();
    }

    void HandleAIError(string error)
    {
        isProcessing = false;
        Debug.LogError($"AI 处理错误：{error}");
        ttsManager.Speak("抱歉，我遇到了一些问题，请稍后再试。");
    }

    void HandleTranscriptionError(string error)
    {
        isProcessing = false;
        Debug.LogError($"语音识别错误：{error}");
        ttsManager.Speak("抱歉，我没有听清楚，请您再说一遍。");
    }

    /// <summary>
    /// 从 AI 回复中提取命令
    /// 格式：[CMD:命令类型，参数]
    /// </summary>
    private bool TryExtractCommand(string response, out string command, out string parameters)
    {
        command = "";
        parameters = "";

        int startIndex = response.IndexOf("[CMD:");
        if (startIndex == -1)
            return false;

        int endIndex = response.IndexOf("]", startIndex);
        if (endIndex == -1)
            return false;

        string cmdContent = response.Substring(startIndex + 5, endIndex - startIndex - 5);
        
        // 分割命令和参数
        string[] parts = cmdContent.Split(',');
        if (parts.Length > 0)
        {
            command = parts[0].Trim().ToUpper();
            if (parts.Length > 1)
            {
                parameters = parts[1].Trim();
            }
        }

        return !string.IsNullOrEmpty(command);
    }

    /// <summary>
    /// 移除命令标记，返回纯净的回复文本
    /// </summary>
    private string RemoveCommandTags(string response)
    {
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\[CMD:[^\]]*\]");
        return regex.Replace(response, "").Trim();
    }

    /// <summary>
    /// 手动触发对话（用于测试）
    /// </summary>
    public void StartConversation()
    {
        sttModule.StartRecognitionAsync();
    }
}