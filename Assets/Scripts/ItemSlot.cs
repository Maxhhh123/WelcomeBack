using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class ItemSlot : XRBaseInteractable
{
    //[SerializeField] private Material highlightMaterial;
    private Renderer slotRenderer;
    private Material originalMaterial;
    private Image highlightImage; // 用于存储高亮图像组件

    public ItemData itemData; // 包含名称、图标、介绍图等数据
    
    protected override void Awake()
    {
        base.Awake();
        slotRenderer = GetComponent<Renderer>();
        originalMaterial = slotRenderer.material;
        
        // 查找子物体中的高亮图像
        Transform highlightChild = transform.Find("Highlight");
        if (highlightChild != null)
        {
            highlightImage = highlightChild.GetComponent<Image>();
            if (highlightImage != null)
            {
                highlightImage.gameObject.SetActive(false); // 默认隐藏
            }
        }
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        //slotRenderer.material = highlightMaterial;
        // 显示高亮图像
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(true);
        }
        base.OnHoverEntered(args);
    }

    protected override void OnHoverExited(HoverExitEventArgs args)
    {
        slotRenderer.material = originalMaterial;
        // 隐藏高亮图像
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }
        base.OnHoverExited(args);
    }
    
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        UIManager.Instance.ShowItemDetails(itemData); // 显示详情,唯一的作用就是显示详情
        base.OnSelectEntered(args);
    }
}

