// PlacementTaskChecker.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlacementTaskChecker : MonoBehaviour
{
    [Header("放置配置")]
    public TaskType thisTask = TaskType.PlaceRouter;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor targetSocket; // 目标Socket Interactor
    public GameObject targetObject; // 需要放置的对象（可选）
    public string requiredTag = "Router"; // 需要放置的物体标签
    
    private TaskManager taskManager;
    private bool taskCompleted = false;
    
    void Start()
    {
        taskManager = FindObjectOfType<TaskManager>();
        
        // 订阅Socket事件
        if (targetSocket != null)
        {
            targetSocket.selectEntered.AddListener(OnObjectPlaced);
        }
    }
    
    void OnObjectPlaced(SelectEnterEventArgs args)
    {
        if (taskCompleted) return;
        
        // 检查当前任务是否正确
        if (taskManager == null || taskManager.currentTask != thisTask)
            return;
            
        // 检查放置的物体是否符合要求
        GameObject placedObject = args.interactableObject.transform.gameObject;
        
        bool isCorrectObject = false;
        
        if (!string.IsNullOrEmpty(requiredTag) && placedObject.CompareTag(requiredTag))
        {
            isCorrectObject = true;
        }
        
        if (targetObject != null && placedObject == targetObject)
        {
            isCorrectObject = true;
        }
        
        if (isCorrectObject)
        {
            CompleteTask();
        }
    }
    
    void CompleteTask()
    {
        taskCompleted = true;
        if (taskManager != null)
        {
            taskManager.AdvanceToNextTask(thisTask);
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅，避免内存泄漏
        if (targetSocket != null)
        {
            targetSocket.selectEntered.RemoveListener(OnObjectPlaced);
        }
    }
}