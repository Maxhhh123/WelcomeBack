using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DroppedItemReturner : MonoBehaviour
{
    [Header("设置")]
    public ItemData itemData;
    public float returnDelay = 1f; // 掉落多久后返回
    public bool destroyOnReturn = true; // 是否销毁物体
    
    [Header("重力控制")]
    public bool useGravityWhenGrabbed = true; // 抓取时启用重力
    public bool useGravityWhenDropped = true; // 释放时启用重力
    
    private XRGrabInteractable grabbable;
    private Rigidbody rb;
    private bool isGrabbed = false;
    private Coroutine returnCoroutine;
    private Vector3 lastPosition;
    private float stationaryTime = 0f;
    private bool isReturning = false; // 添加这个标志
    
    void Start()
    {
        InitializeComponents();
        SetupEventListeners();
        
        // 初始状态：无重力（如果物体刚生成）
        //InitializeGravity();
    }
    
    private void InitializeComponents()
    {
        grabbable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        
        if (grabbable == null)
        {
            Debug.LogError($"{gameObject.name}: 未找到XRGrabInteractable组件");
            enabled = false;
            return;
        }
        
        if (rb == null)
        {
            Debug.LogWarning($"{gameObject.name}: 未找到Rigidbody组件，正在添加");
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }
    
    
    private void SetupEventListeners()
    {
        // 使用新版事件系统
        grabbable.selectEntered.AddListener(OnGrabbed);
        grabbable.selectExited.AddListener(OnReleased);
    }
    
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        
        // 设置抓取时的重力状态
        if (rb != null && useGravityWhenGrabbed)
        {
            rb.useGravity = true; // 抓取时启用重力
        }
        
        // 重置速度，避免抓取时物体有惯性
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // 如果正在等待返回，取消返回
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }
    
    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        
        // 设置释放时的重力状态
        if (rb != null)
        {
            rb.useGravity = useGravityWhenDropped;
            
            // 如果是掉落状态，添加一点惯性效果
            if (useGravityWhenDropped && rb.velocity.magnitude < 0.1f)
            {
                // 给一个微小的随机力，让掉落更自然
                Vector3 randomForce = new Vector3(
                    Random.Range(-0.1f, 0.1f),
                    Random.Range(0.05f, 0.15f),
                    Random.Range(-0.1f, 0.1f)
                );
                rb.AddForce(randomForce, ForceMode.Impulse);
            }
        }
        
        // 开始检测是否应该返回
        returnCoroutine = StartCoroutine(CheckForReturn());
    }
    
    private IEnumerator CheckForReturn()
    {
        float timer = 0f;
        lastPosition = transform.position;
        stationaryTime = 0f;
        
        while (timer < returnDelay && !isGrabbed)
        {
            yield return null;
            timer += Time.deltaTime;
            
            // 检查物体是否静止（可选）
            //CheckIfStationary();
            
            // 如果被重新抓取，退出协程
            if (isGrabbed)
            {
                yield break;
            }
        }
        
        // 到达延迟时间后，如果没有被重新抓取，则返回物品
        if (!isGrabbed)
        {
            ReturnItemToInventory();
        }
    }
    
    private void ReturnItemToInventory()
    {
        if (isReturning || gameObject == null)
            return;
        
        // 检查是否已经被放置
        PlaceableObjectMarker marker = GetComponent<PlaceableObjectMarker>();
        if (marker != null && marker.IsPlaced)
        {
            Debug.Log("物品已被放置，不返回库存");
            return;
        }
        
        isReturning = true;
        
        if (itemData != null)
        {
            // 返回物品到库存
            Debug.Log("return now");
            InventorySystem.Instance?.AddItemBack(itemData);
        }
        
        if (destroyOnReturn)
        {
            Debug.Log("Destroying object: " + gameObject.name);
            Destroy(gameObject);
        }
        else
        {
            // 可选：隐藏物体而不是销毁
            gameObject.SetActive(false);
        }
    }
    
    // 手动触发返回（可用于按钮或其他事件）
    public void ManualReturnToInventory()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
        }
        ReturnItemToInventory();
    }
    
    // 外部调用：手动设置重力状态
    public void SetGravity(bool enable)
    {
        if (rb != null)
        {
            rb.useGravity = enable;
            
            // 如果禁用重力，也重置速度
            if (!enable && !rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    
    // 清理事件监听（重要！避免内存泄漏）
    private void OnDestroy()
    {
        if (grabbable != null)
        {
            grabbable.selectEntered.RemoveListener(OnGrabbed);
            grabbable.selectExited.RemoveListener(OnReleased);
        }
        
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
        }
    }
}