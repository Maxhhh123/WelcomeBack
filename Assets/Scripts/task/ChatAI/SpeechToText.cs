using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections;

public class SpeechToText : MonoBehaviour
{
    [Header("麦克风设置")]
    public int sampleRate = 16000;
    public int recordLength = 10; // 最大录音时长（秒）
    
    [Header("模块引用")]
    [Tooltip("百度语音识别服务")]
    public BaiduSpeechService baiduService;
    
    private AudioClip micClip;
    private bool isRecording = false;
    private float[] recordedAudioData;
    
    public event Action<string> OnTranscriptionComplete;
    public event Action<string> OnTranscriptionError;

    void Start()
    {
        // 如果没有手动分配，尝试自动查找
        if (baiduService == null)
        {
            baiduService = FindObjectOfType<BaiduSpeechService>();
        }
        
        if (baiduService == null)
        {
            Debug.LogError("❌ 未找到 BaiduSpeechService 组件！请确保场景中有该组件");
            return;
        }
        
        // 订阅百度服务的识别完成事件
        baiduService.OnRecognitionComplete += HandleBaiduRecognitionComplete;
        baiduService.OnRecognitionError += HandleBaiduRecognitionError;
        
        Debug.Log("✅ 语音识别服务已初始化（百度智能云）");
    }

    /// <summary>
    /// 开始实时语音识别（异步版本，返回 Task）
    /// </summary>
    public async Task StartRecognitionAsync()
    {
        if (isRecording)
        {
            Debug.LogWarning("⚠️ 已经在录音中...");
            return;
        }
        
        Debug.Log("🎙️ 开始录音...");
        
        // 更新状态
        isRecording = true;
        
        // 启动麦克风录音
        string micName = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        micClip = Microphone.Start(micName, false, recordLength, sampleRate);
        
        // 等待麦克风准备就绪
        while (!(Microphone.GetPosition(micName) > 0)) { }
        
        Debug.Log("🎤 麦克风录音已开始");
    }

    /// <summary>
    /// 停止语音识别并发送到百度 API 识别
    /// </summary>
    public async void StopRecognition()
    {
        if (!isRecording) return;

        Debug.Log("⏹️ 停止录音，准备识别...");
        
        // 停止麦克风
        Microphone.End(null);
        isRecording = false;
        
        // 获取录音数据
        if (micClip != null)
        {
            // 获取实际录制的样本数
            int samples = micClip.samples;
            float[] audioData = new float[samples];
            micClip.GetData(audioData, 0);
            
            Debug.Log($"📊 录音数据长度：{samples} 样本");
            
            // 发送到百度 API 识别
            await baiduService.RecognizeAsync(audioData);
        }
        else
        {
            Debug.LogError("❌ 麦克风录音数据为空");
            OnTranscriptionError?.Invoke("录音数据无效");
        }
    }

    /// <summary>
    /// 处理百度识别完成
    /// </summary>
    private void HandleBaiduRecognitionComplete(string result)
    {
        Debug.Log($"✅ 识别完成：{result}");
        OnTranscriptionComplete?.Invoke(result);
    }

    /// <summary>
    /// 处理百度识别错误
    /// </summary>
    private void HandleBaiduRecognitionError(string error)
    {
        Debug.LogError($"❌ 识别错误：{error}");
        OnTranscriptionError?.Invoke(error);
    }

    void OnDestroy()
    {
        if (baiduService != null)
        {
            baiduService.OnRecognitionComplete -= HandleBaiduRecognitionComplete;
            baiduService.OnRecognitionError -= HandleBaiduRecognitionError;
        }
        
        if (isRecording)
        {
            Microphone.End(null);
        }
    }
}