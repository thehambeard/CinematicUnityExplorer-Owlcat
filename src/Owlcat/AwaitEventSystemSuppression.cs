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
            if (array[i - 1].opcode == OpCodes.Ldstr &&
                array[i].Calls(AccessTools.Method(typeof(Debug), nameof(Debug.Log), [typeof(string)])))
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