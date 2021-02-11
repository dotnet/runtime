// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly BranchingTests
{
}

// Custom Comparer class implementing IEqualityComparer for testing
.class private auto ansi beforefieldinit CustomComparer
    extends [System.Runtime]System.Object
    implements [System.Runtime]System.Collections.IEqualityComparer
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call      instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public final hidebysig newslot virtual 
        instance bool Equals (
            object x,
            object y
        ) cil managed 
    {
        ldc.i4.0
        ret
    }

    .method public final hidebysig newslot virtual 
        instance int32 GetHashCode (
            object obj
        ) cil managed 
    {
        ldarg.1
        callvirt  instance int32 [System.Runtime]System.Object::GetHashCode()
        ret
    }
}

.class public auto ansi beforefieldinit BranchingTestsType
       extends [System.Runtime]System.Object
{
    // Volatile field for testing
    .field private static int32 modreq([System.Runtime]System.Runtime.CompilerServices.IsVolatile) volatileField

    .method static public hidebysig void StaticMethod() cil managed
    {
        ret
    }

    .method static public hidebysig void Branching.NullConditional_Valid() cil managed
    {
        //object o = null;
        //Type t = o != null ? o.GetType() : null;
        //Type.GetTypeCode(t);

        .maxstack 1
        .locals init (
            [0] object o
        )

        IL_0000: ldnull
        IL_0001: stloc.0
        IL_0002: ldloc.0
        IL_0003: brtrue.s  IL_0008

        IL_0005: ldnull
        IL_0006: br.s      IL_000E

        IL_0008: ldloc.0
        IL_0009: callvirt  instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()

        IL_000E: call      valuetype [System.Runtime]System.TypeCode [System.Runtime]System.Type::GetTypeCode(class [System.Runtime]System.Type)
        IL_0013: pop
        IL_0014: ret
    }

    .method static public hidebysig void Branching.NullConditional_Invalid_StackUnexpected() cil managed
    {
        //object o = null;
        //Type t = o != null ? o.GetType() : o;
        //Type.GetTypeCode(t);

        .maxstack 1
        .locals init (
            [0] object o
        )

        IL_0000: ldnull
        IL_0001: stloc.0
        IL_0002: ldloc.0
        IL_0003: brtrue.s  IL_0008

        IL_0005: ldloc.0
        IL_0006: br.s      IL_000E

        IL_0008: ldloc.0
        IL_0009: callvirt  instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()

        IL_000E: call      valuetype [System.Runtime]System.TypeCode [System.Runtime]System.Type::GetTypeCode(class [System.Runtime]System.Type)
        IL_0013: pop
        IL_0014: ret
    }

    .method static public hidebysig void Branching.UnequalStackMerge_Valid(class [System.Runtime]System.Collections.IEqualityComparer equalityComparer,
        class CustomComparer customComparer) cil managed
    {
        .maxstack 1
        .locals init(
            [0] class [System.Runtime]System.Collections.IEqualityComparer
        )

        ldc.i4.0
        brfalse.s   lbl_cComp

        ldarg.0
        br.s        lbl_store

    lbl_cComp:
        ldarg.1

    lbl_store:
        stloc.0
        ret
    }

    .method static public hidebysig void Branching.UnequalStackMergeConstrained_Valid<([System.Runtime]System.Collections.IEqualityComparer) T>
        (class [System.Runtime]System.Collections.IEqualityComparer equalityComparer, !!T customComparer) cil managed
    {
        .maxstack 1
        .locals init(
            [0] class [System.Runtime]System.Collections.IEqualityComparer
        )

        ldc.i4.0
        brfalse.s   lbl_cComp

        ldarg.0
        br.s        lbl_store

    lbl_cComp:
        ldarg.1
        box     !!T

    lbl_store:
        stloc.0
        ret
    }

    .method static public hidebysig void Branching.UnequalStackMergeArrays_Valid(class CustomComparer[] comparers,
        class [System.Runtime]System.Collections.IEqualityComparer[] customComparer) cil managed
    {
        .maxstack 1
        .locals init(
            [0] class [System.Runtime]System.Collections.IEqualityComparer[]
        )

        ldarg.0
        brfalse.s   lbl_cComp

        ldarg.0
        br.s        lbl_store

    lbl_cComp:
        ldarg.1

    lbl_store:
        stloc.0
        ret
    }

    .method static public hidebysig void Branching.UnequalStackMergeArrays_Invalid_StackUnexpected(class CustomComparer[] comparers,
        class [System.Runtime]System.Collections.IEqualityComparer[] customComparer) cil managed
    {
        .maxstack 1
        .locals init(
            [0] class CustomComparer[]
        )

        ldarg.0
        brfalse.s   lbl_cComp

        ldarg.0
        br.s        lbl_store

    lbl_cComp:
        ldarg.1

    lbl_store:
        stloc.0
        ret
    }

    .method static public hidebysig void Branching.UnequalStackMergeGeneric_Invalid_PathStackUnexpected<T>(!!T generic,
        object obj) cil managed
    {
        .maxstack 1
        .locals init(
            [0] object obj
        )

        ldarg.1
        brfalse.s   lbl_gen

        ldarg.1
        br.s        lbl_store

    lbl_gen:
        ldarg.0

    lbl_store:
        stloc.0
        ret
    }

    .method static public hidebysig void Branching.UnequalStackMergeGenericArrayConstrain_Valid<(int32[]) T>(!!T generic,
        int32[] arr) cil managed
    {
        .maxstack 1
        .locals init(
            [0] int32[] dest
        )

        ldarg.1
        brfalse.s   lbl_arr

        ldarg.0
        box     !!T
        br.s        lbl_store

    lbl_arr:
        ldarg.1

    lbl_store:
        stloc.0
        ret
    }

    .method static public hidebysig void Branching.InsideTry_Valid() cil managed
    {
        .maxstack 2

        .try
        {
            ldc.i4.s  10
            ldc.i4.s  5
            bne.un.s  lbl_true

            nop
            br.s      lbl_leave

            lbl_true: nop

            lbl_leave: leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Branching.OutOfTry_Invalid_BranchOutOfTry() cil managed
    {
        .maxstack 2

        .try
        {
            ldc.i4.s  10
            ldc.i4.s  5
            bne.un.s  lbl_true

            nop
            br.s      lbl_ret

            lbl_true: nop

            lbl_leave: leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Branching.IntoTryStart_Valid() cil managed
    {
        .maxstack 2

        ldc.i4.s  10
        ldc.i4.s  5
        bne.un.s  lbl_true

        nop
        br.s      lbl_ret

        .try
        {
            lbl_true: nop

            leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Branching.IntoTryMiddle_Invalid_BranchIntoTry() cil managed
    {
        .maxstack 2

        ldc.i4.s  10
        ldc.i4.s  5
        bne.un.s  lbl_true

        nop
        br.s      lbl_ret

        .try
        {
            nop
            lbl_true: nop

            leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Branching.IntoNestedTryStart_Valid() cil managed
    {
        .maxstack 2

        .try
        {
            ldc.i4.s  10
            ldc.i4.s  5
            bne.un.s  lbl_true

            nop
            br.s      lbl_leave
            .try
            {
                lbl_true: nop

                leave.s   lbl_leave
            }
            catch [System.Runtime]System.Object
            {
                pop
                leave.s   lbl_leave
            }

            lbl_leave: leave.s lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Branching.IntoDoubleNestedTryStart_Invalid_BranchIntoTry() cil managed
    {
        .try
        {
            nop
            br    Inner

            .try
            {
                nop

                .try
                {
                    Inner:
                    leave.s    A
                }
                catch [System.Runtime]System.Object 
                {
                    pop
                    leave.s    A
                }
                A:

                leave.s    B
            }
            catch [System.Runtime]System.Object 
            {
                pop
                leave.s    B
            }
            B:

            leave.s    C
        }
        catch [System.Runtime]System.Object 
        {
            pop
            leave.s    C
        }
        C:

        ret
    }

    .method static public hidebysig void Branching.IntoDoubleNestedTryStartStart_Valid() cil managed
    {
        .try
        {
            nop
            br    Inner

            .try
            {
                .try
                {
                    Inner:
                    leave.s    A
                }
                catch [System.Runtime]System.Object 
                {
                    pop
                    leave.s    A
                }
                A:

                leave.s    B
            }
            catch [System.Runtime]System.Object 
            {
                pop
                leave.s    B
            }
            B:

            leave.s    C
        }
        catch [System.Runtime]System.Object 
        {
            pop
            leave.s    C
        }
        C:

        ret
    }

    .method static public hidebysig void Branching.IntoDoubleNestedTryStartFromOutside_Invalid_BranchIntoTry() cil managed
    {
        br    Inner

        .try
        {
            nop

            .try
            {
                Inner:
                leave.s    A
            }
            catch [System.Runtime]System.Object 
            {
                pop
                leave.s    A
            }
            A:

            leave.s    B
        }
        catch [System.Runtime]System.Object 
        {
            pop
            leave.s    B
        }
        B:

        ret
    }

    .method static public hidebysig void Branching.IntoTryStartWithPredecessor_Valid() cil managed
    {
        .try
        {
            nop
            br    Inner

            .try
            {
                leave.s    A
            }
            catch [System.Runtime]System.Object 
            {
                pop
                leave.s    A
            }

            .try
            {
                Inner:
                leave.s    A
            }
            catch [System.Runtime]System.Object 
            {
                pop
                leave.s    A
            }

            A:

            leave.s    C
        }
        catch [System.Runtime]System.Object 
        {
            pop
            leave.s    C
        }
        C:

        ret
    }

    .method static public hidebysig void Branching.IntoNestedTryMiddle_Invalid_BranchIntoTry() cil managed
    {
        .maxstack 2

        .try
        {
            ldc.i4.s  10
            ldc.i4.s  5
            bne.un.s  lbl_true

            nop
            br.s      lbl_leave
            .try
            {
                nop
                lbl_true: nop

                leave.s   lbl_leave
            }
            catch [System.Runtime]System.Object
            {
                pop
                leave.s   lbl_leave
            }

            lbl_leave: leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Branching.FromTryIntoCatch_Invalid_BranchOutOfTry.BranchIntoHandler() cil managed
    {
        .maxstack 2

        .try
        {
            ldc.i4.s  10
            ldc.i4.s  5
            bne.un.s  lbl_true

            nop
            leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            lbl_true: nop
            pop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Branching.FromTryIntoFilter_Invalid_BranchOutOfTry.BranchIntoFilter() cil managed
    {
        .try
        {
            br.s  lbl_filter
        }
        filter
        {
            lbl_filter: pop
            ldc.i4.0
            endfilter
        } 
        { 
            pop
            leave.s lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Branching.FromTryIntoFinally_Invalid_BranchOutOfTry.BranchIntoHandler() cil managed
    {
        .maxstack 2

        .try
        {
            ldc.i4.s  10
            ldc.i4.s  5
            bne.un.s  lbl_finally

            nop
            leave.s   lbl_ret
        }
        finally
        {
            lbl_finally: nop
            endfinally
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Branching.OutOfFinally_Invalid_BranchOutOfFinally() cil managed
    {
        .maxstack 2

        .try
        {
            leave.s   lbl_ret
        }
        finally
        {
            br  lbl_ret
            endfinally
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Branching.FromTryIntoOtherTryStart_Invalid_BranchOutOfTry() cil managed
    {
        .maxstack 2

        .try
        {
            ldc.i4.s  10
            ldc.i4.s  5
            bne.un.s  lbl_try2

            nop
            leave.s   lbl_try
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_try
        }

        lbl_try: nop
        .try
        {
            lbl_try2: nop
            leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Branching.OutOfTrySameStart_Invalid_BranchOutOfTry() cil managed
    {
        .try
        {
            .try
            {
                br    A

                leave.s    A
            }
            catch [System.Runtime]System.Object 
            {
                pop
                leave.s    A
            }
            A:

            leave.s    B
        }
        catch [System.Runtime]System.Object 
        {
            pop
            leave.s    B
        }
        B:

        ret
    }

    .method static public hidebysig void Branching.FromTryIntoOtherTryMiddle_Invalid_BranchOutOfTry() cil managed
    {
        .maxstack 2

        .try
        {
            ldc.i4.s  10
            ldc.i4.s  5
            bne.un.s  lbl_try2

            nop
            leave.s   lbl_try
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_try
        }

        lbl_try: nop
        .try
        {
            nop
            lbl_try2: leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Fallthrough.OutOfTryIntoFinally_Invalid_FallthroughException.FallthroughIntoHandler() cil managed
    {
        .try
        {
            nop
        }
        finally
        {
            endfinally
        }
    }

    .method static public hidebysig void Fallthrough.OutOfTryIntoCatch_Invalid_FallthroughException.FallthroughIntoHandler() cil managed
    {
        .try
        {
            nop
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s   lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Fallthrough.OutOfCatch_Invalid_FallthroughException() cil managed
    {
        .try
        {
            nop
            leave.s   lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Fallthrough.OutOfTryIntoFilter_Invalid_FallthroughException.FallthroughIntoFilter() cil managed
    {
        .try
        {
            nop
        }
        filter
        {
            pop
            ldc.i4.0
            endfilter
        } 
        { 
            pop
            leave.s lbl_ret
        }

        lbl_ret: ret
    }

    .method static public hidebysig void Branch.BeforeReadonlyInstruction_Valid(object[] objectArray) cil managed
    {
        // objectArray[0];

        ldarg.0
        ldc.i4.0
        br          BeforeInstr

    BeforeInstr:
        readonly.
        ldelema     [System.Runtime]System.Object
        pop
        ret
    }

    .method static public hidebysig void Branch.IntoReadonlyInstruction_Invalid_BadJumpTarget(object[] objectArray) cil managed
    {
        // objectArray[0];

        ldarg.0
        ldc.i4.0
        br          MidInstr

        readonly.
    MidInstr:
        ldelema     [System.Runtime]System.Object
        pop
        ret
    }

    .method static public hidebysig void Branch.IntoConstrainedInstruction_Invalid_BadJumpTarget<T>(!!T arg) cil managed
    {
        // arg.ToString();
        ldarga.s    arg
        br          MidInstr

        constrained. !!T
    MidInstr:
        callvirt    instance string [System.Runtime]System.Object::ToString()
        pop
        ret
    }

    .method static public hidebysig void Branch.AfterConstrainedInstruction_Valid<T>(!!T arg) cil managed
    {
        // arg.ToString();
        ldarga.s    arg
        br          AfterInstr

        constrained. !!T
        callvirt    instance string [System.Runtime]System.Object::ToString()
    AfterInstr:
        pop
        ret
    }

    .method static public hidebysig void Branch.IntoVolatileInstruction_Invalid_BadJumpTarget() cil managed
    {
        // volatileField = 0;

        ldc.i4.0
        br          MidInstr

        volatile.
    MidInstr:
        stsfld    int32 modreq([System.Runtime]System.Runtime.CompilerServices.IsVolatile) BranchingTestsType::volatileField
        ret
    }

    .method static public hidebysig void Branch.IntoUnalignedInstruction_Invalid_BadJumpTarget() cil managed 
    {
        .locals init (
            int32&
        )

        ldloc.0
        br      MidInstr

        unaligned. 4
    MidInstr:
        ldind.i4
        pop
        ret
    }

    .method static public hidebysig void Branch.IntoUnalignedVolatileInstruction_Invalid_BadJumpTarget() cil managed 
    {
        .locals init (
            int32&
        )

        ldloc.0
        br      MidInstr

        unaligned. 4
        volatile.
    MidInstr:
        ldind.i4
        pop
        ret
    }

    .method static public hidebysig void Branch.IntoTailInstruction_Invalid_BadJumpTarget() cil managed 
    {
        br      MidInstr

        tail.
    MidInstr:
        call    void BranchingTestsType::StaticMethod()
        ret
    }

    .method static public hidebysig void Branch.BackwardWithPredecessor_Valid() cil managed 
    {
        ldnull
        ldc.i4.0
        brfalse     MethodEnd

    BackwardBranch:
        pop

        ldnull
        ldc.i4.1
        brtrue      BackwardBranch

    MethodEnd:
        pop
        ret
    }

    .method static public hidebysig void Branch.BackwardWithoutPredecessor_Invalid_BackwardBranch() cil managed 
    {
        br      MethodEnd

    BackwardBranch:
        pop

    MethodEnd:
        ldnull
        ldc.i4.1
        brtrue      BackwardBranch

        pop
        ret
    }

    .method static public hidebysig void BrFalseS.Backward_Valid() cil managed 
    {
    BackwardBranch:
        // Note: additional nops to increase branch delta
        nop
        nop
        ldc.i4.0
        brfalse.s   BackwardBranch

        ret
    }

    .method static public hidebysig void Branch.BackwardEmptyStack_Valid() cil managed 
    {
        br      MethodEnd

    BackwardBranch:
        nop

    MethodEnd:
        ldc.i4.1
        brtrue      BackwardBranch

        ret
    }

    .method static public hidebysig void Branch.BackwardWithComplexPredecessor_Valid() cil managed 
    {
        ldnull
        br          MethodEnd

    Predecessor:
        ldnull

    BackwardBranch:
        pop

        ldnull
        ldc.i4.1
        brtrue      BackwardBranch

    MethodEnd:
        pop
        ldc.i4.1
        brtrue          Predecessor

        ret
    }
}
