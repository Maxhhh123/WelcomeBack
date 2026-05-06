// PressureSensor.cs
// 压力传感器模拟：魔法阵感知 → 路由器 → 手机 → 路由器 → 灯
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BodySensor : MonoBehaviour
{
    [Header("网络层设备")]
    public string routerName = "maxwifi(Clone)_Preview";
    public string routerNameFallbackKeyword = "maxwifi";

    [Header("应用层设备名称")]
    public string phoneName = "phone1k";
    public string controlLightName = "controlllight2";
    public string finalLightName = "lamp01";

    [Header("灯光设置")]
    public float onIntensity = 5f;
    public float offIntensity = 0f;

    [Header("人体感应特效")]
    public float magicSquareWidth = 0.55f;
    public float magicSquareDepth = 0.55f;
    public float magicSquareYOffset = 0.075f;
    public Vector3 sensorEmitterLocalOffset = new Vector3(0.0009f, 0.0007f, 0f);
    public Vector3 sensorConeDirection = new Vector3(0f, 0f, -1f);
    public float borderWidth = 0f;
    public float sensorConeHeight = -0.13f;
    public float sensorTopRadius = 0.006f;


    [Range(24, 96)] public int coneSegments = 48;
    [Range(4, 16)] public int coneEdgeCount = 8;
    public float topRingWidth = 0.035f;
    public float sideLineWidth = 0.03f;
    public float groundGlowWidth = 0.18f;
    [Range(0.02f, 0.8f)] public float idleConeAlpha = 0.09f;
    [Range(0.02f, 0.95f)] public float triggeredConeAlpha = 0.18f;


    public Color normalRayColor = new Color(1f, 0.9f, 0.65f, 0.5f);
    public Color triggeredRayColor = new Color(1f, 0.72f, 0.42f, 0.5f);
    public float colorTransitionSpeed = 8f;
    public float idleGlowIntensity = 1.1f;
    public float triggeredGlowIntensity = 2.2f;
    public float glowRange = 5f;

    [Header("数据传输设置")]
    public GameObject dataPacketPrefab;
    public float dataPacketSpeed = 3f;
    public float dataPacketSpawnInterval = 0.3f;
    public Color dataPacketColor = Color.cyan;

    [Header("延迟设置")]
    public float turnOffDelay = 10f;

    [Header("检测设置")]
    public string playerTag = "Player";

    private Light targetLight;
    private Light controlLight;
    private GameObject router;
    private GameObject phoneObject;
    private Transform lightTransform;

    private bool isPlayerOnSensor = false;
    private bool isTransmitting = false;

    private LineRenderer coneBaseRenderer;
    private LineRenderer coneGroundGlowRenderer;
    private LineRenderer coneTopRenderer;
    private LineRenderer dataLineRenderer;
    private readonly List<LineRenderer> coneSideRenderers = new List<LineRenderer>();
    private MeshFilter coneVolumeMeshFilter;
    private MeshRenderer coneVolumeMeshRenderer;
    private Material coneVolumeMaterial;
    private Light sensorGlowLight;


    private readonly List<GameObject> dataPackets = new List<GameObject>();
    private readonly HashSet<int> activeTriggerIds = new HashSet<int>();

    private Coroutine spawnCoroutine;
    private Coroutine turnOffCoroutine;

    private Color currentSenseColor;
    private float currentGlowIntensity;
    private float currentConeAlpha;
    private Vector2 lastSquareSize = new Vector2(-1f, -1f);

    void Awake()
    {
        SetupLineRenderers();
        SetupDetectionConeEffect();

        currentSenseColor = normalRayColor;
        currentGlowIntensity = idleGlowIntensity;
        currentConeAlpha = idleConeAlpha;
        UpdatePerceptionVisual(true);
    }

    void Start()
    {
        FindDevices();
        InitializeLight();
    }

    void Update()
    {
        UpdatePerceptionVisual();
        UpdateDataPackets();
    }

    #region 初始化

    private void SetupLineRenderers()
    {
        GameObject dataLineObj = new GameObject("DataTransferLine");
        dataLineObj.transform.SetParent(transform, false);
        dataLineRenderer = dataLineObj.AddComponent<LineRenderer>();
        dataLineRenderer.positionCount = 5;
        dataLineRenderer.material = CreateMaterial("Sprites/Default", "Unlit/Color");
        dataLineRenderer.startWidth = 0.025f;
        dataLineRenderer.endWidth = 0.025f;
        dataLineRenderer.startColor = new Color(1f, 0.92f, 0.3f, 0.72f);
        dataLineRenderer.endColor = new Color(0.25f, 1f, 1f, 0.6f);

        dataLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        dataLineRenderer.receiveShadows = false;
        dataLineRenderer.enabled = false;
    }

    private void SetupDetectionConeEffect()
    {
        coneBaseRenderer = CreateEffectLineRenderer("DetectionConeBase", transform, borderWidth, borderWidth, true, coneSegments);
        coneGroundGlowRenderer = CreateEffectLineRenderer("DetectionConeGroundGlow", transform, groundGlowWidth, groundGlowWidth, true, coneSegments);
        coneTopRenderer = CreateEffectLineRenderer("DetectionConeTop", transform, topRingWidth, topRingWidth, true, coneSegments);

        coneGroundGlowRenderer.alignment = LineAlignment.View;
        coneBaseRenderer.alignment = LineAlignment.View;
        coneTopRenderer.alignment = LineAlignment.View;

        for (int i = 0; i < coneEdgeCount; i++)
        {
            LineRenderer sideRenderer = CreateEffectLineRenderer($"DetectionConeSide_{i}", transform, sideLineWidth, sideLineWidth * 0.65f, false, 2);
            sideRenderer.alignment = LineAlignment.View;
            coneSideRenderers.Add(sideRenderer);
        }

        GameObject coneVolumeObj = new GameObject("DetectionConeVolume");
        coneVolumeObj.transform.SetParent(transform, false);
        coneVolumeMeshFilter = coneVolumeObj.AddComponent<MeshFilter>();
        coneVolumeMeshRenderer = coneVolumeObj.AddComponent<MeshRenderer>();
        Mesh coneMesh = new Mesh();
        coneMesh.name = "DetectionConeVolumeMesh";
        coneMesh.MarkDynamic();
        coneVolumeMeshFilter.sharedMesh = coneMesh;
        coneVolumeMaterial = CreateTransparentConeMaterial();
        coneVolumeMeshRenderer.material = coneVolumeMaterial;
        coneVolumeMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        coneVolumeMeshRenderer.receiveShadows = false;

        GameObject glowObj = new GameObject("SensorGlow");
        glowObj.transform.SetParent(transform, false);
        sensorGlowLight = glowObj.AddComponent<Light>();

        sensorGlowLight.type = LightType.Spot;
        sensorGlowLight.range = sensorConeHeight + 0.4f;
        sensorGlowLight.intensity = idleGlowIntensity;
        sensorGlowLight.color = normalRayColor;
        sensorGlowLight.spotAngle = 65f;
        sensorGlowLight.innerSpotAngle = 28f;
        sensorGlowLight.shadows = LightShadows.None;

        UpdateDetectionConeGeometry();
    }

    private LineRenderer CreateEffectLineRenderer(string objectName, Transform parent, float startWidth, float endWidth, bool loop, int positionCount)
    {
        GameObject lineObj = new GameObject(objectName);
        lineObj.transform.SetParent(parent, false);

        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = loop;
        lineRenderer.positionCount = positionCount;
        lineRenderer.material = CreateMaterial("Sprites/Default", "Unlit/Color");
        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        return lineRenderer;
    }

    private Material CreateMaterial(string preferredShader, string fallbackShader)
    {
        Shader shader = Shader.Find(preferredShader);
        if (shader == null)
        {
            shader = Shader.Find(fallbackShader);
        }
        return new Material(shader);
    }

    private Material CreateTransparentConeMaterial()
    {
        Color initialColor = new Color(normalRayColor.r, normalRayColor.g, normalRayColor.b, idleConeAlpha);

        Shader urpUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpUnlitShader != null)
        {
            Material material = new Material(urpUnlitShader);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            material.SetFloat("_ZWrite", 0f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            SetMaterialColor(material, initialColor);
            return material;
        }

        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
        {
            Material material = new Material(spriteShader);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            SetMaterialColor(material, initialColor);
            return material;
        }

        Shader standardShader = Shader.Find("Standard");
        if (standardShader != null)
        {
            Material material = new Material(standardShader);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            SetMaterialColor(material, initialColor);
            SetMaterialEmission(material, normalRayColor * 0.45f);
            return material;
        }

        Material fallback = CreateMaterial("Legacy Shaders/Transparent/Diffuse", "Unlit/Color");
        fallback.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        SetMaterialColor(fallback, initialColor);
        return fallback;
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null) return;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private void SetMaterialEmission(Material material, Color color)
    {
        if (material == null || !material.HasProperty("_EmissionColor")) return;

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color);
    }


    private void FindDevices()
    {
        FindRouter();
        FindPhone();
        FindControlLight();
        FindTargetLight();
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return null;

        GameObject foundObject = GameObject.Find(objectName);
        if (foundObject != null) return foundObject;

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform current in allTransforms)
        {
            if (current == null || !current.gameObject.scene.IsValid()) continue;
            if (current.name == objectName)
            {
                return current.gameObject;
            }
        }

        return null;
    }

    private void FindPhone()
    {
        phoneObject = FindSceneObjectByName(phoneName);
    }

    private void FindControlLight()
    {
        GameObject controlLightObject = FindSceneObjectByName(controlLightName);
        if (controlLightObject != null)
        {
            controlLight = controlLightObject.GetComponent<Light>();
            if (controlLight == null)
            {
                controlLight = controlLightObject.GetComponentInChildren<Light>();
            }
            return;
        }

        controlLight = null;
    }

    private void FindTargetLight()
    {
        GameObject finalLightObject = FindSceneObjectByName(finalLightName);
        if (finalLightObject != null)
        {
            targetLight = finalLightObject.GetComponent<Light>();
            if (targetLight == null)
            {
                targetLight = finalLightObject.GetComponentInChildren<Light>();
            }

            lightTransform = targetLight != null ? targetLight.transform : finalLightObject.transform;
            return;
        }

        targetLight = null;
        lightTransform = null;
    }

    private void FindRouter()
    {
        if (router != null) return;

        router = FindSceneObjectByName(routerName);
        if (router != null) return;

        if (string.IsNullOrEmpty(routerNameFallbackKeyword)) return;

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform current in allTransforms)
        {
            if (current == null || !current.gameObject.scene.IsValid()) continue;
            if (current.name.Contains(routerNameFallbackKeyword))
            {
                router = current.gameObject;
                return;
            }
        }
    }

    private void InitializeLight()
    {
        if (controlLight != null)
        {
            controlLight.intensity = offIntensity;
        }

        if (targetLight != null)
        {
            targetLight.intensity = offIntensity;
        }
    }

    private Vector3 GetRouterPosition()
    {
        return router != null ? router.transform.position : transform.position + Vector3.up * 1.5f;
    }

    private Vector3 GetPhonePosition(Vector3 routerPos)
    {
        return phoneObject != null ? phoneObject.transform.position : routerPos + Vector3.right * 1.2f;
    }

    private Vector3 GetLightPosition(Vector3 phonePos)
    {
        return lightTransform != null ? lightTransform.position : phonePos + Vector3.right * 1.2f;
    }

    #endregion

    #region 感知特效

    private void UpdatePerceptionVisual(bool immediate = false)
    {
        UpdateDetectionConeGeometry();

        Color targetColor = isPlayerOnSensor ? triggeredRayColor : normalRayColor;
        float targetGlow = isPlayerOnSensor ? triggeredGlowIntensity : idleGlowIntensity;
        float targetAlpha = isPlayerOnSensor ? triggeredConeAlpha : idleConeAlpha;
        float blend = immediate ? 1f : 1f - Mathf.Exp(-colorTransitionSpeed * Time.deltaTime);

        currentSenseColor = Color.Lerp(currentSenseColor, targetColor, blend);
        currentGlowIntensity = Mathf.Lerp(currentGlowIntensity, targetGlow, blend);
        currentConeAlpha = Mathf.Lerp(currentConeAlpha, targetAlpha, blend);

        float pulse = 0.94f + Mathf.Sin(Time.time * 2.2f) * 0.06f;
        Color baseColor = new Color(currentSenseColor.r, currentSenseColor.g, currentSenseColor.b, Mathf.Clamp01(currentConeAlpha + 0.025f));
        Color topColor = new Color(currentSenseColor.r, currentSenseColor.g, currentSenseColor.b, Mathf.Clamp01(currentConeAlpha * 0.56f));
        Color glowColor = new Color(currentSenseColor.r, currentSenseColor.g, currentSenseColor.b, Mathf.Clamp01(currentConeAlpha * 0.18f));
        Color sideStartColor = new Color(currentSenseColor.r, currentSenseColor.g, currentSenseColor.b, Mathf.Clamp01(currentConeAlpha * 0.3f));
        Color sideEndColor = new Color(currentSenseColor.r, currentSenseColor.g, currentSenseColor.b, Mathf.Clamp01(currentConeAlpha * 0.06f));

        Color volumeColor = new Color(currentSenseColor.r, currentSenseColor.g, currentSenseColor.b, Mathf.Clamp01(currentConeAlpha * 0.5f + 0.035f));




        if (coneBaseRenderer != null)
        {
            coneBaseRenderer.startColor = baseColor;
            coneBaseRenderer.endColor = baseColor;
            coneBaseRenderer.widthMultiplier = borderWidth * pulse;
        }

        if (coneGroundGlowRenderer != null)
        {
            coneGroundGlowRenderer.startColor = glowColor;
            coneGroundGlowRenderer.endColor = glowColor;
            coneGroundGlowRenderer.widthMultiplier = groundGlowWidth * (0.92f + Mathf.Sin(Time.time * 1.3f) * 0.05f);
        }

        if (coneTopRenderer != null)
        {
            coneTopRenderer.startColor = topColor;
            coneTopRenderer.endColor = topColor;
            coneTopRenderer.widthMultiplier = topRingWidth * (0.96f + Mathf.Sin(Time.time * 2.8f) * 0.04f);
        }

        for (int i = 0; i < coneSideRenderers.Count; i++)
        {
            LineRenderer sideRenderer = coneSideRenderers[i];
            if (sideRenderer == null) continue;

            float sidePulse = 0.88f + Mathf.Sin(Time.time * 2.1f + i * 0.45f) * 0.12f;
            sideRenderer.startColor = new Color(sideStartColor.r, sideStartColor.g, sideStartColor.b, sideStartColor.a * sidePulse);
            sideRenderer.endColor = sideEndColor;
            sideRenderer.widthMultiplier = sideLineWidth * sidePulse;
        }

        if (coneVolumeMaterial != null)
        {
            SetMaterialColor(coneVolumeMaterial, volumeColor);
            SetMaterialEmission(coneVolumeMaterial, currentSenseColor * (0.34f + currentGlowIntensity * 0.17f));
        }



        if (sensorGlowLight != null)
        {
            sensorGlowLight.color = currentSenseColor;
            sensorGlowLight.intensity = currentGlowIntensity * (0.94f + Mathf.Sin(Time.time * 2.6f) * 0.08f);
            sensorGlowLight.range = sensorConeHeight + 0.4f;
            sensorGlowLight.spotAngle = ComputeSpotAngle();
            sensorGlowLight.innerSpotAngle = Mathf.Max(10f, sensorGlowLight.spotAngle * 0.45f);
        }
    }

    private void UpdateDetectionConeGeometry()
    {
        Vector2 size = GetLocalSquareSize();
        if (!Mathf.Approximately(size.x, lastSquareSize.x) || !Mathf.Approximately(size.y, lastSquareSize.y))
        {
            lastSquareSize = size;
        }

        float baseRadiusX = size.x * 0.5f;
        float baseRadiusZ = size.y * 0.5f;
        Vector3 coneDirection = GetConeDirectionLocal();
        Vector3 topCenter = GetConeOriginLocal(coneDirection);
        Vector3 baseCenter = topCenter + coneDirection * sensorConeHeight;

        UpdateEllipseRenderer(coneBaseRenderer, baseCenter, baseRadiusX, baseRadiusZ, coneSegments, coneDirection);
        UpdateEllipseRenderer(coneGroundGlowRenderer, baseCenter, baseRadiusX, baseRadiusZ, coneSegments, coneDirection);
        UpdateEllipseRenderer(coneTopRenderer, topCenter, sensorTopRadius, sensorTopRadius, coneSegments, coneDirection);
        UpdateConeVolumeMesh(topCenter, sensorTopRadius, sensorTopRadius, baseCenter, baseRadiusX, baseRadiusZ, coneSegments, coneDirection);

        GetConeBasis(coneDirection, out Vector3 tangent, out Vector3 bitangent);
        for (int i = 0; i < coneSideRenderers.Count; i++)
        {
            float angle = (i / (float)coneSideRenderers.Count) * Mathf.PI * 2f;
            Vector3 radialOffsetTop = tangent * (Mathf.Cos(angle) * sensorTopRadius) + bitangent * (Mathf.Sin(angle) * sensorTopRadius);
            Vector3 radialOffsetBottom = tangent * (Mathf.Cos(angle) * baseRadiusX) + bitangent * (Mathf.Sin(angle) * baseRadiusZ);
            coneSideRenderers[i].SetPosition(0, topCenter + radialOffsetTop);
            coneSideRenderers[i].SetPosition(1, baseCenter + radialOffsetBottom);
        }

        if (sensorGlowLight != null)
        {
            sensorGlowLight.transform.localPosition = topCenter;
            sensorGlowLight.transform.localRotation = Quaternion.LookRotation(coneDirection, bitangent);
        }
    }

    private void UpdateEllipseRenderer(LineRenderer lineRenderer, Vector3 center, float radiusX, float radiusZ, int segments, Vector3 axis)
    {
        if (lineRenderer == null) return;

        if (lineRenderer.positionCount != segments)
        {
            lineRenderer.positionCount = segments;
        }

        GetConeBasis(axis, out Vector3 tangent, out Vector3 bitangent);
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 point = center + tangent * (Mathf.Cos(angle) * radiusX) + bitangent * (Mathf.Sin(angle) * radiusZ);
            lineRenderer.SetPosition(i, point);
        }
    }

    private void UpdateConeVolumeMesh(Vector3 topCenter, float topRadiusX, float topRadiusZ, Vector3 baseCenter, float baseRadiusX, float baseRadiusZ, int segments, Vector3 axis)
    {
        if (coneVolumeMeshFilter == null || coneVolumeMeshFilter.sharedMesh == null) return;

        Mesh mesh = coneVolumeMeshFilter.sharedMesh;
        int ringVertexCount = segments + 1;
        Vector3[] vertices = new Vector3[ringVertexCount * 2];
        Vector2[] uv = new Vector2[ringVertexCount * 2];

        GetConeBasis(axis, out Vector3 tangent, out Vector3 bitangent);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 2f;
            Vector3 radialTop = tangent * (Mathf.Cos(angle) * topRadiusX) + bitangent * (Mathf.Sin(angle) * topRadiusZ);
            Vector3 radialBottom = tangent * (Mathf.Cos(angle) * baseRadiusX) + bitangent * (Mathf.Sin(angle) * baseRadiusZ);

            int vertexIndex = i * 2;
            vertices[vertexIndex] = topCenter + radialTop;
            vertices[vertexIndex + 1] = baseCenter + radialBottom;
            uv[vertexIndex] = new Vector2(t, 1f);
            uv[vertexIndex + 1] = new Vector2(t, 0f);
        }

        int[] triangles = new int[segments * 12];
        int triangleIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int topA = i * 2;
            int bottomA = topA + 1;
            int topB = topA + 2;
            int bottomB = topA + 3;

            triangles[triangleIndex++] = topA;
            triangles[triangleIndex++] = topB;
            triangles[triangleIndex++] = bottomB;
            triangles[triangleIndex++] = topA;
            triangles[triangleIndex++] = bottomB;
            triangles[triangleIndex++] = bottomA;

            triangles[triangleIndex++] = topA;
            triangles[triangleIndex++] = bottomB;
            triangles[triangleIndex++] = topB;
            triangles[triangleIndex++] = topA;
            triangles[triangleIndex++] = bottomA;
            triangles[triangleIndex++] = bottomB;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private Vector3 GetConeOriginLocal(Vector3 coneDirection)
    {
        BoxCollider box = GetComponent<BoxCollider>();
        Vector3 baseOrigin = box != null ? box.center : Vector3.zero;
        return baseOrigin + sensorEmitterLocalOffset + coneDirection * magicSquareYOffset;
    }

    private Vector3 GetConeDirectionLocal()
    {
        if (sensorConeDirection.sqrMagnitude < 0.0001f)
        {
            return Vector3.down;
        }

        return sensorConeDirection.normalized;
    }

    private void GetConeBasis(Vector3 axis, out Vector3 tangent, out Vector3 bitangent)
    {
        Vector3 normalizedAxis = axis.sqrMagnitude < 0.0001f ? Vector3.down : axis.normalized;
        Vector3 reference = Mathf.Abs(Vector3.Dot(normalizedAxis, Vector3.up)) > 0.98f ? Vector3.right : Vector3.up;
        tangent = Vector3.Normalize(Vector3.Cross(normalizedAxis, reference));
        bitangent = Vector3.Normalize(Vector3.Cross(normalizedAxis, tangent));
    }

    private float ComputeSpotAngle()
    {

        Vector2 size = GetLocalSquareSize();
        float baseRadius = Mathf.Max(size.x, size.y) * 0.5f;
        return Mathf.Clamp(Mathf.Atan2(baseRadius, Mathf.Max(0.05f, sensorConeHeight)) * Mathf.Rad2Deg * 2f, 15f, 140f);
    }

    private Vector2 GetLocalSquareSize()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
        {
            return new Vector2(magicSquareWidth, magicSquareDepth);
        }

        float width = Mathf.Max(0.05f, box.size.x * 0.95f);
        float depth = Mathf.Max(0.05f, box.size.z * 0.95f);
        return new Vector2(width, depth);
    }

    private void EmitPerceptionBurst(int count)
    {
        // 粒子特效已移除
    }

    #endregion

    #region 数据传输可视化

    private void UpdateDataTransferLine()
    {
        FindPhone();
        FindControlLight();
        FindTargetLight();

        if (!isTransmitting || lightTransform == null)
        {
            dataLineRenderer.enabled = false;
            return;
        }

        FindRouter();
        dataLineRenderer.enabled = true;

        Vector3 sensorPos = transform.position;
        Vector3 routerPos = GetRouterPosition();
        Vector3 phonePos = GetPhonePosition(routerPos);
        Vector3 lightPos = GetLightPosition(phonePos);

        dataLineRenderer.SetPosition(0, sensorPos);
        dataLineRenderer.SetPosition(1, routerPos);
        dataLineRenderer.SetPosition(2, phonePos);
        dataLineRenderer.SetPosition(3, routerPos);
        dataLineRenderer.SetPosition(4, lightPos);
    }

    private IEnumerator SpawnDataPackets()
    {
        while (isTransmitting)
        {
            FindRouter();
            FindPhone();
            FindControlLight();
            FindTargetLight();

            Vector3 sensorPos = transform.position;
            Vector3 routerPos = GetRouterPosition();
            Vector3 phonePos = GetPhonePosition(routerPos);
            Vector3 lightPos = GetLightPosition(phonePos);
            float stepDelay = Mathf.Max(0.05f, dataPacketSpawnInterval * 0.25f);

            SpawnDataPacket(sensorPos, routerPos, "sensor_to_router");
            yield return new WaitForSeconds(stepDelay);

            SpawnDataPacket(routerPos, phonePos, "router_to_phone");
            yield return new WaitForSeconds(stepDelay);

            SpawnDataPacket(phonePos, routerPos, "phone_to_router");
            yield return new WaitForSeconds(stepDelay);

            if (lightTransform != null)
            {
                SpawnDataPacket(routerPos, lightPos, "router_to_light");
            }

            yield return new WaitForSeconds(stepDelay);
        }
    }

    private void SpawnDataPacket(Vector3 startPos, Vector3 endPos, string routeName)
    {
        GameObject packet;

        if (dataPacketPrefab != null)
        {
            packet = Instantiate(dataPacketPrefab, startPos, Quaternion.identity);
        }
        else
        {
            packet = CreateDefaultDataPacket();
            packet.transform.position = startPos;
        }

        packet.name = $"DataPacket_{routeName}";
        packet.transform.SetParent(transform, true);

        DataPacketInfo info = packet.AddComponent<DataPacketInfo>();
        info.startPos = startPos;
        info.endPos = endPos;
        info.speed = dataPacketSpeed;

        dataPackets.Add(packet);
    }

    private GameObject CreateDefaultDataPacket()
    {
        GameObject packet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        packet.transform.localScale = Vector3.one * 0.1f;
        Destroy(packet.GetComponent<Collider>());

        Renderer renderer = packet.GetComponent<Renderer>();
        Material mat = CreateMaterial("Standard", "Sprites/Default");
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", dataPacketColor * 1.4f);
        }
        if (mat.HasProperty("_Color"))
        {
            mat.color = dataPacketColor;
        }
        renderer.material = mat;

        Light packetLight = packet.AddComponent<Light>();
        packetLight.color = dataPacketColor;
        packetLight.intensity = 1f;
        packetLight.range = 0.5f;
        packetLight.shadows = LightShadows.None;

        return packet;
    }

    private void UpdateDataPackets()
    {
        UpdateDataTransferLine();

        for (int i = dataPackets.Count - 1; i >= 0; i--)
        {
            GameObject packet = dataPackets[i];
            if (packet == null)
            {
                dataPackets.RemoveAt(i);
                continue;
            }

            DataPacketInfo info = packet.GetComponent<DataPacketInfo>();
            if (info == null)
            {
                continue;
            }

            packet.transform.position = Vector3.MoveTowards(
                packet.transform.position,
                info.endPos,
                info.speed * Time.deltaTime);

            if (Vector3.Distance(packet.transform.position, info.endPos) < 0.01f)
            {
                Destroy(packet);
                dataPackets.RemoveAt(i);
            }
        }
    }

    private void ClearDataPackets()
    {
        foreach (GameObject packet in dataPackets)
        {
            if (packet != null)
            {
                Destroy(packet);
            }
        }
        dataPackets.Clear();
    }

    #endregion

    #region 触发检测

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        bool wasEmpty = activeTriggerIds.Count == 0;
        activeTriggerIds.Add(other.GetInstanceID());
        if (!wasEmpty) return;

        isPlayerOnSensor = true;
        EmitPerceptionBurst(14);

        if (turnOffCoroutine != null)
        {
            StopCoroutine(turnOffCoroutine);
            turnOffCoroutine = null;
        }

        StartDataTransfer();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        activeTriggerIds.Remove(other.GetInstanceID());
        if (activeTriggerIds.Count > 0) return;

        isPlayerOnSensor = false;
        EmitPerceptionBurst(8);

        if (turnOffCoroutine != null)
        {
            StopCoroutine(turnOffCoroutine);
        }
        turnOffCoroutine = StartCoroutine(DelayedTurnOff());
    }

    #endregion

    #region 数据传输控制

    private void StartDataTransfer()
    {
        FindPhone();
        FindControlLight();
        FindTargetLight();
        FindRouter();

        bool wasTransmitting = isTransmitting;
        isTransmitting = true;

        TurnOnControlLight();
        TurnOnLight();

        if (!wasTransmitting)
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }
            spawnCoroutine = StartCoroutine(SpawnDataPackets());
            Debug.Log("【数据传输开始】压力传感器 → 路由器 → 手机 → 路由器 → 灯");
        }
    }

    private void StopDataTransfer()
    {
        if (!isTransmitting)
        {
            return;
        }

        isTransmitting = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        ClearDataPackets();
        dataLineRenderer.enabled = false;

        TurnOffControlLight();
        TurnOffLight();

        Debug.Log("【数据传输结束】智能灯已关闭");
    }

    #endregion

    #region 灯光控制

    private void TurnOnControlLight()
    {
        if (controlLight != null)
        {
            controlLight.intensity = onIntensity;
        }
    }

    private void TurnOffControlLight()
    {
        if (controlLight != null)
        {
            controlLight.intensity = offIntensity;
        }
    }

    private void TurnOnLight()
    {
        if (targetLight != null)
        {
            targetLight.intensity = onIntensity;
        }
    }

    private void TurnOffLight()
    {
        if (targetLight != null)
        {
            targetLight.intensity = offIntensity;
        }
    }

    #endregion

    #region 延迟关灯

    private IEnumerator DelayedTurnOff()
    {
        Debug.Log($"玩家离开传感器，魔法阵恢复待机色，数据传输继续 {turnOffDelay} 秒...");
        yield return new WaitForSeconds(turnOffDelay);
        StopDataTransfer();
        turnOffCoroutine = null;
    }

    #endregion

    #region 编辑器可视化

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }

    #endregion
}

public class DataPacketInfo : MonoBehaviour
{
    public Vector3 startPos;
    public Vector3 endPos;
    public float speed;
}
