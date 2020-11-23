using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

/// <summary>
/// ビルド用クラス
/// </summary>
[InitializeOnLoad]
public class AppBuilder
{
	/// <summary>
	/// アンドロイドのKeyAliasパスワード
	/// </summary>
	static string KeyAliasPass = null;

	static string AppName = "castle";

	/// <summary>
	/// ビルド実行
	/// </summary>
	static void Build()
	{
		//string outPutPath = "";
		bool isRelease = false;
		string buildTargetString = "";
		string[] args = System.Environment.GetCommandLineArgs();
		var outputPath = Path.Combine(Path.Combine(Application.dataPath, ".."), "AppBuilder");

		for (int i = 0; i < args.Length ; ++i )
		{
			switch (args[i])
			{
				case "-outputpath":
					Debug.Log("-outputpath :" + args[i + 1]);
					outputPath = args[i + 1];
					break;

				case "-target":
					Debug.Log("-target :" + args[i + 1]);
					buildTargetString = args[i + 1];
					break;

				case "-release":
					Debug.Log("-release :" + args[i + 1]);
					isRelease = true;
					break;

				case "-keyalias":
					Debug.Log("-keyalias :" + args[i + 1]);
					KeyAliasPass = args[i + 1];
					i++;
					break;
				case "-buildVersion":
					Debug.Log("-buildVersion :" + args[i + 1]);
					PlayerSettings.bundleVersion = args[i + 1];
					break;
				case "-buildNumber":
					Debug.Log("-buildNumber :" + args[i + 1]);
					PlayerSettings.iOS.buildNumber = args[i + 1];
					PlayerSettings.Android.bundleVersionCode = int.Parse(args[i + 1]);
					break;
			}
		}

		var target = buildTargetFromString(buildTargetString);
		var outputPath2 = Path.Combine(outputPath, OutputPathFromBuildTarget(target));

		buildForTarget(target, outputPath2, isRelease);
	}

	/// <summary>
	/// ビルドターゲットから、出力フォルダを取得する
	/// </summary>
	static string OutputPathFromBuildTarget(BuildTarget target)
	{
		return target.ToString();
	}

	static string getExtension(BuildTarget target)
	{
		switch (target)
		{
			case BuildTarget.StandaloneOSX:
				return "";
			case BuildTarget.Switch:
				return ".nspd";
			case BuildTarget.PS4:
				return "";
			case BuildTarget.WSAPlayer:
				return "";
			case BuildTarget.iOS:
				return "";
			case BuildTarget.Android:
				return ".apk";
			default:
				return ".exe";
		}
	}

	/// <summary>
	/// 文字列から、BuildTargetに変換する
	/// </summary>
	static BuildTarget buildTargetFromString(string val)
	{
		return (BuildTarget)System.Enum.Parse(typeof(BuildTarget), val, true);
	}

	[MenuItem("Tools/アプリビルダー/ビルド")]
	public static void BuildApp()
	{
		var target = EditorUserBuildSettings.activeBuildTarget;
		var assetBundlePath = Path.Combine(Path.Combine(Application.dataPath, ".."), "AppBuilder");
		var outputPath = Path.Combine(assetBundlePath, OutputPathFromBuildTarget(target));
		buildForTarget(target, outputPath, false);
	}


	static void buildForTarget(BuildTarget target, string outputPath, bool isRelease)
	{
		if (System.IO.Directory.Exists(outputPath))
		{
			System.IO.Directory.Delete(outputPath, true);
		}
		System.IO.Directory.CreateDirectory(outputPath);

		var opt = new BuildPlayerOptions
		{
			target = target,
			scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
			options = (isRelease ? BuildOptions.None : BuildOptions.Development) | BuildOptions.CompressWithLz4,
			locationPathName = System.IO.Path.Combine(outputPath, AppName + getExtension(target)),
		};

		switch( target)
		{
			case BuildTarget.Switch:
				EditorUserBuildSettings.switchCreateRomFile = false; // NSPDを出力する
				EditorUserBuildSettings.connectProfiler = true;
				break;
			case BuildTarget.WSAPlayer:
				EditorUserBuildSettings.wsaBuildAndRunDeployTarget = WSABuildAndRunDeployTarget.LocalMachine;
				EditorUserBuildSettings.wsaSubtarget = WSASubtarget.AnyDevice;
				EditorUserBuildSettings.wsaUWPBuildType = WSAUWPBuildType.D3D;
				if (!isRelease)
				{
					EditorUserBuildSettings.connectProfiler = true;
				}
				PlayerSettings.runInBackground = false; // XBOXでは、これを無効にする
				break;

			case BuildTarget.Android:
				PlayerSettings.applicationIdentifier = "com.toydea.dragonfangz";
				PlayerSettings.Android.keystorePass = KeyAliasPass;
				PlayerSettings.Android.keyaliasPass = KeyAliasPass;
				break;

			default:
				break;
		}

		var result = BuildPipeline.BuildPlayer(opt);
		Debug.Log(result);
	}

	/// <summary>
	/// XCode用の設定を行う
	/// </summary>
	[PostProcessBuildAttribute(9999)]
	static void OnPostprocessBuild(BuildTarget buildTarget, string path)
	{
		if (buildTarget != BuildTarget.iOS)
		{
			return;
		}

		#if UNITY_IOS
		string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";

		PBXProject proj = new PBXProject();
		proj.ReadFromFile(projPath);
		string target = proj.TargetGuidByName("Unity-iPhone");
		proj.SetBuildProperty (target, "ENABLE_BITCODE", "NO");


		proj.AddCapability(target, PBXCapabilityType.InAppPurchase);

		proj.WriteToFile(projPath);
		#endif
	}
}