using UnityEngine;

public class PlaceableSurface : MonoBehaviour
{
    [Header("表面类型")]
    public SurfaceType surfaceType = SurfaceType.Floor;
    
    [Header("允许的物体类型")]
    public bool allowTV =false;
    public bool allowFurniture =false ;
    public bool allowWall =false ;
    
    [Header("表面约束")]
    public bool snapToCenter = false; // 吸附到表面中心
    public Vector3 allowedRotation = Vector3.zero; // 允许的旋转（0表示任意）
    public bool allowScale = true; // 是否允许缩放
    
    public enum SurfaceType
    {
        Floor,      // 地面
        Wall,       // 墙面
        Ceiling,    // 天花板
        Table       // 桌面
    }
    
    // 检查物体是否允许放置在这个表面上
    public bool CanPlaceObject(GameObject obj)
    {
        string objName = obj.name.ToLower();
        
        return true; // 默认允许
        if (objName.Contains("tv") || objName.Contains("电视"))
            return allowTV;
        
        if (objName.Contains("maxwall") || objName.Contains("墙"))
            return allowWall;
        
        if (objName.Contains("chair") || objName.Contains("table") || objName.Contains("家具")|| objName.Contains("wifi")|| objName.Contains("ai"))
            return allowFurniture;
        
        return true; // 默认允许
    }
    
    // private void OnDrawGizmos()
    // {
    //     // 可视化表面
    //     Collider col = GetComponent<Collider>();
    //     if (col != null)
    //     {
    //         Gizmos.color = new Color(0, 1, 0, 0.3f);
    //         Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    //         
    //         // 绘制法线方向
    //         Gizmos.color = Color.blue;
    //         Vector3 normal = transform.up;
    //         if (surfaceType == SurfaceType.Wall)
    //             normal = transform.forward;
    //         
    //         Gizmos.DrawRay(col.bounds.center, normal * 0.5f);
    //     }
    // }
}