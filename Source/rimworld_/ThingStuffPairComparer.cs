using System.Collections.Generic;

namespace MySidearms.Rimworld
{
    public class ThingDefStuffDefComparer : IEqualityComparer<ThingDefStuffDefPair>
    {
        public bool Equals(ThingDefStuffDefPair x, ThingDefStuffDefPair y)
        {
            if (x == y)
                return true;
            else
                return false;
        }

        public int GetHashCode(ThingDefStuffDefPair obj)
        {
            return obj.GetHashCode();
        }
    }
}
