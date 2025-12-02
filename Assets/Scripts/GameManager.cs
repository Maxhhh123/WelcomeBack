using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class GameManager : Singleton<GameManager>
{
    // Start is called before the first frame update
    public InputActionReference MenuAction;
    void Start()
    {
        MenuAction.action.performed += ToggleMenu;
    }

    private void ToggleMenu(InputAction.CallbackContext obj)
    {
        UIManager.Instance.ToggleMenu();
    }
}
