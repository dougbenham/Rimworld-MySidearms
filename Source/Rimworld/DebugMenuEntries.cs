using LudeonTK;
using Verse;

namespace MySidearms.Rimworld
{
    [StaticConstructorOnStartup]
    public static class DebugMenuEntries
    {
        private const string CATEGORY = "Simple Sidearms";

        [DebugAction(category = CATEGORY, actionType = DebugActionType.Action)]
        static void ToggleBrainscope()
        {
            MySidearms.Settings.ShowBrainscope = !MySidearms.Settings.ShowBrainscope;
        }
    }
}
