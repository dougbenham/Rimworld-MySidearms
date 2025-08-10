using System.Collections.Generic;
using HarmonyLib;
using MySidearms.Rimworld;
using RimWorld.Planet;

namespace MySidearms.Intercepts
{
	//Reset the cache on game load
    [HarmonyPatch(typeof(World), nameof(World.FinalizeInit))]
    public static class Patch_World_FinalizeInit
    {
        public static void Postfix()
        {
            CompSidearmMemory._cache = new Dictionary<int, CompSidearmMemory>();
        }
    }
}
