using System;
using System.Collections.Generic;
using Bees;
using Verse;
using Verse.AI;
using RimWorld;

namespace Bees
{
    class JobDriver_TendToBeehive : JobDriver
    {
        private float workLeft;

        private float totalNeededWork;
        
        protected Beehive Beehive
        {
            get { return (Beehive)this.job.GetTarget(TargetIndex.A).Thing; }
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
                int skill = pawn.skills.skills.Find((SkillRecord r) => r.def.defName == "Animals").levelInt / 2;
                if (Rand.RangeInclusive(0, 11 - skill) <= 5)
                {
                    Beehive.Heal();
                }
                else
                {
                    if (Rand.RangeInclusive(1, 2) == 1)
                    {
                        pawn.TakeDamage(new DamageInfo(InternalDefOf.Bees_DamageSting, 1));
                        pawn.TakeDamage(new DamageInfo(DamageDefOf.Stun, 1));
                    }
                    pawn.needs.mood.thoughts.memories.TryGainMemoryFast(InternalDefOf.Bees_StingMoodDebuff);
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Tending failed", 5f);
                    pawn.jobs.StartJob(new Job(InternalDefOf.Bees_TendToBeehive, TargetA));
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
        }
    }
}
