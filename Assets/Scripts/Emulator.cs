using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;

#pragma warning disable IDE1006

public class Emulator : MonoSingleton<Emulator>
{
	public static bool LogEnabled = false;
	public static bool LogAllMessageEnabled = false;

	int[] KeyInputs = new int[4 * 2 * 1 * 14];
	static HashSet<LibRetro.Environment> UnknownMessageWarned = new HashSet<LibRetro.Environment>();

	public Texture2D ScreenTexture { get; private set; }

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

			if (LogAllMessageEnabled)
			{
				Debug.Log($"CMD {cmd} {d}");
			}

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
					if (!UnknownMessageWarned.Contains(cmd))
					{
						UnknownMessageWarned.Add(cmd);
						Debug.LogWarning($"Unknown CMD {cmd} {d}");
					}
					return 0;
			}
		}
	}

	[AOT.MonoPInvokeCallback(typeof(LibRetro.retro_video_refresh_t))]
	public static unsafe void video_refresh_callback(UInt32* data, UInt32 width, UInt32 height, UInt64 pitch)
	{
		//Debug.Log($"video callback {width} {height} {pitch}");
		Instance.ScreenTexture.LoadRawTextureData(new IntPtr((void*)data), (int)(width * height * sizeof(UInt32)));
		Instance.ScreenTexture.Apply();
	}

	[AOT.MonoPInvokeCallback(typeof(LibRetro.retro_audio_sample_batch_t))]
	public static unsafe UInt64 audio_sample_batch_callback(UInt16* data, UInt64 frames)
	{
		return 0;
	}

	public static void log(string message)
	{
		if (LogEnabled)
		{
			Debug.Log(message);
		}
	}

	[AOT.MonoPInvokeCallback(typeof(LibRetro.retro_input_poll_t))]
	public static void input_poll_callback()
	{
		log("input_poll callback");
	}

	[AOT.MonoPInvokeCallback(typeof(LibRetro.retro_input_state_t))]
	public static Int16 input_state_callback(UInt32 port, UInt32 device, UInt32 index, UInt32 id)
	{
		//Debug.Log($"input state callback {port} {device} {index} {id}");
		return (Int16)Instance.KeyInputs[keyIndex((int)port, (int)device, (int)index, (int)id)];
	}

	[AOT.MonoPInvokeCallback(typeof(LibRetro.retro_log_printf_t))]
	public unsafe static void log_callback(Int32 level, string fmt)
	{
		log($"LOG: {fmt}");
	}

	public unsafe void Setup()
	{
		ScreenTexture = new Texture2D(256, 240, TextureFormat.BGRA32, false);
		ScreenTexture.wrapMode = TextureWrapMode.Mirror;

		LibRetro.retro_set_environment(environment_callback);
		log("set_environment");

		LibRetro.retro_set_video_refresh(video_refresh_callback);
		log("set_video_refresh");

		LibRetro.retro_set_audio_sample_batch(audio_sample_batch_callback);
		log("set_audio_sample_batch");

		LibRetro.retro_set_input_poll(input_poll_callback);

		LibRetro.retro_set_input_state(input_state_callback);

		LibRetro.retro_init();
		log("retro_init");

		string path = Path.Combine(Application.persistentDataPath, "castle.nes");
		byte[] rom = System.IO.File.ReadAllBytes(path);
		LibRetro.retro_game_info gameInfo = new LibRetro.retro_game_info { path = path, data = rom, size = (UInt64)rom.Length, meta = new byte[0] };
		if (LibRetro.retro_load_game(gameInfo) == 0)
		{
			Debug.LogError("Load failed");
		}
	}

	public void RunOneFrame()
	{
		Profiler.BeginSample("retro_run");
		LibRetro.retro_run();
		Profiler.EndSample();
		//Debug.Log("retru_run");

		// Clear inputs.
		for (int i = 0; i < KeyInputs.Length; i++)
		{
			KeyInputs[i] = 0;
		}

	}

	static int keyIndex(int port, int device, int index, int id)
	{
		return ((port * 2 + device) * 1 + index) * 14 + id;
	}

	public void UpdateKey(int port, int device, int index, int id, int val)
	{
		KeyInputs[keyIndex(port, device, index, id)] = val;
	}

	public void ResetMachine()
	{
		LibRetro.retro_reset();
	}

	string savePath()
	{
		return Path.Combine(Application.persistentDataPath, "save.dat");
	}

	public void SaveState()
	{
		var len = LibRetro.retro_serialize_size();
		var buf = new byte[len];
		if (!LibRetro.retro_serialize(buf, len))
		{
			throw new Exception("Serialize failed");
		}
		File.WriteAllBytes(savePath(), buf);
		Debug.Log($"save len={len}");
	}

	public void LoadState()
	{
		if (!File.Exists(savePath()))
		{
			return;
		}

		var len = LibRetro.retro_serialize_size();
		var buf = File.ReadAllBytes(savePath());
		if (!LibRetro.retro_unserialize(buf, len))
		{
			throw new Exception("Unserialize failed");
		}
		Debug.Log($"load len={len}");
	}


}

