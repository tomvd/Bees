﻿using System;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;

namespace Bees
{
    class WorkGiver_TendToBeehive : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(InternalDefOf.Bees_Beehive);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Beehive { NeedTend: true })
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
            return new Job(InternalDefOf.Bees_TendToBeehive, t);
        }
    }
}
