using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
public class SmokeSensorSimulator : MonoBehaviour
{
    [Header("场景引用")]
    public GameObject smokeEffectObject;
    public Transform phoneTarget;
    [HideInInspector] public Transform transmissionOrigin;

    [Header("按钮绑定（任选一种或同时使用）")]
    public Button testUIButton;
    public Button stoveSwitchUIButton;
    public XRBaseInteractable testXRButton;
    public XRBaseInteractable stoveSwitchXRButton;

    [Header("网络设备")]
    public string routerName = "maxwifi(Clone)_Preview";
    public string routerNameFallbackKeyword = "maxwifi";

    [Header("时间设置")]
    public float transmitDelay = 3f;
    public float packetSpawnInterval = 0.45f;
    public float packetMoveSpeed = 2.8f;

    [Header("传输表现")]
    public GameObject dataPacketPrefab;
    public Color sensorToRouterColor = new Color(1f, 0.4f, 0.25f, 0.92f);
    public Color routerToPhoneColor = new Color(1f, 0.75f, 0.2f, 0.88f);
    public float lineWidth = 0.025f;

    [Header("音频")]
    public AudioSource alarmAudioSource;
    public AudioSource clearAudioSource;

    private GameObject routerObject;
    private LineRenderer transmissionLine;
    private Coroutine pendingAlarmCoroutine;
    private Coroutine packetCoroutine;
    private bool isSmokeVisible;
    private bool isAlarmActive;
    private readonly List<GameObject> activePackets = new List<GameObject>();

    private void Awake()
    {
        SetupTransmissionLine();
        SetSmokeVisible(false);
    }

    private void Start()
    {
        FindRouter();
        RegisterButtonEvents();
    }

    private void FindSmokeOrigin()
    {
        // 先用 GameObject.Find 精确查找（物体激活时有效）
        GameObject found = GameObject.Find("maxYanWu(Clone)_Preview");
        if (found != null)
        {
            transmissionOrigin = found.transform;
            Debug.Log("【烟雾传感器】已找到传输起点：" + found.name);
            return;
        }

        // 物体未激活时，用 Resources.FindObjectsOfTypeAll 遍历场景内所有物体
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform current in allTransforms)
        {
            if (current == null || !current.gameObject.scene.IsValid())
            {
                continue;
            }
            if (current.name == "maxYanWu(Clone)_Preview")
            {
                transmissionOrigin = current;
                Debug.Log("【烟雾传感器】（非激活状态下）已找到传输起点：" + current.name);
                return;
            }
        }

        Debug.LogWarning("【烟雾传感器】未找到 maxYanWu(Clone)_Preview，传输线将从报警器自身位置发出。");
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    private void Update()
    {
        UpdatePackets();
        UpdateTransmissionLinePositions();
    }

    public void TriggerSmokeTest()
    {
        Debug.Log("【烟雾传感器】TriggerSmokeTest 被调用，smokeEffectObject=" + (smokeEffectObject != null ? smokeEffectObject.name : "NULL"));
        StopPendingOperations();
        StopTransmission();
        SetSmokeVisible(true);
        pendingAlarmCoroutine = StartCoroutine(BeginAlarmAfterDelay());
        Debug.Log("【烟雾传感器】测试按钮触发，烟雾已开始释放。");
    }

    public void TurnOffStoveAndClearSmoke()
    {
        StopPendingOperations();
        StopTransmission();
        SetSmokeVisible(false);
        StopAudio(alarmAudioSource);
        PlayAudio(clearAudioSource);
        Debug.Log("【烟雾传感器】厨房电磁炉已关闭，烟雾与报警流程结束。");
    }

    private IEnumerator BeginAlarmAfterDelay()
    {
        yield return new WaitForSeconds(transmitDelay);
        pendingAlarmCoroutine = null;

        if (!isSmokeVisible)
        {
            yield break;
        }

        StartTransmission();
        PlayAudio(alarmAudioSource);
        Debug.Log("【烟雾传感器】报警已发送：烟感 -> 路由器 -> 手机。");
    }

    private void StartTransmission()
    {
        FindRouter();
        if (transmissionOrigin == null)
        {
            FindSmokeOrigin();
        }
        isAlarmActive = true;

        if (transmissionLine != null)
        {
            transmissionLine.enabled = true;
            UpdateTransmissionLinePositions();
        }

        if (packetCoroutine != null)
        {
            StopCoroutine(packetCoroutine);
        }
        packetCoroutine = StartCoroutine(SpawnPacketsLoop());
    }

    private void StopTransmission()
    {
        isAlarmActive = false;

        if (packetCoroutine != null)
        {
            StopCoroutine(packetCoroutine);
            packetCoroutine = null;
        }

        ClearPackets();

        if (transmissionLine != null)
        {
            transmissionLine.enabled = false;
        }
    }

    private IEnumerator SpawnPacketsLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.08f, packetSpawnInterval));
        while (isAlarmActive)
        {
            SpawnTransmissionPacket(GetSensorOrigin(), GetRouterPosition(), sensorToRouterColor, "sensor_to_router");
            yield return wait;

            if (!isAlarmActive)
            {
                yield break;
            }

            SpawnTransmissionPacket(GetRouterPosition(), GetPhonePosition(), routerToPhoneColor, "router_to_phone");
            yield return wait;
        }
    }

    private void SpawnTransmissionPacket(Vector3 startPos, Vector3 endPos, Color packetColor, string routeName)
    {
        GameObject packet;
        if (dataPacketPrefab != null)
        {
            packet = Instantiate(dataPacketPrefab, startPos, Quaternion.identity);
        }
        else
        {
            packet = CreateDefaultPacket(packetColor);
            packet.transform.position = startPos;
        }

        packet.name = $"SmokePacket_{routeName}";
        packet.transform.SetParent(transform, true);

        SmokePacketInfo info = packet.AddComponent<SmokePacketInfo>();
        info.startPos = startPos;
        info.endPos = endPos;
        info.speed = packetMoveSpeed;
        activePackets.Add(packet);
    }

    private GameObject CreateDefaultPacket(Color packetColor)
    {
        GameObject packet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        packet.transform.localScale = Vector3.one * 0.08f;

        Collider packetCollider = packet.GetComponent<Collider>();
        if (packetCollider != null)
        {
            Destroy(packetCollider);
        }

        Renderer packetRenderer = packet.GetComponent<Renderer>();
        Material material = CreateMaterial("Standard", "Sprites/Default");
        if (material.HasProperty("_Color"))
        {
            material.color = packetColor;
        }
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", packetColor * 1.5f);
        }
        packetRenderer.material = material;

        Light packetLight = packet.AddComponent<Light>();
        packetLight.color = packetColor;
        packetLight.intensity = 0.9f;
        packetLight.range = 0.45f;
        packetLight.shadows = LightShadows.None;

        return packet;
    }

    private void UpdatePackets()
    {
        for (int i = activePackets.Count - 1; i >= 0; i--)
        {
            GameObject packet = activePackets[i];
            if (packet == null)
            {
                activePackets.RemoveAt(i);
                continue;
            }

            SmokePacketInfo info = packet.GetComponent<SmokePacketInfo>();
            if (info == null)
            {
                Destroy(packet);
                activePackets.RemoveAt(i);
                continue;
            }

            packet.transform.position = Vector3.MoveTowards(packet.transform.position, info.endPos, info.speed * Time.deltaTime);
            if (Vector3.Distance(packet.transform.position, info.endPos) < 0.01f)
            {
                Destroy(packet);
                activePackets.RemoveAt(i);
            }
        }
    }

    private void ClearPackets()
    {
        for (int i = 0; i < activePackets.Count; i++)
        {
            if (activePackets[i] != null)
            {
                Destroy(activePackets[i]);
            }
        }
        activePackets.Clear();
    }

    private void SetupTransmissionLine()
    {
        GameObject lineObject = new GameObject("SmokeAlarmTransmissionLine");
        lineObject.transform.SetParent(transform, false);
        transmissionLine = lineObject.AddComponent<LineRenderer>();
        transmissionLine.positionCount = 3;
        transmissionLine.startWidth = lineWidth;
        transmissionLine.endWidth = lineWidth;
        transmissionLine.useWorldSpace = true;
        transmissionLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        transmissionLine.receiveShadows = false;
        transmissionLine.enabled = false;

        Material lineMaterial = CreateMaterial("Sprites/Default", "Unlit/Color");
        transmissionLine.material = lineMaterial;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(sensorToRouterColor, 0f),
                new GradientColorKey(sensorToRouterColor, 0.5f),
                new GradientColorKey(routerToPhoneColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(sensorToRouterColor.a, 0f),
                new GradientAlphaKey(sensorToRouterColor.a, 0.5f),
                new GradientAlphaKey(routerToPhoneColor.a, 1f)
            });
        transmissionLine.colorGradient = gradient;
    }

    private void UpdateTransmissionLinePositions()
    {
        if (transmissionLine == null || !transmissionLine.enabled)
        {
            return;
        }

        FindRouter();
        transmissionLine.SetPosition(0, GetSensorOrigin());
        transmissionLine.SetPosition(1, GetRouterPosition());
        transmissionLine.SetPosition(2, GetPhonePosition());
    }

    private Vector3 GetSensorOrigin()
    {
        return transmissionOrigin != null ? transmissionOrigin.position : transform.position;
    }

    private Vector3 GetRouterPosition()
    {
        return routerObject != null ? routerObject.transform.position : transform.position + Vector3.up * 1.2f + Vector3.right * 0.8f;
    }

    private Vector3 GetPhonePosition()
    {
        return phoneTarget != null ? phoneTarget.position : GetRouterPosition() + Vector3.right * 1.1f;
    }

    private void FindRouter()
    {
        if (routerObject != null)
        {
            return;
        }

        routerObject = FindSceneObjectByName(routerName);
        if (routerObject != null || string.IsNullOrEmpty(routerNameFallbackKeyword))
        {
            return;
        }

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform current in allTransforms)
        {
            if (current == null || !current.gameObject.scene.IsValid())
            {
                continue;
            }

            if (current.name.Contains(routerNameFallbackKeyword))
            {
                routerObject = current.gameObject;
                return;
            }
        }
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        GameObject foundObject = GameObject.Find(objectName);
        if (foundObject != null)
        {
            return foundObject;
        }

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform current in allTransforms)
        {
            if (current == null || !current.gameObject.scene.IsValid())
            {
                continue;
            }

            if (current.name == objectName)
            {
                return current.gameObject;
            }
        }

        return null;
    }

    private Material CreateMaterial(string preferredShader, string fallbackShader)
    {
        Shader shader = Shader.Find(preferredShader);
        if (shader == null)
        {
            shader = Shader.Find(fallbackShader);
        }

        return shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
    }

    private void SetSmokeVisible(bool visible)
    {
        isSmokeVisible = visible;

        if (smokeEffectObject == null)
        {
            return;
        }

        if (visible && !smokeEffectObject.activeSelf)
        {
            smokeEffectObject.SetActive(true);
        }

        ParticleSystem[] particleSystems = smokeEffectObject.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (visible)
            {
                particleSystem.Play(true);
            }
            else
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (!visible)
        {
            smokeEffectObject.SetActive(false);
        }
    }

    private void RegisterButtonEvents()
    {
        if (testUIButton != null)
        {
            testUIButton.onClick.AddListener(TriggerSmokeTest);
        }

        if (stoveSwitchUIButton != null)
        {
            stoveSwitchUIButton.onClick.AddListener(TurnOffStoveAndClearSmoke);
        }

        if (testXRButton != null)
        {
            testXRButton.selectEntered.AddListener(OnTestXRButtonPressed);
        }

        if (stoveSwitchXRButton != null)
        {
            stoveSwitchXRButton.selectEntered.AddListener(OnStoveXRButtonPressed);
        }
    }

    private void UnregisterButtonEvents()
    {
        if (testUIButton != null)
        {
            testUIButton.onClick.RemoveListener(TriggerSmokeTest);
        }

        if (stoveSwitchUIButton != null)
        {
            stoveSwitchUIButton.onClick.RemoveListener(TurnOffStoveAndClearSmoke);
        }

        if (testXRButton != null)
        {
            testXRButton.selectEntered.RemoveListener(OnTestXRButtonPressed);
        }

        if (stoveSwitchXRButton != null)
        {
            stoveSwitchXRButton.selectEntered.RemoveListener(OnStoveXRButtonPressed);
        }
    }

    private void OnTestXRButtonPressed(SelectEnterEventArgs args)
    {
        TriggerSmokeTest();
    }

    private void OnStoveXRButtonPressed(SelectEnterEventArgs args)
    {
        TurnOffStoveAndClearSmoke();
    }

    private void StopPendingOperations()
    {
        if (pendingAlarmCoroutine != null)
        {
            StopCoroutine(pendingAlarmCoroutine);
            pendingAlarmCoroutine = null;
        }
    }

    private void PlayAudio(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.Play();
    }

    private void StopAudio(AudioSource source)
    {
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }

    private void OnDestroy()
    {
        UnregisterButtonEvents();
        StopPendingOperations();
        StopTransmission();
    }
}

public class SmokePacketInfo : MonoBehaviour
{
    public Vector3 startPos;
    public Vector3 endPos;
    public float speed;
}
