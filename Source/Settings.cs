﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace What_Is_My_Purpose
{
	public class Settings : ModSettings
	{
		public int multiGizmoLimit = 6;
		public bool purposeGizmos = true;
		public bool reservedGizmos = true;

		public bool ShowGizmos()
		{
			if (!(Find.UIRoot is UIRoot_Play)) return false;
			int count = Find.Selector.NumSelected;
			return count == 1 || count <= multiGizmoLimit;
		}

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);

			options.CheckboxLabeled("TD.SettingColonistGizmo".Translate(), ref purposeGizmos);
			options.CheckboxLabeled("TD.SettingTargetGizmo".Translate(), ref reservedGizmos);

			options.Label(String.Format("TD.SettingMultiGizmos".Translate(), multiGizmoLimit));
			multiGizmoLimit = (int)options.Slider(multiGizmoLimit, 1, 100);

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref multiGizmoLimit, "multiGizmoLimit", 6);
			Scribe_Values.Look(ref purposeGizmos, "purposeGizmos", true);
			Scribe_Values.Look(ref reservedGizmos, "reservedGizmos", true);
		}
	}
}