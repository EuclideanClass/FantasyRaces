using RimWorld;
using rjw;
using System.Collections.Generic;
using System.Security.Policy;
using Verse;

namespace EFR
{

    /// <summary>
    /// Handles setting custom genitals for fantasy race xenotypes.
    /// </summary>
    public static class RaceSupport
    {
        private static readonly Dictionary<XenotypeDef, List<HediffDef>> GenitalsByXenotype_Female = new Dictionary<XenotypeDef, List<HediffDef>>();

        private static readonly Dictionary<XenotypeDef, List<HediffDef>> GenitalsByXenotype_Male = new Dictionary<XenotypeDef, List<HediffDef>>();

        private static readonly Dictionary<XenotypeDef, HediffDef> AnusesByXenotype = new Dictionary<XenotypeDef, HediffDef>();

        private static readonly Dictionary<XenotypeDef, HashSet<RaceTag>> RaceTagsByXenotype = new Dictionary<XenotypeDef, HashSet<RaceTag>>();

        private static readonly Dictionary<XenotypeDef, float> SexDrivesByXenotype = new Dictionary<XenotypeDef, float>();

        static RaceSupport()
        {
            // catgirl
            GenitalsByXenotype_Female.SetOrAdd(XenotypeDefOf.EFR_Catgirl, new List<HediffDef> { Genital_Helper.feline_vagina });
            GenitalsByXenotype_Male.SetOrAdd(XenotypeDefOf.EFR_Catgirl, new List<HediffDef> { Genital_Helper.feline_penis });
            RaceTagsByXenotype.SetOrAdd(XenotypeDefOf.EFR_Catgirl, new HashSet<RaceTag> { RaceTag.Fur });
            SexDrivesByXenotype.SetOrAdd(XenotypeDefOf.EFR_Catgirl, 1.5f);

            // foxgirl
            GenitalsByXenotype_Female.SetOrAdd(XenotypeDefOf.EFR_Foxgirl, new List<HediffDef> { Genital_Helper.canine_vagina });
            GenitalsByXenotype_Male.SetOrAdd(XenotypeDefOf.EFR_Foxgirl, new List<HediffDef> { Genital_Helper.canine_penis });
            RaceTagsByXenotype.SetOrAdd(XenotypeDefOf.EFR_Foxgirl, new HashSet<RaceTag> { RaceTag.Fur });

            // succubus
            GenitalsByXenotype_Female.SetOrAdd(XenotypeDefOf.EFR_Succubus, new List<HediffDef> { Genital_Helper.demon_vagina});
            GenitalsByXenotype_Male.SetOrAdd(XenotypeDefOf.EFR_Succubus, new List<HediffDef> { Genital_Helper.demon_penis });
            AnusesByXenotype.SetOrAdd(XenotypeDefOf.EFR_Succubus, Genital_Helper.demon_anus);
            RaceTagsByXenotype.SetOrAdd(XenotypeDefOf.EFR_Succubus, new HashSet<RaceTag> { RaceTag.Demon });
            SexDrivesByXenotype.SetOrAdd(XenotypeDefOf.EFR_Succubus, 1.5f); // keep in mind they also get +200% from the 'Nymphomaniac' trait

            // harpy
            GenitalsByXenotype_Female.SetOrAdd(XenotypeDefOf.EFR_Harpy, new List<HediffDef> { HediffDefOf.EFR_HarpyVagina});
            RaceTagsByXenotype.SetOrAdd(XenotypeDefOf.EFR_Harpy, new HashSet<RaceTag> { RaceTag.Feathers });
            SexDrivesByXenotype.SetOrAdd(XenotypeDefOf.EFR_Harpy, 1.5f);

            // arachne
            GenitalsByXenotype_Female.SetOrAdd(XenotypeDefOf.EFR_Arachne, new List<HediffDef> { HediffDefOf.EFR_ArachneOvipositor, HediffDefOf.Vagina });
            GenitalsByXenotype_Male.SetOrAdd(XenotypeDefOf.EFR_Arachne, new List<HediffDef> { HediffDefOf.EFR_ArachneOvipositor });
            AnusesByXenotype.SetOrAdd(XenotypeDefOf.EFR_Arachne, Genital_Helper.insect_anus);
            RaceTagsByXenotype.SetOrAdd(XenotypeDefOf.EFR_Arachne, new HashSet<RaceTag> { RaceTag.Chitin });
            SexDrivesByXenotype.SetOrAdd(XenotypeDefOf.EFR_Arachne, 2f);

            // slimegirl
            GenitalsByXenotype_Female.SetOrAdd(XenotypeDefOf.EFR_Slimegirl, new List<HediffDef> { Genital_Helper.slime_vagina });
            GenitalsByXenotype_Male.SetOrAdd(XenotypeDefOf.EFR_Slimegirl, new List<HediffDef> { Genital_Helper.slime_penis });
            AnusesByXenotype.SetOrAdd(XenotypeDefOf.EFR_Slimegirl, Genital_Helper.slime_anus);
            RaceTagsByXenotype.SetOrAdd(XenotypeDefOf.EFR_Arachne, new HashSet<RaceTag> { RaceTag.Slime });

            // dragongirl
            GenitalsByXenotype_Female.SetOrAdd(XenotypeDefOf.EFR_Dragongirl, new List<HediffDef> { Genital_Helper.dragon_vagina });
            GenitalsByXenotype_Male.SetOrAdd(XenotypeDefOf.EFR_Dragongirl, new List<HediffDef> { Genital_Helper.dragon_penis });
            RaceTagsByXenotype.SetOrAdd(XenotypeDefOf.EFR_Dragongirl, new HashSet<RaceTag> { RaceTag.Scales });
            SexDrivesByXenotype.SetOrAdd(XenotypeDefOf.EFR_Dragongirl, 0.8f);

            // orc
            RaceTagsByXenotype.SetOrAdd(XenotypeDefOf.EFR_Orc, new HashSet<RaceTag> { RaceTag.Skin });
            SexDrivesByXenotype.SetOrAdd(XenotypeDefOf.EFR_Orc, 1.5f);

        }

        public static bool HasCustom_Genitals(XenotypeDef xenotypeDef, Gender gender, out List<HediffDef> customGenitals)
        {
            if (gender == Gender.Male)
            {
                return GenitalsByXenotype_Male.TryGetValue(xenotypeDef, out customGenitals);
            }

            if (gender == Gender.Female)
            {
                return GenitalsByXenotype_Female.TryGetValue(xenotypeDef, out customGenitals);
            }

            customGenitals = null;
            return false;
        }

        public static bool HasCustom_Anus(XenotypeDef xenotypeDef, out HediffDef customAnus)
        {
            return AnusesByXenotype.TryGetValue(xenotypeDef, out customAnus);
        }

        public static bool HasCustom_RaceTags(XenotypeDef xenotypeDef, out HashSet<RaceTag> raceTags)
        {
            return RaceTagsByXenotype.TryGetValue(xenotypeDef, out raceTags);
        }

        public static bool HasCustom_RaceSexDrive(XenotypeDef xenotypeDef, out float raceSexDrive)
        {
            return SexDrivesByXenotype.TryGetValue(xenotypeDef, out raceSexDrive);
        }
    }
}
