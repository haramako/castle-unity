using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppCanvasScalerRect : MonoBehaviour
{
	int width_;
	int height_;

	/// <summary>
	/// 画面全体を覆うかどうか（セーフ領域よりもさらにのびて、絶対に画面全体を占める）
	/// </summary>
	public bool Fullscreen;

	void Update ()
	{
		if (Application.isPlaying)
		{
			resize();
		}
	}

	void resize()
	{
		var ri = CamScaler.CurrentScreenInfo;
		if (ri == null)
		{
			return;
		}

		if (Fullscreen)
		{
			// サイズが変わっていないならなにもしない
			if (width_ == ri.UiFullWidth && height_ == ri.UiFullHeight)
			{
				return;
			}

			width_ = ri.UiFullWidth;
			height_ = ri.UiFullHeight;

			var rt = this.GetComponent<RectTransform>();
			rt.sizeDelta = new Vector2(ri.UiFullWidth, ri.UiFullHeight);
		}
		else
		{
			// サイズが変わっていないならなにもしない
			if (width_ == ri.UiViewWidth && height_ == ri.UiViewHeight)
			{
				return;
			}

			width_ = ri.UiViewWidth;
			height_ = ri.UiViewHeight;

			var rt = this.GetComponent<RectTransform>();
			rt.sizeDelta = new Vector2(ri.UiViewWidth, ri.UiViewHeight);
		}
	}
}
