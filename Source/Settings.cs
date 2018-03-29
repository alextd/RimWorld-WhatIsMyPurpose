using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace What_Is_My_Purpose
{
	class Settings : ModSettings
	{
		public bool multiGizmos = true;

		public static Settings Get()
		{
			return LoadedModManager.GetMod<What_Is_My_Purpose.Mod>().GetSettings<Settings>();
		}

		public bool ShowGizmos()
		{
			return multiGizmos || Find.Selector.SelectedObjects.Count == 1;
		}

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);
			
			options.CheckboxLabeled("Show job gizmo with multiple selected", ref multiGizmos);
			options.Gap();

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref multiGizmos, "multiGizmos", true);
		}
	}
}