using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PlacementModeController : MonoBehaviour
{
    [Header("手柄参考")]
    public NearFarInteractor rightNearFarInteractor;   // 右手NearFar交互器
    public Transform rightHand;                         // 右手控制器位置
    
    [Header("摆放设置")]
    public LayerMask placementMask ;                     // 可摆放表面层级
    public LayerMask ignoreLayers;                       // 忽略的层级（如床、墙壁等）
    public float placementDistance = 10f;                // 最大摆放距离
    public float rotationSpeed = 100f;                  // 旋转速度
    public float scaleSpeed = 0.5f;                     // 缩放速度
    public float minScale = 0.2f;                       // 最小缩放
    public float maxScale = 3f;                         // 最大缩放
    public float surfaceOffset = 0.05f;                 // 表面偏移量
    
    [Header("吸附设置")]
    public bool snapToGrid = true;                      // 吸附到网格
    public float gridSize = 0.5f;                        // 网格大小
    
    [Header("视觉反馈")]
    public GameObject previewPrefab;                     // 预览预制体（可选）
    public Color validColor = Color.green;
    public Color invalidColor = Color.red;
    public float previewTransparency = 0.6f;             // 预览透明度
    
    // 🔥 新增：射线可视化设置
    [Header("射线可视化")]
    public bool showRayVisualization = true;            // 是否显示射线
    public float rayWidth = 0.01f;                      // 射线宽度
    public Material rayMaterial;                        // 射线材质（可选）
    
    [Header("Socket 设置")]
    public bool enableSocketPlacement = true;        // 启用 Socket 放置
    public LayerMask socketMask;                      // Socket 层级
    public float socketDetectionRadius = 0.5f;        // Socket 检测半径
    
    private SocketPlacementPoint currentSocket;       // 当前检测到的 Socket
    
    // 状态变量
    public GameObject currentObject;                    // 手中抓取的物体
    public GameObject previewObject;                    // 预览物体
    private bool isPlacementMode = false;                // 是否在摆放模式
    private bool isValidPlacement = false;                // 当前位置是否可放置
    
    // 变换数据
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float targetScale = 1f;
    private GameObject hitSurface;
    private Vector3 hitNormal;
    private PlaceableSurface currentSurface;
    
    // 🔥 新增：射线渲染器相关
    private LineRenderer rayLineRenderer;
    private GameObject rayVisualizationObject;
    
    // 原始数据
    private Rigidbody objectRb;
    private XRGrabInteractable grabInteractable;
    private Collider[] objectColliders;
    private IXRSelectInteractor currentInteractor;
    private Renderer[] originalRenderers;
    private Material[] originalMaterials;
    
    // 输入状态
    private bool isRotating = false;
    private bool isScaling = false;
    private bool wasGripPressed = false;
    private bool isPlacementConfirmed = false; // 新增：标记是否已确认放置
    private bool isExiting = false; // 新增：防止重复退出
    
    
    
    // 单例
    public static PlacementModeController Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }
    
    private void Start()
    {
        if (rightHand == null)
            Debug.LogError("请将右手控制器拖拽到 PlacementModeController 的 rightHand 字段");
            
        if (rightNearFarInteractor == null)
            Debug.LogError("请将 NearFarInteractor 拖拽到 PlacementModeController 的 rightNearFarInteractor 字段");
            
        if (placementMask == 0)
            placementMask = 1 << LayerMask.NameToLayer("PlaceableSurface");
            
        Debug.Log("PlacementModeController 初始化完成");
    }
    
    // 🔥 修改：将射线可视化组件添加到当前手持物体上

    // 🔥 新增：在进入摆放模式时创建射线可视化
    private void CreateRayVisualizationForCurrentObject()
    {
        if (!showRayVisualization || currentObject == null) return;
        
        // 销毁之前的射线对象（如果存在）
        if (rayVisualizationObject != null)
        {
            Destroy(rayVisualizationObject);
        }
        
        // 在当前手持物体上创建射线可视化对象
        rayVisualizationObject = new GameObject("RayVisualization");
        rayVisualizationObject.transform.SetParent(currentObject.transform);
        rayVisualizationObject.transform.localPosition = Vector3.zero;
        rayVisualizationObject.transform.localRotation = Quaternion.identity;
        
        // 添加 LineRenderer 组件
        rayLineRenderer = rayVisualizationObject.AddComponent<LineRenderer>();
        
        // 设置 LineRenderer 属性
        rayLineRenderer.useWorldSpace = true;
        rayLineRenderer.startWidth = rayWidth;
        rayLineRenderer.endWidth = rayWidth;
        rayLineRenderer.positionCount = 2;
        
        // 如果没有指定材质，创建默认材质
        if (rayMaterial == null)
        {
            rayMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
        rayLineRenderer.material = rayMaterial;
        
        // 默认隐藏射线
        rayLineRenderer.enabled = false;
    }

    // 🔥 修改：更新射线显示方法
    private void UpdateRayVisualization(Vector3 origin, Vector3 direction, float distance, bool isValid)
    {
        if (!showRayVisualization || rayLineRenderer == null) return;
        
        // 设置射线起点和终点
        Vector3 endPoint = origin + direction * distance;
        rayLineRenderer.SetPosition(0, origin);
        rayLineRenderer.SetPosition(1, endPoint);
        
        // 根据放置状态设置颜色
        Color rayColor = isValid ? validColor : invalidColor;
        rayLineRenderer.startColor = rayColor;
        rayLineRenderer.endColor = rayColor;
        
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = rayColor;
        rayLineRenderer.material = mat;
        // 显示射线
        rayLineRenderer.enabled = true;
    }
    
    // 🔥 新增：隐藏射线
    private void HideRayVisualization()
    {
        if (rayLineRenderer != null)
        {
            rayLineRenderer.enabled = false;
        }
    }
    
    private void Update()
    {
        if (!isPlacementMode || currentObject == null) 
        {
            // 不在摆放模式时隐藏射线
            HideRayVisualization();
            return;
        }
        
        // 处理手柄输入
        HandleInput();
        
        // 更新摆放预览
        UpdatePlacementPreview();
        
        // 检查侧键是否松开
        //CheckGripReleased();
    }
    
    private void OnDrawGizmosSelected()
    {
        // 绘制 Socket 检测范围
        if (Application.isPlaying && isPlacementMode && enableSocketPlacement)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, socketDetectionRadius);
        }
    }
    
    /// <summary>
    /// 进入摆放模式（由 GrabListener 调用）
    /// </summary>
    public void EnterPlacementMode(GameObject obj, IXRSelectInteractor interactor)
    {
        if (obj == null || isPlacementMode) return;
        
        Debug.Log($"进入摆放模式: {obj.name}");
        
        currentObject = obj;
        currentInteractor = interactor;
        objectRb = obj.GetComponent<Rigidbody>();
        grabInteractable = obj.GetComponent<XRGrabInteractable>();
        
        // 重要：先移除所有旧的监听，再添加新的
        grabInteractable.selectExited.RemoveListener(OnReleased);
        grabInteractable.selectExited.AddListener(OnReleased);
        
        objectColliders = obj.GetComponentsInChildren<Collider>();
        
        // 保存原始缩放
        targetScale = 1f; 
        targetRotation = obj.transform.rotation;
        
        // 保存原始材质
        originalRenderers = currentObject.GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[originalRenderers.Length];
        for (int i = 0; i < originalRenderers.Length; i++)
        {
            originalMaterials[i] = originalRenderers[i].material;
        }
        
        // 禁用碰撞体避免干扰射线
        foreach (var col in objectColliders)
        {
            col.enabled = false;
        }
        
        // 创建预览物体
        CreatePreviewObject();
        previewObject.SetActive(false);
        
        // 为当前物体创建射线可视化
        CreateRayVisualizationForCurrentObject();
        
        isPlacementMode = true;
        isPlacementConfirmed = false;
        isExiting = false;
        wasGripPressed = true;
        
        // 震动提示
        SendHapticFeedback(0.3f, 0.1f);
    }

    /// <summary>
    /// 修复后的释放处理方法
    /// </summary>
    private void OnReleased(SelectExitEventArgs args)
    {
        // 防止重复调用
        if (isExiting || !isPlacementMode) return;
        
        Debug.Log($"OnReleased 被调用，当前物体: {currentObject?.name}, 预览物体: {previewObject?.name}");
        
        // 标记正在退出，防止重复处理
        isExiting = true;
        
        try
        {
            // 检查是否已经有确认标记（防止重复处理）
            if (isPlacementConfirmed)
            {
                Debug.Log("已经确认过放置，跳过处理");
                return;
            }
            
            isPlacementConfirmed = true;
            
            // 根据放置有效性决定如何处理
            if (IsValidPlacement())
            {
                // ✅ 有效放置 - 用预览物体替换原物体
                HandleValidPlacement();
            }
            else
            {
                // ❌ 无效放置 - 返回原物体
                HandleInvalidPlacement();
            }
            
            // 震动反馈
            SendHapticFeedback(0.5f, 0.2f);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"放置处理出错: {e.Message}");
        }
        finally
        {
            // 确保退出摆放模式
            ExitPlacementMode();
        }
    }
    
/// <summary>
    /// 处理有效放置
    /// </summary>
    private void HandleValidPlacement()
    {
        Debug.Log("处理有效放置");
        
        if (previewObject == null || currentObject == null) return;
        
        // 1. 准备预览物体
        previewObject.transform.position = targetPosition;
        previewObject.transform.rotation = targetRotation;
        previewObject.transform.localScale = Vector3.one * targetScale;
        
        // 2. 如果是 Socket 放置，激活 Socket Interactor
        if (currentSocket != null && enableSocketPlacement)
        {
            ActivateSocketInteractor(previewObject, currentSocket.socketInteractor);
        }
        
        // 3. 启用预览物体的碰撞体
        Collider[] previewColliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (var col in previewColliders)
        {
            col.enabled = true;
        }
        
        // 4. 移除预览物体上不需要的组件
        RemoveUnnecessaryComponents(previewObject);
        
        // 5. 添加放置标记
        var marker = previewObject.AddComponent<PlaceableObjectMarker>();
        marker.IsPlaced = true;
        
        // 6. 设置预览物体的物理属性
        Rigidbody previewRb = previewObject.GetComponent<Rigidbody>();
        if (previewRb != null)
        {
            previewRb.isKinematic = true;
            previewRb.useGravity = false;
            previewRb.drag = 1f;
            previewRb.angularDrag = 1f;
        }
        
        // 7. 强制释放所有选择该物体的交互器
        if (grabInteractable != null)
        {
            // 使用 interactorsSelecting 数组来获取当前正在选择的交互器
            if (grabInteractable.interactorsSelecting.Count > 0)
            {
                var selectingInteractor = grabInteractable.interactorsSelecting[0];
                grabInteractable.interactionManager.SelectExit(selectingInteractor, grabInteractable);
                Debug.Log($"强制释放交互：{selectingInteractor} -> {grabInteractable}");
            }
        }

        MirrorChildBodySensorsToPreview();
        
        // 8. 销毁原物体

        if (currentObject != null)
        {
            Destroy(currentObject);
        }
        
        // 9. 清理 Socket 引用
        currentSocket = null;
        
        // 10. 将预览物体设为 null，避免在 ExitPlacementMode 中被销毁
        previewObject = null;
    }
    
    /// <summary>
    /// 激活 Socket Interactor 以吸附物体
    /// </summary>
    private void ActivateSocketInteractor(GameObject placedObject, XRSocketInteractor socket)
    {
        if (socket == null || placedObject == null) return;
        
        // 确保 Socket 处于激活状态
        socket.socketActive = true;
        
        // 获取物体的 XRGrabInteractable 组件
        XRGrabInteractable grabInteractable = placedObject.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = placedObject.AddComponent<XRGrabInteractable>();
        }
        
        // 将物体移动到 Socket 位置并激活
        placedObject.transform.SetPositionAndRotation(socket.transform.position, socket.transform.rotation);
        
        Debug.Log($"物体 {placedObject.name} 已吸附到 Socket {socket.name}");
    }
    
    /// <summary>
    /// 处理无效放置
    /// </summary>
    private void HandleInvalidPlacement()
    {
        Debug.Log("处理无效放置");
        
        // 1. 销毁预览物体
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
        
        // 2. 恢复原物体
        if (currentObject != null)
        {
            // 重新启用碰撞体
            Collider[] colliders = currentObject.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = true;
            }
            
            // 恢复物理属性
            if (objectRb != null)
            {
                objectRb.useGravity = true;
                objectRb.isKinematic = false;
            }
            
            // 让物体返回背包（如果有返回逻辑）
            DroppedItemReturner returner = currentObject.GetComponent<DroppedItemReturner>();
            if (returner != null)
            {
                returner.ManualReturnToInventory();
            }
        }
    }

    
    /// <summary>
    /// 创建预览物体
    /// </summary>
    private void CreatePreviewObject()
    {
        if (currentObject == null) return;
        
        DroppedItemReturner droppedItemReturner = currentObject.GetComponent<DroppedItemReturner>();
        previewPrefab = droppedItemReturner.itemData.prefab;
        // 使用预览预制体或复制原物体
        if (previewPrefab != null)
        {
            previewObject = Instantiate(previewPrefab, currentObject.transform.position, currentObject.transform.rotation);
            DroppedItemReturner returner =  previewObject.GetComponent<DroppedItemReturner>();
            if (returner != null)
                Destroy(returner);
            Rigidbody previewRb = previewObject.GetComponent<Rigidbody>();
            previewRb.useGravity = false;
            previewRb.isKinematic = true; // 或者设置为运动学刚体
            previewObject.transform.localScale = Vector3.one;
        }
        // else
        // {
        //     previewObject = Instantiate(currentObject, currentObject.transform.position, currentObject.transform.rotation);
        //     
        //     // 移除不必要的组件
        //     Destroy(previewObject.GetComponent<XRGrabInteractable>());
        //     Destroy(previewObject.GetComponent<Rigidbody>());
        //     Destroy(previewObject.GetComponent<DroppedItemReturner>());
        //     Destroy(previewObject.GetComponent<SimpleHoverEffect>());
        //     Destroy(previewObject.GetComponent<GrabListener>());
        // }
        
        previewObject.name = currentObject.name + "_Preview";
        MirrorChildBodySensorsToPreview();
        
        // 设置预览材质为半透明

        // Renderer[] previewRenderers = previewObject.GetComponentsInChildren<Renderer>();
        // foreach (var renderer in previewRenderers)
        // {
        //     Material[] materials = renderer.materials;
        //     for (int i = 0; i < materials.Length; i++)
        //     {
        //         Material tempMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        //         tempMat.CopyPropertiesFromMaterial(materials[i]);
        //         SetMaterialTransparent(tempMat);
        //         materials[i] = tempMat;
        //     }
        //     renderer.materials = materials;
        // }
        
        // 禁用预览物体的碰撞体
        Collider[] previewColliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (var col in previewColliders)
        {
            col.enabled = false;
        }
        
        // 初始颜色设为无效
        //UpdatePreviewColor(false);
    }
    
    /// <summary>
    /// 设置材质为透明
    /// </summary>
    private void SetMaterialTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }
    
    /// <summary>
    /// 更新预览颜色
    /// </summary>
    private void UpdatePreviewColor(bool isValid)
    {
        if (previewObject == null) return;
        
        Color targetColor = isValid ? validColor : invalidColor;
        targetColor.a = previewTransparency;
        
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.materials)
            {
                mat.color = targetColor;
                renderer.material = mat;
            }
        }
    }
    
    /// <summary>
    /// 处理手柄输入
    /// </summary>
    private void HandleInput()
    {
        var controller = rightHand.GetComponent<XRController>();
        if (controller == null) return;
        
        var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(controller.controllerNode);
        
        // 摇杆控制
        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 thumbstick))
        {
            // 按下摇杆切换模式
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out bool stickClick) && stickClick)
            {
                isRotating = !isRotating;
                isScaling = !isRotating;
                SendHapticFeedback(0.2f, 0.05f);
                Debug.Log($"切换模式: {(isRotating ? "旋转" : "缩放")}");
            }
            
            // 根据模式处理
            if (thumbstick.magnitude > 0.2f)
            {
                if (isRotating)
                {
                    // 左右旋转
                    float rotationDelta = thumbstick.y * rotationSpeed * Time.deltaTime;
                    targetRotation *= Quaternion.Euler(0, rotationDelta, 0);
                }
                else if (isScaling)
                {
                    // 上下缩放
                    float scaleDelta = thumbstick.y * scaleSpeed * Time.deltaTime;
                    targetScale = Mathf.Clamp(targetScale + scaleDelta, minScale, maxScale);
                }
            }
        }
    }
    
    
    /// <summary>
    /// 检查侧键是否松开
    /// </summary>
    // private void CheckGripReleased()
    // {
    //     var controller = rightHand.GetComponent<XRController>();
    //     if (controller == null) return;
    //     
    //     var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(controller.controllerNode);
    //     
    //     if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool gripPressed))
    //     {
    //         // 检测从按下到松开的边沿触发
    //         if (wasGripPressed && !gripPressed)
    //         {
    //             // 🔥 松开Grip键时确认放置
    //             ConfirmPlacement();
    //         }
    //         
    //         wasGripPressed = gripPressed;
    //     }
    // }
    
    private bool IsValidPlacement()
    {
        return isValidPlacement && currentSurface != null && currentSurface.CanPlaceObject(currentObject);
    }
    
    /// <summary>
    /// 移除预览物体上不需要的组件
    /// </summary>
    private void RemoveUnnecessaryComponents(GameObject obj)
    {
        // 移除返回脚本
        DroppedItemReturner returner = obj.GetComponent<DroppedItemReturner>();
        if (returner != null)
            Destroy(returner);
    
        // 移除悬停效果
        SimpleHoverEffect hover = obj.GetComponent<SimpleHoverEffect>();
        if (hover != null)
            Destroy(hover);
    
        // 移除抓取监听
    }

    private void MirrorChildBodySensorsToPreview()
    {
        if (currentObject == null || previewObject == null)
        {
            return;
        }

        AttachBodySensorOnGrab bodySensorAttacher = currentObject.GetComponent<AttachBodySensorOnGrab>();
        if (bodySensorAttacher != null)
        {
            bodySensorAttacher.AttachToClone(previewObject);
            return;
        }

        CopyExistingBodySensorsToMatchingChildren(currentObject.transform, previewObject.transform);
    }

    private void CopyExistingBodySensorsToMatchingChildren(Transform sourceRoot, Transform targetRoot)
    {
        if (sourceRoot == null || targetRoot == null)
        {
            return;
        }

        BodySensor[] sourceSensors = sourceRoot.GetComponentsInChildren<BodySensor>(true);
        foreach (BodySensor sourceSensor in sourceSensors)
        {
            if (sourceSensor == null || sourceSensor.transform == sourceRoot)
            {
                continue;
            }

            string relativePath = GetRelativeTransformPath(sourceRoot, sourceSensor.transform);
            if (string.IsNullOrEmpty(relativePath))
            {
                continue;
            }

            Transform targetChild = targetRoot.Find(relativePath);
            if (targetChild == null || targetChild.GetComponent<BodySensor>() != null)
            {
                continue;
            }

            targetChild.gameObject.AddComponent<BodySensor>();
        }
    }

    private string GetRelativeTransformPath(Transform root, Transform target)
    {
        if (root == null || target == null)
        {
            return null;
        }

        System.Collections.Generic.List<string> pathParts = new System.Collections.Generic.List<string>();
        Transform current = target;
        while (current != null && current != root)
        {
            pathParts.Insert(0, current.name);
            current = current.parent;
        }

        if (current != root)
        {
            return null;
        }

        return string.Join("/", pathParts);
    }
    
    /// <summary>
    /// 退出摆放模式

    /// </summary>

    /// <summary>
    /// 退出摆放模式
    /// </summary>
    public void ExitPlacementMode()
    {
        if (!isPlacementMode) return;
        
        Debug.Log("退出摆放模式");
        
        // 销毁射线可视化
        if (rayVisualizationObject != null)
        {
            Destroy(rayVisualizationObject);
            rayVisualizationObject = null;
            rayLineRenderer = null;
        }
        
        // 如果没有成功放置，需要清理预览物体
        if (previewObject != null && !isPlacementConfirmed)
        {
            Destroy(previewObject);
            previewObject = null;
        }
        
        // 恢复原物体的碰撞体（如果没有被销毁）
        if (currentObject != null && objectColliders != null)
        {
            foreach (var col in objectColliders)
            {
                if (col != null)
                    col.enabled = true;
            }
        }
        
        // 恢复原始材质
        if (originalRenderers != null && originalMaterials != null)
        {
            for (int i = 0; i < originalRenderers.Length && i < originalMaterials.Length; i++)
            {
                if (originalRenderers[i] != null && originalMaterials[i] != null)
                {
                    originalRenderers[i].material = originalMaterials[i];
                }
            }
        }
        
        // 移除监听器
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
        
        // 清理引用
        currentObject = null;
        previewObject = null;
        currentInteractor = null;
        objectRb = null;
        grabInteractable = null;
        objectColliders = null;
        originalRenderers = null;
        originalMaterials = null;
        hitSurface = null;
        currentSurface = null;
        
        isPlacementMode = false;
        isPlacementConfirmed = false;
        isExiting = false;
    }
    
    private void UpdatePlacementPreview()
    {
        if (previewObject == null || rightNearFarInteractor == null) 
        {
            HideRayVisualization();
            return;
        }
        
        // 🔥 优先检查 Socket 放置
        if (enableSocketPlacement && TryFindSocket(out SocketPlacementPoint socket))
        {
            HandleSocketPlacement(socket);
            return;
        }
        
        // 🔥 使用标准 Physics.Raycast 进行射线检测
        // 🔥 使用 ~ignoreLayers 来排除不需要检测的层（如床、墙壁等）
        LayerMask combinedMask = placementMask & ~ignoreLayers;
        RaycastHit hit;
        Vector3 rayOrigin = rightNearFarInteractor.transform.position;
        Vector3 rayDirection = rightNearFarInteractor.transform.forward;
        
        // 🔥 先更新射线显示（无论是否击中都显示）
        bool raycastHitSomething = Physics.Raycast(rayOrigin, rayDirection, out hit, placementDistance, combinedMask);
        
        if (raycastHitSomething)
        {
            // 检查是否在可摆放层级
            if (((placementMask & (1 << hit.collider.gameObject.layer)) != 0))
            {
                PlaceableSurface surface = hit.collider.GetComponent<PlaceableSurface>();
                
                // 检查物体是否符合表面要求
                bool canPlace = surface != null && surface.CanPlaceObject(currentObject);
                
                // 🔥 更新射线颜色
                UpdateRayVisualization(rayOrigin, rayDirection, hit.distance, canPlace);
                
                if (canPlace)
                {
                    isValidPlacement = true;
                    hitSurface = hit.collider.gameObject;
                    hitNormal = hit.normal;
                    currentSurface = surface;
                    
                    // 计算目标位置
                    targetPosition = hit.point + hitNormal * surfaceOffset;
                    
                    // 根据表面类型计算基础旋转
                    Quaternion baseRotation;
                    switch (surface.surfaceType)
                    {
                        case PlaceableSurface.SurfaceType.Floor:
                            baseRotation = Quaternion.FromToRotation(Vector3.up, hitNormal);
                            break;
                        case PlaceableSurface.SurfaceType.Wall:
                            baseRotation = Quaternion.FromToRotation(Vector3.forward, -hitNormal);
                            break;
                        case PlaceableSurface.SurfaceType.Ceiling:
                            baseRotation = Quaternion.FromToRotation(Vector3.down, hitNormal);
                            break;
                        default:
                            baseRotation = Quaternion.FromToRotation(Vector3.up, hitNormal);
                            break;
                    }
                    
                    // 应用用户旋转
                    targetRotation = baseRotation * Quaternion.identity;
                    
                    // 吸附到网格
                    if (snapToGrid)
                        targetPosition = SnapToGrid(targetPosition);
                    
                    // 更新预览物体位置和旋转
                    previewObject.transform.position = targetPosition;
                    previewObject.transform.rotation = targetRotation;
                    previewObject.transform.localScale = Vector3.one * targetScale;
                    
                    //UpdatePreviewColor(true);
                    
                    previewObject.SetActive(true);
                }
                else
                {
                    isValidPlacement = false;
                    //UpdatePreviewColor(false);
                    previewObject.SetActive(false);
                }
            }
            else
            {
                isValidPlacement = false;
                //UpdatePreviewColor(false);
                previewObject.SetActive(false);
                // 🔥 击中了不可摆放的物体，显示红色射线
                UpdateRayVisualization(rayOrigin, rayDirection, hit.distance, false);
            }
        }
        else
        {
            isValidPlacement = false;
            //UpdatePreviewColor(false);
            previewObject.SetActive(false);
            // 🔥 没有击中任何物体，显示红色射线到最大距离
            UpdateRayVisualization(rayOrigin, rayDirection, placementDistance, false);
        }
    }
    
    /// <summary>
    /// 尝试在检测半径内找到 Socket
    /// </summary>
    private bool TryFindSocket(out SocketPlacementPoint socket)
    {
        socket = null;
        
        // 使用 OverlapSphere 检测附近的 Socket
        Collider[] colliders = Physics.OverlapSphere(
            previewObject.transform.position, 
            socketDetectionRadius, 
            socketMask
        );
        
        foreach (Collider col in colliders)
        {
            SocketPlacementPoint foundSocket = col.GetComponentInParent<SocketPlacementPoint>();
            if (foundSocket != null && foundSocket.CanAcceptObject(currentObject))
            {
                socket = foundSocket;
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 处理 Socket 放置逻辑
    /// </summary>
    private void HandleSocketPlacement(SocketPlacementPoint socket)
    {
        currentSocket = socket;
        
        // 将预览物体对齐到 Socket 位置
        targetPosition = socket.transform.position;
        targetRotation = socket.transform.rotation;
        
        // 更新预览物体
        previewObject.transform.position = targetPosition;
        previewObject.transform.rotation = targetRotation;
        previewObject.transform.localScale = Vector3.one * targetScale;
        
        // 更新射线可视化（绿色表示有效）
        Vector3 rayOrigin = rightNearFarInteractor.transform.position;
        Vector3 rayDirection = (targetPosition - rayOrigin).normalized;
        UpdateRayVisualization(rayOrigin, rayDirection, Vector3.Distance(rayOrigin, targetPosition), true);
        
        isValidPlacement = true;
        previewObject.SetActive(true);
    }
    
    private Vector3 SnapToGrid(Vector3 position)
    {
        float snapX = Mathf.Round(position.x / gridSize) * gridSize;
        float snapY = Mathf.Round(position.y / gridSize) * gridSize;
        float snapZ = Mathf.Round(position.z / gridSize) * gridSize;
        return new Vector3(snapX, snapY, snapZ);
    }
    
    private void SendHapticFeedback(float amplitude, float duration)
    {
        var controller = rightHand.GetComponent<XRController>();
        if (controller != null)
        {
            var baseController = controller as XRBaseController;
            baseController?.SendHapticImpulse(amplitude, duration);
        }
    }
    
    private void OnDestroy()
    {
        // 清理射线可视化对象
        if (rayVisualizationObject != null)
        {
            Destroy(rayVisualizationObject);
        }
    }
}

/// <summary>
/// 标记物体为已放置

public class PlaceableObjectMarker : MonoBehaviour
{
    public bool IsPlaced = false;
}