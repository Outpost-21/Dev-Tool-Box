using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace DevToolBox
{
    [StaticConstructorOnStartup]
    public static class DevToolBoxPrefs
	{
		public static DevToolBoxSettings Setting => DevToolBoxMod.settings;

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
					DevToolBoxMod.settings.Write();
				}
			}
		}

		public static Vector2 BuildingEditorPosition
		{
			get
			{
				return Setting.buildingEditorPosition;
			}
			set
			{
				if (Setting.buildingEditorPosition != value)
				{
					Setting.buildingEditorPosition = value;
					DevToolBoxMod.settings.Write();
				}
			}
		}

		static DevToolBoxPrefs()
        {

        }
    }
}
