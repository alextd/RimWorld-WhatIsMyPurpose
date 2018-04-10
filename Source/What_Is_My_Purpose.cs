using System.Reflection;
using Verse;
using UnityEngine;
using Harmony;

namespace What_Is_My_Purpose
{
	public class Mod : Verse.Mod
	{
		public Mod(ModContentPack content) : base(content)
		{
			// initialize settings
			GetSettings<Settings>();
#if DEBUG
			HarmonyInstance.DEBUG = true;
#endif
			HarmonyInstance harmony = HarmonyInstance.Create("Uuugggg.rimworld.What_Is_My_Purpose.main");
			harmony.PatchAll(Assembly.GetExecutingAssembly());

			harmony.Patch(AccessTools.Method(AccessTools.TypeByName("InspectGizmoGrid"), "DrawInspectGizmoGridFor"),
				null, null, new HarmonyMethod(typeof(DrawInspectGizmoGridFor_Patch), "Transpiler"));
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);
			GetSettings<Settings>().DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "TD.WhatIsMyPurpose".Translate();
		}
	}
}