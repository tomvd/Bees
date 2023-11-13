using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Bees;

public class JobDriver_TakeHoneyOutOfBeehive : JobDriver
    {
        private float workLeft;

        private float totalNeededWork;
        private const TargetIndex BarrelInd = TargetIndex.A;
        private const TargetIndex MeadToHaulInd = TargetIndex.B;
        private const TargetIndex StorageCellInd = TargetIndex.C;

        protected Beehive Beehive
        {
            get
            {
                return (Beehive)this.job.GetTarget(TargetIndex.A).Thing;
            }
        }

        protected Thing Honey
        {
            get
            {
                return this.job.GetTarget(TargetIndex.B).Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = this.pawn;
            LocalTargetInfo target = this.Beehive;
            Job job = this.job;
            return pawn.Reserve(target, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil doWork = ToilMaker.MakeToil("MakeNewToils").FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
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
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate
            {
                Thing thing = this.Beehive.TakeOutHoney();
                GenPlace.TryPlaceThing(thing, this.pawn.Position, base.Map, ThingPlaceMode.Near, null, null);
                StoragePriority currentPriority = StoreUtility.CurrentStoragePriorityOf(thing);
                IntVec3 c;
                if (StoreUtility.TryFindBestBetterStoreCellFor(thing, this.pawn, base.Map, currentPriority, this.pawn.Faction, out c, true))
                {
                    this.job.SetTarget(TargetIndex.C, c);
                    this.job.SetTarget(TargetIndex.B, thing);
                    this.job.count = thing.stackCount;
                }
                else
                {
                    base.EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
            yield return Toils_Reserve.Reserve(TargetIndex.B, 1, -1, null);
            yield return Toils_Reserve.Reserve(TargetIndex.C, 1, -1, null);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false);
            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.C);
            yield return carryToCell;
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, carryToCell, true);
        }
    }