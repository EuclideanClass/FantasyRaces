using UnityEngine;
using Verse;

namespace EFR
{
    public class FantasyRaceSettings : ModSettings
    {
        public static bool DevMode = false;

        public static bool FastGestation = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref DevMode, "devMode");
            Scribe_Values.Look(ref FastGestation, "fastGestation");
        }

        public void Draw(Rect inRect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect);

            listing_Standard.CheckboxLabeled("Development Mode", ref DevMode, "Enables verbose logging.");
            
            if (DevMode)
            {
                listing_Standard.CheckboxLabeled("Fast Egg Gestations", ref FastGestation, "Sets the gestation rate factor on all egg-laying fantasy race xenotypes to be 100.");
            }
            else
            {
                if (FastGestation) FastGestation = false;
            }

            listing_Standard.End();
        }
    }
}
