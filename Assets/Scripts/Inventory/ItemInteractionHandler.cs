using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class  ItemInteractionHandler : Singleton<ItemInteractionHandler>
{
    [Header("生成设置")]
    public Transform playerHead; // 玩家头部/摄像机
    public float spawnDistance = 1.5f; // 生成距离
    public float spawnHeight = 0.5f; // 相对于视线的Y轴偏移
    
    public float maxSize = 1f; // 最大尺寸（scaleToFit=true时）
    
    [Header("摆放系统")]
    public PlacementModeController placementController; // 摆放控制器
    public bool autoEnterPlacementOnGrab = true; // 抓取时自动进入摆放模式
    private void Start()
    {
        if (playerHead == null)
        {
            playerHead = Camera.main?.transform;
        }
    }
    

    /// 在玩家前方生成物体
    public GameObject SpawnItem(GameObject itemPrefab)
    {
        if (itemPrefab == null || playerHead == null)
        {
            Debug.LogWarning("无法生成物体：缺少预制件或玩家头部参考");
            return null;
        }
        
        // // 计算生成位置（玩家视线前方）
        // Vector3 spawnPosition = playerHead.position +playerHead.forward * spawnDistance + Vector3.up * spawnHeight;
        //
        // // 生成物体
        // GameObject spawnedObject = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);
        
        // 设置生成位置在玩家面前一定距离处
        Vector3 spawnPosition = Camera.main.transform.position + Camera.main.transform.forward * 1f;
    
        // 获取玩家的水平朝向
        Vector3 playerForward = Camera.main.transform.forward;
        playerForward.y = 0;
        playerForward.Normalize();
    
        // 计算朝向玩家前方的旋转
        Quaternion spawnRotation = Quaternion.LookRotation(playerForward, Vector3.up);
    
        // 实例化物体
        GameObject spawnedObject = Instantiate(itemPrefab, spawnPosition, spawnRotation);
        
        ScaleObject(spawnedObject);
        
        // 配置可抓取组件
        SetupGrabbableObject(spawnedObject);
        
        // 添加抓取监听，用于进入摆放模式
        AddGrabListener(spawnedObject);
        
        return spawnedObject;
    }
    private void AddGrabListener(GameObject obj)
    {
        XRGrabInteractable grab = obj.GetComponent<XRGrabInteractable>();
        if (grab != null && autoEnterPlacementOnGrab)
        {
            grab.selectEntered.AddListener((args) => 
            {
                if (placementController != null)
                {
                    placementController.EnterPlacementMode(obj, args.interactorObject);
                }
            });
        }
    }
    private void ScaleObject(GameObject obj)
    { 
        // 方法2：根据边界盒自动缩放
        Bounds bounds = CalculateBounds(obj);
        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDimension > 0)
            {
                float scaleFactor = maxSize / maxDimension;
                obj.transform.localScale = Vector3.one * scaleFactor;
            }
        
        
        // 方法3：保留原始缩放（不处理）
    }
    /// <summary>
    /// 计算物体的边界盒
    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.one);
        
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }
    
    /// <summary>
    /// 配置物体为可抓取状态
    /// </summary>
    private void SetupGrabbableObject(GameObject obj)
    {
        // 确保有XRGrabInteractable组件
        XRGrabInteractable grabInteractable = obj.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = obj.AddComponent<XRGrabInteractable>();
        }
        
        // 确保有刚体
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }
        
        // 初始设置：无重力，可抓取
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.drag = 1f; // 防止物体飘走
        rb.angularDrag = 1f;
        
        // 确保有碰撞体（如果预制件没有）
        if (obj.GetComponent<Collider>() == null)
        {
            // 尝试自动添加合适的碰撞体
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.mesh != null)
            {
                obj.AddComponent<MeshCollider>().convex = true;
            }
            else
            {
                obj.AddComponent<BoxCollider>();
            }
        }
        
        // 可选：添加视觉反馈
        AddHoverEffect(obj);
    }
    
    /// <summary>
    /// 可选：添加悬停效果
    /// </summary>
    private void AddHoverEffect(GameObject obj)
    {
        // 可以添加一个简单的悬停脚本，让物体轻微浮动
        obj.AddComponent<SimpleHoverEffect>();
    }
    
}

// 可选：简单的悬停效果组件
public class SimpleHoverEffect : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 startPosition;
    
    [SerializeField] private float hoverHeight = 0.2f;
    [SerializeField] private float hoverSpeed = 2f;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
    }
    
    private void FixedUpdate()
    {
        if (rb != null)
        {
            // 简单的正弦波悬停
            float newY = startPosition.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight * 0.1f;
            Vector3 targetPosition = new Vector3(startPosition.x, newY, startPosition.z);
            
            // 使用力保持悬停位置
            Vector3 forceDirection = (targetPosition - transform.position);
            rb.AddForce(forceDirection * 5f);
            
            // 限制速度防止飘走
            if (rb.velocity.magnitude > 1f)
            {
                rb.velocity = rb.velocity.normalized * 1f;
            }
        }
    }
    
    // 当被抓取时禁用悬停效果
    public void OnGrabbed()
    {
        enabled = false;
        if (rb != null)
        {
            rb.useGravity = true;
        }
    }
}

