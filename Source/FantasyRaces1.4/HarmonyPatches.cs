using Verse;
using HarmonyLib;
using System;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using rjw;
using System.Reflection;
using System.Linq;

namespace EFR
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);

        static HarmonyPatches()
        {
            var harmony = new Harmony("Euclidean.FantasyRaces");

            harmony.Patch(AccessTools.PropertyGetter(typeof(Hediff_InsectEgg), nameof(Hediff_InsectEgg.LabelBase)),
                postfix: new HarmonyMethod(patchType, nameof(EggLabelBase_Postfix)));

            harmony.Patch(AccessTools.PropertyGetter(typeof(Hediff_InsectEgg), nameof(Hediff_InsectEgg.LabelInBrackets)),
                postfix: new HarmonyMethod(patchType, nameof(EggLabelInBrackets_Postfix)));

            harmony.Patch(AccessTools.PropertyGetter(typeof(HediffWithComps), nameof(HediffWithComps.TipStringExtra)),
                postfix: new HarmonyMethod(patchType, nameof(HediffWithCompsTipStringExtra_Postfix)));

            harmony.Patch(AccessTools.Method(typeof(Hediff_InsectEgg), nameof(Hediff_InsectEgg.ChangeEgg)),
                postfix: new HarmonyMethod(patchType, nameof(ChangeEgg_Postfix)));

            harmony.Patch(AccessTools.Method(typeof(Genital_Helper), nameof(Genital_Helper.min_EggsProduced)),
                postfix: new HarmonyMethod(patchType, nameof(EggsProduced_Postfix)));

            harmony.Patch(AccessTools.Method(typeof(Genital_Helper), nameof(Genital_Helper.max_EggsProduced)),
                postfix: new HarmonyMethod(patchType, nameof(EggsProduced_Postfix)));

            harmony.Patch(AccessTools.Method(typeof(Hediff_InsectEgg), "BirthUnfertilizedEggs"),
                prefix: new HarmonyMethod(patchType, nameof(BirthUnfertilizedEggs_Prefix)));

            harmony.Patch(AccessTools.Method(typeof(Hediff_InsectEgg), "ProcessHumanLikeInsectEgg"),
                postfix: new HarmonyMethod(patchType, nameof(ProcessHumanLikeInsectEgg_Postfix)));

            harmony.Patch(AccessTools.Method(typeof(Hediff_InsectEgg), nameof(Hediff_InsectEgg.ExposeData)),
                postfix: new HarmonyMethod(patchType, nameof(InsectEgg_ExposeData_Postfix)));

            harmony.Patch(AccessTools.Method(typeof(Genital_Helper), nameof(Genital_Helper.has_ovipositorF)),
                postfix: new HarmonyMethod(patchType, nameof(HasOvipositorF_Postfix)));

            harmony.Patch(AccessTools.Method(typeof(PregnancyHelper), nameof(PregnancyHelper.DoEgg)),
                postfix: new HarmonyMethod(patchType, nameof(DoEgg_Postfix)));

            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "TryGenerateNewPawnInternal"),
                prefix: new HarmonyMethod(patchType, nameof(TryGenerateNewPawnInternal_Prefix)));

            harmony.Patch(AccessTools.Method(typeof(SexPartAdder), nameof(SexPartAdder.add_genitals)),
                prefix: new HarmonyMethod(patchType, nameof(AddGenitals_Prefix)));

            harmony.Patch(AccessTools.Method(typeof(SexPartAdder), nameof(SexPartAdder.add_anus)),
                prefix: new HarmonyMethod(patchType, nameof(AddAnus_Prefix)));

            harmony.Patch(AccessTools.Method(typeof(Hediff_PartBaseNatural), "Tick"),
                transpiler: new HarmonyMethod(patchType, nameof(NaturalPartTick_Patch)));

            harmony.Patch(AccessTools.Method(typeof(PawnExtensions), nameof(PawnExtensions.Has), new[] { typeof(Pawn), typeof(RaceTag) }),
                postfix: new HarmonyMethod(patchType, nameof(PawnExtensionsHas_Postfix)));

            harmony.Patch(AccessTools.Constructor(typeof(PawnData), new[] { typeof(Pawn) }),
                postfix: new HarmonyMethod(patchType, nameof(PawnData_Postfix)));

        }

        /// <summary>
        /// Ensures pawn generation requests that produce pawns of a fantasy race xenotype also enforce
        /// fantasy race characteristics like age and traits.
        /// </summary>
        private static void TryGenerateNewPawnInternal_Prefix(ref PawnGenerationRequest request)
        {
            if (!FantasyRaces.IsFantasyRace_Generation(ref request)) return;
            XenotypeDef xenotypeDef = request.ForcedXenotype;

            // fantasy race xenotypes shouldn't appear as being > 18 years old since they have the 'Ageless' gene
            if (request.FixedBiologicalAge == null)
            {
                if (FantasyRaceSettings.DevMode)
                {
                    Log.Message($"[Fantasy Races] Forcing age of pawn request ({xenotypeDef}) to be 18");
                }

                request.FixedBiologicalAge = 18;
            }

            // all fantasy race xenotypes have the 'Naked Speed' gene, so they should also get the 'Nudist' trait to complement this
            {
                if (FantasyRaceSettings.DevMode)
                {
                    Log.Message($"[Fantasy Races] Adding {TraitDefOf.Nudist} trait to pawn request ({xenotypeDef})");
                }

                request.ForcedTraits = request.ForcedTraits.AddItem(TraitDefOf.Nudist);
            }

            // succubi also get the nymphomaniac trait
            if (xenotypeDef == XenotypeDefOf.EFR_Succubus)
            {
                if (FantasyRaceSettings.DevMode)
                {
                    Log.Message($"[Fantasy Races] Adding {xxx.nymphomaniac} trait to pawn request ({xenotypeDef})");
                }

                request.ForcedTraits = request.ForcedTraits.AddItem(xxx.nymphomaniac);
            }
        }

        /// <summary>
        /// Fantasy race xenotype genital setter.
        /// </summary>
        private static bool AddGenitals_Prefix(Pawn pawn, Gender gender)
        {
            if (!FantasyRaces.IsFantasyRace_Pawn(pawn, out XenotypeDef xenotypeDef))
            {
                return true;
            }

            if (!RaceSupport.HasCustom_Genitals(xenotypeDef, gender, out List<HediffDef> customGenitals))
            {
                return true;
            }

            BodyPartRecord bpr = Genital_Helper.get_genitalsBPR(pawn);
            if (bpr == null || pawn.health.hediffSet.PartIsMissing(bpr)) return true;

            foreach (HediffDef hediffDef in customGenitals)
            {
                if (FantasyRaceSettings.DevMode)
                {
                    Log.Message($"[Fantasy Races] Adding {hediffDef} hediff to pawn {pawn} ({xenotypeDef})");
                }

                Hediff hediff = SexPartAdder.MakePart(hediffDef, pawn, bpr);
                pawn.health.AddHediff(hediff, bpr);
            }

            return false;
        }

        /// <summary>
        /// Fantasy race xenotype anus setter.
        /// </summary>
        private static bool AddAnus_Prefix(Pawn pawn)
        {

            if (!FantasyRaces.IsFantasyRace_Pawn(pawn, out XenotypeDef xenotypeDef))
            {
                return true;
            }

            if (!RaceSupport.HasCustom_Anus(xenotypeDef, out HediffDef customAnus))
            {
                return true;
            }

            BodyPartRecord bpr = Genital_Helper.get_anusBPR(pawn);
            if (bpr == null || pawn.health.hediffSet.PartIsMissing(bpr)) return true;

            if (FantasyRaceSettings.DevMode)
            {
                Log.Message($"[Fantasy Races] Adding {customAnus} hediff to pawn {pawn} ({xenotypeDef})");
            }

            Hediff hediff = SexPartAdder.MakePart(customAnus, pawn, bpr);
            pawn.health.AddHediff(hediff, bpr);


            return false;
        }

        /// <summary>
        /// Extra label information for eggs of egg-laying fantasy race xenotypes.
        /// </summary>
        private static void EggLabelBase_Postfix(Pawn ___implanter, ref string __result)
        {
            if (EggLayers.IsEggLayingPawn(___implanter, out XenotypeDef xenotypeDef))
            {
                __result = EggLayers.CustomEgg_Label(xenotypeDef);
            }
        }

        /// <summary>
        /// Extra label information for eggs of egg-laying fantasy race xenotypes.
        /// </summary>
        private static void EggLabelInBrackets_Postfix(Hediff_InsectEgg __instance, ref string __result)
        {
            if (EggLayers.IsEggLayingPawn(__instance.implanter, out _))
            {
                __result = EggLayers.CustomEgg_LabelInBrackets(__instance.fertilized);
            }
        }

        /// <summary>
        /// Extra tooltip information for eggs of egg-laying fantasy race xenotypes.
        /// </summary>
        private static void HediffWithCompsTipStringExtra_Postfix(Hediff_InsectEgg __instance, ref string __result)
        {
            // since this patch is applied on HediffWithComps (as Hediff_InsectEgg doesn't override its TipStringExtra method),
            // we need to make sure this hediff class is the right one, we don't want to show this information on all HediffWithComps after all
            if (__instance.GetType() != typeof(Hediff_InsectEgg))
            {
                return;
            }

            if (EggLayers.IsEggLayingPawn(__instance.implanter, out _))
            {
                __result = EggLayers.CustomEgg_TipStringExtra(__result, __instance.GestationProgress, __instance.implanter, __instance.father);
            }
        }

        /// <summary>
        /// Does additional gestation rate calculations for egg-laying custom races.
        /// This also disables birthing alerts ("{0} is having contractions! Eggs are on the way!") and prevents the pawn from gaining
        /// the "submitting" hediff (useful in harpies case to stop bone fractures).
        /// </summary>
        private static void ChangeEgg_Postfix(Hediff_InsectEgg __instance, Pawn Pawn, ref int ___contractions)
        {
            if (EggLayers.IsEggLayingPawn(Pawn, out XenotypeDef xenotypeDef))
            {
                float oldGestationPeriod = __instance.p_end_tick - __instance.p_start_tick;
                float newGestationPeriod = EggLayers.CalculateGestationPeriod(oldGestationPeriod, xenotypeDef);

                if (FantasyRaceSettings.DevMode)
                {
                    Log.Message($"[Fantasy Races] Changing gestation period of {Pawn} ({xenotypeDef}) from {oldGestationPeriod} to {newGestationPeriod} ticks");
                }

                __instance.p_end_tick = __instance.p_start_tick + newGestationPeriod;

                // silence contractions messages and hediff
                ___contractions = 1;
            }

        }

        /// <summary>
        /// Prevents eggs spawning on egg-laying fantasy race xenotypes if they are not at a valid age.
        /// </summary>
        private static void EggsProduced_Postfix(Pawn pawn, ref int __result)
        {
            if (EggLayers.IsEggLayingPawn(pawn, out XenotypeDef xenotypeDef) && __result > 0 && !EggLayers.ShouldLayEggs(pawn))
            {
                if (FantasyRaceSettings.DevMode)
                {
                    Log.Message($"[Fantasy Races] Forcing number of eggs produced by pawn {pawn} ({xenotypeDef}) to be 0 (age = {pawn.ageTracker.AgeBiologicalYears} years) ");
                }
                    
                __result = 0;
            }
        }

        /// <summary>
        /// Prevents egg-laying fantasy race xenotypes spawning unfertilized eggs once gestation reaches 100%,
        /// this also silences the "EggDead" related messages.
        /// </summary>
        private static bool BirthUnfertilizedEggs_Prefix(Pawn mother)
        {
            return !EggLayers.IsEggLayingPawn(mother, out _);
        }

        /// <summary>
        /// Gene and trait checks for hatched babies of fantasy race xenotypes.
        /// </summary>
        private static void ProcessHumanLikeInsectEgg_Postfix(Pawn mother, ref Pawn __result)
        {
            if (EggLayers.IsEggLayingPawn(__result, out XenotypeDef xenotypeDef) || EggLayers.IsEggLayingPawn(mother, out xenotypeDef))
            {
                if (FantasyRaceSettings.DevMode)
                {
                    Log.Message($"[Fantasy Races] Applying xenotype {xenotypeDef} to hatched pawn {__result}");
                }

                EggLayers.ApplyXenotypeToChild(__result, xenotypeDef);

                // all fantasy race xenotypes have the 'Naked Speed' gene, so they should also get the 'Nudist' trait to complement this
                // this is done in the pawn generation request prefix, but RJW seems to be overwriting or ignoring it somewhere
                {
                    if (FantasyRaceSettings.DevMode)
                    {
                        Log.Message($"[Fantasy Races] Adding {TraitDefOf.Nudist} trait to hatched pawn {__result} ({xenotypeDef})");
                    }

                    __result.story.traits.GainTrait(new Trait(TraitDefOf.Nudist));
                }
            }
        }

        private static void InsectEgg_ExposeData_Postfix(ref int ___contractions)
        {
            Scribe_Values.Look(ref ___contractions, "contractions");
        }

        /// <summary>
        /// Arachne can egg too!
        /// </summary>
        private static void HasOvipositorF_Postfix(ref bool __result, Pawn pawn)
        {
            // no logging here since this method is called a lot

            if (__result) return; // already eggin'
            if (!EggLayers.IsEggLayingPawn(pawn, out XenotypeDef xenotypeDef)) return; // not egg-laying pawn kind
            if (xenotypeDef != XenotypeDefOf.EFR_Arachne) return; // not arachne

            if (pawn.GetGenitalsList().Any(hediff => hediff.def == HediffDefOf.EFR_ArachneOvipositor))
            {
                __result = true;
            }
        }

        /// <summary>
        /// Runs after egging to fertilize arachne eggs.
        /// </summary>
        private static void DoEgg_Postfix(SexProps props)
        {
            if (!EggLayers.IsEggLayingPawn(props.pawn, out XenotypeDef xenotypeDef)) return;
            if (xenotypeDef != XenotypeDefOf.EFR_Arachne) return;

            List<Hediff_InsectEgg> eggs = new List<Hediff_InsectEgg>(); // unfertilized implanted arachne eggs
            props.partner.health.hediffSet.GetHediffs(ref eggs, egg => egg.def == HediffDefOf.EFR_ArachneEgg && !egg.fertilized);
            if (eggs.Count == 0) return;

            if (FantasyRaceSettings.DevMode)
            {
                Log.Message($"[Fantasy Races] Fertilizing {eggs.Count} arachne egg{(eggs.Count != 1 ? "s" : "")} implanted by pawn {props.pawn} ({xenotypeDef}) into pawn {props.partner}");
            }

            foreach (Hediff_InsectEgg egg in eggs)
            {
                egg.Fertilize(props.partner);
            }
        }

        /// <summary>
        /// Since fantasy race xenotypes aren't tied to pawn kinds, we need to patch callers of the TryGetEgg() method to
        /// call our own custom method to check for fantasy race xenotypes.
        /// </summary>
        private static IEnumerable<CodeInstruction> NaturalPartTick_Patch(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo originalMethod = AccessTools.Method(typeof(Hediff_PartBaseNatural), "TryGetEgg");
            MethodInfo newMethod = AccessTools.Method(patchType, nameof(TryGetEgg_Helper));
            FieldInfo pawnField = AccessTools.Field(typeof(Hediff_PartBaseNatural), nameof(Hediff_PartBaseNatural.pawn));

            List<CodeInstruction> instructionList = instructions.ToList();

            bool complete = false;

            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;

                if (instruction.Calls(originalMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // load hediff class onto evaluation stack
                    yield return new CodeInstruction(OpCodes.Ldfld, pawnField); // load pawn onto evaluation stack
                    yield return new CodeInstruction(OpCodes.Call, newMethod); // call helper method with pawn + other stuff that was already on the stack
                    complete = true;
                }
            }

            if (complete)
            {
                if (FantasyRaceSettings.DevMode)
                {
                    Log.Message($"[Fantasy Races] Successfully patched {nameof(Hediff_PartBaseNatural)}.{nameof(Hediff_PartBaseNatural.Tick)}");
                }
            } else
            {
                Log.Error($"[Fantasy Races] Failed to patch {nameof(Hediff_PartBaseNatural)}.{nameof(Hediff_PartBaseNatural.Tick)}");
            }
        }

        /// <summary>
        /// Helper method to replace the TryGetEgg result with our own hediff def, if present.
        /// </summary>
        private static HediffDef TryGetEgg_Helper(HediffDef egg, Pawn pawn)
        {
            if (EggLayers.IsEggLayingPawn(pawn, out XenotypeDef xenotypeDef))
            {
                HediffDef newEgg = EggLayers.GetEggOfXenotype(xenotypeDef);

                if (FantasyRaceSettings.DevMode)
                {
                    Log.Message($"[Fantasy Races] Setting egg of pawn {pawn} ({xenotypeDef}) to be {newEgg} (was {(egg == null ? "null" : egg.ToString())})");
                }

                return newEgg;
            }

            return egg;
        }

        /// <summary>
        /// Modifications to the race tag checker to return race tags of fantasy race xenotypes if applicable.
        /// </summary>
        private static void PawnExtensionsHas_Postfix(ref bool __result, Pawn pawn, RaceTag tag)
        {
            if (FantasyRaces.IsFantasyRace_Pawn(pawn, out XenotypeDef xenotypeDef) && __result == false &&
                RaceSupport.HasCustom_RaceTags(xenotypeDef, out HashSet<RaceTag> raceTags) && raceTags.Contains(tag))
            {
                if (FantasyRaceSettings.DevMode)
                {
                    Log.Message($"[Fantasy Races] Marking pawn {pawn} ({xenotypeDef}) as having race tag {tag.Key}");
                }

                __result = true;
            }
        }

        /// <summary>
        /// Modifications to the pawn data constructor to set race sex drives of fantasy race xenotypes if applicable.
        /// 
        /// Although this does affect the decay rate of the sex need (see rjw.Need_Sex.NeedInterval()), it unfortunately doesn't
        /// show on the pawn stats.
        /// </summary>
        private static void PawnData_Postfix(PawnData __instance, Pawn pawn)
        {
            if (FantasyRaces.IsFantasyRace_Pawn(pawn, out XenotypeDef xenotypeDef) &&
                RaceSupport.HasCustom_RaceSexDrive(xenotypeDef, out float raceSexDrive))
            {
                if (FantasyRaceSettings.DevMode)
                {
                    Log.Message($"[Fantasy Races] Marking pawn {pawn} ({xenotypeDef}) as having race sex drive {raceSexDrive}");
                }

                __instance.raceSexDrive = raceSexDrive;
            }
        }
    }

}
