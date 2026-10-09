// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly ExceptionRegionTests
{
}

.class public sequential ansi sealed beforefieldinit ExceptionRegionTests
       extends [System.Runtime]System.Object
{
    .field private static int32 Value

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        .maxstack 1

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Object::.ctor()
        IL_0006: ret
    }

    .method public instance void ExceptionRegion.NestedTryFinally_Valid() cil managed
    {
        .try
        {
            nop
            leave   EndTryCatch
        }
        finally
        {
            .try
            {
                nop
                leave   EndFinally
            }
            catch [System.Runtime]System.Exception
            {
                nop
                leave   EndFinally
            }
        EndFinally:
            endfinally
        }
    EndTryCatch:
        ret
    }

    .method public instance void Leave.ToSameFilter_Valid() cil managed
    {
        .try
        {
            leave   MethodEnd
        }
        filter
        {
            pop
            leave   SameFilter

        SameFilter:
            ldc.i4.0
            endfilter
        }
        {
            pop
            leave   MethodEnd
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.ToOtherFilter_Invalid_LeaveOutOfFilter.LeaveIntoFilter() cil managed
    {
        .try
        {
            leave   AfterTry
        }
        filter
        {
            pop
            leave   OtherFilter

            ldc.i4.0
            endfilter
        }
        {
            pop
            leave   MethodEnd
        }
    AfterTry:
        nop

        .try
        {
            leave   MethodEnd
        }
        filter
        {
            pop

        OtherFilter:
            ldc.i4.0
            endfilter
        }
        {
            pop
            leave   MethodEnd
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.OutOfFilter_Invalid_LeaveOutOfFilter() cil managed
    {
        .try
        {
            leave   MethodEnd
        }
        filter
        {
            pop
            leave   MethodEnd

            ldc.i4.0
            endfilter
        }
        {
            pop
            leave   MethodEnd
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.ToSameFault_Valid() cil managed
    {
        .try
        {
            leave   MethodEnd
        }
        fault
        {
            leave   SameFault

        SameFault:
            endfault
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.ToSameFinally_Valid() cil managed
    {
        .try
        {
            leave   MethodEnd
        }
        finally
        {
            leave   SameFinally

        SameFinally:
            endfinally
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.ToOtherFinally_Invalid_LeaveOutOfFinally.LeaveIntoHandler() cil managed
    {
        .try
        {
            leave   AfterTry
        }
        finally
        {
            leave   OtherFinally
            endfinally
        }
    AfterTry:
        nop

        .try
        {
            leave   MethodEnd
        }
        finally
        {
        OtherFinally:
            endfinally
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.ToOtherFault_Invalid_LeaveOutOfFault.LeaveIntoFault.LeaveIntoHandler() cil managed
    {
        .try
        {
            leave   AfterTry
        }
        fault
        {
            leave   OtherFault
            endfault
        }
    AfterTry:
        nop

        .try
        {
            leave   MethodEnd
        }
        fault
        {
        OtherFault:
            endfault
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.OutOfFinally_Invalid_LeaveOutOfFinally() cil managed
    {
        .try
        {
            leave   MethodEnd
        }
        finally
        {
            leave   MethodEnd
            endfinally
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.OutOfFault_Invalid_LeaveOutOfFault() cil managed
    {
        .try
        {
            leave   MethodEnd
        }
        fault
        {
            leave   MethodEnd
            endfault
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.ToSameTry_Valid() cil managed
    {
        .try
        {
            leave   SameTry

        SameTry:
            leave   MethodEnd
        }
        finally
        {
            endfinally
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.ToEnclosingTry_Valid() cil managed
    {
        .try
        {
            .try
            {
                leave   EnclosingTry
            }
            finally
            {
                endfinally
            }
        EnclosingTry:
            leave   MethodEnd
        }
        finally
        {
            endfinally
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.ToFirstInstrOfDisjointTry_Valid() cil managed
    {
        .try
        {
           leave    DisjointTry
        }
        finally
        {
            endfinally
        }

        .try
        {
        DisjointTry:
            leave   MethodEnd
        }
        finally
        {
            endfinally
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.ToFirstInstrOfNestedDisjointTry_Valid() cil managed
    {
        .try
        {
           leave    DisjointTry
        }
        finally
        {
            endfinally
        }

        .try
        {
            .try
            {
            DisjointTry:
                leave   MethodEnd
            }
            finally
            {
                endfinally
            }
        }
        finally
        {
            endfinally
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.IntoOtherTry_Invalid_LeaveIntoTry() cil managed
    {
        .try
        {
           leave    OtherTry
        }
        finally
        {
            endfinally
        }

        .try
        {
            nop
        OtherTry:
            leave   MethodEnd
        }
        finally
        {
            endfinally
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.ToSecondInstrOfNestedDisjointTry_Invalid_LeaveIntoTry() cil managed
    {
        .try
        {
           leave    DisjointTry
        }
        finally
        {
            endfinally
        }

        .try
        {
            nop
            .try
            {
            DisjointTry:
                leave   MethodEnd
            }
            finally
            {
                endfinally
            }
        }
        finally
        {
            endfinally
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.ToFirstInstrOfEnclosedTry_Valid() cil managed
    {
        .try
        {
           leave    EnclosedTry

            .try
            {
            EnclosedTry:
                leave   TryEnd
            }
            finally
            {
                endfinally
            }
        TryEnd:
            leave   MethodEnd
        }
        finally
        {
            endfinally
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.FromCatchIntoAssociatedTry_Valid() cil managed
    {
        .try
        {
        TryStart:
            leave    MethodEnd
        }
        catch [System.Runtime]System.Object
        {
            leave   TryStart
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.ToSameCatch_Valid() cil managed
    {
        .try
        {
           leave    MethodEnd
        }
        catch [System.Runtime]System.Object
        {
            leave   SameCatch

        SameCatch:
            leave   MethodEnd
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.OutOfCatch_Valid() cil managed
    {
        .try
        {
            leave   MethodEnd
        }
        catch [System.Runtime]System.Object
        {
            leave   MethodEnd
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.CatchToFirstInstrOfDisjointTry_Valid() cil managed
    {
        .try
        {
            leave   AfterTry
        }
        catch [System.Runtime]System.Object
        {
            leave   DisjointTry
        }
    AfterTry:
        nop

        .try
        {
        DisjointTry:
            leave   MethodEnd
        }
        finally
        {
            endfinally
        }
    MethodEnd:
        ret
    }

    .method public instance void Leave.CatchToFirstInstrOfNestedTry_Valid() cil managed
    {
        .try
        {
            leave   MethodEnd
        }
        catch [System.Runtime]System.Object
        {
            leave   NestedTry

            .try
            {
            NestedTry:
                leave   MethodEnd
            }
            finally
            {
                endfinally
            }

            leave   MethodEnd
        }

    MethodEnd:
        ret
    }

    .method public instance void Leave.CatchToSecondInstrOfNestedTry_Invalid_LeaveIntoTry() cil managed
    {
        .try
        {
            leave   MethodEnd
        }
        catch [System.Runtime]System.Object
        {
            leave   NestedTry

            .try
            {
                nop
            NestedTry:
                leave   MethodEnd
            }
            finally
            {
                endfinally
            }

            leave   MethodEnd
        }

    MethodEnd:
        ret
    }

    .method public instance void Leave.TryToEnclosingCatch_Valid() cil managed
    {
        .try
        {
            leave   MethodEnd
        }
        catch [System.Runtime]System.Object
        {
            pop
            .try
            {
                leave   EnclosingCatch
            }
            finally
            {
                endfinally
            }

        EnclosingCatch:
            leave   MethodEnd
        }

    MethodEnd:
        ret
    }

    .method public instance void Leave.CatchToEnclosingTry_Valid() cil managed
    {
        .try
        {
            .try
            {
                leave   MethodEnd
            }
            catch [System.Runtime]System.Object
            {
                leave   EnclosingTry
            }

        EnclosingTry:
            leave   MethodEnd
        }
        filter
        {
            pop
            ldc.i4.0
            endfilter
        }
        {
            leave   MethodEnd
        }

    MethodEnd:
        ret
    }

    .method public instance void Leave.FromOutsideIntoFilter_Invalid_LeaveIntoFilter() cil managed
    {
        leave   IntoFilter

        .try
        {
            leave   MethodEnd
        }
        filter
        {
            pop
        IntoFilter:
            ldc.i4.0
            endfilter
        }
        {
            leave   MethodEnd
        }

    MethodEnd:
        ret
    }

    .method public instance void Leave.FromOutsideIntoHandler_Invalid_LeaveIntoHandler() cil managed
    {
        leave   IntoHandler

        .try
        {
            leave   MethodEnd
        }
        catch [System.Runtime]System.Object
        {
            pop
        IntoHandler:
            leave   MethodEnd
        }

    MethodEnd:
        ret
    }

    .method public instance void Leave.FromOutsideIntoTry_Invalid_LeaveIntoTry() cil managed
    {
        leave   IntoTry

        .try
        {
            nop
        IntoTry:
            leave   MethodEnd
        }
        finally
        {
            endfinally
        }

    MethodEnd:
        ret
    }

    .method public instance void Leave.FromNestedTryToDisjointTry_Valid() cil managed
    {
        .try
        {
            .try
            {
                leave   DisjointTry
            }
            finally
            {
                endfinally
            }
            leave   AfterTry
        }
        finally
        {
            endfinally
        }
    AfterTry:
        nop

        .try
        {
        DisjointTry:
            leave   MethodEnd
        }
        finally
        {
            endfinally
        }

    MethodEnd:
        ret
    }

    .method public instance void Leave.FromNestedTryToDisjointTryBackward_Valid() cil managed
    {
        .try
        {
        DisjointTry:
            leave   AfterTry
        }
        finally
        {
            endfinally
        }
    AfterTry:
        nop

        .try
        {
            .try
            {
                leave   DisjointTry
            }
            finally
            {
                endfinally
            }
            leave   MethodEnd
        }
        finally
        {
            endfinally
        }

    MethodEnd:
        ret
    }

    .method public hidebysig instance void Catch.NonExceptionDerivedType_Invalid_ThrowOrCatchOnlyExceptionType() cil managed
    {
        .maxstack 1

        .try { leave.s MethodEnd }
        catch ExceptionRegionTests
        {
            leave.s MethodEnd
        }

        MethodEnd:
            ret
    }

    .method public hidebysig instance void Throw.NonExceptionDerivedType_Invalid_ThrowOrCatchOnlyExceptionType() cil managed
    {
        .maxstack 1

        newobj instance void ExceptionRegionTests::.ctor()
        throw
        ret
    }

    .method public hidebysig instance void TryOffset.ExceedsCodeSize_Invalid_EHClauseOutOfRange() cil managed
    {
        .maxstack 1

        ret
        // Try region starts at offset 1000 which is beyond code size (code size is 1)
        .try 1000 to 1001 catch [System.Runtime]System.Exception handler 0 to 1
    }

    .method public hidebysig instance void HandlerOffset.ExceedsCodeSize_Invalid_EHClauseOutOfRange() cil managed
    {
        .maxstack 1

        ret
        // Handler region starts at offset 1000 which is beyond code size (code size is 1)
        .try 0 to 1 catch [System.Runtime]System.Exception handler 1000 to 1001
    }

    .method public hidebysig instance void FilterOffset.ExceedsCodeSize_Invalid_EHClauseOutOfRange() cil managed
    {
        .maxstack 1

        ldc.i4.0
        endfilter
        leave.s IL_0004
        IL_0004: ret
        // Filter region starts at offset 1000 which is beyond code size (code size is 5)
        .try 0 to 2 filter 1000 handler 2 to 4
    }

    .method public hidebysig instance void TryEndOffset.ExceedsCodeSize_Invalid_EHClauseOutOfRange() cil managed
    {
        .maxstack 1

        nop
        ret
        // Try region ends at offset 1000 which is beyond code size (code size is 2)
        .try 0 to 1000 catch [System.Runtime]System.Exception handler 1 to 2
    }

    .method public hidebysig instance void HandlerEndOffset.ExceedsCodeSize_Invalid_EHClauseOutOfRange() cil managed
    {
        .maxstack 1

        nop
        ret
        // Handler region ends at offset 1000 which is beyond code size (code size is 2)
        .try 0 to 1 catch [System.Runtime]System.Exception handler 1 to 1000
    }

    .method public hidebysig instance void FilterOffset.NotBeforeHandlerOffset_Invalid_EHClauseOutOfRange() cil managed
    {
        .maxstack 1

        ldc.i4.0
        endfilter
        leave.s IL_0004
        IL_0004: ret
        // Filter region starts at the same offset as handler region.
        .try 0 to 2 filter 2 handler 2 to 4
    }

    .method public static void Try.Empty_Invalid_EHClauseEmpty() cil managed
    {
        ret
    TryStart:
        nop
    HandlerStart:
        pop
        ret
    HandlerEnd:
        .try TryStart to TryStart catch [System.Runtime]System.Exception handler HandlerStart to HandlerEnd
    }

    .method public static void Handler.EmptyInsideFilter_Invalid_EHClauseEmpty.FallthroughException() cil managed
    {
    OuterTry:
        leave MethodEnd
    FilterStart:
        pop
    InnerTry:
        nop
    InnerHandler:
        ldc.i4.1
        endfilter
    OuterHandler:
        pop
        leave MethodEnd
    MethodEnd:
        ret
        .try InnerTry to InnerHandler catch [System.Runtime]System.Exception handler InnerHandler to InnerHandler
        .try OuterTry to FilterStart filter FilterStart handler OuterHandler to MethodEnd
    }

    .method public static void Try.StartInsideInstruction_Invalid_EHClauseBoundary() cil managed
    {
        // ldc.i4 occupies offsets 1 through 5, even though it is unreachable.
        ret
        ldc.i4 0x12345678
        ret
        .try 2 to 6 catch [System.Runtime]System.Exception handler 6 to 7
    }

    .method public static void Try.EndInsideInstruction_Invalid_EHClauseBoundary() cil managed
    {
        ret
        ldc.i4 0x12345678
        ret
        .try 1 to 3 catch [System.Runtime]System.Exception handler 6 to 7
    }

    .method public static void Try.StartsAfterPrefix_Invalid_EHClauseBoundary() cil managed
    {
        ret
        volatile.
        ldsfld int32 ExceptionRegionTests::Value
        pop
        ret
        // The prefixed instruction starts at offset 1; ldsfld alone is not a valid target.
        .try 3 to 8 catch [System.Runtime]System.Exception handler 8 to 10
    }

    .method public static void Filter.EndfilterBeforeLastInstruction_Invalid_EHClauseFilterEnd() cil managed
    {
        ret
    TryStart:
        nop
    FilterStart:
        pop
        ldc.i4.1
        endfilter
        nop
    HandlerStart:
        pop
        ret
    End:
        .try TryStart to FilterStart filter FilterStart handler HandlerStart to End
    }

    .method public static void Filter.MultipleEndfilters_Invalid_EHClauseFilterEnd() cil managed
    {
        ret
    TryStart:
        nop
    FilterStart:
        pop
        ldc.i4.1
        endfilter
        ldc.i4.1
        endfilter
    HandlerStart:
        pop
        ret
    End:
        .try TryStart to FilterStart filter FilterStart handler HandlerStart to End
    }

    .method public static void Filter.PrefixedExtraEndfilter_Invalid_EHClauseFilterEnd() cil managed
    {
        .maxstack 1
        ret
    TryStart:
        leave MethodEnd
    FilterStart:
        pop
        ldc.i4.1
        // A prefix makes the following opcode an invalid branch target, but the
        // layout check must still detect this extra endfilter in unreachable IL.
        volatile.
        endfilter
        ldc.i4.1
        endfilter
    HandlerStart:
        pop
        leave MethodEnd
    MethodEnd:
        ret
        .try TryStart to FilterStart filter FilterStart handler HandlerStart to MethodEnd
    }

    .method public static void Try.Overlapping_Invalid_EHClauseOverlap() cil managed
    {
        // Report the overlapping layout from the PR review and still import the loops.
    T1:
        br T1
    H1:
        br H1
    HE1:
    T2:
        br T2
    TE1:
    TE2:
    H2:
        br H2
    HE2:
        .try T1 to TE1 finally handler H1 to HE1
        .try T2 to TE2 finally handler H2 to HE2
    }

    .method public static void Clause.HandlerOutsideEnclosingTry_Invalid_EHClauseNesting() cil managed
    {
        ret
    OuterTry:
        nop
    InnerTry:
        nop
    InnerTryEnd:
        nop
    OuterTryEnd:
        nop
    InnerHandler:
        endfinally
    OuterHandler:
        endfinally
    End:
        .try InnerTry to InnerTryEnd finally handler InnerHandler to OuterHandler
        .try OuterTry to OuterTryEnd finally handler OuterHandler to End
    }

    .method public static void Try.EquivalentToHandler_Invalid_EHClauseOverlap() cil managed
    {
        ret
    Start:
        nop
    End:
        .try Start to End catch [System.Runtime]System.Exception handler Start to End
    }

    .method public static void Try.SharedWithFinallyAndFault_Invalid_EHClauseOverlap.EHClauseOverlap.EHClauseOverlap() cil managed
    {
        // Each pair is invalid, even though the three handlers are disjoint.
        ret
    TryStart:
        nop
    CatchStart:
        pop
    FinallyStart:
        endfinally
    FaultStart:
        endfault
    End:
        .try TryStart to CatchStart catch [System.Runtime]System.Exception handler CatchStart to FinallyStart
        .try TryStart to CatchStart finally handler FinallyStart to FaultStart
        .try TryStart to CatchStart fault handler FaultStart to End
    }

    .method public static void Filter.ContainsClauseInnerFirst_Invalid_EHClauseNesting() cil managed
    {
        ret
    OuterTry:
        nop
    FilterStart:
        pop
    InnerTry:
        nop
    InnerHandler:
        endfinally
    InnerEnd:
        ldc.i4.1
        endfilter
    HandlerStart:
        pop
        ret
    End:
        .try InnerTry to InnerHandler finally handler InnerHandler to InnerEnd
        .try OuterTry to FilterStart filter FilterStart handler HandlerStart to End
    }

    .method public static void Try.OuterBeforeInner_Invalid_EHClauseOrder() cil managed
    {
        ret
    OuterTry:
        nop
    InnerTry:
        nop
    InnerHandler:
        endfinally
    OuterHandler:
        endfinally
    End:
        .try OuterTry to OuterHandler finally handler OuterHandler to End
        .try InnerTry to InnerHandler finally handler InnerHandler to OuterHandler
    }

    .method public static void Try.StartsAtCatchEntry_Invalid_TryNonEmptyStack() cil managed
    {
        .maxstack 1
    OuterTry:
        leave MethodEnd
    CatchStart:
        pop
        leave CatchEnd
    InnerHandler:
        endfinally
    CatchEnd:
        leave MethodEnd
    MethodEnd:
        ret
        .try CatchStart to InnerHandler finally handler InnerHandler to CatchEnd
        .try OuterTry to CatchStart catch [System.Runtime]System.Exception handler CatchStart to MethodEnd
    }

    .method public static void Try.SharedByCatchesAndFilter_Valid() cil managed
    {
        .maxstack 1
    TryStart:
        nop
        leave MethodEnd
    Catch1:
        pop
        leave MethodEnd
    FilterStart:
        pop
        // FE 11 in an operand is not an endfilter instruction.
        ldc.i4 0x11FE
        pop
        ldc.i4.1
        endfilter
    FilterHandler:
        pop
        leave MethodEnd
    Catch2:
        pop
        leave MethodEnd
    MethodEnd:
        ret
        .try TryStart to Catch1 catch [System.Runtime]System.InvalidOperationException handler Catch1 to FilterStart
        .try TryStart to Catch1 filter FilterStart handler FilterHandler to Catch2
        .try TryStart to Catch1 catch [System.Runtime]System.Exception handler Catch2 to MethodEnd
    }

    .method public static void Finally.BeforeNestedFilterInTable_Valid() cil managed
    {
        // I.12.4.2.5 imposes ordering for nested try regions, not nested handlers.
        // See also the JIT's fgSortEHTable: the outer handler may appear first.
        .maxstack 1
    OuterTry:
        leave MethodEnd
    FinallyStart:
        nop
    InnerTry:
        nop
        leave FinallyEnd
    FilterStart:
        pop
        ldc.i4.1
        endfilter
    HandlerStart:
        pop
        leave FinallyEnd
    FinallyEnd:
        endfinally
    MethodEnd:
        ret
        .try OuterTry to FinallyStart finally handler FinallyStart to MethodEnd
        .try InnerTry to FilterStart filter FilterStart handler HandlerStart to FinallyEnd
    }

    .method public static void Catch.BeforeNestedCatchWithSharedEnd_Valid() cil managed
    {
        .maxstack 1
    OuterTry:
        leave MethodEnd
    OuterCatch:
        pop
    InnerTry:
        leave MethodEnd
    InnerCatch:
        callvirt instance string [System.Runtime]System.Exception::get_Message()
        pop
        leave MethodEnd
    MethodEnd:
        ret
        .try OuterTry to OuterCatch catch [System.Runtime]System.Object handler OuterCatch to MethodEnd
        .try InnerTry to InnerCatch catch [System.Runtime]System.Exception handler InnerCatch to MethodEnd
    }

    .method public static void Clause.PartialOverlapContinuesVerification_Invalid_EHClauseOverlap.EHClauseOrder.EHClauseEmpty.StackUnderflow() cil managed
    {
        // The second clause overlaps the first handler, but must still be compared
        // with the third clause. The empty fourth clause and stack error are also reported.
        pop
        ret
    ConflictingTry:
        nop
    LaterTry:
        nop
    LaterHandler:
        endfinally
    LaterEnd:
        nop
    FirstTry:
        nop
    FirstTryEnd:
        nop
    ConflictingTryEnd:
        nop
    FirstHandler:
        nop
    ConflictingHandler:
        nop
    FirstHandlerEnd:
        nop
    ConflictingHandlerEnd:
        ret
        .try FirstTry to FirstTryEnd finally handler FirstHandler to FirstHandlerEnd
        .try ConflictingTry to ConflictingTryEnd finally handler ConflictingHandler to ConflictingHandlerEnd
        .try LaterTry to LaterHandler finally handler LaterHandler to LaterEnd
        .try ConflictingHandlerEnd to ConflictingHandlerEnd finally handler ConflictingHandler to ConflictingHandlerEnd
    }

    .method public static void Clause.HandlerEqualsOtherTry_Invalid_EHClauseOverlap() cil managed
    {
        // The second handler has the same bounds as the first try.
        ret
    FirstTry:
        nop
    LaterTry:
        nop
    FirstHandler:
        endfinally
    End:
        .try FirstTry to LaterTry finally handler FirstHandler to End
        .try LaterTry to FirstHandler finally handler FirstTry to LaterTry
    }

    .method public static void Filter.MissingEndfilterContinuesBranchVerification_Invalid_EHClauseFilterEnd.BranchIntoHandler() cil managed
    {
        br HandlerStart
    TryStart:
        nop
    FilterStart:
        pop
        ldc.i4.1
    HandlerStart:
        pop
        ret
    End:
        ret
        .try TryStart to FilterStart filter FilterStart handler HandlerStart to End
    }

    .method public static void Filter.MissingEndfilterFallsThroughToHandler_Invalid_EHClauseFilterEnd.FallthroughException.FallthroughIntoHandler() cil managed
    {
        .maxstack 1
    TryStart:
        leave MethodEnd
    FilterStart:
        pop
        ldc.i4.1
    HandlerStart:
        pop
        leave MethodEnd
    MethodEnd:
        ret
        .try TryStart to FilterStart filter FilterStart handler HandlerStart to MethodEnd
    }

    .method public static void Handler.EmptyContinuesStackVerification_Invalid_EHClauseEmpty.StackUnderflow() cil managed
    {
    TryStart:
        leave MethodEnd
    MethodEnd:
        pop
        ret
        .try TryStart to MethodEnd catch [System.Runtime]System.Exception handler MethodEnd to MethodEnd
    }

    .method public static void Clauses.InterleavedDisjoint_Valid() cil managed
    {
        // The overall spans intersect, but all regions are disjoint.
        // The first handler also precedes its own try in IL.
        .maxstack 1
        br FirstTry
    FirstHandler:
        pop
        leave MethodEnd
    SecondTry:
        nop
        leave MethodEnd
    FirstTry:
        nop
        leave MethodEnd
    SecondHandler:
        pop
        leave MethodEnd
    MethodEnd:
        ret
        .try FirstTry to SecondHandler catch [System.Runtime]System.Exception handler FirstHandler to SecondTry
        .try SecondTry to FirstTry catch [System.Runtime]System.Exception handler SecondHandler to MethodEnd
    }

    .method public static void Try.SharedWithNestedHandlers_Invalid_EHClauseOverlap() cil managed
    {
        // Handlers protecting the same try must be disjoint, even if one contains the other.
        ret
    TryStart:
        nop
    FirstHandler:
        pop
    SecondHandler:
        pop
    SecondHandlerEnd:
        nop
    End:
        ret
        .try TryStart to FirstHandler catch [System.Runtime]System.Exception handler FirstHandler to End
        .try TryStart to FirstHandler catch [System.Runtime]System.Exception handler SecondHandler to SecondHandlerEnd
    }

    .method public static void Handlers.SameStartWithDifferentEnds_Invalid_EHClauseOverlap() cil managed
    {
        // Whole-clause containment does not permit two handlers to share an entry.
        ret
    OuterTry:
        nop
    SharedHandler:
        pop
        nop
    InnerTry:
        nop
    InnerTryEnd:
        nop
    End:
        ret
        .try InnerTry to InnerTryEnd catch [System.Runtime]System.Exception handler SharedHandler to InnerTry
        .try OuterTry to SharedHandler catch [System.Runtime]System.Exception handler SharedHandler to End
    }

    .method public static void Clause.SplitBetweenFilterAndHandler_Invalid_EHClauseNesting() cil managed
    {
        // The inner try and handler must fit within one outer region, not their union.
        ret
    OuterTry:
        nop
    FilterStart:
        pop
    InnerTry:
        nop
    InnerTryEnd:
        ldc.i4.1
        endfilter
    OuterHandler:
        pop
    InnerHandler:
        endfinally
    InnerHandlerEnd:
        nop
    End:
        ret
        .try InnerTry to InnerTryEnd finally handler InnerHandler to InnerHandlerEnd
        .try OuterTry to FilterStart filter FilterStart handler OuterHandler to End
    }

    .method public static void Clause.ConflictsWithTwoEarlierClauses_Invalid_EHClauseOverlap.EHClauseOverlap() cil managed
    {
        // Failing the first pair must not skip another conflict involving the same clause.
        ret
    FirstTry:
        nop
    CrossingTry:
        nop
    FirstTryEnd:
        nop
    SecondTry:
        nop
    CrossingTryEnd:
        nop
    SecondTryEnd:
        nop
    FirstHandler:
        endfinally
    SecondHandler:
        endfinally
    CrossingHandler:
        endfinally
    End:
        ret
        .try FirstTry to FirstTryEnd finally handler FirstHandler to SecondHandler
        .try SecondTry to SecondTryEnd finally handler SecondHandler to CrossingHandler
        .try CrossingTry to CrossingTryEnd finally handler CrossingHandler to End
    }

    .method public static void Filter.NestedInFinally_Valid() cil managed
    {
        .maxstack 1

        .try
        {
            nop
            leave MethodEnd
        }
        finally
        {
            .try
            {
                nop
                leave HandlerEnd
            }
            filter
            {
                isinst [System.Runtime]System.Exception
                pop
                ldc.i4.1
                endfilter
            }
            {
                pop
                leave HandlerEnd
            }
        HandlerEnd:
            endfinally
        }
    MethodEnd:
        ret
    }

    .method public static void Filter.BranchToEnclosingFinally_Invalid_BranchOutOfFilter() cil managed
    {
        .maxstack 1

        .try
        {
            leave MethodEnd
        }
        finally
        {
            .try
            {
                leave HandlerEnd
            }
            filter
            {
                pop
                // The target belongs to the enclosing finally and has no FilterIndex.
                br HandlerEnd
                ldc.i4.1
                endfilter
            }
            {
                pop
                leave HandlerEnd
            }
        HandlerEnd:
            endfinally
        }
    MethodEnd:
        ret
    }

    .method public static void Filter.NestedInFault_Valid() cil managed
    {
        .maxstack 1

        .try
        {
            nop
            leave MethodEnd
        }
        fault
        {
            .try
            {
                nop
                leave HandlerEnd
            }
            filter
            {
                pop
                ldc.i4.1
                endfilter
            }
            {
                pop
                leave HandlerEnd
            }
        HandlerEnd:
            endfault
        }
    MethodEnd:
        ret
    }

    .method public static void Filter.NestedInCatchWithoutCast_Invalid_StackUnexpected() cil managed
    {
        .maxstack 1

        .try
        {
            nop
            leave MethodEnd
        }
        catch [System.Runtime]System.InvalidOperationException
        {
            pop
            .try
            {
                nop
                leave MethodEnd
            }
            filter
            {
                // This is object, not the enclosing catch's exception type.
                callvirt instance string [System.Runtime]System.Exception::get_Message()
                pop
                ldc.i4.1
                endfilter
            }
            {
                pop
                leave MethodEnd
            }
        }
    MethodEnd:
        ret
    }

    .method public static void Filter.SharesFinallyStart_Invalid_EHClauseOverlap() cil managed
    {
        .maxstack 1

    OuterTry:
        nop
        leave MethodEnd
    SharedStart:
        pop
        ldc.i4.1
        endfilter
    InnerHandler:
        pop
        leave FinallyEnd
    InnerTry:
        nop
        leave FinallyEnd
    FinallyEnd:
        endfinally
    MethodEnd:
        ret

        // The filter and the outer finally cannot share their entry point.
        .try InnerTry to FinallyEnd filter SharedStart handler InnerHandler to InnerTry
        .try OuterTry to SharedStart finally handler SharedStart to MethodEnd
    }

    .method public static void Filter.SharesUnreachableFinallyStart_Invalid_EHClauseOverlap() cil managed
    {
        .maxstack 1

        // The conflicting entry point must be rejected without importing it.
        ret
    OuterTry:
        nop
        leave MethodEnd
    SharedStart:
        pop
        ldc.i4.1
        endfilter
    InnerHandler:
        pop
        leave FinallyEnd
    InnerTry:
        nop
        leave FinallyEnd
    FinallyEnd:
        endfinally
    MethodEnd:
        ret

        .try InnerTry to FinallyEnd filter SharedStart handler InnerHandler to InnerTry
        .try OuterTry to SharedStart finally handler SharedStart to MethodEnd
    }
}
