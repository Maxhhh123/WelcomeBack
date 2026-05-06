// UIRewindSetup.cs
// UI场景回溯设置指南和辅助脚本
using UnityEngine;
using UnityEngine.UI;

public class UIRewindSetup : MonoBehaviour
{
    [Header("自动设置")]
    public bool autoSetupOnStart = true;
    
    [Header("UI元素")]
    public Canvas targetCanvas;
    public Button rewindButton;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupCanvasForXR();
        }
    }

    /// <summary>
    /// 自动设置Canvas支持XR射线交互
    /// </summary>
    [ContextMenu("Setup Canvas for XR")]
    public void SetupCanvasForXR()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponent<Canvas>();
        }

        if (targetCanvas == null)
        {
            Debug.LogError("[UI设置] 未找到Canvas组件！");
            return;
        }

        // 设置Canvas为World Space（XR中必须的）
        targetCanvas.renderMode = RenderMode.WorldSpace;
        
        // 设置合适的尺寸
        RectTransform rectTransform = targetCanvas.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(800, 600);
        }

        // 添加或获取Graphic Raycaster（用于射线检测UI）
        GraphicRaycaster graphicRaycaster = targetCanvas.GetComponent<GraphicRaycaster>();
        if (graphicRaycaster == null)
        {
            graphicRaycaster = targetCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        Debug.Log("[UI设置] Canvas已设置为XR射线交互模式");
    }

    /// <summary>
    /// 创建默认的回溯按钮
    /// </summary>
    [ContextMenu("Create Default Rewind Button")]
    public void CreateDefaultRewindButton()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponent<Canvas>();
        }

        if (targetCanvas == null)
        {
            Debug.LogError("[UI设置] 需要先设置Canvas！");
            return;
        }

        // 创建按钮GameObject
        GameObject buttonObj = new GameObject("RewindButton");
        buttonObj.transform.SetParent(targetCanvas.transform, false);

        // 添加RectTransform并设置大小
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 80);
        rectTransform.anchoredPosition = Vector2.zero;

        // 添加Image组件
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.8f, 1f, 1f);

        // 添加Button组件
        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.8f, 1f, 1f);
        colors.highlightedColor = new Color(0.4f, 0.9f, 1f, 1f);
        colors.pressedColor = new Color(0.1f, 0.6f, 0.9f, 1f);
        button.colors = colors;

        // 添加自定义脚本
        RewindUIButton rewindUI = buttonObj.AddComponent<RewindUIButton>();

        // 创建文字子物体
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text text = textObj.AddComponent<Text>();
        text.text = "回溯";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        rewindUI.buttonText = text;

        Debug.Log("[UI设置] 已创建默认回溯按钮");
    }

    private void OnDrawGizmos()
    {
        // 显示Canvas的朝向和位置
        if (targetCanvas != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(targetCanvas.transform.position, new Vector3(1.6f, 1.2f, 0.05f));
            
            // 绘制朝向
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(targetCanvas.transform.position, targetCanvas.transform.forward * 0.5f);
        }
    }
}
