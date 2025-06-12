using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;

namespace DevToolBox
{
    [HarmonyPatch(typeof(RecipeDefGenerator), "SetIngredients")]
    public static class Patch_RecipeDefGenerator_SetIngredients
    {
        [HarmonyPrefix]
        public static void Prefix(RecipeDef r, ThingDef def, int adjustedCount)
        {
            if(def.CostList != null)
            {
                if (def.CostList.Any(cl => cl.thingDef == null))
                {
                    Log.Error($"Caught a null value in the costList of def: {def.defName} ({def.LabelCap})");
                }
            }
        }
    }
}
