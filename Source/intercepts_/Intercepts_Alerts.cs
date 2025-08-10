namespace MySidearms.Intercepts
{

    /*[HarmonyPatch(typeof(Alert_HunterHasShieldAndRangedWeapon), "HuntersWithoutRangedWeapon")]
    [HarmonyPatch(MethodType.Getter)]
    public static class Alert_HunterLacksRangedWeapon_HuntersWithoutRangedWeapon_Postfix
    {
        [HarmonyPostfix]
        public static void HuntersWithoutRangedWeapon(Alert_HunterLacksRangedWeapon __instance, List<Pawn> __result)
        {
            for (int i = __result.Count - 1; i >= 0; i--)
            {
                Pawn pawn = __result[i];
                CompSidearmMemory pawnMemory = CompSidearmMemory.GetMemoryCompForPawn(pawn);
                if (pawnMemory != null && pawn.IsValidSidearmsCarrierRightNow() && pawnMemory.IsUsingAutotool(true, true))
                {
                    __result.Remove(pawn);
                }
            }
        }
    }*/
}
