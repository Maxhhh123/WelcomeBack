// RewindUIButton.cs
// UI按钮触发回溯 - 使用Unity uGUI系统，支持手柄射线交互
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class RewindUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("按钮样式")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.cyan;
    public Color pressedColor = Color.blue;
    public Color disabledColor = Color.gray;
    public Color hasRecordColor = new Color(0.2f, 1f, 0.2f, 1f); // 绿色

    [Header("可选：文字显示")]
    public Text buttonText;
    public string normalText = "回溯";
    public string noRecordText = "无记录";
    public string readyText = "可回溯";

    [Header("可选：图标")]
    public Image iconImage;
    public Sprite rewindIcon;
    public Sprite noRecordIcon;

    [Header("音效")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    public AudioClip successSound;
    private AudioSource audioSource;

    [Header("动画")]
    public float hoverScale = 1.1f;
    public float scaleSpeed = 10f;

    private Button button;
    private Image buttonImage;
    private Vector3 originalScale;
    private bool isHovered = false;
    private bool isPressed = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        originalScale = transform.localScale;

        // 添加或获取AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (hoverSound != null || clickSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 注册按钮点击事件
        button.onClick.AddListener(OnButtonClicked);
    }

    private void Update()
    {
        UpdateButtonVisuals();
        UpdateButtonScale();
        UpdateButtonText();
        UpdateButtonState();
    }

    private void UpdateButtonVisuals()
    {
        if (buttonImage == null) return;

        Color targetColor;

        if (!CanRewind())
        {
            targetColor = disabledColor;
        }
        else if (isPressed)
        {
            targetColor = pressedColor;
        }
        else if (isHovered)
        {
            targetColor = highlightColor;
        }
        else if (SceneStateRecorder.Instance != null && SceneStateRecorder.Instance.HasRecords())
        {
            targetColor = hasRecordColor;
        }
        else
        {
            targetColor = normalColor;
        }

        buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.deltaTime * 10f);
    }

    private void UpdateButtonScale()
    {
        Vector3 targetScale = (isHovered && CanRewind()) ? originalScale * hoverScale : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }

    private void UpdateButtonText()
    {
        if (buttonText == null) return;

        if (SceneStateRecorder.Instance == null)
        {
            buttonText.text = "未初始化";
            buttonText.color = disabledColor;
        }
        else if (SceneStateRecorder.Instance.HasRecords())
        {
            int count = SceneStateRecorder.Instance.GetRecordCount();
            buttonText.text = $"{readyText}\n({count})";
            buttonText.color = Color.white;
        }
        else
        {
            buttonText.text = noRecordText;
            buttonText.color = disabledColor;
        }
    }

    private void UpdateButtonState()
    {
        // 更新按钮可交互状态
        button.interactable = CanRewind();
    }

    private void UpdateButtonIcon()
    {
        if (iconImage == null || rewindIcon == null || noRecordIcon == null) return;

        iconImage.sprite = (CanRewind()) ? rewindIcon : noRecordIcon;
    }

    private bool CanRewind()
    {
        return SceneStateRecorder.Instance != null && SceneStateRecorder.Instance.HasRecords();
    }

    private void OnButtonClicked()
    {
        if (!CanRewind())
        {
            PlaySound(hoverSound); // 播放错误提示音
            return;
        }

        // 执行回溯
        SceneStateRecorder.Instance.RewindToLastState();

        // 播放成功音效
        PlaySound(successSound != null ? successSound : clickSound);

        Debug.Log("[回溯UI按钮] 场景回溯已触发");
    }

    // 指针进入（射线悬停）
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        PlaySound(hoverSound);
    }

    // 指针离开
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    // 指针按下
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        PlaySound(clickSound);
    }

    // 指针抬起
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonClicked);
    }
}
