using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Audio;

/// <summary>
/// 电视控制器
/// 负责管理电视的开关、换台、音量等功能
/// </summary>
public class TVController : MonoBehaviour
{
    [Header("电视组件")]
    [Tooltip("电视的 3D 模型物体（用于开关控制）")]
    public GameObject tvModel;
    
    [Tooltip("电视的 VideoPlayer 组件（用于播放视频）")]
    public VideoPlayer videoPlayer;
    
    [Tooltip("电视频道与视频片段的映射")]
    public ChannelVideoMapping[] channelMappings;
    
    [Header("音频输出设置")]
    [Tooltip("视频播放时使用的 AudioSource（如果为空则自动创建）")]
    public AudioSource videoAudioSource;
    
    [Header("音量设置")]
    [Range(0, 1)]
    [Tooltip("默认音量")]
    public float defaultVolume = 0.5f;
    
    [Tooltip("每次音量调整的幅度")]
    public float volumeStep = 0.1f;

    // 当前状态
    private bool isTVOn = false;
    private string currentChannel = "";
    private int currentChannelIndex = 0;

    void Start()
    {
        // 初始化组件引用（如果 Inspector 中没设置）
        if (tvModel == null)
            tvModel = gameObject;
        
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();
        
        // 配置 VideoPlayer 的音频输出
        if (videoPlayer != null)
        {
            SetupVideoAudio();
            
            // 设置初始音量
            SetVolumeInternal(defaultVolume);
        }
    }

    /// <summary>
    /// 配置 VideoPlayer 的音频输出
    /// </summary>
    private void SetupVideoAudio()
    {
        // 确保 VideoPlayer 使用 AudioSource 输出音频
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        
        // 如果没有指定 AudioSource，尝试获取或创建
        if (videoAudioSource == null)
        {
            videoAudioSource = GetComponent<AudioSource>();
            
            if (videoAudioSource == null)
            {
                videoAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // 将 AudioSource 分配给 VideoPlayer
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);
        
        // 配置 AudioSource
        videoAudioSource.playOnAwake = false;
        videoAudioSource.spatialBlend = 0; // 2D 声音
        videoAudioSource.volume = defaultVolume;
    }

    /// <summary>
    /// 内部方法：设置音量
    /// </summary>
    private void SetVolumeInternal(float volume)
    {
        if (videoAudioSource != null)
        {
            videoAudioSource.volume = Mathf.Clamp01(volume);
        }
    }

    /// <summary>
    /// 获取当前音量
    /// </summary>
    private float GetVolumeInternal()
    {
        return videoAudioSource != null ? videoAudioSource.volume : 0f;
    }

    #region 电视开关控制
    
    /// <summary>
    /// 打开电视
    /// </summary>
    public void PowerOn()
    {
        if (isTVOn)
        {
            Debug.LogWarning("电视已经是开启状态");
            return;
        }

        Debug.Log("【TVController】打开电视");
        
        // 显示电视模型
        if (tvModel != null)
        {
            tvModel.SetActive(true);
        }
        
        // 准备并播放视频
        if (videoPlayer != null)
        {
            // 如果没有当前频道，播放第一个频道
            if (videoPlayer.clip == null && channelMappings.Length > 0)
            {
                PlayChannel(channelMappings[0].channelName);
            }
            else if (!videoPlayer.isPlaying)
            {
                videoPlayer.Play();
            }
        }
        
        isTVOn = true;
        Debug.Log("✅ 电视已开启");
    }

    /// <summary>
    /// 关闭电视
    /// </summary>
    public void PowerOff()
    {
        if (!isTVOn)
        {
            Debug.LogWarning("电视已经是关闭状态");
            return;
        }

        Debug.Log("【TVController】关闭电视");
        
        // 停止播放
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        // 隐藏电视模型
        if (tvModel != null)
        {
            tvModel.SetActive(false);
        }
        
        isTVOn = false;
        Debug.Log("✅ 电视已关闭");
    }

    /// <summary>
    /// 切换电视频道
    /// </summary>
    public void ChangeChannel(string channelName)
    {
        if (!isTVOn)
        {
            Debug.LogWarning("电视未开启，无法换台");
            return;
        }

        Debug.Log($"【TVController】切换到频道：{channelName}");
        
        bool success = PlayChannel(channelName);
        
        if (success)
        {
            currentChannel = channelName;
            Debug.Log($"✅ 已切换到频道：{channelName}");
        }
        else
        {
            Debug.LogError($"❌ 无法找到频道：{channelName}");
        }
    }

    /// <summary>
    /// 根据频道名称播放对应的视频
    /// </summary>
    private bool PlayChannel(string channelName)
    {
        if (channelMappings == null || channelMappings.Length == 0)
        {
            Debug.LogError("❌ 频道映射表为空！");
            return false;
        }
        
        // 精确匹配
        foreach (var mapping in channelMappings)
        {
            if (mapping.channelName.Equals(channelName, System.StringComparison.OrdinalIgnoreCase))
            {
                SetVideoClip(mapping.videoClip);
                currentChannel = mapping.channelName;
                return true;
            }
        }
        
        // 部分匹配
        foreach (var mapping in channelMappings)
        {
            if (mapping.channelName.Contains(channelName, System.StringComparison.OrdinalIgnoreCase) ||
                channelName.Contains(mapping.channelName, System.StringComparison.OrdinalIgnoreCase))
            {
                SetVideoClip(mapping.videoClip);
                currentChannel = mapping.channelName;
                return true;
            }
        }
        
        // 模糊匹配（处理 CCTV、卫视等）
        VideoClip fuzzyMatch = FuzzyMatchChannel(channelName);
        if (fuzzyMatch != null)
        {
            SetVideoClip(fuzzyMatch);
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 设置并播放视频片段
    /// </summary>
    private void SetVideoClip(VideoClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("❌ 视频片段为空！");
            return;
        }
        
        if (videoPlayer == null)
        {
            Debug.LogError("❌ VideoPlayer 组件为空！");
            return;
        }
        
        // 切换视频片段
        videoPlayer.clip = clip;
        videoPlayer.Play();
    }

    /// <summary>
    /// 增加音量
    /// </summary>
    public void VolumeUp()
    {
        float newVolume = Mathf.Min(GetVolumeInternal() + volumeStep, 1.0f);
        SetVolumeInternal(newVolume);
        Debug.Log($"🔊 音量已增加至：{(int)(newVolume * 100)}%");
    }

    /// <summary>
    /// 降低音量
    /// </summary>
    public void VolumeDown()
    {
        float newVolume = Mathf.Max(GetVolumeInternal() - volumeStep, 0.0f);
        SetVolumeInternal(newVolume);
        Debug.Log($"🔉 音量已降低至：{(int)(newVolume * 100)}%");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 模糊匹配频道（处理简称、别名等）
    /// </summary>
    private VideoClip FuzzyMatchChannel(string userInput)
    {
        string input = userInput.ToLower().Trim();
        
        // CCTV 系列匹配
        if (input.Contains("cctv") || input.Contains("中央"))
        {
            string inputNum = ExtractNumber(input);
            
            foreach (var mapping in channelMappings)
            {
                if (mapping.channelName.ToUpper().Contains("CCTV"))
                {
                    string channelNum = ExtractNumber(mapping.channelName);
                    
                    if (!string.IsNullOrEmpty(inputNum) && inputNum == channelNum)
                    {
                        return mapping.videoClip;
                    }
                }
            }
        }
        
        // 卫视系列匹配
        if (input.Contains("卫视"))
        {
            foreach (var mapping in channelMappings)
            {
                if (mapping.channelName.Contains("卫视"))
                {
                    string province = mapping.channelName.Replace("卫视", "");
                    if (input.Contains(province) || province.Contains(input.Replace("卫视", "")))
                    {
                        return mapping.videoClip;
                    }
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// 从字符串中提取数字
    /// </summary>
    private string ExtractNumber(string text)
    {
        string result = "";
        foreach (char c in text)
        {
            if (char.IsDigit(c))
            {
                result += c;
            }
        }
        return result;
    }

    #endregion

    #region 状态查询

    public bool IsTVOn() => isTVOn;
    public string GetCurrentChannel() => currentChannel;
    public float GetCurrentVolume() => GetVolumeInternal();

    #endregion
}

/// <summary>
/// 频道与视频片段的映射配置
/// </summary>
[System.Serializable]
public class ChannelVideoMapping
{
    [Tooltip("频道名称（如：CCTV-1、湖南卫视）")]
    public string channelName;
    
    [Tooltip("对应的视频片段")]
    public VideoClip videoClip;
}
