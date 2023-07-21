using RimWorld;

namespace EFR
{
    [DefOf]
    public class XenotypeDefOf
    {
        public static XenotypeDef EFR_Catgirl;

        public static XenotypeDef EFR_Foxgirl;

        public static XenotypeDef EFR_Succubus;

        public static XenotypeDef EFR_Harpy;

        public static XenotypeDef EFR_Arachne;

        public static XenotypeDef EFR_Slimegirl;

        public static XenotypeDef EFR_Dragongirl;

        public static XenotypeDef EFR_Orc;

        static XenotypeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(XenotypeDefOf));
        }
    }
}
