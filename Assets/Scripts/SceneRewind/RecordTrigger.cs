// RecordTrigger.cs
// 碰撞触发记录 - 当玩家或其他物体进入碰撞器时触发场景记录
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RecordTrigger : MonoBehaviour
{
    [Header("触发设置")]
    [Tooltip("触发记录的标签（如Player）")]
    public string triggerTag = "Player";
    
    [Tooltip("是否只触发一次")]
    public bool triggerOnce = true;
    
    [Tooltip("触发冷却时间（秒）")]
    public float cooldownTime = 1f;

    [Header("视觉效果")]
    public bool showTriggerEffect = true;
    public Color triggerColor = Color.cyan;
    
    private bool hasTriggered = false;
    private float lastTriggerTime = -999f;
    private Collider triggerCollider;
    private Renderer[] renderers;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
        
        // 获取所有Renderer用于变色反馈
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 检查标签
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag))
        {
            return;
        }

        // 检查是否只触发一次
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        // 检查冷却时间
        if (Time.time - lastTriggerTime < cooldownTime)
        {
            return;
        }

        // 触发记录
        TriggerRecord();
    }

    private void TriggerRecord()
    {
        hasTriggered = true;
        lastTriggerTime = Time.time;

        // 调用场景记录
        if (SceneStateRecorder.Instance != null)
        {
            SceneStateRecorder.Instance.RecordSceneState();
            
            if (showTriggerEffect)
            {
                ShowTriggerFeedback();
            }
        }
        else
        {
            Debug.LogError("[记录触发器] 未找到SceneStateRecorder实例！请在场景中创建一个带有SceneStateRecorder组件的空物体。");
        }
    }

    private void ShowTriggerFeedback()
    {
        // 简单的视觉反馈 - 临时变色
        foreach (Renderer rend in renderers)
        {
            if (rend != null && rend.material != null)
            {
                Color originalColor = rend.material.color;
                rend.material.color = triggerColor;
                
                // 0.3秒后恢复
                StartCoroutine(RestoreColor(rend, originalColor, 0.3f));
            }
        }
    }

    private System.Collections.IEnumerator RestoreColor(Renderer rend, Color originalColor, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rend != null && rend.material != null)
        {
            rend.material.color = originalColor;
        }
    }

    private void OnDrawGizmos()
    {
        // 在编辑器中显示触发区域
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.TransformPoint(sphere.center), sphere.radius);
        }
    }
}
