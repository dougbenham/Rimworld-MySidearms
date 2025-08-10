using System;
using HarmonyLib;
using Verse;

namespace MySidearms.Compat
{
    public static class OptionalPatches
    {
        public static void Patch(Harmony harmony)
        {
            //Log.Warning("Doing optional patches...");
        }

        public static void PatchDelayed(Harmony harmony)
        {
            if (Tacticowl.active)
            {
                //Log.Warning("Doing Tacticowl patches...");
                try
                {
                    Tacticowl.Patch_Delayed_Tacticowl(harmony);
                }
                catch (Exception e) 
                {
                    Log.Error("MS: Error during patching Tacticowl: " + e);
                }
            }
        }
    }
}
