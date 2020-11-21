using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine.UI;

public class TestScene : MonoBehaviour
{
    Retro.nes_ntsc_t ntsc;
    Retro.nes_ntsc_setup_t setup;
    static Texture2D tex_;
    public RawImage RawImage;

    public static unsafe int environment_callback(Retro.Environment cmd, void* data)
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
                case Retro.Environment.GET_LANGUAGE:
                    {
                        *((uint*)data) = 1; // Japanese
                        return 1;
                    }
                case Retro.Environment.GET_CORE_OPTIONS_VERSION:
                    {
                        *((uint*)data) = 1;
                        return 1;
                    }
                case Retro.Environment.SET_INPUT_DESCRIPTORS:
                    {
                        var input_desc = new Retro.retro_input_descriptor { };
                        *((Retro.retro_input_descriptor*)data) = input_desc;
                        return 1;
                    }
                case Retro.Environment.GET_SYSTEM_DIRECTORY:
                    {
                        var cwd = Encoding.UTF8.GetBytes(System.IO.Directory.GetCurrentDirectory());
                        fixed (byte* cwdPtr = &cwd[0])
                        {
                            *((byte**)data) = cwdPtr;
                        }
                        return 1;
                    }
                case Retro.Environment.GET_LOG_INTERFACE:
                    {
                        var func = Marshal.GetFunctionPointerForDelegate<Retro.retro_log_printf_t>(log_callback);
                        *((Retro.retro_log_callback*)data) = new Retro.retro_log_callback { log = func };
                        return 1;
                    }
                case Retro.Environment.SET_PIXEL_FORMAT:
                    {
                        return 1;
                    }
                default:
                    Debug.LogWarning($"Unknown CMD {cmd} {d}");
                    return 0;
            }
        }
    }

    static byte[] texData_ = new byte[256*240*sizeof(UInt32)];

    public static unsafe void video_refresh_callback(UInt32* data, UInt32 width, UInt32 height, UInt64 pitch)
    {
        Debug.Log($"video callback {width} {height} {pitch}");
        data[0] = 0;
        data[1] = 0;
        tex_.LoadRawTextureData(new IntPtr((void*)data), (int)(width * height * sizeof(UInt32)));
        tex_.IncrementUpdateCount();
        tex_.Apply();
    }

    public static unsafe UInt64 audio_sample_batch_callback(UInt16* data, UInt64 frames)
    {
        return 0;
    }

    public static void input_poll_callback()
    {
        Debug.Log("input callback");
    }

    public static Int16 input_state_callback(UInt32 port, UInt32 device, UInt32 index, UInt32 id)
    {
        //Debug.Log($"input state callback {port} {device} {index} {id}");
        return 0;
    }

    public unsafe static void log_callback(Int32 level, string fmt)
    {
        Debug.Log($"LOG: {fmt}");
    }


    unsafe void setupRetro()
    {
        tex_ = new Texture2D(256, 240, TextureFormat.BGRA32, false);
        //tex_ = new Texture2D(256, 240, TextureFormat.R8, false);
        RawImage.texture = tex_;

        Retro.retro_set_environment(environment_callback);
        Debug.Log("set_environment");

        Retro.retro_set_video_refresh(video_refresh_callback);
        Debug.Log("set_video_refresh");

        Retro.retro_set_audio_sample_batch(audio_sample_batch_callback);
        Debug.Log("set_audio_sample_batch");

        Retro.retro_set_input_poll(input_poll_callback);

        Retro.retro_set_input_state(input_state_callback);

        Retro.retro_init();
        Debug.Log("retro_init");

        string path = "castle.nes";
        byte[] rom = System.IO.File.ReadAllBytes(path);
        Retro.retro_game_info gameInfo = new Retro.retro_game_info { path = path, data = rom, size = (UInt64)rom.Length, meta = new byte[0] };
        if (Retro.retro_load_game(gameInfo) == 0)
        {
            Debug.LogError("Load failed");
        }
    }

    // Start is called before the first frame update
    IEnumerator Start()
    {
        setupRetro();

        while (true)
        {
            Retro.retro_run();
            Debug.Log("run");
            yield return null;
        }
    }
}
