using UnityEngine;

public class DynamicParticleEffect : MonoBehaviour
{
    void Start()
    {
        // 1. 创建空物体并挂载 ParticleSystem 组件
        GameObject psObj = new GameObject("CoolAirEffect");
        psObj.transform.SetParent(transform); // 设为空调的子物体
        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();

        // 2. 获取主模块（Main）并修改参数
        var main = ps.main;
        main.loop = true;               // 循环播放
        main.startLifetime = 1.5f;      // 粒子寿命 1.5 秒
        main.startSpeed = 4f;           // 初始速度
        main.startSize = 0.2f;          // 粒子大小
        main.gravityModifier = -0.1f;    // 负重力，向上飘

        // 3. 配置发射模块（Emission）
        var emission = ps.emission;
        emission.rateOverTime = 30f;     // 每秒发射 30 个粒子

        // 4. 配置形状模块（Shape）
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;                // 0 度角 -> 圆柱形
        shape.radius = 0.5f;

        // 5. 配置颜色随生命周期变化（Color over Lifetime）
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.cyan, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // 6. 启动粒子系统
        ps.Play();
    }
}