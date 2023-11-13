using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Bees;

public class WorkGiver_TakeMeadOutOfFermentingBarrel : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(InternalDefOf.Bees_MeadFermentingBarrel);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        List<Thing> list = pawn.Map.listerThings.ThingsOfDef(InternalDefOf.Bees_MeadFermentingBarrel);
        for (int i = 0; i < list.Count; i++)
        {
            if (((Building_MeadFermentingBarrel)list[i]).Fermented)
            {
                return false;
            }
        }
        return true;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!(t is Building_MeadFermentingBarrel building_FermentingBarrel) || !building_FermentingBarrel.Fermented)
        {
            return false;
        }
        if (t.IsBurning())
        {
            return false;
        }
        if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        {
            return false;
        }
        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return JobMaker.MakeJob(InternalDefOf.Bees_TakeMeadOutOfFermentingBarrel, t);
    }    
}