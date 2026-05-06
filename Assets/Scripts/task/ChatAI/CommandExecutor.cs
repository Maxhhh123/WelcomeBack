using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 命令执行器
/// 负责解析并执行各种设备控制命令
/// </summary>
public class CommandExecutor : MonoBehaviour
{
    public TTSManager ttsManager;
    
    [Header("设备控制器引用")]
    [Tooltip("电视控制器（负责管理电视的开关、换台等）")]
    public TVController tvController;

    // 命令关键词映射表
    private Dictionary<string, List<string>> commandKeywords = new Dictionary<string, List<string>>
    {
        { "TV_ON", new List<string> { "打开电视", "开启电视", "开电视", "启动电视" } },
        { "TV_OFF", new List<string> { "关闭电视", "关掉电视", "关电视", "停止电视" } },
        { "TV_CHANNEL", new List<string> { "换台", "切换频道", "调到", "转到", "看 CCTV", "看中央" } },
        { "TV_VOLUME_UP", new List<string> { "音量加大", "声音大点", "大声点", "调高音量", "增加音量" } },
        { "TV_VOLUME_DOWN", new List<string> { "音量减小", "声音小点", "小声点", "调低音量", "降低音量" } },
        { "LIGHT_ON", new List<string> { "开灯", "打开灯", "亮灯" } },
        { "LIGHT_OFF", new List<string> { "关灯", "关闭灯", "熄灯" } },
    };

    /// <summary>
    /// 执行命令（通过 AI 提取的命令）
    /// </summary>
    public void ExecuteCommand(string command, string parameter, string originalMessage)
    {
        Debug.Log($"【命令执行】类型：{command}, 参数：{parameter}, 原始消息：{originalMessage}");

        switch (command)
        {
            case "TV_ON":
                ExecuteTVOn();
                break;
            
            case "TV_OFF":
                ExecuteTVOff();
                break;
            
            case "TV_CHANNEL":
                ExecuteTVChannelChange(parameter);
                break;
            
            case "TV_VOLUME_UP":
                ExecuteTVVolumeUp();
                break;
            
            case "TV_VOLUME_DOWN":
                ExecuteTVVolumeDown();
                break;
            
            case "LIGHT_ON":
                TurnOnLight();
                break;
            
            case "LIGHT_OFF":
                TurnOffLight();
                break;
            
            default:
                Debug.LogWarning($"未知命令：{command}");
                break;
        }
    }

    #region 电视命令执行方法
    
    private void ExecuteTVOn()
    {
        Debug.Log("【命令执行】打开电视");
        
        if (tvController != null)
        {
            tvController.PowerOn();
        }
        else
        {
            Debug.LogError("❌ TVController 未设置！");
        }
    }

    private void ExecuteTVOff()
    {
        Debug.Log("【命令执行】关闭电视");
        
        if (tvController != null)
        {
            tvController.PowerOff();
        }
        else
        {
            Debug.LogError("❌ TVController 未设置！");
        }
    }

    private void ExecuteTVChannelChange(string channel)
    {
        Debug.Log($"【命令执行】切换频道：{channel}");
        
        if (tvController != null)
        {
            tvController.ChangeChannel(channel);
        }
        else
        {
            Debug.LogError("❌ TVController 未设置！");
        }
    }

    private void ExecuteTVVolumeUp()
    {
        Debug.Log("【命令执行】增加音量");
        
        if (tvController != null)
        {
            tvController.VolumeUp();
        }
        else
        {
            Debug.LogError("❌ TVController 未设置！");
        }
    }

    private void ExecuteTVVolumeDown()
    {
        Debug.Log("【命令执行】降低音量");
        
        if (tvController != null)
        {
            tvController.VolumeDown();
        }
        else
        {
            Debug.LogError("❌ TVController 未设置！");
        }
    }

    #endregion

    #region 灯光控制方法
    
    private void TurnOnLight()
    {
        Debug.Log("【灯光控制】打开灯光");
    }

    private void TurnOffLight()
    {
        Debug.Log("【灯光控制】关闭灯光");
    }

    #endregion

    /// <summary>
    /// 【备用方案】直接从用户话语中识别命令（不依赖 AI）
    /// </summary>
    public bool TryRecognizeCommand(string userMessage, out string command, out string parameter)
    {
        command = "";
        parameter = "";
        
        string message = userMessage.ToLower().Trim();

        // 遍历所有命令关键词
        foreach (var kvp in commandKeywords)
        {
            foreach (string keyword in kvp.Value)
            {
                if (message.Contains(keyword.ToLower()))
                {
                    command = kvp.Key;
                    
                    // 提取参数（如频道名称）
                    if (kvp.Key == "TV_CHANNEL")
                    {
                        parameter = ExtractChannelName(userMessage);
                    }
                    
                    Debug.Log($"【关键词匹配】识别到命令：{command}, 参数：{parameter}");
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 从句子中提取频道名称
    /// </summary>
    private string ExtractChannelName(string message)
    {
        // 常见频道列表
        string[] channels = {
            "CCTV-1", "CCTV-2", "CCTV-3", "CCTV-4", "CCTV-5", 
            "湖南卫视", "浙江卫视", "江苏卫视", "北京卫视",
            "新闻频道", "体育频道", "电影频道"
        };

        foreach (string channel in channels)
        {
            if (message.Contains(channel))
            {
                return channel;
            }
        }

        // 默认返回 CCTV-1
        return "CCTV-1";
    }

    /// <summary>
    /// 添加自定义命令关键词
    /// </summary>
    public void AddCommandKeyword(string commandType, string keyword)
    {
        if (!commandKeywords.ContainsKey(commandType))
        {
            commandKeywords[commandType] = new List<string>();
        }
        
        commandKeywords[commandType].Add(keyword);
        Debug.Log($"添加命令关键词：{commandType} -> {keyword}");
    }
}