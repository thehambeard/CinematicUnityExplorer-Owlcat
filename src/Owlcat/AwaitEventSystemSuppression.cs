using CinematicUnityExplorer;
using HarmonyLib;
using Kingmaker.UI.Selection;
#if (RT || WOTR)
using Owlcat.Runtime.Core.Logging;
#endif
using System.Reflection.Emit;

[HarmonyPatch(typeof(KingmakerInputModule), nameof(KingmakerInputModule.CheckEventSystem), methodType: MethodType.Enumerator)]
static class AwaitEventSystemSuppression
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var array = instructions.ToArray();
#if RT

        var methodInfo = AccessTools.Method(typeof(LogChannel), nameof(LogChannel.Log), [typeof(string)]);
#endif
#if WOTR

        var methodInfo = AccessTools.Method(typeof(LogChannel), nameof(LogChannel.Log), [typeof(string), typeof(object[])]);
#endif
#if KM

        var methodInfo = AccessTools.Method(typeof(Debug), nameof(Debug.Log), [typeof(object)]);
#endif

        for (var i = 1; i < array.Length; i++)
        {
            
            if (array[i - 1].opcode == OpCodes.Ldstr && array[i].Calls(methodInfo))
            {
                Main.Logger.Log($"Silencing \"{array[i - 1].operand}\"");

                array[i - 1].opcode = OpCodes.Nop;
                array[i - 1].operand = null;

                array[i].opcode = OpCodes.Pop;
                array[i].operand = null;
            }
        }

        return instructions;
    }
}