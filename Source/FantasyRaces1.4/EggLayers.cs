using RimWorld;
using rjw;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace EFR
{
    /// <summary>
    /// Patches and configuration for egg laying xenotypes, such as gestation rates, egg labels, and minimum ages.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class EggLayers
    {
        private static readonly int MinAgeForEggsToAppear = 18;

        // custom xenotypes that lay eggs
        private static readonly HashSet<XenotypeDef> EggLayingXenotypes;

        private static readonly Dictionary<XenotypeDef, HediffDef> EggsByXenotype;

        // how much faster egg gestation is
        private static readonly Dictionary<XenotypeDef, float> GestationRateFactorsByXenotype;

        // what age pawns of each xenotype should hatch at
        private static readonly Dictionary<XenotypeDef, int> SpawnAgesByXenotype;

        static EggLayers()
        {
            EggLayingXenotypes = new HashSet<XenotypeDef> { XenotypeDefOf.EFR_Harpy, XenotypeDefOf.EFR_Arachne };

            EggsByXenotype = new Dictionary<XenotypeDef, HediffDef>(EggLayingXenotypes.Count);

            GestationRateFactorsByXenotype = new Dictionary<XenotypeDef, float>(EggLayingXenotypes.Count);

            SpawnAgesByXenotype = new Dictionary<XenotypeDef, int>(EggLayingXenotypes.Count);

            // harpy
            EggsByXenotype.SetOrAdd(XenotypeDefOf.EFR_Harpy, HediffDefOf.EFR_HarpyEgg);
            GestationRateFactorsByXenotype.SetOrAdd(XenotypeDefOf.EFR_Harpy, 1.5f);
            SpawnAgesByXenotype.SetOrAdd(XenotypeDefOf.EFR_Harpy, 16);

            // arachne
            EggsByXenotype.SetOrAdd(XenotypeDefOf.EFR_Arachne, HediffDefOf.EFR_ArachneEgg);
            GestationRateFactorsByXenotype.SetOrAdd(XenotypeDefOf.EFR_Arachne, 6f);
            SpawnAgesByXenotype.SetOrAdd(XenotypeDefOf.EFR_Arachne, 18);
        }

        /// <summary>
        /// Whether this pawn has a fantasy race xenotype that lays eggs.
        /// </summary>
        public static bool IsEggLayingPawn(Pawn pawn, out XenotypeDef xenotypeDef)
        {
            return FantasyRaces.IsFantasyRace_Pawn(pawn, out xenotypeDef) && EggLayingXenotypes.Contains(xenotypeDef);
        }

        /// <summary>
        /// Modifies the egg label to show the pawns xenotype instead of race.
        /// E.g. "Human egg" becomes "Harpy egg" for eggs of the harpy xenotype.
        /// </summary>
        public static string CustomEgg_Label(XenotypeDef xenotypeDef)
        {
            return xenotypeDef.label + " egg";
        }

        /// <summary>
        /// Modifies the trailing brackets portion of the egg label to show whether the egg is fertilized,
        /// regardless of devmode being active.
        /// E.g. "Harpy egg" becomes "Harpy egg (Fertilized)" or "Harpy egg (Unfertilized)"
        /// </summary>
        public static string CustomEgg_LabelInBrackets(bool fertilized)
        {
            return fertilized ? "Fertilized" : "Unfertilized";
        }

        /// <summary>
        /// Modifies the hover text of the egg label to show gestation progress, as well as the pawn that
        /// implanted the egg and the "father" of the egg, if defined.
        /// </summary>
        public static string CustomEgg_TipStringExtra(string existingTipString, float gestationProgress, Pawn implanter, Pawn father)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(existingTipString);
            stringBuilder.AppendLine($" Gestation progress: {Math.Min(gestationProgress, 1.0f).ToStringPercent()}");

            if (implanter != null) // shouldn't ever be null, but just in case
            {
                stringBuilder.AppendLine($" Implanter: {xxx.get_pawnname(implanter)}");
            }
            if (father != null)
            {
                stringBuilder.AppendLine($" Father: {xxx.get_pawnname(father)}");
            }

            return stringBuilder.ToString();
        }

        public static float CalculateGestationPeriod(float oldGestationPeriod, XenotypeDef xenotypeDef)
        {
            float gestationFactor;
            if (FantasyRaceSettings.FastGestation)
            {
                gestationFactor = 100f;
            }
            else
            {
                gestationFactor = GestationRateFactorsByXenotype.TryGetValue(xenotypeDef, 1f);
            }

            return oldGestationPeriod / gestationFactor;
        }

        /// <summary>
        /// Prevents eggs spawning on egg-laying pawn kinds if they are not at a valid age.
        /// 
        /// I've tried doing this automatically with life stages and developmental stages,
        /// but all result in eggs appearing at 13 years old which is NOT what I want.
        /// </summary>
        public static bool ShouldLayEggs(Pawn pawn)
        {
            return pawn.ageTracker.AgeBiologicalYears >= MinAgeForEggsToAppear;
        }

        /// <summary>
        /// Ensures the child pawn has the entire xenotype and nothing else.
        /// 
        /// Also does age calculations.
        /// </summary>
        public static void ApplyXenotypeToChild(Pawn pawn, XenotypeDef xenotypeDef)
        {
            foreach (Gene gene in pawn.genes.GenesListForReading)
            {
                pawn.genes.RemoveGene(gene);
            }

            pawn.genes.SetXenotype(xenotypeDef);

            if (SpawnAgesByXenotype.TryGetValue(xenotypeDef, out int age))
            {
                float ageRate = pawn.ageTracker.BiologicalTicksPerTick;
                if (ageRate == 0f) ageRate = 1f;

                if (FantasyRaceSettings.DevMode)
                {
                    Log.Message($"[Fantasy Races] Increasing age of hatched pawn {pawn} ({xenotypeDef}) to {age} (ageFactor = {pawn.ageTracker.BiologicalTicksPerTick:2F})");
                }

                pawn.ageTracker.AgeTickMothballed(Mathf.RoundToInt(age * GenDate.TicksPerYear / ageRate));
            }
        }

        public static HediffDef GetEggOfXenotype(XenotypeDef xenotypeDef)
        {
            return EggsByXenotype.TryGetValue(xenotypeDef);
        }
    }
}
