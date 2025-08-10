using HarmonyLib;
using MySidearms.Rimworld;
using MySidearms.Utilities;
using RimWorld;
using Verse;
using static MySidearms.Utilities.Enums;

namespace MySidearms.Intercepts
{

    [HarmonyPatch(typeof(Verb_ShootOneUse), "SelfConsume")]
    public static class Verb_ShootOneUse_SelfConsume_Postfix
    {
        [HarmonyPostfix]
        public static void SelfConsume(Verb_ShootOneUse __instance)
        {
            if (__instance.caster is Pawn pawn)
            {
                ThingDefStuffDefPair weapon = __instance.EquipmentSource.toThingDefStuffDefPair();
                bool anotherFound = WeaponAssignment.EquipSpecificWeaponTypeFromInventory(pawn, weapon, false, false);
                if (!anotherFound)
                    WeaponAssignment.EquipBestWeaponFromInventoryByPreference(pawn, DroppingModeEnum.UsedUp);
            }
        }
    }

    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class Verb_MeleeAttack_TryCastShot_PostFix
    {

        [HarmonyPostfix]
        public static void TryCastShot(Verb_MeleeAttack __instance)
        {
            if (__instance.CurrentTarget != null)
            {
                Thing targetThing = __instance.CurrentTarget.Thing;
                if (__instance.CasterPawn != null && targetThing != null && targetThing is Pawn && !(targetThing as Pawn).Dead && (targetThing as Pawn).RaceProps.Humanlike && (targetThing as Pawn).equipment != null)
                {
                    WeaponAssignment.DoCqc(targetThing as Pawn, __instance.CasterPawn);
                }
            }
        }
    }
}
