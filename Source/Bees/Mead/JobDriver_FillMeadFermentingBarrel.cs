using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Bees;

public class JobDriver_FillMeadFermentingBarrel : JobDriver
{
    private const TargetIndex BarrelInd = TargetIndex.A;

    private const TargetIndex HoneyInd = TargetIndex.B;

    private const int Duration = 200;

    protected Building_MeadFermentingBarrel Barrel => (Building_MeadFermentingBarrel)job.GetTarget(TargetIndex.A).Thing;

    protected Thing Honey => job.GetTarget(TargetIndex.B).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (pawn.Reserve(Barrel, job, 1, -1, null, errorOnFailed))
        {
            return pawn.Reserve(Honey, job, 1, -1, null, errorOnFailed);
        }
        return false;
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnBurningImmobile(TargetIndex.A);
        AddEndCondition(() => (Barrel.SpaceLeftForHoney > 0) ? JobCondition.Ongoing : JobCondition.Succeeded);
        yield return Toils_General.DoAtomic(delegate
        {
            job.count = Barrel.SpaceLeftForHoney;
        });
        Toil reserveHoney = Toils_Reserve.Reserve(TargetIndex.B);
        yield return reserveHoney;
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
        yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.B);
        yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveHoney, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.Wait(200).FailOnDestroyedNullOrForbidden(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.A)
            .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
            .WithProgressBarToilDelay(TargetIndex.A);
        Toil toil = ToilMaker.MakeToil("MakeNewToils");
        toil.initAction = delegate
        {
            Barrel.AddHoney(Honey);
        };
        toil.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return toil;
    }    
}