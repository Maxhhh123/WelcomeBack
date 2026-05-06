using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class audiomanager : Singleton<audiomanager>
{
    [Header("Audio Settings")]
    public AudioSource bgmAudioSource;           // 背景音乐音频源
    public AudioClip initialBgm;                 // 初始背景音乐
    public AudioClip gameStartBgm;               // 游戏开始后背景音乐
    public AudioClip doorOpenSound;              // 开门音效
    public AudioClip doorCloseSound;             // 关门音效
    
    [Header("Game Objects")]
    public GameObject uiCanvas;                  // UI画布
    public Animation elevatorAnimation;          // 电梯动画组件
    public string doorOpenAnimationName = "ElevatorDoorsInsideOpen"; // 开门动画名称
    
    private bool gameStarted = false;            // 游戏是否已开始标记
    private bool doorsClosed = false;            // 门是否已经关闭
    
    // Start is called before the first frame update
    void Start()
    {
        // 播放初始背景音乐
        if (!gameStarted)
        {
            Debug.Log("kaishi");
        }
        
        if (bgmAudioSource != null && initialBgm != null)
        {
            bgmAudioSource.clip = initialBgm;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }
    }

    /// <summary>
    /// 开始游戏的方法 - 需要绑定到Button的OnClick事件
    /// </summary>
    public void StartGame()
    {
        // 防止重复触发
        if (gameStarted) return;
        gameStarted = true;
        Debug.Log("开始游戏");
        // 隐藏UI Canvas
        if (uiCanvas != null)
        {
            uiCanvas.SetActive(false);
            
        }
        
        // 播放电梯开门动画 - 使用Animation组件
        if (elevatorAnimation != null && !string.IsNullOrEmpty(doorOpenAnimationName))
        {
            // 检查动画是否存在
            if (elevatorAnimation.GetClip(doorOpenAnimationName) != null)
            {
                elevatorAnimation.Play(doorOpenAnimationName);
            }
            else
            {
                Debug.LogWarning($"动画片段 '{doorOpenAnimationName}' 未找到，请检查动画名称是否正确");
            }
        }
        else
        {
            Debug.LogWarning("电梯动画组件未设置或动画名称为空");
        }
        
        // 播放开门音效并切换背景音乐
        StartCoroutine(PlayDoorSoundAndBgm());
    }
    
    /// <summary>
    /// 播放开门音效并在音效播放完成后切换背景音乐
    /// </summary>
    private IEnumerator PlayDoorSoundAndBgm()
    {
        // 播放开门音效
        if (bgmAudioSource != null && doorOpenSound != null)
        {
            bgmAudioSource.Stop();
            bgmAudioSource.PlayOneShot(doorOpenSound);
            
            // 等待开门音效播放完成
            yield return new WaitForSeconds(doorOpenSound.length);
        }
        
        // 切换为游戏开始后的背景音乐
        if (bgmAudioSource != null && gameStartBgm != null)
        {
            bgmAudioSource.clip = gameStartBgm;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }
    }
    
    /// <summary>
    /// 触发电梯关门动画
    /// </summary>
    public void TriggerElevatorDoorsClose()
    {
        // 防止重复触发或在游戏未开始时触发
        if (doorsClosed || !gameStarted) return;
        
        doorsClosed = true;
        
        // 反向播放开门动画来实现关门效果
        if (elevatorAnimation != null && !string.IsNullOrEmpty(doorOpenAnimationName))
        {
            AnimationState animState = elevatorAnimation[doorOpenAnimationName];
            if (animState != null)
            {
                // 设置动画速度为负数来反向播放（关门效果）
                animState.speed = -1f;
                // 将时间设置为动画末尾，然后反向播放
                animState.time = animState.length;
                elevatorAnimation.Play(doorOpenAnimationName);
                
                // 播放关门音效
                if (bgmAudioSource != null && doorCloseSound != null)
                {
                    bgmAudioSource.PlayOneShot(doorCloseSound);
                }
            }
        }
    }
}
