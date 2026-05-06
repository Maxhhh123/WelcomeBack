using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InventorySystem :Singleton<InventorySystem> 
{
    //public List<ItemData> items = new List<ItemData>();
    private Queue<ItemData> itemQueue = new Queue<ItemData>();
    public Transform[] slotTransforms; // 物品栏格子的 Transform 数组
    public int maxItemCount = 9; // 最大物品数量
    public ItemSlot itemSlot;

    // 初始物品列表（用于重置）
    private List<ItemData> initialItems = new List<ItemData>();

    void Start()
    {
        Debug.Log("Refreshing inventory display...");
        InitializeInventory();

        // 注册回溯事件监听
        SceneStateRecorder.OnRewindCompleted += OnSceneRewind;
    }

    void OnDestroy()
    {
        // 取消注册事件监听
        SceneStateRecorder.OnRewindCompleted -= OnSceneRewind;
    }

    /// <summary>
    /// 初始化背包物品
    /// </summary>
    private void InitializeInventory()
    {
        itemQueue.Clear();
        initialItems.Clear();

        ItemData phoneItem = Resources.Load<ItemData>("ItemData/PhoneItem");
        ItemData TV = Resources.Load<ItemData>("ItemData/TV");
        ItemData Door = Resources.Load<ItemData>("ItemData/door");
        ItemData wall = Resources.Load<ItemData>("ItemData/wall");
        ItemData wifi = Resources.Load<ItemData>("ItemData/wifi");
        ItemData AI = Resources.Load<ItemData>("ItemData/AI");
        ItemData chair = Resources.Load<ItemData>("ItemData/chair");
        ItemData table = Resources.Load<ItemData>("ItemData/table");
        ItemData bodydetect = Resources.Load<ItemData>("ItemData/BodyDetect");
        ItemData press = Resources.Load<ItemData>("ItemData/pressure");      
        ItemData yanwu = Resources.Load<ItemData>("ItemData/yanwu");  
        ItemData AirConditioner = Resources.Load<ItemData>("ItemData/AirConditioner");

        // 添加到初始列表
        initialItems.Add(phoneItem);
        initialItems.Add(TV);
        initialItems.Add(bodydetect);
        initialItems.Add(AirConditioner);
        initialItems.Add(yanwu);
        initialItems.Add(wifi);
        initialItems.Add(AI);
        initialItems.Add(press);

        // 添加到队列
        foreach (var item in initialItems)
        {
            if (item != null)
            {
                itemQueue.Enqueue(item);
            }
        }

        RefreshInventoryDisplay();
    }

    /// <summary>
    /// 场景回溯时调用 - 重置背包到初始状态
    /// </summary>
    private void OnSceneRewind()
    {
        Debug.Log("[InventorySystem] 场景回溯 - 重置背包");
        
        // 清空当前背包
        itemQueue.Clear();
        
        // 重新添加初始物品
        foreach (var item in initialItems)
        {
            if (item != null)
            {
                itemQueue.Enqueue(item);
            }
        }
        
        // 刷新显示
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
    
    public void RefreshInventoryDisplay()
    {
        // 清空现有显示
        foreach (var slot in slotTransforms)
        {
            if (slot != null)
            {
                // 查找名为 "image" 的子物体
                Transform imageChild = slot.Find("image");
                if (imageChild != null)
                {
                    UnityEngine.UI.Image slotImage = imageChild.GetComponent<UnityEngine.UI.Image>();
                    if (slotImage != null)
                    {
                        imageChild.GameObject().SetActive(false);
                        slotImage.sprite = null;
                        slotImage.enabled = false;
                    }
                }
            }
        }
    
        // 重新填充物品
        for (int i = 0; i < itemQueue.Count; i++)
        {
            ItemData[] queuedItems = itemQueue.ToArray();
            if (i < slotTransforms.Length && slotTransforms[i] != null)
            {
                // 查找名为 "image" 的子物体
                Transform imageChild = slotTransforms[i].Find("image");
                if (imageChild != null)
                {
                    UnityEngine.UI.Image slotImage = imageChild.GetComponent<UnityEngine.UI.Image>();
                    if (slotImage != null && queuedItems[i].icon != null)
                    {
                        imageChild.GameObject().SetActive(true);
                        slotImage.sprite = queuedItems[i].icon;
                        slotImage.enabled = true;
                    }
                }
            }
        }
        itemSlot.LoadItemsFromInventory(itemQueue);
    }

    public void RemoveSpecificItem(ItemData itemToRemove)
    {
        // 将队列转换为列表以便操作
        List<ItemData> itemList = new List<ItemData>(itemQueue);
    
        // 从列表中移除指定物品
        bool removed = itemList.Remove(itemToRemove);
    
        if (removed)
        {
            // 如果成功移除了物品，重新构建队列
            itemQueue.Clear();
            foreach (var item in itemList)
            {
                itemQueue.Enqueue(item);
            }
        
            RefreshInventoryDisplay(); // 更新UI显示
        }
        else
        {
            Debug.LogWarning("尝试移除不存在的物品");
        }
    }


}
