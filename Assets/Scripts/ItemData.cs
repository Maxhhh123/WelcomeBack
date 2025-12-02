using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]  //创建data资产
public class ItemData : ScriptableObject
{
    [Header("基本信息")]
    public string name;                    // 物品名称
    public Sprite icon;                       // 物品图标（格子中显示）
    public Sprite detailImage;                // 物品详情图（右侧展示）

    [Header("预制体相关")]
    public GameObject prefab;                 // 物品的3D预制体
    [HideInInspector]
    public GameObject spawnedObject;          // 已生成的实例引用

    [Header("交互设置")]
    public bool isConsumable = false;         // 是否为消耗品
    public int maxStackCount = 1;             // 最大堆叠数量
}
