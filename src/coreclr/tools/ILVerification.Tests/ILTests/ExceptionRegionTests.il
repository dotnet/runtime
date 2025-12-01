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

        .try {}
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
}
