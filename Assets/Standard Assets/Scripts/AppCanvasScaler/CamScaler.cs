using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CamScaler : MonoBehaviour
{
	public Camera MainCam;
	public Camera SelfCam;

	int originalScreenWidth_;
	int originalScreenHeight_;

	public static ResolutionInfo CurrentScreenInfo;

	public class ResolutionInfo
	{
		/// <summary>
		/// スクリーン全体のオリジナルの幅[pixel]
		/// </summary>
		public int OriginalWidth;
		/// <summary>
		/// スクリーン全体のオリジナルの高さ[pixel]
		/// </summary>
		public int OriginalHeight;

		/// <summary>
		/// UIの幅[pixel]
		/// </summary>
		public int Width;
		/// <summary>
		/// UIの高さpixel]
		/// </summary>
		public int Height;

		/// <summary>
		/// UIの幅[Canvas座標系]
		/// </summary>
		public int UiWidth;
		/// <summary>
		/// UIの高さ[Canvas座標系]
		/// </summary>
		public int UiHeight;

		public int UiViewWidth;
		public int UiViewHeight;

		public int UiFullWidth;
		public int UiFullHeight;

		public float UiScale;
		public float FieldOfView;
	}

	private void Start()
	{
		ResetAspect();
	}

	/// <summary>
	/// 最適な解像度を取得する
	///
	/// 9:16 までの場合は、横720固定
	/// 9:16より縦長の場合は、縦1280固定にする
	///
	/// </summary>
	/// <returns>The resolution.</returns>
	public static ResolutionInfo CalcResolution(int width, int height)
	{
		const int maxWidth = 960;
		const int maxHeight = 720;
		const int superWidth = 1440;
		const float ratio9v16 = 9.0f / 16; // 9:16 のratio
		const float ratio9v18 = 9.0f / 18; // 9:18 のratio
		const float baseFieldOfView = 60f;

		var r = new ResolutionInfo
		{
			OriginalWidth = width,
			OriginalHeight = height,
			FieldOfView = baseFieldOfView,
		};

		var ratio = 1.0f * height / width; // 縦/横の比率

		if (ratio <= ratio9v16)
		{
			// 9:16より横長
			r.Height = height;
			r.Width = Mathf.RoundToInt(r.Height / ratio);
			r.UiHeight = maxHeight;
			r.UiWidth = maxWidth;
			r.UiViewHeight = maxHeight;
			r.UiViewWidth = Mathf.RoundToInt(maxHeight / ratio);
			r.UiFullHeight = r.UiViewHeight;
			r.UiFullWidth = r.UiViewWidth;
			r.FieldOfView = baseFieldOfView * r.UiViewHeight / maxHeight;
			if (ratio <= ratio9v18)
			{
				// 9:18より横長
				r.UiViewWidth = superWidth;
			}
		}
		else
		{
			// 9:16より縦長
			r.Width = width;
			r.Height = Mathf.RoundToInt(width * ratio);
			r.UiHeight = maxHeight;
			r.UiWidth = maxWidth;
			r.UiViewHeight = Mathf.RoundToInt(maxWidth * ratio);
			r.UiViewWidth = maxWidth;
			r.UiFullHeight = r.UiViewHeight;
			r.UiFullWidth = r.UiViewWidth;
		}

		r.UiScale = Mathf.Min(1f * r.OriginalWidth / r.UiWidth, 1f * r.OriginalHeight / r.UiHeight);

		return r;
	}

	void ResetAspect()
	{
		var ri = CalcResolution(Screen.width, Screen.height);
		Debug.LogFormat("ResolutionInfo={0}", JsonUtility.ToJson(ri));
		CurrentScreenInfo = ri;

		if (MainCam != null)
		{
			MainCam.fieldOfView = ri.FieldOfView;
		}
		if (SelfCam != null)
		{
			if (SelfCam.orthographic == false)
			{
				SelfCam.fieldOfView = ri.FieldOfView;
			}
			else
			{
				SelfCam.orthographicSize = (ri.FieldOfView / 10) - (SelfCam.orthographicSize / 10);
			}
		}
	}

	void Update()
	{
		#if UNITY_EDITOR
		// 解像度の変更を検知する
		if (originalScreenWidth_ != Screen.width || originalScreenHeight_ != Screen.height)
		{
			// 初回は無視する
			if (originalScreenWidth_ != 0)
			{
				Debug.Log("Resolution changed!");
				ResetAspect();
			}
			originalScreenWidth_ = Screen.width;
			originalScreenHeight_ = Screen.height;
		}
		#endif
	}
}
