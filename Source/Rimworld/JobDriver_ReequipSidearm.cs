namespace MySidearms.Rimworld
{
    public class JobDriver_ReequipSidearm : JobDriver_EquipSidearm
    {
        public override bool MemorizeOnPickup { get { return false; } }
    }
}
