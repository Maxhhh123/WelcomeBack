using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public class AttachBodySensorOnGrab : MonoBehaviour
{
    [Header("挂载目标")]
    public Transform targetChild;
    public bool attachToAllChildren = false;
    public bool useFirstChildWhenTargetMissing = true;
    public bool includeInactiveChildren = true;
    public bool onlyAttachOnce = true;

    private XRGrabInteractable grabInteractable;
    private bool hasAttached;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
        }

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        AttachNow();
    }

    public int AttachNow()
    {
        if (onlyAttachOnce && hasAttached)
        {
            return 0;
        }

        int attachedCount = AttachBodySensorToTargets(transform);
        if (attachedCount > 0)
        {
            hasAttached = true;
            Debug.Log($"{name} 被拾取后，已为 {attachedCount} 个子物体挂上 BodySensor。");
        }

        return attachedCount;
    }

    public int AttachToClone(GameObject cloneRoot)
    {
        if (cloneRoot == null)
        {
            return 0;
        }

        return AttachBodySensorToTargets(cloneRoot.transform);
    }

    private int AttachBodySensorToTargets(Transform root)
    {
        List<Transform> targets = CollectTargets(root);
        if (targets.Count == 0)
        {
            Debug.LogWarning($"{name} 没有找到可挂载 BodySensor 的子物体，请检查 targetChild 或子物体层级。");
            return 0;
        }

        int attachedCount = 0;
        foreach (Transform target in targets)
        {
            if (target == null || target == root)
            {
                continue;
            }

            if (target.GetComponent<BodySensor>() != null)
            {
                continue;
            }

            target.gameObject.AddComponent<BodySensor>();
            attachedCount++;
        }

        return attachedCount;
    }

    private List<Transform> CollectTargets(Transform root)
    {
        List<Transform> targets = new List<Transform>();

        if (attachToAllChildren)
        {
            Transform[] children = includeInactiveChildren
                ? root.GetComponentsInChildren<Transform>(true)
                : root.GetComponentsInChildren<Transform>();

            foreach (Transform child in children)
            {
                if (child != null && child != root)
                {
                    targets.Add(child);
                }
            }

            return targets;
        }

        Transform singleTarget = ResolveSingleTarget(root);
        if (singleTarget != null && singleTarget != root)
        {
            targets.Add(singleTarget);
        }

        return targets;
    }

    private Transform ResolveSingleTarget(Transform root)
    {
        if (targetChild != null)
        {
            if (root == transform)
            {
                return targetChild;
            }

            string relativePath = GetRelativePath(transform, targetChild);
            if (!string.IsNullOrEmpty(relativePath))
            {
                Transform mappedTarget = root.Find(relativePath);
                if (mappedTarget != null)
                {
                    return mappedTarget;
                }
            }
        }

        if (!useFirstChildWhenTargetMissing)
        {
            return null;
        }

        Transform meaningfulChild = FindFirstMeaningfulChild(root);
        if (meaningfulChild != null)
        {
            return meaningfulChild;
        }

        return root.childCount > 0 ? root.GetChild(0) : null;
    }

    private Transform FindFirstMeaningfulChild(Transform root)
    {
        Transform[] children = includeInactiveChildren
            ? root.GetComponentsInChildren<Transform>(true)
            : root.GetComponentsInChildren<Transform>();

        foreach (Transform child in children)
        {
            if (child == null || child == root)
            {
                continue;
            }

            if (child.GetComponent<Renderer>() != null || child.GetComponent<Collider>() != null || child.childCount == 0)
            {
                return child;
            }
        }

        return null;
    }

    private string GetRelativePath(Transform root, Transform target)
    {
        if (root == null || target == null)
        {
            return null;
        }

        List<string> pathParts = new List<string>();
        Transform current = target;
        while (current != null && current != root)
        {
            pathParts.Insert(0, current.name);
            current = current.parent;
        }

        if (current != root)
        {
            return null;
        }

        return string.Join("/", pathParts);
    }
}

