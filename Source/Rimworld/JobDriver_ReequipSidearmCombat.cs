using Verse.AI;

namespace MySidearms.Rimworld
{
    public class JobDriver_ReequipSidearmCombat : JobDriver_ReequipSidearm
    {
        public override Toil OnFinish()
        {
            return Toils_Goto.Goto(TargetIndex.B, PathEndMode.OnCell);
        }

    }
}
