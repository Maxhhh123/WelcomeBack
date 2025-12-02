using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ItemInteractionHandler : Singleton<ItemInteractionHandler>
{
    public Transform rightHandAnchor;

    public void SpawnAndAttachToHand(ItemData itemData)
    {
        if (itemData.spawnedObject != null) return; // 已存在则不重复生成
        
        var go = Instantiate(itemData.prefab, rightHandAnchor.position, rightHandAnchor.rotation);
        go.transform.SetParent(rightHandAnchor);

        var grabbable = go.AddComponent<XRGrabInteractable>();
        itemData.spawnedObject = go;
    }
}