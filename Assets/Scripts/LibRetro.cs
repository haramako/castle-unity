using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;

public class Retro
{
    public const int nes_ntsc_palette_size = 64 * 8;
    public const int nes_ntsc_entry_size = 128;

    public enum Environment
    {
        EXPERIMENTAL = 0x10000,
        PRIVATE = 0x20000,

        SET_ROTATION = 1,
        GET_OVERSCAN = 2,
        GET_CAN_DUPE = 3,
        SET_MESSAGE = 6,
        SHUTDOWN = 7,
        SET_PERFORMANCE_LEVEL = 8,
        GET_SYSTEM_DIRECTORY = 9,
        SET_PIXEL_FORMAT = 10,
        SET_INPUT_DESCRIPTORS = 11,
        SET_KEYBOARD_CALLBACK = 12,
        SET_DISK_CONTROL_INTERFACE = 13,
        SET_HW_RENDER = 14,
        GET_VARIABLE = 15,
        SET_VARIABLES = 16,
        GET_VARIABLE_UPDATE = 17,
        SET_SUPPORT_NO_GAME = 18,
        GET_LIBRETRO_PATH = 19,
        SET_FRAME_TIME_CALLBACK = 21,
        SET_AUDIO_CALLBACK = 22,
        GET_RUMBLE_INTERFACE = 23,
        GET_INPUT_DEVICE_CAPABILITIES = 24,
        GET_SENSOR_INTERFACE = (25 | EXPERIMENTAL),
        GET_CAMERA_INTERFACE = (26 | EXPERIMENTAL),
        GET_LOG_INTERFACE = 27,
        GET_PERF_INTERFACE = 28,
        GET_LOCATION_INTERFACE = 29,
        GET_CONTENT_DIRECTORY = 30,
        GET_CORE_ASSETS_DIRECTORY = 30,
        GET_SAVE_DIRECTORY = 31,
        SET_SYSTEM_AV_INFO = 32,
        SET_PROC_ADDRESS_CALLBACK = 33,
        SET_SUBSYSTEM_INFO = 34,
        SET_CONTROLLER_INFO = 35,
        SET_MEMORY_MAPS = (36 | EXPERIMENTAL),
        SET_GEOMETRY = 37,
        GET_USERNAME = 38,
        GET_LANGUAGE = 39,
        GET_CURRENT_SOFTWARE_FRAMEBUFFER = (40 | EXPERIMENTAL),
        GET_HW_RENDER_INTERFACE = (41 | EXPERIMENTAL),
        SET_SUPPORT_ACHIEVEMENTS = (42 | EXPERIMENTAL),
        SET_HW_RENDER_CONTEXT_NEGOTIATION_INTERFACE = (43 | EXPERIMENTAL),
        SET_SERIALIZATION_QUIRKS = 44,
        SET_HW_SHARED_CONTEXT = (44 | EXPERIMENTAL),
        GET_VFS_INTERFACE = (45 | EXPERIMENTAL),
        GET_LED_INTERFACE = (46 | EXPERIMENTAL),
        GET_AUDIO_VIDEO_ENABLE = (47 | EXPERIMENTAL),
        GET_MIDI_INTERFACE = (48 | EXPERIMENTAL),
        GET_FASTFORWARDING = (49 | EXPERIMENTAL),
        GET_TARGET_REFRESH_RATE = (50 | EXPERIMENTAL),
        GET_INPUT_BITMASKS = (51 | EXPERIMENTAL),
        GET_CORE_OPTIONS_VERSION = 52,
        SET_CORE_OPTIONS = 53,
        SET_CORE_OPTIONS_INTL = 54,
        SET_CORE_OPTIONS_DISPLAY = 55,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public unsafe class retro_game_info
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string path;
        [MarshalAs(UnmanagedType.LPArray)]
        public byte[] data;
        public UInt64 size;
        [MarshalAs(UnmanagedType.LPArray)]
        public byte[] meta;
    };

    public unsafe struct nes_ntsc_t
    {
        fixed long table[Retro.nes_ntsc_palette_size * Retro.nes_ntsc_entry_size];
    }

    public struct nes_ntsc_setup_t
    {
        /* Basic parameters */
        double hue;        /* -1 = -180 degrees     +1 = +180 degrees */
        double saturation; /* -1 = grayscale (0.0)  +1 = oversaturated colors (2.0) */
        double contrast;   /* -1 = dark (0.5)       +1 = light (1.5) */
        double brightness; /* -1 = dark (0.5)       +1 = light (1.5) */
        double sharpness;  /* edge contrast enhancement/blurring */

        /* Advanced parameters */
        double gamma;      /* -1 = dark (1.5)       +1 = light (0.5) */
        double resolution; /* image resolution */
        double artifacts;  /* artifacts caused by color changes */
        double fringing;   /* color artifacts caused by brightness changes */
        double bleed;      /* color bleed (color resolution reduction) */
        int merge_fields;  /* if 1, merges even and odd fields together to reduce flicker */
        long /*float const* */ decoder_matrix; /* optional RGB decoder matrix, 6 elements */

        long /*char* */ palette_out;  /* optional RGB palette out, 3 bytes per color */

        /* You can replace the standard NES color generation with an RGB palette. The
        first replaces all color generation, while the second replaces only the core
        64-color generation and does standard color emphasis calculations on it. */
        long /*char const* */ palette;/* optional 512-entry RGB palette in, 3 bytes per color */
        long /* char const* */ base_palette;/* optional 64-entry RGB palette in, 3 bytes per color */
    }

    public unsafe struct retro_input_descriptor
    {
        public UInt32 port;
        public UInt32 device;
        public UInt32 index;
        public UInt32 id;
        public char* description;
    };

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void retro_log_printf_t(Int32 level, [MarshalAs(UnmanagedType.LPStr)] string fmt);

    public unsafe struct retro_log_callback
    {
        public IntPtr /* retro_log_printf_t */ log;
    };

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int retro_environment_t(Environment cmd, void* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void retro_video_refresh_t(UInt32 *data, UInt32 width, UInt32 height, UInt64 pitch);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate UInt64 retro_audio_sample_batch_t(UInt16* data, UInt64 frames);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void retro_input_poll_t();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int16 retro_input_state_t(UInt32 port, UInt32 device, UInt32 index, UInt32 id);

    [DllImport(LibRetroDll)]
    public static unsafe extern void retro_init();

    [DllImport(LibRetroDll)]
    public static unsafe extern void retro_run();

#if UNITY_ANDROID && !UNITY_EDITOR
    const string LibRetroDll = "libretro.so";
#else
    const string LibRetroDll = "vs2019.dll";
#endif

    [DllImport(LibRetroDll)]
    public static unsafe extern void nes_ntsc_init(ref nes_ntsc_t ntsc, ref nes_ntsc_setup_t setup);

    [DllImport(LibRetroDll)]
    public static extern void retro_set_environment(retro_environment_t callback);

    [DllImport(LibRetroDll)]
    public static extern void retro_set_video_refresh(retro_video_refresh_t callback);

    [DllImport(LibRetroDll)]
    public static extern void retro_set_audio_sample_batch(retro_audio_sample_batch_t callback);

    [DllImport(LibRetroDll)]
    public static extern void retro_set_input_poll(retro_input_poll_t callback);

    [DllImport(LibRetroDll)]
    public static extern void retro_set_input_state(retro_input_state_t callback);

    [DllImport(LibRetroDll)]
    public static extern void retro_reset();

    [DllImport(LibRetroDll)]
    public static extern UInt64 retro_serialize_size();

    [DllImport(LibRetroDll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool retro_serialize([MarshalAs(UnmanagedType.LPArray), Out] byte[] data, UInt64 size);

    [DllImport(LibRetroDll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool retro_unserialize([MarshalAs(UnmanagedType.LPArray), In] byte[] data, UInt64 size);

    [DllImport(LibRetroDll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe extern int retro_load_game([MarshalAs(UnmanagedType.LPStruct)] retro_game_info game);
}
