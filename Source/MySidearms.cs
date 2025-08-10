using System.Reflection;
using HarmonyLib;
using MySidearms.Compat;
using UnityEngine;
using Verse;

namespace MySidearms
{
    public class MySidearms : Mod
    {
        public static MySidearms_Settings Settings { get; internal set; }
        public static MySidearms ModSingleton { get; private set; }
        public static Harmony Harmony { get; private set; } 

        public MySidearms(ModContentPack content) : base(content)
        {
            ModSingleton = this;
            Harmony = new Harmony("doug.MySidearms");
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
            OptionalPatches.Patch(Harmony);
        }

        public override string SettingsCategory()
        {
            return "MySidearms_ModTitle".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }

    [StaticConstructorOnStartup]
    public static class MySidearms_PostInit 
    {
        static MySidearms_PostInit()
        {
            OptionalPatches.PatchDelayed(MySidearms.Harmony);

            MySidearms.Settings = MySidearms.ModSingleton.GetSettings<MySidearms_Settings>();
            InferredValues.Init();
            MySidearms.Settings.StartupChecks();

            if (MySidearms.Settings.NeedsResaving)
            {
                Log.Message($"MS: Resaving settings by request (one-time migration or clearing out invalid defs).");
                MySidearms.Settings.NeedsResaving = false;
                MySidearms.ModSingleton.WriteSettings();
            }
        }
    }

}
