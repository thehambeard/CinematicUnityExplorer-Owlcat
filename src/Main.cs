using HarmonyLib;
using UnityModManagerNet;

namespace CinematicUnityExplorer;

#if DEBUG
[EnableReloading]
#endif

static class Main
{
    internal static Harmony HarmonyInstance { get; private set; }
    internal static UnityModManager.ModEntry.ModLogger Logger { get; private set; }
    internal static UnityModManager.ModEntry ModEntry { get; private set; }

    internal static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;
        Logger = modEntry.Logger;
        modEntry.OnUnload = OnUnload;
        modEntry.OnGUI = OnGUI;
        HarmonyInstance = new Harmony(modEntry.Info.Id);
        HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        return true;
    }

    static void OnGUI(UnityModManager.ModEntry modEntry) { }

    static bool OnUnload(UnityModManager.ModEntry modEntry)
    {
        HarmonyInstance.UnpatchAll(modEntry.Info.Id);
        return true;
    }
}
