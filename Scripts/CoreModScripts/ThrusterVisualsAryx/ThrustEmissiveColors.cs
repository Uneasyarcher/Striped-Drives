using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRageMath;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;

// TODO
// * Simplify even more for non-coders.
// * Add separate emissive mat handling with different colors by material name.

// REMARKS
// The code only runs when/while needed. Should be pretty performance friendly.
// However, some tests needed for SP VS MP because those two really don't like each others code. Thanks Keen for ensuring it's never boring... Or easy. :P
// Smack that coder. xD

// CHANGE THE NAMESPACE TO AVOID CONFLICTS!
// For instance, Your Space Engineers moniker, unless it's super common, then use some more unique ID or a combo of your player name and mod name.
// E.g. SomeSuperCoolPlayerNameYouProbablyHave_ModName.ThrusterEmissiveColors.
// FancyJoe_SuperCoolThrusters.ThrusterEmissiveColors
namespace Aryx_ErinMod.ThrusterEmissiveColors
{
    // CHANGE THE SUBTYPES HERE
    // ADD AS MANY AS YOU NEED
    // E.g.
    // [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "YourSubTypeHere", "YourOtherSubTypeHere", "AndAnotherThruster", "AndYetAnotherOne")]
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "ARYLYNX_SILVERSMITH_DRIVE", "TrumanDrive", "ARYLNX_SCIRCOCCO_Epstein_Drive", "ARYLNX_Mega_Epstein_Drive")]

    public class ThrusterEmissiveColorsLogic : MyGameLogicComponent
    {
        public IMyThrust block;



        // ========================
        // USER CHANGABLE VARIABLES
        // ========================

        // MATERIAL NAMES
        string EmissiveMaterialName = "Emissive";

        // CHANGE THESE TO DESIRED IN RGB 0-255
        Color OnColor = new Color(100, 200, 250); // On color, when thruster is ON.
        Color OffColor = new Color(0, 0, 0);    // Off color, when thruster is OFF.
        Color NonWorkingColor = new Color(0, 0, 0);    // When the block is not working, like no power.
        Color NonFunctionalColor = new Color(0, 0, 0);    // When the block is damaged, like from impact or weapon fire.

        // GLOW STRENGTH MULTIPLIERS
        float ThrusterOn_EmissiveMultiplier = 200f;
        float ThrusterOff_EmissiveMultiplier = 10f;
        float ThrusterNotWorking_EmissiveMultiplier = 0f;
        float ThrusterNonFunctional_EmissiveMultiplier = 0f;

        bool ChangeColorByThrustOutput = true;           // Change this for static or dynamic colors.
        float AntiFlickerThreshold = 0.01f;              // If the emissive flicker, increase the threshold.
        Color ColorAtMaxTrhust = new Color(50, 153, 255); // Color to reach at max thrust, otherwise 'OnColor' will be used when thruster is idle.
        float MaxTrhust_EmissiveMultiplierMin = 1;
        float MaxTrhust_EmissiveMultiplierMax = 10000f;

        // CHANGE THIS ONLY IF IT CLASHES WITH YOUR COLORS
        // Set these defaults to a color easy to spot if something isn't working correctly.
        Color ErrorColor = Color.Magenta;
        Color CurrentColor = Color.Magenta;



        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            block = (IMyThrust)Entity;

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }



        public override void UpdateOnceBeforeFrame()
        {
            if (block == null)
                return;

            // Hook to events.
            block.IsWorkingChanged += IsWorkingChanged;
            block.PropertiesChanged += PropertiesChanged;

            CheckAndSetEmissives();
        }



        public override void Close()
        {
            // Unhook from events.
            block.IsWorkingChanged -= IsWorkingChanged;
            block.PropertiesChanged -= PropertiesChanged;

            block = null;

            NeedsUpdate = MyEntityUpdateEnum.NONE;
        }



        private void IsWorkingChanged(IMyCubeBlock block)
        {
            CheckAndSetEmissives();
        }



        public void PropertiesChanged(IMyTerminalBlock block)
        {
            CheckAndSetEmissives();
        }



        // True entry.
        public void CheckAndSetEmissives()
        {
            if (block == null) // Null check all the things.
                return;

            if (ChangeColorByThrustOutput)
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
                return;
            }

            if (block.IsFunctional)
            {
                CurrentColor = ErrorColor; // Set to error color by default to easily spot an error.
                float mult = 1f;

                if (!block.IsWorking)
                {
                    NeedsUpdate = MyEntityUpdateEnum.NONE;
                    CurrentColor = NonWorkingColor;
                    mult = ThrusterNotWorking_EmissiveMultiplier;
                }
                else if (block.Enabled)
                {
                    CurrentColor = OnColor;
                    mult = ThrusterOn_EmissiveMultiplier;
                }
                else
                {
                    CurrentColor = OffColor;
                    mult = ThrusterOff_EmissiveMultiplier;
                }

                block.SetEmissiveParts(EmissiveMaterialName, CurrentColor, mult);
            }
            else
            {
                if (!ChangeColorByThrustOutput)
                {
                    NeedsUpdate = MyEntityUpdateEnum.NONE;
                    block.SetEmissiveParts(EmissiveMaterialName, NonFunctionalColor, ThrusterNonFunctional_EmissiveMultiplier);
                }
            }
        }

        float glow;
        float CurrentEmissiveMultiplier = 0f;
        // Handle dynamic color changes.
        public override void UpdateAfterSimulation()
        {
            if (block == null || block.MarkedForClose || block.Closed)
            {
                NeedsUpdate = MyEntityUpdateEnum.NONE;
                return;
            }

            if (block.IsFunctional && block.IsWorking && block.Enabled)
            {
                float thrustPercent = block.CurrentThrust / block.MaxThrust;

                if (glow <= 0 && thrustPercent <= AntiFlickerThreshold)
                    glow = 0f;
                else if (glow < thrustPercent)
                    glow += 0.05f;
                else if (glow > thrustPercent)
                    glow -= 0.05f;

                float mult = MaxTrhust_EmissiveMultiplierMin + (MaxTrhust_EmissiveMultiplierMax - MaxTrhust_EmissiveMultiplierMin);
                if (CurrentEmissiveMultiplier < mult)
                    CurrentEmissiveMultiplier += 0.005f;
                else
                    CurrentEmissiveMultiplier = MaxTrhust_EmissiveMultiplierMin + (MaxTrhust_EmissiveMultiplierMax - MaxTrhust_EmissiveMultiplierMin) * glow;

                CurrentColor = Color.Lerp(OnColor, ColorAtMaxTrhust, glow);

                block.SetEmissiveParts(EmissiveMaterialName, CurrentColor, CurrentEmissiveMultiplier);
            }
            else
            {
                if (glow > 0)
                    glow -= 0.005f;

                if (CurrentEmissiveMultiplier > 0)
                    CurrentEmissiveMultiplier -= 0.005f;

                Color color = ErrorColor;

                if (!block.IsWorking)
                {
                    color = Color.Lerp(CurrentColor, NonWorkingColor, glow);
                }
                else if (block.Enabled)
                {
                    color = Color.Lerp(CurrentColor, OnColor, glow);
                }
                else
                {
                    color = Color.Lerp(CurrentColor, OffColor, glow);
                }

                block.SetEmissiveParts(EmissiveMaterialName, color, CurrentEmissiveMultiplier);

                if (glow <= 0 && CurrentEmissiveMultiplier <= 0)
                {
                    glow = 0f;
                    CurrentEmissiveMultiplier = 0f;
                    block.SetEmissiveParts(EmissiveMaterialName, NonFunctionalColor, ThrusterNonFunctional_EmissiveMultiplier);
                    NeedsUpdate = MyEntityUpdateEnum.NONE;
                }
            }
        }
    }
}