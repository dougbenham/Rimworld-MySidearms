using System;
using RimWorld;
using Verse;
using static MySidearms.MySidearms;
using static MySidearms.Utilities.Enums;

namespace MySidearms.Utilities
{
    public static class MiscUtils
    {
        public static readonly float ANTI_OSCILLATION_FACTOR = 0.1f;

        public static bool shouldDrop(Pawn pawn, DroppingModeEnum mode, bool ignoreRecoveryChance)
        {
            bool drop;
            switch (Settings.FumbleMode)
            {
                case FumbleModeOptionsEnum.Never:
                    drop = false;
                    break;
                case FumbleModeOptionsEnum.InDistress:
                    if (mode == DroppingModeEnum.InDistress)
                        drop = true;
                    else
                        drop = false;
                    break;
                case FumbleModeOptionsEnum.InCombat:
                    if (mode == DroppingModeEnum.InDistress || mode == DroppingModeEnum.Combat)
                        drop = true;
                    else
                        drop = false;
                    break;
                case FumbleModeOptionsEnum.Always:
                default:
                    drop = true;
                    break;
            }
            if (ignoreRecoveryChance)
            {
                return drop;
            }
            else if (drop) 
            {
                var bestSkill = Math.Max(pawn.skills.GetSkill(SkillDefOf.Shooting).Level, pawn.skills.GetSkill(SkillDefOf.Melee).Level);
                var chance = Settings.FumbleRecoveryChance.Evaluate(bestSkill);
                var recovered = Rand.Chance(chance);
                return !recovered;
            }
            return false;
        }

        public static void DoNothing()
        {
        }

        public static WeaponSearchType LimitTypeToListType(WeaponListKind type)
        {
            switch (type)
            {
                case WeaponListKind.Melee:
                    return WeaponSearchType.Melee;
                case WeaponListKind.Ranged:
                    return WeaponSearchType.Ranged;
                case WeaponListKind.Both:
                default:
                    return WeaponSearchType.Both;
            }
        }
    }

}
