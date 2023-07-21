using RimWorld;
using UnityEngine;
using Verse;

namespace EFR
{
    public class FantasyRaces : Mod
    {
        private readonly FantasyRaceSettings Settings;

        public FantasyRaces(ModContentPack content) : base(content)
        {
            Settings = GetSettings<FantasyRaceSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Settings.Draw(inRect);
        }

        public override string SettingsCategory()
        {
            return "Fantasy Races";
        }

        /// <summary>
        /// Whether this xenotype is a fantasy race xenotype.
        /// </summary>
        public static bool IsFantasyRace_Xenotype(XenotypeDef xenotypeDef)
        {
            // there's probably a better way to check that the def comes from this mod
            return xenotypeDef.defName.StartsWith("EFR_");
        }

        /// <summary>
        /// Whether this pawn has a fantasy race xenotype.
        /// </summary>
        public static bool IsFantasyRace_Pawn(Pawn pawn, out XenotypeDef xenotypeDef)
        {
            // genes are null on non-human pawns
            if (pawn.genes != null && IsFantasyRace_Xenotype(pawn.genes.Xenotype))
            {
                xenotypeDef = pawn.genes.Xenotype;
                return true;
            }

            xenotypeDef = null;
            return false;

        }

        /// <summary>
        /// Whether this pawn generation request will result in a pawn of a fantasy race xenotype being generated.
        /// 
        /// Chance is based on the request's faction xenotype chances.
        /// 
        /// This will set the request.ForcedXenotype property.
        /// </summary>
        public static bool IsFantasyRace_Generation(ref PawnGenerationRequest request)
        {
            request.ForcedXenotype = PawnGenerator.GetXenotypeForGeneratedPawn(request);
            return IsFantasyRace_Xenotype(request.ForcedXenotype);
        }

    }
}
