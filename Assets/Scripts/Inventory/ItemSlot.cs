using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class ItemSlot :  MonoBehaviour
{
    [System.Serializable]
    public class Item
    {
        [Tooltip("导航栏按钮组件")]
        public Button button;

        [Tooltip("高亮指示器（Indicator）")]
        public GameObject Indicator;
        
        [Tooltip("图标资源(itemData)")]
        public ItemData itemData;
    }

    [Header("导航项配置")]
    public List<Item> Items = new List<Item>();

    [Header("外观设置")]
    // 选中状态下的颜色
    public Color selectedColor = new Color(0f, 0.282f, 1f, 1f);
    // 悬停状态下的颜色
    public Color hoverColor = new Color(0.122f, 0.122f, 0.122f, 1f);
    
    
    private int currentIndex = -1;
    
    
    private Transform highlightChild;
    private Button slotButton;
    void Start()
    {
        // 初始化导航栏
        
    }

    void Initialize()
    {
        foreach (var item in Items)
        {
            // 初始化按钮状态，隐藏高亮标识Indicator
            SetBtnVisualState(item, false, false);
            // 绑定按钮的点击事件
            item.button.onClick.AddListener(() => OnButtonClick(item));
        }
    }

    public void LoadItemsFromInventory(Queue<ItemData> itemQueue)
    {
        int index = 0;
        foreach (ItemData itemData in itemQueue)
        {
            Item newItem = new Item
            {
                itemData = itemData,
                button = GetButtonAtIndex(index),
                Indicator = GetIndicatorAtIndex(index)
            };
            
            Items.Add(newItem);
            index++;
        }
        Initialize();  //先将数据加载到Items中，再初始化导航栏
    }

    private GameObject GetIndicatorAtIndex(int index)
    {
        return transform.GetChild(index).Find("Highlight").GameObject();
    }

    private Button GetButtonAtIndex(int index)
    {
        if (index < transform.childCount)
        {
            return transform.GetChild(index).GetComponent<Button>();
        }
        return null;
    }
    
    
    public void OnButtonClick(Item clickedItem)
    {
        int targetIndex = Items.IndexOf(clickedItem);
        
        if (currentIndex != targetIndex)
        {
            if (currentIndex >= 0)
            {
                var prevItem = Items[currentIndex];
                if (ValidateItem(prevItem))
                {
                    SetBtnVisualState(prevItem, false, false);
                }
            }
            var newItem = Items[targetIndex];
            if (ValidateItem(newItem))
            {
                UIManager.Instance.ShowItemDetails(clickedItem.itemData); //显示物品信息
                // 设置新面板的视觉状态为选中且非悬停
                SetBtnVisualState(newItem, true, false);

                currentIndex = targetIndex;
            }
        }
        
        
    }

    // 设置导航项的视觉状态，item为导航项，isSelected为是否选中，isHovered为是否悬停，
    private void SetBtnVisualState(Item item, bool isSelected, bool isHovered)
    {
        if (item.Indicator != null)
        {
            // 如果被点击或悬停，显示高亮背景，Indicator根据两种状态分别显示不同的颜色
            if (isSelected || isHovered)
            {
                // 显示按钮高亮背景Indicator，即当前按钮呈现被点击的颜色视觉反馈
                item.Indicator.SetActive(true);
                // 选中状态优先于悬停状态,即选中状态显示选中颜色，悬停状态显示悬停颜色
                item.Indicator.GetComponent<Image>().color = isSelected ? selectedColor : hoverColor;
            }
            else
            {
                // 非选中且非悬停状态下，隐藏高亮标识Indicator
                item.Indicator.SetActive(false);
            }
        }
    }

    private bool ValidateItem(Item item)
    {
        bool isValid = item.button != null && item.itemData != null && item.Indicator != null;
        return isValid;
    }
    
    
    // 用于Event Trigger的公共方法 - 指针进入，ItemIndex为导航项的索引
    public void OnPointerEnter(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= Items.Count) return;

        Item item = Items[itemIndex];
        // 如果当前按钮是选中的，不响应悬停移入
        if (itemIndex == currentIndex) return;

        // 显示指示器并设置为悬停颜色
        if (item.Indicator != null)
        {
            item.Indicator.SetActive(true);
            item.Indicator.GetComponent<Image>().color = hoverColor;
        }
    }

    // 用于Event Trigger的公共方法 - 指针离开，ItemIndex为导航项的索引
    public void OnPointerExit(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= Items.Count) return;

        Item item = Items[itemIndex];

        // 如果当前按钮是选中的，不响应悬停移出
        if (itemIndex == currentIndex) return;

        // 如果指示器Indicator存在，则隐藏指示器
        if (item.Indicator != null)
        {
            item.Indicator.SetActive(false);
        }
    }
}


