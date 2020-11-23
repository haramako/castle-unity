using UnityEngine.EventSystems;

/// <summary>
/// UnityEngine.UI の CanvasScalerをコピーしてカスタマイズしたもの
/// </summary>
namespace UnityEngine.UI
{
	[RequireComponent(typeof(Canvas))]
	[ExecuteInEditMode]
	public class AppCanvasScaler : UIBehaviour
	{
		public RectTransform BaseUi;

		[Tooltip("If a sprite has this 'Pixels Per Unit' setting, then one pixel in the sprite will cover one unit in the UI.")]
		[SerializeField] protected float m_ReferencePixelsPerUnit = 100;
		public float referencePixelsPerUnit { get { return m_ReferencePixelsPerUnit; } set { m_ReferencePixelsPerUnit = value; } }


		// Constant Pixel Size settings

		float scaleFactor;


		[Tooltip("The resolution the UI layout is designed for. If the screen resolution is larger, the UI will be scaled up, and if it's smaller, the UI will be scaled down. This is done in accordance with the Screen Match Mode.")]
		[SerializeField] protected Vector2 m_ReferenceResolution = new Vector2(800, 600);
		public Vector2 referenceResolution
		{
			get
			{
				return m_ReferenceResolution;
			}
			set
			{
				m_ReferenceResolution = value;

				const float k_MinimumResolution = 0.00001f;

				if (m_ReferenceResolution.x > -k_MinimumResolution && m_ReferenceResolution.x < k_MinimumResolution) m_ReferenceResolution.x = k_MinimumResolution * Mathf.Sign(m_ReferenceResolution.x);
				if (m_ReferenceResolution.y > -k_MinimumResolution && m_ReferenceResolution.y < k_MinimumResolution) m_ReferenceResolution.y = k_MinimumResolution * Mathf.Sign(m_ReferenceResolution.y);
			}
		}

		// General variables

		private Canvas m_Canvas;
		[System.NonSerialized]
		private float m_PrevScaleFactor = 1;
		[System.NonSerialized]
		private float m_PrevReferencePixelsPerUnit = 100;

		protected AppCanvasScaler() {}

		protected override void OnEnable()
		{
			base.OnEnable();
			m_Canvas = GetComponent<Canvas>();
			Handle();
		}

		protected override void OnDisable()
		{
			SetScaleFactor(1);
			SetReferencePixelsPerUnit(100);
			base.OnDisable();
		}

		protected virtual void Update()
		{
			Handle();
		}

		protected virtual void Handle()
		{
			if (m_Canvas == null || !m_Canvas.isRootCanvas)
				return;

			if (m_Canvas.renderMode == RenderMode.WorldSpace)
			{
				HandleWorldCanvas();
				return;
			}

			HandleScaleWithScreenSize();
		}

		protected virtual void HandleWorldCanvas()
		{
			SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
		}

		protected virtual void HandleScaleWithScreenSize()
		{
			Vector2 screenSize = new Vector2(Screen.width, Screen.height);

			// Multiple display support only when not the main display. For display 0 the reported
			// resolution is always the desktops resolution since its part of the display API,
			// so we use the standard none multiple display method. (case 741751)
			int displayIndex = m_Canvas.targetDisplay;
			if (displayIndex > 0 && displayIndex < Display.displays.Length)
			{
				Display disp = Display.displays[displayIndex];
				screenSize = new Vector2(disp.renderingWidth, disp.renderingHeight);
			}

			var ri = CamScaler.CalcResolution((int)screenSize.x, (int)screenSize.y);

			if (BaseUi != null)
			{
				BaseUi.sizeDelta = new Vector2(ri.UiWidth, ri.UiHeight);
			}

			scaleFactor = ri.UiScale;
			SetScaleFactor(scaleFactor);
			SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
		}

		protected void SetScaleFactor(float scaleFactor)
		{
			if (scaleFactor == m_PrevScaleFactor)
				return;

			m_Canvas.scaleFactor = scaleFactor;
			m_PrevScaleFactor = scaleFactor;
		}

		protected void SetReferencePixelsPerUnit(float referencePixelsPerUnit)
		{
			if (referencePixelsPerUnit == m_PrevReferencePixelsPerUnit)
				return;

			m_Canvas.referencePixelsPerUnit = referencePixelsPerUnit;
			m_PrevReferencePixelsPerUnit = referencePixelsPerUnit;
		}

		#if UNITY_EDITOR
		protected override void OnValidate()
		{
			scaleFactor = Mathf.Max(0.01f, scaleFactor);
		}

		#endif
	}
}
