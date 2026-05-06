using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class GameManager : Singleton<GameManager>
{
    // Start is called before the first frame update
    public InputActionReference MenuAction;

    public InputActionReference UIAction;
    void Start()
    {
        MenuAction.action.performed += ToggleMenu;
        UIAction.action.performed += ToggleUI;
    }

    private void ToggleUI(InputAction.CallbackContext obj)
    {
        UIManager.Instance.TogglePhone();
    }

    private void ToggleMenu(InputAction.CallbackContext obj)
    {
        UIManager.Instance.ToggleMenu();
    }
}
