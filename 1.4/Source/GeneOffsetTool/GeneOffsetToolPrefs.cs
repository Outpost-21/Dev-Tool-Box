using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace GeneOffsetTool
{
    [StaticConstructorOnStartup]
    public static class GeneOffsetToolPrefs
	{
		public static GeneOffsetToolSettings Setting => GeneOffsetToolMod.settings;

		public static Vector2 GeneEditorPosition
		{
			get
			{
				return Setting.geneEditorPosition;
			}
			set
			{
				if (Setting.geneEditorPosition != value)
				{
					Setting.geneEditorPosition = value;
					Setting.Write();
				}
			}
		}

		static GeneOffsetToolPrefs()
        {

        }
    }
}
