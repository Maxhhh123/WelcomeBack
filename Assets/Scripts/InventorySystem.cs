using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem :Singleton<InventorySystem> 
{
    //public List<ItemData> items = new List<ItemData>();
    private Queue<ItemData> itemQueue = new Queue<ItemData>();
    public Transform[] slotTransforms; // 物品栏格子的 Transform 数组
    public int maxItemCount = 9; // 最大物品数量
    void Start()
    {
        Debug.Log("Refreshing inventory display...");
        ItemData phoneItem = Resources.Load<ItemData>("ItemData/PhoneItem"); // 从 Resources 加载
        itemQueue.Enqueue(phoneItem);
        RefreshInventoryDisplay();
    }
    
    public void AddItemBack(ItemData itemData)
    {
        itemQueue.Enqueue(itemData);
        if (itemQueue.Count > maxItemCount)
        {
            Debug.Log("背包满了，请清理物品");
        }
        RefreshInventoryDisplay(); // 更新UI
    }
    private void UpdateSlots()
    {
        ItemData[] queuedItems = itemQueue.ToArray(); // 转换为数组
        for (int i = 0; i < itemQueue.Count; i++)
        {
            if (i < slotTransforms.Length && slotTransforms[i] != null)
            {
                // 获取或创建 ItemSlot 组件
                ItemSlot itemSlot = slotTransforms[i].GetComponent<ItemSlot>();
                if (itemSlot == null)
                {
                    itemSlot = slotTransforms[i].gameObject.AddComponent<ItemSlot>();
                }
                // 设置物品数据
                itemSlot.itemData = queuedItems[i];
            }
        }
    }
    
    
    public void RefreshInventoryDisplay()
{
    
    // 清空现有显示
    foreach (var slot in slotTransforms)
    {
        if (slot != null)
        {
            UnityEngine.UI.Image slotImage = slot.GetComponent<UnityEngine.UI.Image>();
            if (slotImage != null)
            {
                slotImage.gameObject.SetActive(false);
                slotImage.sprite = null;
                slotImage.enabled = false;
            }
        }
    }
    
    // 重新填充物品
    for (int i = 0; i < itemQueue.Count; i++)
    {
        ItemData[] queuedItems = itemQueue.ToArray(); // 转换为数组
        if (i < slotTransforms.Length && slotTransforms[i] != null)
        {
            UnityEngine.UI.Image slotImage = slotTransforms[i].GetComponent<UnityEngine.UI.Image>();
            if (slotImage != null && queuedItems[i].icon != null)
            {
                slotImage.gameObject.SetActive(true);
                slotImage.sprite = queuedItems[i].icon;
                slotImage.enabled = true;
            }
        }
    }
    
    UpdateSlots();//先更新每一个itemSlot的数据
    
}

}
