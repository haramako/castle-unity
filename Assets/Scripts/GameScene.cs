using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using UnityEngine.Profiling;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.EventSystems;

public class GameScene : MonoSingleton<GameScene>
{
    public RawImage RawImage;
    public Emulator Emulator;

    Canvas canvas_;

    static readonly Dictionary<KeyCode, int> KeyMap = new Dictionary<KeyCode, int> {
        {KeyCode.UpArrow, 4 },
        {KeyCode.DownArrow, 5 },
        {KeyCode.LeftArrow, 6 },
        {KeyCode.RightArrow, 7 },
        {KeyCode.X, 8 },
        {KeyCode.Z, 0 },
        {KeyCode.Q, 2 },
        {KeyCode.W, 3 },
    };

    void updateInput()
    {
        updateJoypad();
        updateTouchAndMouse();
    }

    void updateJoypad()
    {
        foreach (var kv in KeyMap)
        {
            if (Input.GetKey(kv.Key))
            {
                Emulator.UpdateKey(0, 1, 0, kv.Value, 1);
            }
        }
    }

    void updateTouchAndMouse()
    {
        // Touch
        foreach (var t in Input.touches)
        {
            var pos = t.position;
            Debug.LogError($"touch {t.fingerId}");
            touchButton(pos);
        }

        // Mouse
        if (Input.GetMouseButton(0))
        {
            touchButton(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
        }

    }

    void touchButton(Vector2 pos)
    {
        //Debug.LogError(pos);
        //var ray = canvas.worldCamera.ScreenPointToRay(pos);
        var raycaster = canvas_.GetComponent<GraphicRaycaster>();
        foreach (var touchArea in touchAreas_)
        {
            var rt = touchArea.GetComponent<RectTransform>();
            var contains = RectTransformUtility.RectangleContainsScreenPoint(rt, pos);
            if (contains)
            {
                if (touchArea.ButtonId == -1)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, pos, null, out var localPoint))
                    {
                        if (localPoint.x < -30)
                        {
                            Emulator.UpdateKey(0, 1, 0, 6, 1);
                        }
                        else if (localPoint.x > 30)
                        {
                            Emulator.UpdateKey(0, 1, 0, 7, 1);
                        }
                        if (localPoint.y < -30)
                        {
                            Emulator.UpdateKey(0, 1, 0, 5, 1);
                        }
                        else if (localPoint.y > 30)
                        {
                            Emulator.UpdateKey(0, 1, 0, 4, 1);
                        }
                    }
                }
                else
                {
                    Emulator.UpdateKey(0, 1, 0, touchArea.ButtonId,1);
                }
            }
        }
    }

    // Start is called before the first frame update
    IEnumerator Start()
    {
        Application.targetFrameRate = 60;

        setupUi();
        yield return setupFiles();
        Emulator.Setup();

        RawImage.texture = Emulator.ScreenTexture;

        Emulator.LoadState();

        while (true)
        {
            if (Input.GetKey(KeyCode.S))
            {
                Emulator.SaveState();
            }
            else if (Input.GetKey(KeyCode.L))
            {
                Emulator.LoadState();
            }
            else if (Input.GetKey(KeyCode.R))
            {
                Emulator.ResetMachine();
            }

            updateInput();
            Emulator.RunOneFrame();
            yield return null;
        }

    }

#pragma warning disable CS0618 // WWWを使う
    IEnumerator download(string url, string savePath)
    {
        var request = new WWW(url);
        yield return request;
        if( request.error != null)
        {
            throw new Exception(request.error);
        }
        File.WriteAllBytes(savePath, request.bytes);
        Debug.Log($"Download {url}");
    }
#pragma warning restore CS0618

    IEnumerator setupFiles()
    {
        yield return download(Application.streamingAssetsPath + "/castle.nes", Path.Combine(Application.persistentDataPath, "castle.nes"));
        yield return download(Application.streamingAssetsPath + "/custom.pal", Path.Combine(Application.persistentDataPath, "custom.pal"));
    }

    TouchArea[] touchAreas_;

    void setupUi()
    {
        canvas_ = FindObjectOfType<Canvas>();
        touchAreas_ = FindObjectsOfType<TouchArea>();
    }

    void OnApplicationQuit()
    {
        Emulator.SaveState();
    }

    private void OnApplicationPause(bool pause)
    {
        if( pause)
        {
            Emulator.SaveState();
        }
    }

    public void OnResetButtonClick()
    {
        Emulator.ResetMachine();
    }

}
