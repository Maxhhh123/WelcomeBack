// RewindButton.cs
// 物理按钮触发回溯 - 可用手柄触碰/抓取触发
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable))]
public class RewindButton : MonoBehaviour
{
    [Header("交互设置")]
    [Tooltip("按钮被按下时的位移距离")]
    public float pressDistance = 0.02f;
    
    [Tooltip("按钮恢复速度")]
    public float returnSpeed = 5f;
    
    [Header("视觉效果")]
    public Color normalColor = Color.white;
    public Color pressedColor = Color.cyan;
    public Color canRewindColor = Color.green;
    public Color noRecordColor = Color.red;

    [Header("可选：按钮文字显示")]
    public TextMesh infoText;

    private Vector3 originalPosition;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;
    private Renderer buttonRenderer;
    private Material buttonMaterial;
    private bool isPressed = false;
    private float currentPressAmount = 0f;

    private void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        buttonRenderer = GetComponentInChildren<Renderer>();
        
        if (buttonRenderer != null)
        {
            // 创建材质实例避免影响其他物体
            buttonMaterial = new Material(buttonRenderer.material);
            buttonRenderer.material = buttonMaterial;
        }

        originalPosition = transform.localPosition;

        // 注册交互事件
        interactable.selectEntered.AddListener(OnButtonPressed);
        interactable.selectExited.AddListener(OnButtonReleased);
        interactable.hoverEntered.AddListener(OnHoverEntered);
    }

    private void Update()
    {
        // 更新按钮位置（按压动画）
        UpdateButtonPosition();
        
        // 更新颜色状态
        UpdateButtonColor();
        
        // 更新文字
        UpdateInfoText();
    }

    private void UpdateButtonPosition()
    {
        float targetPressAmount = isPressed ? 1f : 0f;
        currentPressAmount = Mathf.MoveTowards(currentPressAmount, targetPressAmount, Time.deltaTime * returnSpeed);
        
        Vector3 targetPosition = originalPosition + Vector3.down * (pressDistance * currentPressAmount);
        transform.localPosition = targetPosition;
    }

    private void UpdateButtonColor()
    {
        if (buttonMaterial == null) return;

        Color targetColor;
        
        if (isPressed)
        {
            targetColor = pressedColor;
        }
        else if (SceneStateRecorder.Instance == null)
        {
            targetColor = noRecordColor;
        }
        else if (SceneStateRecorder.Instance.HasRecords())
        {
            targetColor = canRewindColor;
        }
        else
        {
            targetColor = normalColor;
        }

        buttonMaterial.color = Color.Lerp(buttonMaterial.color, targetColor, Time.deltaTime * 10f);
    }

    private void UpdateInfoText()
    {
        if (infoText == null) return;

        if (SceneStateRecorder.Instance == null)
        {
            infoText.text = "未初始化";
            infoText.color = Color.red;
        }
        else if (SceneStateRecorder.Instance.HasRecords())
        {
            int count = SceneStateRecorder.Instance.GetRecordCount();
            infoText.text = $"可回溯\n({count})";
            infoText.color = Color.green;
        }
        else
        {
            infoText.text = "无记录";
            infoText.color = Color.gray;
        }
    }

    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        isPressed = true;
        
        // 触发回溯
        if (SceneStateRecorder.Instance != null && SceneStateRecorder.Instance.HasRecords())
        {
            SceneStateRecorder.Instance.RewindToLastState();
            
            // 控制器震动反馈
            if (args.interactorObject.transform.TryGetComponent<XRBaseController>(out var controller))
            {
                controller.SendHapticImpulse(0.5f, 0.2f);
            }
        }
    }

    private void OnButtonReleased(SelectExitEventArgs args)
    {
        isPressed = false;
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // 悬停时轻微震动提示
        if (args.interactorObject.transform.TryGetComponent<XRBaseController>(out var controller))
        {
            controller.SendHapticImpulse(0.1f, 0.05f);
        }
    }

    private void OnDestroy()
    {
        // 取消注册事件
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnButtonPressed);
            interactable.selectExited.RemoveListener(OnButtonReleased);
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
        }
    }

    private void OnDrawGizmos()
    {
        // 显示按压范围
        Gizmos.color = Color.cyan;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + transform.TransformDirection(Vector3.down) * pressDistance;
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawWireSphere(endPos, 0.01f);
    }
}
