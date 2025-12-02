using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 导航栏控制器
public class NavbarController : MonoBehaviour
{
    // 创建一个数据类型，用于存储导航栏项的配置，可以在Inspector窗口中配置
    [System.Serializable]
    public class NavItem
    {
        [Tooltip("导航栏按钮组件")]
        public Button button;

        [Tooltip("需要控制显示/隐藏的右侧页面")]
        public GameObject targetPanel;

        [Tooltip("高亮指示器（Indicator）")]
        public Image Indicator;
    }

    [Header("导航项配置")]
    public List<NavItem> navItems = new List<NavItem>();

    [Header("外观设置")]
    // 选中状态下的颜色
    public Color selectedColor = new Color(0f, 0.282f, 1f, 1f);
    // 悬停状态下的颜色
    public Color hoverColor = new Color(0.122f, 0.122f, 0.122f, 1f);
    // 说明：设置这一机制而不是仅通过Button组件的状态颜色
    // 是因为Button组件的状态颜色在导航栏的交互逻辑下，可能无法正确反映当前状态
    // 例如，当点击按钮以后再与当前页面中的控件交互，那么当前按钮焦点就会丢失，导致
    // 按钮状态颜色无法正确更新

    // 当前选中的索引，默认为-1，表示没有选中的面板
    private int currentIndex = -1;

    void Start()
    {
        // 初始化导航栏
        InitializeNavigation();
        // 自动选择第一个有效的导航项
        AutoSelectFirstValidItem();
    }

    // 初始化导航栏
    void InitializeNavigation()
    {
        foreach (var item in navItems)
        {
            if (!ValidateNavItem(item)) continue;

            // 先隐藏所有页面
            item.targetPanel.SetActive(false);

            // 初始化按钮状态，隐藏高亮标识Indicator
            SetBtnVisualState(item, false, false);

            // 绑定按钮的点击事件
            item.button.onClick.AddListener(() => OnNavButtonClick(item));
        }
    }

    // 导航按钮点击事件处理函数
    void OnNavButtonClick(NavItem clickedItem)
    {
        // 获取点击按钮的索引序号，确定目标页面
        int targetIndex = navItems.IndexOf(clickedItem);
        // 如果点击按钮的索引序号为-1，则不进行切换
        if (targetIndex == -1) return;

        // 如果当前索引不等于目标索引，则切换到目标页面
        if (currentIndex != targetIndex)
        {
            SwitchToPanel(targetIndex);
        }
    }

    // 切换到指定页面
    void SwitchToPanel(int newIndex)
    {
        // 关闭旧页面
        // 如果当前索引大于0且小于导航项数量，则关闭旧面板
        if (currentIndex >= 0 && currentIndex < navItems.Count)
        {
            if (newIndex == currentIndex)
                // 如果新索引等于当前索引，则不进行切换
                return;

            var prevItem = navItems[currentIndex];
            // 如果旧面板有效，则关闭旧面板
            if (ValidateNavItem(prevItem))
            {
                prevItem.targetPanel.SetActive(false);
                // 设置旧面板的视觉状态为非选中且非悬停
                SetBtnVisualState(prevItem, false, false);
            }
        }

        // 打开新页面
        var newItem = navItems[newIndex];
        // 如果新面板有效，则打开新面板
        if (ValidateNavItem(newItem))
        {
            // 打开新面板
            newItem.targetPanel.SetActive(true);
            // 设置新面板的视觉状态为选中且非悬停
            SetBtnVisualState(newItem, true, false);
            // 更新当前索引
            currentIndex = newIndex;
        }
    }

    // 设置导航项的视觉状态，item为导航项，isSelected为是否选中，isHovered为是否悬停，
    void SetBtnVisualState(NavItem item, bool isSelected, bool isHovered)
    {
        // 确定指示器状态和颜色
        if (item.Indicator != null)
        {
            // 如果被点击或悬停，显示高亮背景，Indicator根据两种状态分别显示不同的颜色
            if (isSelected || isHovered)
            {
                // 显示按钮高亮背景Indicator，即当前按钮呈现被点击的颜色视觉反馈
                item.Indicator.gameObject.SetActive(true);
                // 选中状态优先于悬停状态,即选中状态显示选中颜色，悬停状态显示悬停颜色
                item.Indicator.color = isSelected ? selectedColor : hoverColor;
            }
            else
            {
                // 非选中且非悬停状态下，隐藏高亮标识Indicator
                item.Indicator.gameObject.SetActive(false);
            }
        }
    }

    // 自动选择第一个有效的导航项
    void AutoSelectFirstValidItem()
    {
        for (int i = 0; i < navItems.Count; i++)
        {
            if (ValidateNavItem(navItems[i]))
            {
                SwitchToPanel(i);
                return;
            }
        }
        Debug.LogError("没有找到有效的导航项配置！");
    }

    // 验证导航项配置,确保按钮和面板不为空
    bool ValidateNavItem(NavItem item)
    {
        // 确保配置成员包含的按钮、面板和指示器均不为空
        bool isValid = item.button != null && item.targetPanel != null && item.Indicator != null;

        if (!isValid)
        {
            Debug.LogWarning("发现无效导航项配置，请检查以下对象：\n" +
                           $"Button: {item.button?.name ?? "null"}\n" +
                           $"Panel: {item.targetPanel?.name ?? "null"}");
        }
        return isValid;
    }

    
    
    // 用于Event Trigger的公共方法 - 指针进入，ItemIndex为导航项的索引
    public void OnPointerEnter(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= navItems.Count) return;

        NavItem item = navItems[itemIndex];
        // 如果当前按钮是选中的，不响应悬停移入
        if (itemIndex == currentIndex) return;

        // 显示指示器并设置为悬停颜色
        if (item.Indicator != null)
        {
            item.Indicator.gameObject.SetActive(true);
            item.Indicator.color = hoverColor;
        }
    }

    // 用于Event Trigger的公共方法 - 指针离开，ItemIndex为导航项的索引
    public void OnPointerExit(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= navItems.Count) return;

        NavItem item = navItems[itemIndex];

        // 如果当前按钮是选中的，不响应悬停移出
        if (itemIndex == currentIndex) return;

        // 如果指示器Indicator存在，则隐藏指示器
        if (item.Indicator != null)
        {
            item.Indicator.gameObject.SetActive(false);
        }
    }
}