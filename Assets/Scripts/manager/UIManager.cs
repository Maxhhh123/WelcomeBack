using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class UIManager : Singleton<UIManager>
{
    public GameObject menu;
    public GameObject phoneUI;
    private bool isMenuOpen=false;
    private bool isPhoneOpen=false;
    private LazyFollow lazyFollow;
    private LazyFollow phoneUILazyFollow; 
    
    //public GameObject detailPanel;
    public Image detailImage;
    public Button useButton;

    // Start is called before the first frame update
    void Start()
    {
        menu.SetActive(false);
        lazyFollow = menu.GetComponent<LazyFollow>();
        phoneUI.SetActive(false);
        phoneUILazyFollow = phoneUI.GetComponent<LazyFollow>();

        // 注册回溯事件监听
        SceneStateRecorder.OnRewindCompleted += OnSceneRewind;
    }

    void OnDestroy()
    {
        // 取消注册事件监听
        SceneStateRecorder.OnRewindCompleted -= OnSceneRewind;
    }

    /// <summary>
    /// 场景回溯时调用 - 关闭所有UI
    /// </summary>
    private void OnSceneRewind()
    {
        // 关闭 Menu
        if (isMenuOpen)
        {
            menu.SetActive(false);
            isMenuOpen = false;
            if (lazyFollow != null) lazyFollow.enabled = true;
        }

        // 关闭 PhoneUI
        if (isPhoneOpen)
        {
            phoneUI.SetActive(false);
            isPhoneOpen = false;
            if (phoneUILazyFollow != null) phoneUILazyFollow.enabled = true;
        }

        Debug.Log("[UIManager] 场景回溯 - UI已重置");
    }

    public void ToggleMenu()
    {
        if (isMenuOpen)
        {
            menu.SetActive(false);
            isMenuOpen = false;
            lazyFollow.enabled = true;
        }
        else
        {
            menu.SetActive(true);
            isMenuOpen = true;
            StartCoroutine(DisableLazyFollow());
        }
        
    }

    public void TogglePhone()
    {
        if (isPhoneOpen)
        {
            phoneUI.SetActive(false);
            isPhoneOpen = false;
            phoneUILazyFollow.enabled = true;
        }
        else
        {
            phoneUI.SetActive(true);
            isPhoneOpen = true;
            StartCoroutine(DisableLazyFollowPhone());
        }
    }

    private IEnumerator DisableLazyFollowPhone()
    {
        yield return new WaitForSeconds(0.8f);
        phoneUILazyFollow.enabled = false;//
    }

    private IEnumerator DisableLazyFollow()
    {
        yield return new WaitForSeconds(0.8f);
        lazyFollow.enabled = false;//随后禁用lazyfollow
    }
    
    public void ShowItemDetails(ItemData data)
    {
        detailImage.gameObject.SetActive(true);
        detailImage.sprite = data.detailImage;
        useButton.onClick.RemoveAllListeners();
        useButton.onClick.AddListener(() => UseItem(data));
    }

    private void UseItem(ItemData data)
    {
        InventorySystem.Instance.RemoveSpecificItem(data);
        ItemInteractionHandler.Instance.SpawnItem(data.prefab);
    }

    
}
