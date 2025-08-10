using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using MySidearms.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace MySidearms.Intercepts
{
    [HarmonyPatch(typeof(Verb), nameof(Verb.Notify_EquipmentLost))]
    public static class Verb_Notify_EquipmentLost_Patches
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Verb_Notify_EquipmentLost_Transpiler(IEnumerable<CodeInstruction> instructions) 
        {
            //prevent EquipmentLost from cancelling the current job if we just switched to another valid ranged weapon

            var codeMatcher = new CodeMatcher(instructions);

            var toMatch = new CodeMatch[]
            {
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Job), nameof(Job.def))),
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(JobDefOf), nameof(JobDefOf.AttackStatic))),
                new CodeMatch(OpCodes.Bne_Un_S)
            };

            CodeInstruction[] toInsert = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Verb_Notify_EquipmentLost_Patches), nameof(CheckIfEquippingRangedWeapon))),
                new CodeInstruction(OpCodes.Brtrue_S),
            };

            codeMatcher.MatchEndForward(toMatch);
            if (!codeMatcher.IsInvalid)
                toInsert[2].operand = codeMatcher.Instruction.operand;
            codeMatcher.Advance(1);
            codeMatcher.Insert(toInsert);

            if (codeMatcher.IsInvalid)
            {
                Log.Warning("MS: failed to apply transpiler on Verb_Notify_EquipmentLost!");
                return instructions;
            }
            else
                return codeMatcher.InstructionEnumeration();
        }

        private static bool CheckIfEquippingRangedWeapon(Pawn pawn)
        {
            //yes, evil static global. I know.
            return WeaponAssignment.CurrentlyEquippingWeapon?.def.IsRangedWeapon ?? false;
        }
    }
}
