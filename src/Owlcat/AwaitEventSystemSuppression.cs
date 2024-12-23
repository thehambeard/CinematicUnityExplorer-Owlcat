using CinematicUnityExplorer;
using HarmonyLib;
using Kingmaker.UI.Selection;
using System.Reflection.Emit;

[HarmonyPatch(typeof(KingmakerInputModule), nameof(KingmakerInputModule.CheckEventSystem), methodType: MethodType.Enumerator)]
static class AwaitEventSystemSuppression
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var array = instructions.ToArray();
        for (var i = 1; i < array.Length; i++)
        {
            var test = AccessTools.Method(typeof(Debug), nameof(Debug.Log), [typeof(object)]);

            if (array[i - 1].opcode == OpCodes.Ldstr &&
                array[i].Calls(AccessTools.Method(typeof(Debug), nameof(Debug.Log), [typeof(object)])))
            {
                Main.Logger.Log($"Silencing \"{array[i - 1].operand}\"");

                array[i - 1].opcode = OpCodes.Nop;
                array[i - 1].operand = null;

                array[i].opcode = OpCodes.Nop;
                array[i].operand = null;
            }
        }

        return instructions;
    }
}