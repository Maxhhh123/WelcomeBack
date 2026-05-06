using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// 标记场景中的 Socket 放置点
/// 将此组件添加到带有 XRSocketInteractor 的空物体上
/// </summary>
public class SocketPlacementPoint : MonoBehaviour
{
    [Header("Socket 设置")]
    public XRSocketInteractor socketInteractor;
    
    [Header("可吸附的物体标签")]
    public string[] allowedTags; // 可以被此 Socket 吸附的物体标签
    
    [Header("可选：可视化")]
    public bool showGizmos = true;
    public float gizmoRadius = 0.1f;
    public Color gizmoColor = Color.blue;
    
    private void Reset()
    {
        socketInteractor = GetComponent<XRSocketInteractor>();
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.2f);
    }
    
    /// <summary>
    /// 检查给定的物体是否可以被此 Socket 吸附
    /// </summary>
    public bool CanAcceptObject(GameObject obj)
    {
        if (obj == null) return false;
        
        foreach (string tag in allowedTags)
        {
            if (obj.CompareTag(tag))
                return true;
        }
        
        return false;
    }
}