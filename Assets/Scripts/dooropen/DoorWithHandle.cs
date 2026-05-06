using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRDoor_WithReturn : MonoBehaviour
{
    [Header("References")]
    public Transform doorRoot;              // Door_Root空物体
    public Transform doorHandle;             // 把手模型（视觉）
    public Transform handleGrabPoint;        // 抓取点空物体
    public XRGrabInteractable grabInteractable;
    public Rigidbody doorRigidbody;
    
    [Header("Settings")]
    [Range(10f, 5000f)]
    public float rotationSensitivity = 5000f;  // 旋转灵敏度（调低到合理值）
    
    [Header("Return Settings")]
    public float returnSmoothTime = 0.2f;     // 回归时间
    public bool smoothReturn = true;           // 平滑回归
    public AnimationCurve returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 回归曲线
    
    private bool isGrabbed = false;
    private IXRSelectInteractor grabbingInteractor;
    
    // 回归相关变量
    private bool isReturning = false;
    private float returnTimer = 0f;
    private Vector3 returnStartLocalPos;
    private Quaternion returnStartLocalRot;
    private Vector3 targetLocalPos;
    private Quaternion targetLocalRot;
    
    // 记录初始位置
    private Vector3 originalGrabLocalPos;
    private Quaternion originalGrabLocalRot;
    
    // 用于计算旋转
    private Quaternion grabStartDoorRotation;
    private Vector3 grabStartHandlePosition;  // 新增：记录抓取时把手的 world 位置
    private float grabStartAngle;              // 新增：记录抓取时的角度
    
    // Joint 相关
    private ConfigurableJoint doorJoint;
    private JointDrive originalDrive;

    void Start()
    {
        // 记录抓取点的初始局部位置（相对于doorHandle）
        originalGrabLocalPos = handleGrabPoint.localPosition;
        originalGrabLocalRot = handleGrabPoint.localRotation;
        
        // 设置Rigidbody约束
        SetupRigidbodyConstraints();
        
        // 🔥 修复1：确保Joint的Spring为0
        FixJointSettings();
        
        // 监听抓取事件
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    void SetupRigidbodyConstraints()
    {
        doorRigidbody.constraints = RigidbodyConstraints.FreezePositionX | 
                                    RigidbodyConstraints.FreezePositionY | 
                                    RigidbodyConstraints.FreezePositionZ;
        doorRigidbody.angularDrag = 1f; // 添加适当角阻尼
    }

    // 🔥 修复2：确保Joint设置正确
    void FixJointSettings()
    {
        if (doorRigidbody.TryGetComponent<ConfigurableJoint>(out doorJoint))
        {
            // 保存原始驱动设置
            originalDrive = doorJoint.angularYZDrive;
            
            // 确保Spring为0（防止自动回正）
            var drive = doorJoint.angularYZDrive;
            drive.positionSpring = 0;
            drive.positionDamper = 2f; // 适当阻尼
            drive.maximumForce = Mathf.Infinity;
            doorJoint.angularYZDrive = drive;
            
            Debug.Log($"Joint设置已修复: Spring={drive.positionSpring}, Damper={drive.positionDamper}");
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        isReturning = false;  // 打断回归动画
        grabbingInteractor = args.interactorObject;
        
        // 记录抓取时的门旋转和把手位置
        grabStartDoorRotation = doorRoot.rotation;
        grabStartHandlePosition = handleGrabPoint.position;  // 记录把手位置
        grabStartAngle = doorRoot.eulerAngles.y;  // 记录角度
        
        // 手柄震动
        if (grabbingInteractor is XRBaseInputInteractor controller)
        {
            controller.SendHapticImpulse(0.3f, 0.1f);
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        grabbingInteractor = null;
        
        // 开始回归动画
        StartReturnToHandle();
    }

    void StartReturnToHandle()
    {
        isReturning = true;
        returnTimer = 0f;
        
        // 记录当前位置（起点）
        returnStartLocalPos = handleGrabPoint.localPosition;
        returnStartLocalRot = handleGrabPoint.localRotation;
        
        // 目标位置（原始位置）
        targetLocalPos = originalGrabLocalPos;
        targetLocalRot = originalGrabLocalRot;
        
        // 停止抓取点的物理运动
        Rigidbody grabRb = handleGrabPoint.GetComponent<Rigidbody>();
        if (grabRb != null)
        {
            grabRb.velocity = Vector3.zero;
            grabRb.angularVelocity = Vector3.zero;
        }
    }

    void Update()
    {
        // 处理回归动画（在Update中执行，更平滑）
        if (isReturning)
        {
            HandleReturn();
        }
    }

    void HandleReturn()
    {
        returnTimer += Time.deltaTime;
        float t = Mathf.Clamp01(returnTimer / returnSmoothTime);
        
        if (smoothReturn)
        {
            t = returnCurve.Evaluate(t);
        }
        
        handleGrabPoint.localPosition = Vector3.Lerp(returnStartLocalPos, targetLocalPos, t);
        handleGrabPoint.localRotation = Quaternion.Slerp(returnStartLocalRot, targetLocalRot, t);
        
        if (t >= 1f)
        {
            handleGrabPoint.localPosition = targetLocalPos;
            handleGrabPoint.localRotation = targetLocalRot;
            isReturning = false;
        }
    }

    void FixedUpdate()
    {
        if (!isGrabbed || grabbingInteractor == null) return;

        var interactor = grabbingInteractor as XRBaseInteractor;
        if (interactor?.attachTransform == null) return;

        // 🔥 修复3：改进位移到旋转的映射
        Vector3 targetHandPos = interactor.attachTransform.position;
        
        // 计算手柄相对于抓取起始点的位移
        Vector3 handDelta = targetHandPos - grabStartHandlePosition;
        
        // 转换到门的局部空间
        Vector3 localDelta = doorRoot.InverseTransformDirection(handDelta);
        
        // 🔥 修复4：优化灵敏度计算
        // 使用更稳定的映射：1cm位移 ≈ 1度旋转（可调）
        float rotationAmount = localDelta.x * rotationSensitivity * 0.01f; // 移除Time.deltaTime
        
        // 限制单帧最大旋转（防止突变）
        rotationAmount = Mathf.Clamp(rotationAmount, -90f, 90f);
        
        // 计算目标旋转
        float targetAngle = grabStartAngle + rotationAmount;
        
        // 让Joint自己处理角度限制，我们不手动clamp
        
        // 应用旋转
        Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
        doorRigidbody.MoveRotation(targetRotation);
        
        // 🔥 修复5：调试输出（发现问题时启用）
        // Debug.Log($"位移: {localDelta.x}, 旋转: {rotationAmount}, 目标角度: {targetAngle}, 当前角度: {doorRoot.eulerAngles.y}");
    }

    // 🔥 修复6：新增方法，可在运行时调整Joint阻尼
    public void SetJointDamper(float damper)
    {
        if (doorJoint != null)
        {
            var drive = doorJoint.angularYZDrive;
            drive.positionDamper = damper;
            doorJoint.angularYZDrive = drive;
        }
    }

    // 可选：手动触发回归
    public void ForceReturnHandle()
    {
        if (!isGrabbed && !isReturning)
        {
            StartReturnToHandle();
        }
    }

    // 可视化调试
    void OnDrawGizmosSelected()
    {
        if (handleGrabPoint == null || doorHandle == null) return;
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(doorHandle.TransformPoint(originalGrabLocalPos), 0.05f);
        
        Gizmos.color = isReturning ? Color.yellow : (isGrabbed ? Color.red : Color.blue);
        Gizmos.DrawSphere(handleGrabPoint.position, 0.03f);
        
        Gizmos.color = Color.white;
        Gizmos.DrawLine(handleGrabPoint.position, doorHandle.position);
    }

    void OnDestroy()
    {
        // 清理监听器
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }
}