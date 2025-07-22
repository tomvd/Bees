using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Bees;

public class Beehive : Building
{
    private float size;
    private float honeyAmount;
    private float hoursInactive;
    private float flowerPercentage;
    private bool active;
    private string inactiveReason;
    private bool unhealthy;
    private bool toomany;
    private bool forceHoneyRemoval;
    private bool markedForSplitting;

    public bool MarkedForSplitting
    {
        get => markedForSplitting;
    }

    private const float HoneyPerTenthHour = 0.05f; // amount of honey a perfect hive makes per 1/10 of an hour - 0.5/h on 12h/day active means 6 honey/day or a full stack for a season
    private const float GrowthPerTenthHour = 0.002f; // how much a hive grows per 1/10th of an hour when active - 2%/h on 12h/day active means full hive in about 4-5 days
    private static readonly float PlantGrowthMultiplier = 0.5f; // 0f is normal growth, 1f is twice the growth
    
    
    public bool HoneyReady
    {
        get
        {
            return honeyAmount >= 75.0f || forceHoneyRemoval;
        }
    }

    public bool NeedTend
    {
        get
        {
            return unhealthy;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref this.flowerPercentage, "flowerPercentage");
        Scribe_Values.Look(ref this.size, "size");
        Scribe_Values.Look(ref this.honeyAmount, "honeyAmount");
        Scribe_Values.Look(ref this.active, "active");
        Scribe_Values.Look(ref this.inactiveReason, "inactiveReason", "");
        Scribe_Values.Look(ref this.hoursInactive, "hoursInactive");
        Scribe_Values.Look(ref this.unhealthy, "unhealthy");
        Scribe_Values.Look(ref this.toomany, "toomany");
        Scribe_Values.Look(ref this.forceHoneyRemoval, "forceHoneyRemoval");
        Scribe_Values.Look(ref this.markedForSplitting, "markedForSplitting");
    }

    /*
     * tickrare is called 10x per ingame hour or 250 ticks
     */
    public override void TickRare()
    {
        base.TickRare();
        if (Map is null) return; // beehive is not spawned
        RecalculateFlowerPercentage();
        RecalculateActive();
        if (!unhealthy)
            unhealthy = Rand.Chance(0.0001f);
        if (active)
        {
            honeyAmount += flowerPercentage * size * (toomany?HoneyPerTenthHour/2f:HoneyPerTenthHour);
            honeyAmount = Mathf.Clamp(honeyAmount, 0.0f, 75.0f);
            if (!unhealthy)
                size += (toomany?GrowthPerTenthHour/2f:GrowthPerTenthHour);
        }
        else
        {
            hoursInactive += 0.1f;
            if (hoursInactive > 12f)
            {
                honeyAmount -= HoneyPerTenthHour/20f;
                honeyAmount = Mathf.Clamp(honeyAmount, 0.0f, 75.0f);
                if (honeyAmount < 0.01f)
                {
                    size -= GrowthPerTenthHour/20f;
                }
            }
        }
        size = Mathf.Clamp(size, 0.0f, 1.0f);
    }

    private void Reset()
    {
        this.forceHoneyRemoval = false;
        this.honeyAmount = 0;
    }
/*
    public void ResetTend(Pawn pawn)
    {
        int skill = pawn.skills.skills.Find((SkillRecord r) => r.def.defName == "Animals").levelInt;
        System.Random rnd = new System.Random();
        if(rnd.Next(0, 21 - skill) == 1)
        {
            this.tickBeforeTend = 120000;
        }
        else
        {
            pawn.health.AddHediff(InternalDefOf.Bees_Sting);
            pawn.needs.mood.thoughts.memories.TryGainMemoryFast(InternalDefOf.Bees_StingMoodDebuff);
            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Tending failed", 5f);
        }
        pawn.skills.skills.Find((SkillRecord r) => r.def.defName == "Animals").Learn(100, false);
    }
*/
    public override string GetInspectString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(base.GetInspectString());
        if (stringBuilder.Length != 0)
        {
            stringBuilder.AppendLine();
        }
        stringBuilder.AppendLine("HiveInspectSize".Translate() + size.ToStringPercent());
        stringBuilder.AppendLine("HiveInspectPlants".Translate() + flowerPercentage.ToStringPercent());
        stringBuilder.AppendLine("HiveInspectActive".Translate() + (active?"Yes".Translate():"No".Translate()+" (" + inactiveReason +" "+ hoursInactive.ToStringDecimalIfSmall() + "h)"));
        stringBuilder.AppendLine("HiveInspectHoney".Translate() + honeyAmount.ToStringDecimalIfSmall());
        if (toomany)
        {
            stringBuilder.AppendLine("HiveTooClose".Translate());
        }
        if (unhealthy)
        {
            stringBuilder.AppendLine("HiveUnhealthy".Translate());
        }
        return stringBuilder.ToString().TrimEndNewlines();
    }

    private void RecalculateActive()
    {
        active = false;
        if (flowerPercentage < 0.001f) inactiveReason = "InActiveReasonNoFlowers".Translate(); 
        else if (Map.GameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout) && IsOutdoors()) inactiveReason = "InActiveReasonToxic".Translate();
        else if (AmbientTemperature < 10) inactiveReason = "InActiveReasonCold".Translate();
        else if (Map.weatherManager.RainRate >= 0.01f && IsOutdoors()) inactiveReason = "InActiveReasonRainy".Translate();
        else if (Map.glowGrid.GroundGlowAt(Position) < 0.51f) inactiveReason = "InActiveReasonDark".Translate();
        else {
            active = true;
            hoursInactive = 0f;
        }
    }

    private bool IsOutdoors()
    {
        if (Position.UsesOutdoorTemperature(Map))
        {
            return true;
        }
        RoofDef roofDef = Map.roofGrid.RoofAt(Position);
        if (roofDef == null)
        {
            return true;
        }

        return false;
    }

    public Thing TakeOutHoney()
    {
        if (Mathf.FloorToInt(honeyAmount) < 1)
        {
            Log.Warning("Tried to get honey but there is none.");
            this.Reset();
            return null;
        }
        Thing thing = ThingMaker.MakeThing(InternalDefOf.Bees_Honey);
        thing.stackCount = Mathf.FloorToInt(honeyAmount);
        Reset();
        if (InternalDefOf.Bees_PropolisExtraction.IsFinished && Rand.Chance(0.5f))
        {
            Thing propolis = ThingMaker.MakeThing(InternalDefOf.Bees_Propolis);
            propolis.stackCount = Rand.Chance(0.25f)?2:1;            
            GenPlace.TryPlaceThing(propolis, Position, Map, ThingPlaceMode.Near);
        }
        return thing;
    }
    
    public Thing DoSplitHive()
    {
        if (Mathf.FloorToInt(size) < 0.9f)
        {
            Log.Warning("Size must be 100% to be able to split a hive.");
            markedForSplitting = false;
            return null;
        }

        markedForSplitting = false;
        size = 0.5f;
        return ThingMaker.MakeThing(InternalDefOf.Bees_Bees);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        GenPlace.TryPlaceThing(ThingMaker.MakeThing(InternalDefOf.Bees_Bees), Position, Map, ThingPlaceMode.Near);        
        base.Destroy(mode);
    }

    public void RecalculateFlowerPercentage()
    {
        var flowerCount = 0;
        var cellsAround = CellsAroundA(this.TrueCenter().ToIntVec3(), Map);
        toomany = false;
        foreach (IntVec3 cell in cellsAround)
        {
            foreach (Thing item in cell.GetThingList(Map))
            {
                if (item is Plant plant)
                {
                    if (active && !unhealthy)
                    {
                        plant.growthInt += plant.GrowthPerTick * 250f * PlantGrowthMultiplier * size;
                    }

                    if (plant.growthInt >= 0.5f )
                    {
                        flowerCount++;
                    }
                }

                if (item != this && item.def == InternalDefOf.Bees_Beehive)
                {
                    toomany = true;
                }
            }
        }

        flowerPercentage = Mathf.Clamp(flowerCount / (cellsAround.Count - 8.0f), 0f, 1.0f);
    }

    public List<IntVec3> CellsAroundA(IntVec3 pos, Map map)
    {
        List<IntVec3> cellsAround = new List<IntVec3>();
        if (!pos.InBounds(map))
        {
            return cellsAround;
        }
        IEnumerable<IntVec3> cells = CellRect.CenteredOn(this.Position, 5).Cells;
        foreach (IntVec3 item in cells)
        {
            if (item.InHorDistOf(pos, 4.9f))
            {
                cellsAround.Add(item);
            }
        }
        return cellsAround;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo g in base.GetGizmos())
        {
            yield return g;
        }

        if (Mathf.FloorToInt(honeyAmount) >= 1 && !forceHoneyRemoval)
        {
            yield return new Command_Action
            {
                defaultLabel = "ForceHoneyHarvest".Translate(),
                icon = ContentFinder<Texture2D>.Get($"UI/HarvestHoney"),
                action = delegate
                {
                    forceHoneyRemoval = true;
                    
                }
            };
        }
        
        if (Mathf.FloorToInt(size) >= 1 && !markedForSplitting && InternalDefOf.Bees_HiveSplitting.IsFinished)
        {
            yield return new Command_Action
            {
                defaultLabel = "SplitHive".Translate(),
                icon = ContentFinder<Texture2D>.Get($"UI/SplitHive"),
                action = delegate
                {
                    markedForSplitting = true;
                }
            };
        }        

        /*
         * TODO gizmo to take out honey now
         */
        if (Prefs.DevMode)
        {
            yield return new Command_Action
            {
                defaultLabel = "Debug: Set progress to max",
                action = delegate
                {
                    size = 1;
                    honeyAmount = 75;
                }
            };
            yield return new Command_Action
            {
                defaultLabel = "Debug: increase size",
                action = delegate
                {
                    size += 0.10f;
                    size = Mathf.Clamp(size, 0.0f, 1.0f);
                }
            };
            yield return new Command_Action
            {
                defaultLabel = "Debug: make unhealthy",
                action = delegate
                {
                    unhealthy = true;
                }
            };            
        }
    }

    public void Heal()
    {
        unhealthy = false;
    }
}