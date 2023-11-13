using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace Bees;

public class WorkGiver_FillMeadFermentingBarrel : WorkGiver_Scanner
{
	private static string TemperatureTrans;

	private static string NoHoneyTrans;

	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(InternalDefOf.Bees_MeadFermentingBarrel);

	public override PathEndMode PathEndMode => PathEndMode.Touch;

	public static void ResetStaticData()
	{
		TemperatureTrans = "BadTemperature".Translate().ToLower();
		NoHoneyTrans = "NoHoney".Translate();
	}

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!(t is Building_MeadFermentingBarrel building_FermentingBarrel) || building_FermentingBarrel.Fermented || building_FermentingBarrel.SpaceLeftForHoney <= 0)
		{
			return false;
		}
		float ambientTemperature = building_FermentingBarrel.AmbientTemperature;
		CompProperties_TemperatureRuinable compProperties = building_FermentingBarrel.def.GetCompProperties<CompProperties_TemperatureRuinable>();
		if (ambientTemperature < compProperties.minSafeTemperature + 2f || ambientTemperature > compProperties.maxSafeTemperature - 2f)
		{
			JobFailReason.Is(TemperatureTrans);
			return false;
		}
		if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
		{
			return false;
		}
		if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
		{
			return false;
		}
		if (FindHoney(pawn, building_FermentingBarrel) == null)
		{
			JobFailReason.Is(NoHoneyTrans);
			return false;
		}
		if (t.IsBurning())
		{
			return false;
		}
		return true;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		Building_MeadFermentingBarrel barrel = (Building_MeadFermentingBarrel)t;
		Thing thing = FindHoney(pawn, barrel);
		return JobMaker.MakeJob(InternalDefOf.Bees_FillMeadFermentingBarrel, t, thing);
	}

	private Thing FindHoney(Pawn pawn, Building_MeadFermentingBarrel barrel)
	{
		Predicate<Thing> validator = (Thing x) => (!x.IsForbidden(pawn) && pawn.CanReserve(x)) ? true : false;
		return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(InternalDefOf.Bees_Honey), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, validator);
	}    
}