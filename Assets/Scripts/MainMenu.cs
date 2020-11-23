using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(false);
    }

    public void OnControlBoxChangeClick(GameObject target)
    {
        var id = target.GetId();
        GameScene.Instance.SetControlBox(id);
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void OnCloseClick()
    {
        Close();
    }

    public void OnResetButtonClick()
    {
        GameScene.Instance.Emulator.ResetMachine();
    }

}
