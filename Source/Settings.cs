using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace What_Is_My_Purpose
{
	class Settings : ModSettings
	{
		public int multiGizmoLimit = 6;

		public static Settings Get()
		{
			return LoadedModManager.GetMod<What_Is_My_Purpose.Mod>().GetSettings<Settings>();
		}

		public bool ShowGizmos()
		{
			int count = Find.Selector.SelectedObjects.Count;
			return count == 1 || count <= multiGizmoLimit;
		}

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);
			
			options.Label(String.Format("Show Target Gizmo with up to this many selected: {0}", multiGizmoLimit));
			multiGizmoLimit = (int)options.Slider(multiGizmoLimit, 1, 100);

			options.Label("(The order of gizmos from other mods can be erratic ; blame the game for not providing gizmo ordering)");

			options.Gap();

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref multiGizmoLimit, "multiGizmoLimit", 6);
		}
	}
}