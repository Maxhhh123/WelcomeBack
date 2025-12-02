using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public audiomanager gameManager;  // 引用gamemanager实例

    void Start()
    {
        // 如果没有在Inspector中分配，尝试自动查找gamemanager
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<audiomanager>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 检查进入触发器的是玩家（假设玩家有"Player"标签）
        if (other.CompareTag("Player") && gameManager != null)
        {
            gameManager.TriggerElevatorDoorsClose();
        }
    }
}