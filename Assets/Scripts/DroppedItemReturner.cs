using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class DroppedItemReturner : MonoBehaviour
{
    private XRGrabInteractable grabbable;
    public ItemData itemData; // 添加这个字段

    [Obsolete("Obsolete")]
    void Start()
    {
        grabbable = GetComponent<XRGrabInteractable>();
        grabbable.onSelectExited.AddListener(ReturnToInventory);
    } 

    private void ReturnToInventory(XRBaseInteractor interactor)
    {
        if (itemData != null)
        {
            InventorySystem.Instance.AddItemBack(itemData);
        }
    }
}
