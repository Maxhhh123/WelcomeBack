using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DoorHandle : XRGrabInteractable
{
    [Header("关联组件")]
    public DoorController doorController; // 引用门控制器
    public Transform handlePoint;         // 手柄抓取点

    private Vector3 lastPosition;         // 上一帧位置
    private bool isGrabbed = false;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        isGrabbed = true;
        lastPosition = handlePoint.position;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        isGrabbed = false;
        doorController.ResetToClosed(); // 松开后自动回弹
    }

    void Update()
    {
        if (isGrabbed)
        {
            // 计算手柄移动距离，转换为门旋转角度
            Vector3 delta = handlePoint.position - lastPosition;
            float angleDelta = delta.x * 100f; // 调整系数以适配灵敏度
            doorController.SetTargetRotation(angleDelta);

            lastPosition = handlePoint.position;
        }
    }
}
