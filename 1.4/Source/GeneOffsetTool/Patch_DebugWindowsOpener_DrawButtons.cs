using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using TabulaRasa;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;

namespace GeneOffsetTool
{
	[HarmonyPatch(typeof(DebugWindowsOpener), "DrawButtons")]
	public static class Patch_DebugWindowsOpener_DrawButtons
	{
		public static bool patched;

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> DrawAdditionalButtons(IEnumerable<CodeInstruction> instructions)
		{
			patched = false;
			CodeInstruction[] instructionsArr = instructions.ToArray();
			FieldInfo widgetRowField = AccessTools.Field(typeof(DebugWindowsOpener), "widgetRow");
			CodeInstruction[] array = instructionsArr;
			foreach (CodeInstruction inst in array)
			{
				if (!patched && widgetRowField != null && inst.opcode == OpCodes.Bne_Un)
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0, (object)null);
					yield return new CodeInstruction(OpCodes.Ldfld, (object)widgetRowField);
					yield return new CodeInstruction(OpCodes.Call, (object)new Action<WidgetRow>(DrawDebugToolbarButton).Method);
					patched = true;
				}
				yield return inst;
			}
		}

		public static void DrawDebugToolbarButton(WidgetRow widgets)
		{
			if (widgets.ButtonIcon(TexTabulaRasa.DebugXenotypeEditor, "Open the Gene Editor. \n\nAllows editing of genes, mainly their offsets, for easier determining of values for visuals in-game without having to reload constantly."))
			{
				WindowStack windowStack = Find.WindowStack;
				if (windowStack.IsOpen<Dialog_GeneEditor>())
				{
					windowStack.TryRemove(typeof(Dialog_GeneEditor));
				}
				else
				{
					windowStack.Add(new Dialog_GeneEditor());
				}
			}
		}
	}
}
