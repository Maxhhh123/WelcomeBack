using System;
using UnityEngine;
using System.Collections;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// 文字转语音管理器
/// 在 Android 上使用原生 TTS 引擎，在编辑器中模拟输出（Debug.Log）
/// </summary>
public class TTSManager : MonoBehaviour
{
    [Header("编辑器模拟")]
    [Tooltip("在 Unity 编辑器中是否用 Debug.Log 代替实际语音")]
    public bool simulateInEditor = true;

    [Header("Android TTS 配置")]
    [Tooltip("语言区域，例如 'zh-CN'（中文）或 'en-US'（英文）")]
    public string language = "zh-CN";

    // Android Java 对象引用
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject ttsObject;
    private bool isInitialized = false;
#endif

    // 初始化完成事件
    public event Action OnInitialized;
    public event Action<string> OnSpeakStarted;
    public event Action<string> OnSpeakCompleted;
    public event Action<string, string> OnError; // (errorMessage, originalText)

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeAndroidTTS();
#else
        if (!simulateInEditor)
            Debug.LogWarning("TTSManager: 当前在编辑器模式，语音将模拟输出。如需真机测试，请打包到 Android 设备。");
        else
            Debug.Log("TTSManager 初始化（模拟模式）");
        OnInitialized?.Invoke();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void InitializeAndroidTTS()
    {
        // 检查并请求录音权限（如果需要）
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }

        try
        {
            // 创建 TextToSpeech 对象，传入当前 Context（通过 UnityPlayer.currentActivity 获取）
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                ttsObject = new AndroidJavaObject("android.speech.tts.TextToSpeech", currentActivity, new TTSListener(this));
            }
        }
        catch (Exception e)
        {
            Debug.LogError("TTSManager: 初始化 Android TTS 失败 - " + e.Message);
            OnError?.Invoke("初始化失败: " + e.Message, "");
        }
    }

    // 内部监听类，用于接收 TTS 初始化回调
    private class TTSListener : AndroidJavaProxy
    {
        private TTSManager manager;

        public TTSListener(TTSManager mgr) : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            manager = mgr;
        }

        public void onInit(int status)
        {
            if (status == 0) // TextToSpeech.SUCCESS
            {
                // 设置语言
                using (AndroidJavaClass localeClass = new AndroidJavaClass("java.util.Locale"))
                {
                    AndroidJavaObject locale = null;
                    // 根据 language 字符串获取 Locale 对象
                    if (manager.language.StartsWith("zh"))
                        locale = localeClass.CallStatic<AndroidJavaObject>("forLanguageTag", "cmn-Hans-CN"); // 中文
                    else if (manager.language.StartsWith("en"))
                        locale = localeClass.CallStatic<AndroidJavaObject>("forLanguageTag", "en-US");
                    else
                        locale = localeClass.CallStatic<AndroidJavaObject>("forLanguageTag", manager.language);

                    int setLangResult = manager.ttsObject.Call<int>("setLanguage", locale);
                    if (setLangResult == -1 || setLangResult == -2) // LANG_MISSING_DATA / LANG_NOT_SUPPORTED
                    {
                        Debug.LogWarning("TTSManager: 指定的语言可能不支持，使用默认语言。");
                    }
                }

                manager.isInitialized = true;
                Debug.Log("TTSManager: Android TTS 初始化成功");
                manager.OnInitialized?.Invoke();
            }
            else
            {
                Debug.LogError("TTSManager: Android TTS 初始化失败，status = " + status);
                manager.OnError?.Invoke("TTS 初始化失败，状态码: " + status, "");
            }
        }
    }
#endif

    /// <summary>
    /// 播放语音（异步）
    /// </summary>
    /// <param name="text">要朗读的文本</param>
    public void Speak(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("TTSManager: Speak 文本为空");
            return;
        }

        OnSpeakStarted?.Invoke(text);

#if UNITY_ANDROID && !UNITY_EDITOR
        if (ttsObject == null || !isInitialized)
        {
            Debug.LogError("TTSManager: TTS 未初始化完成，无法播放");
            OnError?.Invoke("TTS 未初始化", text);
            return;
        }

        // 调用 speak 方法
        int speakResult = ttsObject.Call<int>("speak", text, 0, null, null); // 参数：文本，队列模式（0代表立即播放），参数，唯一标识
        if (speakResult == -1) // ERROR
        {
            Debug.LogError("TTSManager: speak 调用失败");
            OnError?.Invoke("speak 调用失败", text);
        }
        else
        {
            // Android TTS 播放完成时不会自动通知 Unity，需要额外监听，这里简单模拟延迟回调
            StartCoroutine(SimulateCompletion(text, text.Length * 0.1f)); // 粗略估计时长
        }
#else
        // 编辑器模拟
        if (simulateInEditor)
        {
            Debug.Log($"[TTS模拟] 说: {text}");
            // 模拟播放延迟，以便测试连续流程
            StartCoroutine(SimulateCompletion(text, text.Length * 0.1f));
        }
        else
        {
            // 如果不需要模拟，直接完成
            OnSpeakCompleted?.Invoke(text);
        }
#endif
    }

    /// <summary>
    /// 停止当前播放
    /// </summary>
    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ttsObject != null && isInitialized)
        {
            ttsObject.Call<int>("stop");
        }
#endif
        StopAllCoroutines(); // 停止模拟协程
    }

    /// <summary>
    /// 释放 TTS 资源
    /// </summary>
    public void Shutdown()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ttsObject != null)
        {
            ttsObject.Call("shutdown");
            ttsObject.Dispose();
            ttsObject = null;
            isInitialized = false;
        }
#endif
    }

    private IEnumerator SimulateCompletion(string text, float delay)
    {
        yield return new WaitForSeconds(delay);
        OnSpeakCompleted?.Invoke(text);
    }

    void OnDestroy()
    {
        Shutdown();
    }
}