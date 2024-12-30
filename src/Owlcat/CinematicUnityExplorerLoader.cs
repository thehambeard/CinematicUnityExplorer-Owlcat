using HarmonyLib;
#if RT
using Kingmaker.Code.UI.MVVM;
#endif
#if WOTR
using Kingmaker.UI.MVVM;
#endif
#if KM
using Kingmaker;
#endif
using UnityExplorer;

namespace CinematicUnityExplorer.Owlcat;

[HarmonyPatch]
internal static class CinematicUnityExplorerLoader
{
    static bool _loaded;

#if RT
    [HarmonyPatch(typeof(RootUIContext), nameof(RootUIContext.InitializeUiSceneCoroutine))]
    [HarmonyPostfix]
#endif

#if WOTR
    [HarmonyPatch(typeof(RootUIContext), nameof(RootUIContext.InitializeUiScene))]
    [HarmonyPostfix]
#endif

#if KM
    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Awake))]
    [HarmonyPostfix]
#endif

    static void InitializeUiScene_Postfix() => LoadUnityExplorer();

    static void LoadUnityExplorer()
    {
        if (_loaded)
            return;

        Main.Logger.Log("Loading CinematicUnityExplorer...");

        try
        {
            ExplorerStandalone.CreateInstance(delegate (string msg, LogType logType)
            {
                switch (logType)
                {
                    case LogType.Error:
                        Main.Logger.Error(msg);
                        break;
                    case LogType.Assert:
                        Main.Logger.Critical(msg);
                        break;
                    case LogType.Warning:
                        Main.Logger.Warning(msg);
                        break;
                    case LogType.Log:
                        Main.Logger.Log(msg);
                        break;
                    case LogType.Exception:
                        Main.Logger.Error(msg);
                        break;
                }
            });

            _loaded = true;
        }
        catch (Exception e)
        {
            Main.Logger.LogException(e);
        }
    }
}