using System.Collections.Generic;
using Verse;

namespace Bees;

public class Placeworker_Beehive : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
    {
        foreach (IntVec3 c in GenAdj.OccupiedRect(center, rot, def.Size).ExpandedBy(1))
        {
            List<Thing> list = map.thingGrid.ThingsListAt(c);
            foreach (var thing2 in list)
            {
                if (thing2 != thingToIgnore && ((thing2.def.category == ThingCategory.Building && thing2.def.defName == "Bees_Beehive") || ((thing2.def.IsBlueprint || thing2.def.IsFrame) && thing2.def.entityDefToBuild is ThingDef && ((ThingDef)thing2.def.entityDefToBuild).defName == "Bees_Beehive")))
                {
                    return "APlaceWorker".Translate();
                }
            }
        }
        return true;
    }
}
