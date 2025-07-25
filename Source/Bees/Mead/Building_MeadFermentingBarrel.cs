using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Bees;

[StaticConstructorOnStartup]
public class Building_MeadFermentingBarrel : Building
{
	private int honeyCount;

	private float progressInt;

	private Material barFilledCachedMat;

	public const int MaxCapacity = 75;

	private const int BaseFermentationDuration = 360000;

	public const float MinIdealTemperature = 7f;

	private static readonly Vector2 BarSize = new Vector2(0.55f, 0.1f);

	private static readonly Color BarZeroProgressColor = new Color(0.4f, 0.27f, 0.22f);

	private static readonly Color BarFermentedColor = new Color(0.9f, 0.85f, 0.2f);

	private static readonly Material BarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f));

	public float Progress
	{
		get
		{
			return progressInt;
		}
		set
		{
			if (value != progressInt)
			{
				progressInt = value;
				barFilledCachedMat = null;
			}
		}
	}

	private Material BarFilledMat
	{
		get
		{
			if (barFilledCachedMat == null)
			{
				barFilledCachedMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.Lerp(BarZeroProgressColor, BarFermentedColor, Progress));
			}
			return barFilledCachedMat;
		}
	}

	public int SpaceLeftForHoney
	{
		get
		{
			if (Fermented)
			{
				return 0;
			}
			return 25 - honeyCount;
		}
	}

	private bool Empty => honeyCount <= 0;

	public bool Fermented
	{
		get
		{
			if (!Empty)
			{
				return Progress >= 1f;
			}
			return false;
		}
	}

	private float CurrentTempProgressSpeedFactor
	{
		get
		{
			CompProperties_TemperatureRuinable compProperties = def.GetCompProperties<CompProperties_TemperatureRuinable>();
			float ambientTemperature = AmbientTemperature;
			if (ambientTemperature < compProperties.minSafeTemperature)
			{
				return 0.1f;
			}
			if (ambientTemperature < 7f)
			{
				return GenMath.LerpDouble(compProperties.minSafeTemperature, 7f, 0.1f, 1f, ambientTemperature);
			}
			return 1f;
		}
	}

	private float ProgressPerTickAtCurrentTemp => 2.77777781E-06f * CurrentTempProgressSpeedFactor;

	private int EstimatedTicksLeft => Mathf.Max(Mathf.RoundToInt((1f - Progress) / ProgressPerTickAtCurrentTemp), 0);

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref honeyCount, "honeyCount");
		Scribe_Values.Look(ref progressInt, "progress");
	}

	public override void TickRare()
	{
		base.TickRare();
		if (!Empty)
		{
			Progress = Mathf.Min(Progress + 250f * ProgressPerTickAtCurrentTemp, 1f);
		}
	}

	public void AddHoney(int count)
	{
		GetComp<CompTemperatureRuinable>().Reset();
		if (Fermented)
		{
			Log.Warning("Tried to add honey to a barrel full of mead. Colonists should take the mead first.");
			return;
		}
		int num = Mathf.Min(count, 25 - honeyCount);
		if (num > 0)
		{
			Progress = GenMath.WeightedAverage(0f, num, Progress, honeyCount);
			honeyCount += num;
		}
	}

	public override void ReceiveCompSignal(string signal)
	{
		if (signal == "RuinedByTemperature")
		{
			Reset();
		}
	}

	private void Reset()
	{
		honeyCount = 0;
		Progress = 0f;
	}

	public void AddHoney(Thing honey)
	{
		int num = Mathf.Min(honey.stackCount, 25 - honeyCount);
		if (num > 0)
		{
			AddHoney(num);
			honey.SplitOff(num).Destroy();
		}
	}

	public override string GetInspectString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(base.GetInspectString());
		if (stringBuilder.Length != 0)
		{
			stringBuilder.AppendLine();
		}
		CompTemperatureRuinable comp = GetComp<CompTemperatureRuinable>();
		if (!Empty && !comp.Ruined)
		{
			if (Fermented)
			{
				stringBuilder.AppendLine("ContainsMead".Translate(honeyCount, 25));
			}
			else
			{
				stringBuilder.AppendLine("ContainsHoney".Translate(honeyCount, 25));
			}
		}
		if (!Empty)
		{
			if (Fermented)
			{
				stringBuilder.AppendLine("Fermented".Translate());
			}
			else
			{
				stringBuilder.AppendLine("FermentationProgress".Translate(Progress.ToStringPercent(), EstimatedTicksLeft.ToStringTicksToPeriod()));
				if (CurrentTempProgressSpeedFactor != 1f)
				{
					stringBuilder.AppendLine("FermentationBarrelOutOfIdealTemperature".Translate(CurrentTempProgressSpeedFactor.ToStringPercent()));
				}
			}
		}
		stringBuilder.AppendLine("Temperature".Translate() + ": " + base.AmbientTemperature.ToStringTemperature("F0"));
		stringBuilder.AppendLine("IdealFermentingTemperature".Translate() + ": " + 7f.ToStringTemperature("F0") + " ~ " + comp.Props.maxSafeTemperature.ToStringTemperature("F0"));
		return stringBuilder.ToString().TrimEndNewlines();
	}

	public Thing TakeOutMead()
	{
		if (!Fermented)
		{
			Log.Warning("Tried to get mead but it's not yet fermented.");
			return null;
		}
		Thing thing = ThingMaker.MakeThing(InternalDefOf.Bees_Mead);
		thing.stackCount = honeyCount;
		Reset();
		return thing;
	}

	public override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		if (!Empty)
		{
			Vector3 drawPos = DrawPos;
			drawPos.y += 3f / 74f;
			drawPos.z += 0.25f;
			GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
			r.center = drawPos;
			r.size = BarSize;
			r.fillPercent = (float)honeyCount / 25f;
			r.filledMat = BarFilledMat;
			r.unfilledMat = BarUnfilledMat;
			r.margin = 0.1f;
			r.rotation = Rot4.North;
			GenDraw.DrawFillableBar(r);
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (!DebugSettings.ShowDevGizmos)
		{
			yield break;
		}
		if (!Empty)
		{
			Command_Action command_Action = new Command_Action();
			command_Action.defaultLabel = "DEV: Set progress to 1";
			command_Action.action = delegate
			{
				Progress = 1f;
			};
			yield return command_Action;
		}
		if (SpaceLeftForHoney > 0)
		{
			Command_Action command_Action2 = new Command_Action();
			command_Action2.defaultLabel = "DEV: Fill";
			command_Action2.action = delegate
			{
				Progress = 1f;
				honeyCount = 25;
			};
			yield return command_Action2;
		}
	}
}