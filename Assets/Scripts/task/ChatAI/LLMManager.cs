using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class LLMManager : MonoBehaviour
{
    [Header("API 设置")]
    public string apiUrl = "https://api.siliconflow.cn/v1/chat/completions";
    
    [Tooltip("API Key - 建议在运行时动态设置")]
    public string apiKey = "";
    
    [Header("模型设置")]
    public string modelName = "Pro/zai-org/GLM-4.7";
    
    [Header("系统提示词")]
    [TextArea(3, 5)]
    public string systemPrompt = "你是一个友好的 AI 助手，请用简洁清晰的方式回答问题。";

    public Action<string> OnLLMResponse;
    public Action<string> OnLLMError;

    private bool isProcessing = false;

    void Start()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogWarning("API Key 为空！请在 Inspector 中设置或通过 SetApiKey() 方法设置");
        }
    }

    /// <summary>
    /// 安全地设置 API Key（推荐方式）
    /// </summary>
    public void SetApiKey(string key)
    {
        apiKey = key;
    }

    /// <summary>
    /// 发送消息到大模型
    /// </summary>
    public IEnumerator SendToLLM(string userMessage)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API Key 未设置！");
            OnLLMError?.Invoke("API Key 未设置，请联系开发者");
            yield break;
        }

        if (isProcessing)
        {
            Debug.LogWarning("正在处理上一个请求，请稍后再试");
            yield break;
        }

        isProcessing = true;

        // 构建完整的消息列表
        var messages = new[]
        {
            new MessageData { role = "system", content = systemPrompt },
            new MessageData { role = "user", content = userMessage }
        };

        var requestData = new RequestData
        {
            model = modelName,
            messages = messages,
            temperature = 0.7f,
            max_tokens = 512
        };

        // 使用 Unity 内置的 JsonUtility
        string jsonData = JsonUtility.ToJson(requestData);
        byte[] postData = Encoding.UTF8.GetBytes(jsonData);

        Debug.Log($"发送请求到 LLM: {userMessage}");

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            request.timeout = 30;

            yield return request.SendWebRequest();

            isProcessing = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = ParseLLMResponse(request.downloadHandler.text);
                    
                    if (!string.IsNullOrEmpty(responseText))
                    {
                        Debug.Log($"LLM 回复：{responseText}");
                        OnLLMResponse?.Invoke(responseText);
                    }
                    else
                    {
                        OnLLMError?.Invoke("AI 返回了空响应");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"解析响应失败：{ex.Message}");
                    OnLLMError?.Invoke($"解析 AI 响应失败：{ex.Message}");
                }
            }
            else
            {
                string errorMsg = HandleError(request);
                Debug.LogError(errorMsg);
                OnLLMError?.Invoke(errorMsg);
            }
        }
    }

    /// <summary>
    /// 解析大模型的 JSON 响应
    /// </summary>
    private string ParseLLMResponse(string jsonResponse)
    {
        try
        {
            // 反序列化 JSON
            var response = JsonUtility.FromJson<LLMResponse>(jsonResponse);
            
            if (response == null || response.choices == null || response.choices.Length == 0)
            {
                Debug.LogError("响应格式错误：没有 choices 字段");
                return null;
            }

            // 提取 AI 的回复内容
            string content = response.choices[0].message.content;
            
            if (string.IsNullOrEmpty(content))
            {
                Debug.LogWarning("AI 返回的内容为空");
                return "";
            }

            return content.Trim();
        }
        catch (Exception ex)
        {
            Debug.LogError($"JSON 解析失败：{ex.Message}\n原始响应：{jsonResponse}");
            throw;
        }
    }

    /// <summary>
    /// 处理错误信息
    /// </summary>
    private string HandleError(UnityWebRequest request)
    {
        switch (request.result)
        {
            case UnityWebRequest.Result.ConnectionError:
                return "网络连接错误，请检查网络";
            
            case UnityWebRequest.Result.DataProcessingError:
                return "数据处理错误";
            
            case UnityWebRequest.Result.ProtocolError:
                // HTTP 错误码处理
                long statusCode = request.responseCode;
                if (statusCode == 401)
                    return "API Key 无效或已过期";
                else if (statusCode == 429)
                    return "请求过于频繁，请稍后再试";
                else if (statusCode >= 500)
                    return "服务器错误，请稍后再试";
                else
                    return $"HTTP 错误：{statusCode}";
            
            default:
                return $"未知错误：{request.error}";
        }
    }

    /// <summary>
    /// 设置系统提示词（用于设定 AI 角色）
    /// </summary>
    public void SetSystemPrompt(string prompt)
    {
        systemPrompt = prompt;
        Debug.Log($"系统提示词已设置：{prompt}");
    }

    /// <summary>
    /// 切换模型
    /// </summary>
    public void SetModel(string modelName)
    {
        this.modelName = modelName;
        Debug.Log($"模型已切换为：{modelName}");
    }

    #region 数据类
    
    [Serializable]
    private class RequestData
    {
        public string model;
        public MessageData[] messages;
        public float temperature;
        public int max_tokens;
    }

    [Serializable]
    private class MessageData
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class LLMResponse
    {
        public ChoiceData[] choices;
    }

    [Serializable]
    private class ChoiceData
    {
        public MessageData message;
    }

    #endregion
}