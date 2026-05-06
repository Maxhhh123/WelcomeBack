using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 百度智能云语音识别服务
/// 支持短语音识别（中文普通话）
/// </summary>
public class BaiduSpeechService : MonoBehaviour
{
    [Header("百度 API 配置")]
    [Tooltip("百度智能云 API Key")]
    public string apiKey = "你的 API_KEY";
    
    [Tooltip("百度智能云 Secret Key")]
    public string secretKey = "你的 SECRET_KEY";
    
    [Header("识别配置")]
    [Tooltip("语音采样率，百度支持 8000 或 16000")]
    public int sampleRate = 16000;
    
    [Tooltip("音频格式，pcm 为推荐格式")]
    public string audioFormat = "pcm";
    
    [Tooltip("是否开启语速检测")]
    public bool enableSpeed = false;
    
    [Tooltip("是否开启音量检测")]
    public bool enableVolume = false;
    
    [Tooltip("是否开启音轨检测")]
    public bool enablePitch = false;
    
    // AccessToken（通过 API Key 和 Secret Key 获取）
    private string accessToken = "";
    private bool isGettingToken = false;
    private Coroutine tokenCoroutine = null;
    
    // 识别完成事件
    public event Action<string> OnRecognitionComplete;
    public event Action<string> OnRecognitionError;
    
    void Start()
    {
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(secretKey))
        {
            Debug.LogError("❌ 百度 API Key 或 Secret Key 未设置！");
        }
        else
        {
            // 启动获取 Token
            StartCoroutine(GetAccessToken());
        }
    }
    
    /// <summary>
    /// 获取 Access Token
    /// </summary>
    private IEnumerator GetAccessToken()
    {
        if (isGettingToken)
            yield break;
            
        isGettingToken = true;
        
        string url = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={apiKey}&client_secret={secretKey}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
                    accessToken = response.access_token;
                    Debug.Log($"✅ 百度 Access Token 获取成功，有效期：{response.expires_in}秒");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ 解析 Token 失败：{ex.Message}");
                    OnRecognitionError?.Invoke("获取访问令牌失败");
                }
            }
            else
            {
                Debug.LogError($"❌ 获取 Token 失败：{request.error}");
                OnRecognitionError?.Invoke("网络连接失败");
            }
        }
        
        isGettingToken = false;
    }
    
    /// <summary>
    /// 刷新 Access Token（在过期前调用）
    /// </summary>
    public void RefreshToken()
    {
        if (tokenCoroutine != null)
            StopCoroutine(tokenCoroutine);
        tokenCoroutine = StartCoroutine(GetAccessToken());
    }
    
    /// <summary>
    /// 语音识别（异步版本）
    /// </summary>
    /// <param name="audioData">PCM 音频数据（浮点数组，范围 -1 到 1）</param>
    /// <returns>识别结果文本</returns>
    public async Task<string> RecognizeAsync(float[] audioData)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogError("❌ Access Token 为空，请先获取 Token");
            OnRecognitionError?.Invoke("未获取到访问令牌");
            return "";
        }
        
        if (audioData == null || audioData.Length == 0)
        {
            Debug.LogError("❌ 音频数据为空");
            OnRecognitionError?.Invoke("音频数据无效");
            return "";
        }
        
        // 将浮点型音频数据转换为 16 位 PCM
        byte[] pcmData = ConvertToPCM16(audioData);
        
        // Base64 编码
        string base64Audio = Convert.ToBase64String(pcmData);
        
        // 构建请求体
        var requestBody = new BaiduRecognitionRequest
        {
            format = audioFormat,
            rate = sampleRate,
            channel = 1,
            cuid = UnityEngine.SystemInfo.deviceUniqueIdentifier,
            token = accessToken,
            speech = base64Audio,
            len = pcmData.Length,
            lan = "zh",
            pdt = 0, // 模型选择，0 为默认模型
            spd = enableSpeed ? 5 : 0,
            vol = enableVolume ? 5 : 0,
            pitch = enablePitch ? 5 : 0
        };
        
        string json = JsonUtility.ToJson(requestBody);
        byte[] postData = Encoding.UTF8.GetBytes(json);
        
        // 发送识别请求
        string url = $"https://vop.baidu.com/server_api";
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            // 使用 await 等待网络请求完成
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<BaiduRecognitionResponse>(request.downloadHandler.text);
                    
                    if (response.err_no == 0)
                    {
                        string result = response.result != null && response.result.Length > 0 
                            ? response.result[0] : "";
                        
                        Debug.Log($"✅ 百度识别成功：{result}");
                        OnRecognitionComplete?.Invoke(result);
                        return result;
                    }
                    else
                    {
                        string errorMsg = $"百度识别错误：{response.err_msg} (错误码：{response.err_no})";
                        Debug.LogError(errorMsg);
                        OnRecognitionError?.Invoke(response.err_msg);
                        return "";
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ 解析识别结果失败：{ex.Message}");
                    OnRecognitionError?.Invoke("解析识别结果失败");
                    return "";
                }
            }
            else
            {
                string errorMsg = $"网络请求失败：{request.error}";
                Debug.LogError(errorMsg);
                OnRecognitionError?.Invoke("网络连接失败");
                return "";
            }
        }
    }
    
    /// <summary>
    /// 将浮点型音频数据转换为 16 位 PCM 格式
    /// </summary>
    private byte[] ConvertToPCM16(float[] audioData)
    {
        short[] pcmData = new short[audioData.Length];
        
        for (int i = 0; i < audioData.Length; i++)
        {
            // 将 -1 到 1 的浮点数转换为 -32768 到 32767 的 16 位整数
            float sample = Mathf.Clamp(audioData[i], -1f, 1f);
            pcmData[i] = (short)(sample * short.MaxValue);
        }
        
        byte[] bytes = new byte[pcmData.Length * 2];
        Buffer.BlockCopy(pcmData, 0, bytes, 0, bytes.Length);
        
        return bytes;
    }
    
    #region 数据类
    
    [Serializable]
    private class TokenResponse
    {
        public string access_token;
        public int expires_in;
        public string scope;
        public string session_key;
        public string session_secret;
        public string refresh_token;
    }
    
    [Serializable]
    private class BaiduRecognitionRequest
    {
        public string format;
        public int rate;
        public int channel;
        public string cuid;
        public string token;
        public string speech;
        public int len;
        public string lan = "zh";
        public int pdt = 0;
        public int spd;
        public int vol;
        public int pitch;
    }
    
    [Serializable]
    private class BaiduRecognitionResponse
    {
        public long err_no;
        public string err_msg;
        public string sn;
        public string[] result;
    }
    
    #endregion
}
