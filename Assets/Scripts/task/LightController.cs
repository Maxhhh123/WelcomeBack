// LightController.cs
using UnityEngine;

public class LightController : MonoBehaviour
{
    [Header("灯光组件")]
    public Light targetLight; // 要控制的Light组件
    public Light bulbGlow;    // 可选：灯泡的发光材质（如果需要更高级的效果）
    
    [Header("亮度设置")]
    [Range(0f, 1f)]
    public float brightness = 1f; // 当前亮度（0-1归一化值）
    public float maxIntensity = 3f; // 灯泡最大亮度（Light组件的intensity最大值）
    
    [Header("颜色设置")]
    public Color currentColor = Color.white;
    
    // 公开属性，供其他脚本读取当前亮度
    public float Brightness
    {
        get { return brightness; }
        set
        {
            brightness = Mathf.Clamp01(value); // 限制在0-1之间
            UpdateLight();
        }
    }
    
    void Start()
    {
        // 如果没有指定Light组件，尝试从自身获取
        if (targetLight == null)
            targetLight = GetComponent<Light>();
            
        // 初始化灯光状态
        UpdateLight();
    }
    
    // 更新灯光效果
    void UpdateLight()
    {
        if (targetLight != null)
        {
            // 将归一化的亮度（0-1）映射到实际强度（0-maxIntensity）
            targetLight.intensity = brightness * maxIntensity;
            targetLight.color = currentColor;
        }
        
        // 如果有发光材质，可以在这里更新材质颜色
        if (bulbGlow != null)
        {
            // 这里假设bulbGlow是一个可发光的材质或另一个光源
            bulbGlow.color = currentColor * brightness;
        }
    }
    
    // 设置亮度（可以由UI滑块调用）
    public void SetBrightness(float value)
    {
        Brightness = value;
    }
    
    // 设置颜色
    public void SetColor(Color color)
    {
        currentColor = color;
        UpdateLight();
    }
    
    // 开关控制
    public void TurnOn()
    {
        Brightness = 1f;
    }
    
    public void TurnOff()
    {
        Brightness = 0f;
    }
}