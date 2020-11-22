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

#pragma warning disable IDE1006

public class TestScene : MonoBehaviour
{
    static Texture2D tex_;
    public RawImage RawImage;

    [AOT.MonoPInvokeCallback(typeof(LibRetro.retro_environment_t))]
    public static unsafe int environment_callback(LibRetro.Environment cmd, void* data)
    {

        unsafe
        {
            UInt64 d = 0;
            if (data != null)
            {
                d = (UInt64)new IntPtr(data).ToInt64();
            }
            Debug.Log($"CMD {cmd} {d}");

            switch (cmd)
            {
                case LibRetro.Environment.GET_LANGUAGE:
                    {
                        *((uint*)data) = 1; // Japanese
                        return 1;
                    }
                case LibRetro.Environment.GET_CORE_OPTIONS_VERSION:
                    {
                        *((uint*)data) = 1;
                        return 1;
                    }
                case LibRetro.Environment.SET_INPUT_DESCRIPTORS:
                    {
                        var input_desc = new LibRetro.retro_input_descriptor { };
                        *((LibRetro.retro_input_descriptor*)data) = input_desc;
                        return 1;
                    }
                case LibRetro.Environment.GET_SYSTEM_DIRECTORY:
                    {
                        var cwd = Encoding.UTF8.GetBytes(System.IO.Directory.GetCurrentDirectory());
                        fixed (byte* cwdPtr = &cwd[0])
                        {
                            *((byte**)data) = cwdPtr;
                        }
                        return 1;
                    }
                case LibRetro.Environment.GET_LOG_INTERFACE:
                    {
                        var func = Marshal.GetFunctionPointerForDelegate<LibRetro.retro_log_printf_t>(log_callback);
                        *((LibRetro.retro_log_callback*)data) = new LibRetro.retro_log_callback { log = func };
                        return 1;
                    }
                case LibRetro.Environment.SET_PIXEL_FORMAT:
                    {
                        return 1;
                    }
                case LibRetro.Environment.GET_SAVE_DIRECTORY:
                    {
                        var dir = Encoding.UTF8.GetBytes(Application.persistentDataPath);
                        fixed (byte* cwdPtr = &dir[0])
                        {
                            *((byte**)data) = cwdPtr;
                        }
                        return 1;
                    }
                default:
                    Debug.LogWarning($"Unknown CMD {cmd} {d}");
                    return 0;
            }
        }
    }

    [AOT.MonoPInvokeCallback(typeof(LibRetro.retro_video_refresh_t))]
    public static unsafe void video_refresh_callback(UInt32* data, UInt32 width, UInt32 height, UInt64 pitch)
    {
        //Debug.Log($"video callback {width} {height} {pitch}");
        tex_.LoadRawTextureData(new IntPtr((void*)data), (int)(width * height * sizeof(UInt32)));
        tex_.Apply();
    }

    [AOT.MonoPInvokeCallback(typeof(LibRetro.retro_audio_sample_batch_t))]
    public static unsafe UInt64 audio_sample_batch_callback(UInt16* data, UInt64 frames)
    {
        return 0;
    }

    [AOT.MonoPInvokeCallback(typeof(LibRetro.retro_input_poll_t))]
    public static void input_poll_callback()
    {
        Debug.Log("input callback");
    }

    [AOT.MonoPInvokeCallback(typeof(LibRetro.retro_input_state_t))]
    public static Int16 input_state_callback(UInt32 port, UInt32 device, UInt32 index, UInt32 id)
    {
        //Debug.Log($"input state callback {port} {device} {index} {id}");
        return (Int16)KeyInputs[keyIndex((int)port, (int)device, (int)index, (int)id)];
    }

    [AOT.MonoPInvokeCallback(typeof(LibRetro.retro_log_printf_t))]
    public unsafe static void log_callback(Int32 level, string fmt)
    {
        Debug.Log($"LOG: {fmt}");
    }


    unsafe void setupRetro()
    {
        tex_ = new Texture2D(256, 240, TextureFormat.BGRA32, false);
        //tex_ = new Texture2D(256, 240, TextureFormat.R8, false);
        RawImage.texture = tex_;

        LibRetro.retro_set_environment(environment_callback);
        Debug.Log("set_environment");

        LibRetro.retro_set_video_refresh(video_refresh_callback);
        Debug.Log("set_video_refresh");

        LibRetro.retro_set_audio_sample_batch(audio_sample_batch_callback);
        Debug.Log("set_audio_sample_batch");

        LibRetro.retro_set_input_poll(input_poll_callback);

        LibRetro.retro_set_input_state(input_state_callback);

        LibRetro.retro_init();
        Debug.Log("retro_init");

        string path = Path.Combine(Application.persistentDataPath, "castle.nes");
        byte[] rom = System.IO.File.ReadAllBytes(path);
        LibRetro.retro_game_info gameInfo = new LibRetro.retro_game_info { path = path, data = rom, size = (UInt64)rom.Length, meta = new byte[0] };
        if (LibRetro.retro_load_game(gameInfo) == 0)
        {
            Debug.LogError("Load failed");
        }
    }

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

    static int[] KeyInputs = new int[4*2*1*14];

    static int keyIndex(int port, int device, int index, int id)
    {
        return ((port * 2  + device ) * 1 + index) * 14 + id;
    }

    void updateInput()
    {
        for( int i=0; i<KeyInputs.Length; i++)
        {
            KeyInputs[i] = 0;
        }

        foreach ( var kv in KeyMap)
        {
            if( Input.GetKey(kv.Key))
            {
                KeyInputs[keyIndex(0, 1, 0, kv.Value)] = 1;
            }
        }

        updateTouch();
    }


    void updateTouch()
    {
        var canvas = FindObjectOfType<Canvas>();
        foreach (var t in Input.touches)
        {
            var pos = t.position;
            Debug.LogError($"touch {t.fingerId}");
            touchButton(pos);
        }

        if (Input.GetMouseButton(0))
        {
            touchButton(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
        }

    }

    Canvas canvas_;

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
                            KeyInputs[keyIndex(0, 1, 0, 6)] = 1;
                        }
                        else if(localPoint.x > 30 )
                        {
                            KeyInputs[keyIndex(0, 1, 0, 7)] = 1;
                        }
                        if (localPoint.y < -30)
                        {
                            KeyInputs[keyIndex(0, 1, 0, 5)] = 1;
                        }
                        else if (localPoint.y > 30)
                        {
                            KeyInputs[keyIndex(0, 1, 0, 4)] = 1;
                        }
                    }
                }
                else
                {
                    KeyInputs[keyIndex(0, 1, 0, touchArea.ButtonId)] = 1;
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
        setupRetro();

        load();

        while (true)
        {
            if (Input.GetKey(KeyCode.S))
            {
                save();
            }
            else if (Input.GetKey(KeyCode.L))
            {
                load();
            }
            else if (Input.GetKey(KeyCode.R))
            {
                reset();
            }

            updateInput();
            Profiler.BeginSample("retro_run");
            LibRetro.retro_run();
            Profiler.EndSample();
            Debug.Log("run");
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

    void reset()
    {
        LibRetro.retro_reset();
    }

    string savePath()
    {
        return Path.Combine(Application.persistentDataPath, "save.dat");
    }

    void save()
    {
        var len = LibRetro.retro_serialize_size();
        var buf = new byte[len];
        if( !LibRetro.retro_serialize(buf, len))
        {
            throw new Exception("Serialize failed");
        }
        File.WriteAllBytes(savePath(), buf);
        Debug.Log($"save {len}");
    }

    void load()
    {
        if( !File.Exists(savePath()))
        {
            return;
        }

        var len = LibRetro.retro_serialize_size();
        var buf = File.ReadAllBytes(savePath());
        if ( !LibRetro.retro_unserialize(buf, len))
        {
            throw new Exception("Unserialize failed");
        }
        Debug.Log($"load {len}");
    }

    void OnApplicationQuit()
    {
        save();
    }

    private void OnApplicationPause(bool pause)
    {
        if( pause)
        {
            save();
        }
    }

    public void OnResetButtonClick()
    {
        reset();
    }

}
