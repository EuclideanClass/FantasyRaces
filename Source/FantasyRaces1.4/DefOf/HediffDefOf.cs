using RimWorld;
using rjw;
using Verse;

namespace EFR
{
    [DefOf]
    public class HediffDefOf
    {
        public static HediffDef_PartBase EFR_HarpyVagina;

        public static HediffDef_PartBase EFR_ArachneOvipositor;

        public static HediffDef EFR_HarpyEgg;

        public static HediffDef EFR_ArachneEgg;

        public static HediffDef_PartBase Vagina;

        static HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOf));
        }
    }
}
