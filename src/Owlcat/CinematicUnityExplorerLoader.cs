using HarmonyLib;
using Kingmaker.Code.UI.MVVM;
using UnityExplorer;

namespace CinematicUnityExplorer.Owlcat;

[HarmonyPatch]
internal static class CinematicUnityExplorerLoader
{
    static bool _loaded;

    [HarmonyPatch(typeof(RootUIContext), nameof(RootUIContext.InitializeUiSceneCoroutine))]
    [HarmonyPostfix]
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
