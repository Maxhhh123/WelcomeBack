using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class UIManager : Singleton<UIManager>
{
    public GameObject menu;
    private bool isMenuOpen=false;
    private LazyFollow lazyFollow;
    
    //public GameObject detailPanel;
    public Image detailImage;
    public Button useButton;
    // Start is called before the first frame update
    void Start()
    {
        menu.SetActive(false);
        lazyFollow = menu.GetComponent<LazyFollow>();
    }

    public void ToggleMenu()
    {
        if (isMenuOpen)
        {
            menu.SetActive(false);
            lazyFollow.enabled = true;
        }
        else
        {
            menu.SetActive(true);
            isMenuOpen = true;
            StartCoroutine(DisableLazyFollow());
        }
        isMenuOpen = !isMenuOpen;
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
        ItemInteractionHandler.Instance.SpawnAndAttachToHand(data);
    }
}
