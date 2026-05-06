using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("门参数")]
    public Transform doorHinge;       // 门的铰链点
    public float maxOpenAngle = 90f;  // 最大开门角度
    public float closeSpeed = 2f;     // 关门速度
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 回弹缓动曲线

    private Quaternion closedRotation; // 初始关闭状态
    private Quaternion targetRotation; // 目标旋转状态
    private bool isOpening = false;

    void Start()
    {
        closedRotation = doorHinge.rotation;
        targetRotation = closedRotation;
    }

    void Update()
    {
        // 平滑插值到目标旋转
        doorHinge.rotation = Quaternion.Lerp(doorHinge.rotation, targetRotation, Time.deltaTime * closeSpeed);
    }

    public void SetTargetRotation(float angleOffset)
    {
        // 设置目标旋转（带限位）
        float clampedAngle = Mathf.Clamp(angleOffset, 0, maxOpenAngle);
        targetRotation = closedRotation * Quaternion.Euler(0, clampedAngle, 0);
    }

    public void ResetToClosed()
    {
        targetRotation = closedRotation;
    }
}
