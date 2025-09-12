// Assets/Scripts/WXBootstrap.cs
using UnityEngine;

#if UNITY_WEBGL && (WEIXINMINIGAME || PLATFORM_WEIXINMINIGAME || WECHAT)
using WeChatWASM;  // 来自 WX-WASM-SDK-V2
#endif

/// <summary>
/// 微信小游戏 SDK 首场景自动初始化（无需手动挂载）
/// 1) 进入任意场景前，创建常驻 GameObject 并初始化 SDK
/// 2) 在微信开发者工具/真机里弹 Toast + 打 Log，读取系统信息用于校验
/// </summary>
public static class WXBootstrapAuto
{
    // 保证所有场景都能先执行到这里
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        // 避免重复创建
        if (Object.FindObjectOfType<WXBootstrapRunner>() != null) return;

        var go = new GameObject("WXBootstrap");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<WXBootstrapRunner>();
    }
}

public class WXBootstrapRunner : MonoBehaviour
{
    private static bool _inited;

    private void Awake()
    {
        if (_inited) { Debug.Log("[WX] already inited"); return; }
        _inited = true;

#if UNITY_WEBGL && !UNITY_EDITOR && (WEIXINMINIGAME || PLATFORM_WEIXINMINIGAME || WECHAT)
        Debug.Log("[WX] InitSDK() begin");

        // —— 1) 初始化 SDK（通常默认参数即可）
        WX.InitSDK(new WXSDKOption { });

        // —— 2) 给出明确可见的提示，方便在“开发者工具/真机”确认
        WX.ShowToast(new ShowToastOption { title = "WX.InitSDK OK", duration = 1000 });
        Debug.Log("[WX] InitSDK() done, toast shown");

        // —— 3) 读取系统信息，确认处于小游戏环境
        WX.GetSystemInfo(new GetSystemInfoOption
        {
            success = info =>
            {
                Debug.Log($"[WX] SystemInfo: model={info.model}, platform={info.platform}, system={info.system}, SDKVer={info.SDKVersion}");
            },
            fail = err =>
            {
                Debug.LogWarning("[WX] GetSystemInfo fail: " + (err?.errMsg ?? "unknown"));
            }
        });

        // （可选）保持常亮，便于调试
        // WX.SetKeepScreenOn(new SetKeepScreenOnOption { keepScreenOn = true });

#else
        // 非小游戏环境：仅打印提示，防止你在 Editor 里误以为没生效
        Debug.Log("[WX] InitSDK skipped —— 当前不是微信小游戏运行环境（Editor/普通WebGL）");
#endif
    }
}