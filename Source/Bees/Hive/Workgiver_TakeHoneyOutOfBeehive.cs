using RimWorld;
using Verse;
using Verse.AI;

namespace Bees;

public class Workgiver_TakeHoneyOutOfBeehive : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(InternalDefOf.Bees_Beehive);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Beehive beehive = t as Beehive;
        int skill = pawn.skills.skills.Find(r => r.def.defName == "Animals").levelInt;
        if (beehive == null || !beehive.HoneyReady || skill < 5)
        {
            return false;
        }
        if (t.IsBurning())
        {
            return false;
        }
        if (!t.IsForbidden(pawn))
        {
            LocalTargetInfo target = t;
            if (pawn.CanReserve(target, 1, -1, null, forced))
            {
                return true;
            }
        }
        return false;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return new Job(InternalDefOf.Bees_TakeHoneyOutOfBeehive, t);
    }
}