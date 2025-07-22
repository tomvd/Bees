using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Bees;

public class JobDriver_TakeHoneyOutOfBeehive : JobDriver
    {
        private float workLeft;

        private float totalNeededWork;

        private Beehive Beehive => (Beehive)job.GetTarget(TargetIndex.A).Thing;

        protected Thing Honey => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            LocalTargetInfo target = Beehive;
            return pawn.Reserve(target, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil doWork = ToilMaker.MakeToil().FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            doWork.initAction = delegate
            {
                totalNeededWork = 500;
                workLeft = totalNeededWork;
            };
            doWork.tickAction = delegate
            {
                workLeft -= 1;
                if (pawn.skills != null)
                {
                    pawn.skills.Learn(SkillDefOf.Animals, 0.1f);
                }                
                if (workLeft <= 0f)
                {
                    //SoundDefOf.Finish_Wood.PlayOneShot(SoundInfo.InMap(Tree));
                    doWork.actor.jobs.curDriver.ReadyForNextToil();
                }
            };
            doWork.defaultCompleteMode = ToilCompleteMode.Never;
            doWork.WithProgressBar(TargetIndex.A, () => 1f - workLeft / totalNeededWork);
            //doWork.WithEffect(EffecterDefOf.Harvest_Plant, TargetIndex.A);
            doWork.PlaySustainerOrSound(() => InternalDefOf.Bees_Beehive_Ambience);
            doWork.activeSkill = () => SkillDefOf.Animals;
            yield return doWork;
            var toil = ToilMaker.MakeToil();
            toil.initAction = delegate
            {
                Thing thing = Beehive.TakeOutHoney();
                if (Rand.RangeInclusive(1, 3) == 1)
                {
                    pawn.health.AddHediff(InternalDefOf.Bees_Sting);
                    pawn.needs.mood.thoughts.memories.TryGainMemoryFast(InternalDefOf.Bees_StingMoodDebuff);
            
                }                
                GenPlace.TryPlaceThing(thing, pawn.Position, Map, ThingPlaceMode.Near);
                StoragePriority currentPriority = StoreUtility.CurrentStoragePriorityOf(thing);
                if (StoreUtility.TryFindBestBetterStoreCellFor(thing, pawn, Map, currentPriority, pawn.Faction, out var c))
                {
                    job.SetTarget(TargetIndex.C, c);
                    job.SetTarget(TargetIndex.B, thing);
                    job.count = thing.stackCount;
                }
                else
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
            yield return Toils_Reserve.Reserve(TargetIndex.B);
            yield return Toils_Reserve.Reserve(TargetIndex.C);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.C);
            yield return carryToCell;
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, carryToCell, true);
        }
    }