using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using Harmony;
using UnityEngine;
using RimWorld;

namespace What_Is_My_Purpose
{

	[StaticConstructorOnStartup]
	public static class TexPurposeTarget
	{
		public static readonly Texture2D PosTargetIcon = ContentFinder<Texture2D>.Get("UI/Designators/Claim", true);
	}

	public class PurposeInfo
	{
		public Vector3? pos;
		public Color color;
		public Texture icon;
		public float scale;
		public Vector2 proportions;
		public Rect texCoords;

		public PurposeInfo()
		{
			pos = null;
			color = Color.white;
			icon = TexPurposeTarget.PosTargetIcon;
			scale = 1.0f;
			proportions = Vector2.one;
			texCoords = new Rect(0f, 0f, 1f, 1f);
		}

		public bool IsUsed()
		{
			return pos != null;
		}
		
		public static void AddTargetToGizmo(Command_CenterOnTarget gizmo, LocalTargetInfo targetInfo, TargetIndex ind = TargetIndex.A)
		{
			PurposeInfo purposeInfo = Make(targetInfo);
			if (targetInfo.HasThing && purposeInfo.pos == null)
				purposeInfo.pos = gizmo.selectedInfo.pos;
			if (purposeInfo.IsUsed())
			{
				gizmo.SetTarget(ind, purposeInfo);
			}
		}

		public static PurposeInfo Make(LocalTargetInfo targetInfo)
		{
			PurposeInfo purposeInfo = new PurposeInfo();
			if(targetInfo.IsValid)
			{
				purposeInfo.pos = targetInfo.CenterVector3;
			}
			if (targetInfo.Thing is Thing target)
			{
				target = MinifyUtility.GetInnerIfMinified(target);
				BuildableDef def = target.def;

				purposeInfo.pos = target.DrawPos;
				purposeInfo.color = target.DrawColor;

				if (target is Pawn || target is Corpse)
				{
					Pawn pawn = target as Pawn;
					if (pawn == null)
					{
						pawn = ((Corpse)target).InnerPawn;
					}
					if (!pawn.RaceProps.Humanlike)
					{
						//This seems unnecessary
						//if (!pawn.Drawer.renderer.graphics.AllResolved)
						//{
						//	pawn.Drawer.renderer.graphics.ResolveAllGraphics();
						//}
						Material matSingle = pawn.Drawer.renderer.graphics.nakedGraphic.MatSingle;
						purposeInfo.icon = matSingle.mainTexture;
						purposeInfo.color = matSingle.color;
					}
					else
					{
						purposeInfo.icon = PortraitsCache.Get(pawn, Vector2.one * Gizmo.Height, default(Vector3), 1.5f);
					}
					purposeInfo.proportions = new Vector2(purposeInfo.icon.width, purposeInfo.icon.height);
				}
				else
				{
					if (target is IConstructible buildThing)
					{
						def = target.def.entityDefToBuild;
						if (buildThing.UIStuff() != null)
							purposeInfo.color = buildThing.UIStuff().stuffProps.color;
						else
							purposeInfo.color = def.uiIconColor;
					}

					purposeInfo.icon = def.uiIcon;

					if (def is ThingDef td)
					{
						purposeInfo.proportions = td.graphicData.drawSize;
						purposeInfo.scale = GenUI.IconDrawScale(td);
					}

					if (def is TerrainDef)
						//private static readonly Vector2 TerrainTextureCroppedSize = new Vector2(64f, 64f);
						purposeInfo.texCoords = new Rect(0f, 0f, 64f / purposeInfo.icon.width, 64f / purposeInfo.icon.height);
					else if (def.uiIconPath.NullOrEmpty())
					{
						Material iconMat = def.graphic.MatSingle;
						purposeInfo.texCoords = new Rect(iconMat.mainTextureOffset, iconMat.mainTextureScale);
					}
				}
			}
			return purposeInfo;
		}
	}
	//Also known as Command_CenterOnReserver
	public class Command_CenterOnTarget : Command
	{
		public Vector3? clickedPos;

		public Pawn pawn;

		public PurposeInfo targetA = new PurposeInfo();
		public PurposeInfo targetB = new PurposeInfo();
		public PurposeInfo targetC = new PurposeInfo();
		public PurposeInfo selectedInfo = new PurposeInfo();

		public Command_CenterOnTarget(Pawn p) : this()
		{
			pawn = p;
		}
		public Command_CenterOnTarget() : base()
		{
			order = -50f;
		}

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
		{
			get
			{
				foreach (FloatMenuOption o in base.RightClickFloatMenuOptions)
				{
					yield return o;
				}
				yield return new FloatMenuOption("Select all reserved things", SelectAllReserved);
			}
		}

		public void SelectAllReserved()
		{
			Find.Selector.ClearSelection();
			ReservationManager res = pawn.Map.reservationManager;
			foreach(Thing thing in res.AllReservedThings().Where(t => res.ReservedBy(t, pawn)))
				Find.Selector.Select(thing);
		}

		public override void ProcessInput(Event ev)
		{
			base.ProcessInput(ev);
			if (clickedPos != null)
				Current.CameraDriver.JumpToCurrentMapLoc(clickedPos.Value);
		}

		public void SetTarget(TargetIndex ind, PurposeInfo info)
		{
			switch (ind)
			{
				case TargetIndex.A: targetA = info; break;
				case TargetIndex.B: targetB = info; break;
				case TargetIndex.C: targetC = info; break;
			}
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
		{
			bool clicked = false;
			clickedPos = null;
			
			Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), Height);
			GUI.color = Color.white;
			GUI.DrawTexture(rect, Command.BGTex);

			if (Find.Selector.SelectedObjects.Count == 1 && !targetB.IsUsed() && !targetC.IsUsed())
			{
				clicked = DoButton(targetA.IsUsed() ? targetA : selectedInfo, rect, 0.85f);
			}
			else
			{
				topLeft.x += 1;
				topLeft.y += 1;
				float halfW = GetWidth(maxWidth) / 2 - 2;

				Rect rectPortrait = new Rect(topLeft.x, topLeft.y, halfW, halfW);
				Rect rectA = new Rect(topLeft.x + halfW, topLeft.y, halfW, halfW);
				Rect rectB = new Rect(topLeft.x, topLeft.y + halfW, halfW, halfW);
				Rect rectC = new Rect(topLeft.x + halfW, topLeft.y + halfW, halfW, halfW);

				GUI.color = Color.white;
				GUI.DrawTexture(rect, Command.BGTex);

				clicked |= DoButton(selectedInfo, rectPortrait);
				clicked |= DoButton(targetA, rectA);
				clicked |= DoButton(targetB, rectB);
				clicked |= DoButton(targetC, rectC);
			}
			
			//Label
			float num = Text.CalcHeight(LabelCap, rect.width);
			Rect rectLabel = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
			GUI.color = Color.white;
			GUI.DrawTexture(rectLabel, TexUI.GrayTextBG);
			Text.Anchor = TextAnchor.UpperCenter;
			Widgets.Label(rectLabel, LabelCap);
			Text.Anchor = TextAnchor.UpperLeft;

			if (Widgets.ButtonInvisible(rect, false) && Event.current.button == 1)
				return new GizmoResult(GizmoState.OpenedFloatMenu, Event.current);
			if (clicked)
				return new GizmoResult(GizmoState.Interacted, Event.current);
			else if (Mouse.IsOver(rect))
				return new GizmoResult(GizmoState.Mouseover);
			else
				return new GizmoResult(GizmoState.Clear);
		}
		
		private bool DoButton(PurposeInfo purposeInfo, Rect rect, float scale = 1.0f)
		{
			if (!purposeInfo.IsUsed()) return false;

			Texture icon = purposeInfo.icon;
			if (icon == null) return false;
			
			if (Mouse.IsOver(rect))
				GUI.color = GenUI.MouseoverColor;
			else
				GUI.color = purposeInfo.color;
			Verse.Sound.MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Command);
			//GUI.DrawTexture(rect, Command.BGTex);
			Widgets.DrawTextureFitted(rect, icon, scale * purposeInfo.scale, purposeInfo.proportions, purposeInfo.texCoords);

			if (Widgets.ButtonInvisible(rect, false) || Mouse.IsOver(rect) && Input.GetMouseButton(0))
			{
				clickedPos = purposeInfo.pos;
				return true;
			}
			
			return false;
		}

		public override bool GroupsWith(Gizmo other) => false;
	}


	[HarmonyPatch(typeof(ThingWithComps), "GetGizmos")]
	static class GetGizmosAdder
	{
		//public override IEnumerable<Gizmo> GetGizmos()
		public static void Postfix(ref IEnumerable<Gizmo> __result, ThingWithComps __instance)
		{
			//vanilla doesn't call base :(
			GetGizmosAdder_Thing.Postfix(ref __result, __instance);
		}
	}


	[HarmonyPatch(typeof(Thing), "GetGizmos")]
	static class GetGizmosAdder_Thing
	{
		//public override IEnumerable<Gizmo> GetGizmos()
		public static void Postfix(ref IEnumerable<Gizmo> __result, Thing __instance)
		{
			if (!Settings.Get().ShowGizmos()) return;

			List<Gizmo> result = __result.ToList();

			if (Settings.Get().purposeGizmos && __instance is Pawn pawn &&
				PurposeGizmoAdder.PurposeGizmoFor(pawn) is Gizmo gizmo)
				result.Add(gizmo);
			
			if (Settings.Get().reservedGizmos && 
				PurposeGizmoAdder.ReservedGizmoFor(__instance) is Gizmo gizmo2)
				result.Add(gizmo2);

			__result = result;
		}
	}

	public static class PurposeGizmoAdder
	{
		public static bool ShouldDrawGizmoFor(Pawn gizmoPawn)
		{
			return gizmoPawn.IsColonistPlayerControlled || 
				((gizmoPawn.Faction?.IsPlayer ?? false) &&
				 (gizmoPawn.CurJob?.def == JobDefOf.HaulToCell ||
				 gizmoPawn.CurJob?.def == JobDefOf.HaulToContainer));
		}

		public static Gizmo PurposeGizmoFor(Pawn gizmoPawn)
		{
			if (!ShouldDrawGizmoFor(gizmoPawn))
				return null;
			
			Command_CenterOnTarget gizmo = new Command_CenterOnTarget(gizmoPawn);

			gizmo.selectedInfo = PurposeInfo.Make(gizmoPawn);
			if (gizmoPawn.CurJob != null)
			{
				gizmo.defaultLabel = gizmoPawn.jobs.curDriver.GetReport().Split(' ').FirstOrDefault();
				gizmo.defaultLabel.TrimEnd('.');
				PurposeInfo.AddTargetToGizmo(gizmo, gizmoPawn.CurJob.GetTarget(TargetIndex.A), TargetIndex.A);
				PurposeInfo.AddTargetToGizmo(gizmo, gizmoPawn.CurJob.GetTarget(TargetIndex.B), TargetIndex.B);
				PurposeInfo.AddTargetToGizmo(gizmo, gizmoPawn.CurJob.GetTarget(TargetIndex.C), TargetIndex.C);
			}
			else
				gizmo.defaultLabel = "ActivityIconIdle".Translate();

			return gizmo;
		}

		public static Gizmo ReservedGizmoFor(Thing thing)
		{
			Pawn sampleColonist = thing.Map?.mapPawns?.FreeColonists?.FirstOrDefault();
			if (sampleColonist == null) return null;

			Pawn reserver = thing.Map?.reservationManager?.FirstRespectedReserver(thing, sampleColonist);
			if (reserver == null) return null;

			Command_CenterOnTarget gizmo = new Command_CenterOnTarget()
			{
				selectedInfo = PurposeInfo.Make(thing),
				defaultLabel = "TD.Reserved".Translate()
				//defaultLabel = reserver.NameStringShort
			};
			PurposeInfo.AddTargetToGizmo(gizmo, reserver);
			
			return gizmo;   //first is leftmost
		}
	}
}