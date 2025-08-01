using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Bees;

public class WildHiveSpawner : MapComponent
{
    private int lastSpawnTicks;
    
    public WildHiveSpawner(Map map) : base(map)
    {
    }
    
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref lastSpawnTicks, "lastSpawnTicks");
    }
    
    public override void MapComponentTick()
    {
        base.MapComponentTick();
        /*
         * spawn wild hive: destroying it yields 1 bees and 1 honey
         */
        if (GenTicks.TicksGame % 500 == 300) // rare tick
        {
            if ((lastSpawnTicks == 0 || GenDate.DaysToTicks(GenDate.DaysPassedSinceSettleFloat) - lastSpawnTicks >= GenDate.TicksPerYear)  // it has been a year ago, or playing a year
                && map.mapTemperature.OutdoorTemp > 12 // warm enough
                && !map.GameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout) // not toxic fallout
                && map.weatherManager.RainRate < 0.01f) // no rain 
            {
                //if (map.listerThings.ThingsOfDef(InternalDefOf.Bees_Wildhive).Count < 3) // less than 3 on the map
                {
                    var loc = DropCellFinder.TryFindSafeLandingSpotCloseToColony(map, IntVec2.Two);
                    // find the nearest mature tree - if no mature tree on the map - too bad :p
                    IEnumerable<Thing> searchSet = map.listerThings.GetAllThings(t => t.def.defName.ToUpper().Contains("TREE") && t is Plant p && p.Growth > 0.99f);
                    if (searchSet != null)
                    {
                        Thing thing = GenClosest.ClosestThing_Global(loc, searchSet);
                        if (thing != null && (thing.Position + IntVec3.West).InBounds(map))
                        {
                            // the sprite is done in such a way that it only looks good when on the left of a tree...
                            var swarm = GenSpawn.Spawn(InternalDefOf.Bees_Wildhive, thing.Position + IntVec3.West, map);
                            Find.LetterStack.ReceiveLetter("Wild beehive",
                                "Wild beehive found! You can destroy it to get the bees and start your own beehive for honey harvesting.",
                                LetterDefOf.PositiveEvent, swarm);
                            lastSpawnTicks = GenDate.DaysToTicks(GenDate.DaysPassedSinceSettleFloat);
                        }
                    }
                }
            }
        }        
    }
}