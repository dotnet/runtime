// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly ReturnTests
{
}

.module ReturnTests.dll
.custom instance void [System.Runtime]System.Runtime.CompilerServices.RefSafetyRulesAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 )

.class public sequential ansi sealed beforefieldinit Struct
        extends [System.Runtime]System.ValueType
{
    .field public int32 instanceField
}

.class public sequential ansi sealed beforefieldinit IntCarrier
        extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public int32 Value
}

.class interface public abstract auto ansi IRefMutator
{
    .method public hidebysig newslot abstract virtual instance void Capture(int32& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
    }
}

.class public sequential ansi sealed beforefieldinit RefCarrier
        extends [System.Runtime]System.ValueType implements IRefMutator
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public int32& Item

    .method public hidebysig specialname rtspecialname instance void .ctor(int32& input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig instance void 'special.CarrierConstructor..ctor_Valid'(int32& input) cil managed { ret }

    .method public hidebysig newslot virtual final instance void Return.Capture_Valid(int32& input) cil managed
    {
        .override method instance void IRefMutator::Capture(int32&)
        .param [1]
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        stfld int32& RefCarrier::Item
        ret
    }
}

.class public sequential ansi sealed beforefieldinit RefPairCarrier
        extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public int32& First
    .field public int32& Second
}

.class public sequential ansi sealed beforefieldinit OuterCarrier
        extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public valuetype RefCarrier A
    .field public valuetype RefCarrier B
}

.class public sequential ansi sealed beforefieldinit UnscopedAccessor
        extends [System.Runtime]System.ValueType
{
    .field public int32 _value

    .method public hidebysig specialname instance int32& get_Value() cil managed
    {
        ldarg.0
        ldflda int32 UnscopedAccessor::_value
        ret
    }

    .property instance int32& Value()
    {
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        .get instance int32& UnscopedAccessor::get_Value()
    }

    .method public hidebysig instance int32& 'special.UnscopedGetter.get_Value_Valid'() cil managed { ret }
}

.class public sequential ansi sealed beforefieldinit InvalidRefCarrier
        extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public valuetype RefCarrier& Item
}

.class public auto ansi sealed CaptureDelegate extends [System.Runtime]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor(object target, native int fn) runtime managed {}
    .method public hidebysig newslot virtual instance void Invoke(valuetype RefCarrier& destination, int32& source) runtime managed
    {
        .param [2]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.ScopedRefAttribute::.ctor() = ( 01 00 00 00 )
    }
}

.class public auto ansi beforefieldinit ReturnTestsType
        extends [System.Runtime]System.Object
{
    .field private int32 instanceField
    .field private static int32 staticField
    .field private static valuetype RefCarrier invalidStaticCarrier

    .field private valuetype Struct instanceStruct

    .method public hidebysig static valuetype RefCarrier Return.ConstructCarrier_Valid(int32& input) cil managed
    {
        ldarg.0
        newobj instance void RefCarrier::.ctor(int32&)
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.ConstructLocalEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32)
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.DirectConstructorEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldloca.s 1
        ldloca.s 0
        call instance void RefCarrier::.ctor(int32&)
        ldloc.1
        ret
    }

    .method public hidebysig static void Return.ConstructorOutputEscape_Invalid_Unverifiable(valuetype RefCarrier& destination, int32& input) cil managed
    {
        ldarg.0
        ldarg.1
        call instance void RefCarrier::.ctor(int32&)
        ret
    }

    .method public hidebysig static void Return.ConstructorOutput_Valid(valuetype RefCarrier& destination, int32& input) cil managed
    {
        .param [2]
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        call instance void RefCarrier::.ctor(int32&)
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.FilterMutationEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        .try
        {
            newobj instance void [System.Runtime]System.Exception::.ctor()
            throw
        }
        filter
        {
            pop
            ldloca.s 1
            ldloca.s 0
            stfld int32& RefCarrier::Item
            ldc.i4.1
            endfilter
        }
        {
            pop
            leave.s done
        }
    done:
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.NestedFaultEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        .try
        {
            .try
            {
                newobj instance void [System.Runtime]System.Exception::.ctor()
                throw
            }
            fault
            {
                ldloca.s 1
                ldloca.s 0
                stfld int32& RefCarrier::Item
                endfinally
            }
        }
        catch [System.Runtime]System.Exception
        {
            pop
            leave.s done
        }
    done:
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier& Return.InteriorReference_Valid(valuetype OuterCarrier& input) cil managed
    {
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        ret
    }

    .method public hidebysig static valuetype OuterCarrier Return.InteriorResetEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype OuterCarrier)
        ldloca.s 1
        ldflda valuetype RefCarrier OuterCarrier::B
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloca.s 1
        call valuetype RefCarrier& ReturnTestsType::Return.InteriorReference_Valid(valuetype OuterCarrier&)
        initobj RefCarrier
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype OuterCarrier Return.InteriorStoreEscape_Invalid_ReturnPtrToStack(valuetype RefCarrier input) cil managed
    {
        .locals init ([0] int32, [1] valuetype OuterCarrier)
        ldloca.s 1
        ldflda valuetype RefCarrier OuterCarrier::B
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloca.s 1
        call valuetype RefCarrier& ReturnTestsType::Return.InteriorReference_Valid(valuetype OuterCarrier&)
        ldarg.0
        stobj RefCarrier
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier& Return.FirstReference_Valid(valuetype RefCarrier& first, valuetype RefCarrier& second) cil managed
    {
        ldarg.0
        ret
    }

    .method public hidebysig static valuetype RefCarrier& Return.SecondReference_Valid(valuetype RefCarrier& first, valuetype RefCarrier& second) cil managed
    {
        ldarg.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.UntrackedFirstEscape_Invalid_ReturnPtrToStack(valuetype OuterCarrier input) cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        ldloca.s 1
        call valuetype RefCarrier& ReturnTestsType::Return.FirstReference_Valid(valuetype RefCarrier&, valuetype RefCarrier&)
        initobj RefCarrier
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.UntrackedSecondEscape_Invalid_ReturnPtrToStack(valuetype OuterCarrier input) cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloca.s 1
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        call valuetype RefCarrier& ReturnTestsType::Return.SecondReference_Valid(valuetype RefCarrier&, valuetype RefCarrier&)
        initobj RefCarrier
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype IntCarrier Return.RefLikeIdentity_Valid(valuetype IntCarrier input) cil managed
    {
        ldarg.0
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.RefLikeCopy_Valid(valuetype RefCarrier input) cil managed
    {
        .locals init ([0] valuetype RefCarrier)
        ldarg.0
        stloc.0
        ldloc.0
        ret
    }

    .method public hidebysig static valuetype [System.Runtime]System.Span`1<!!T>
        Return.SpanIdentity_Valid<T>(valuetype [System.Runtime]System.Span`1<!!T> input) cil managed
    {
        ldarg.0
        ret
    }

    .method public hidebysig static valuetype IntCarrier Return.InitializedCarrier_Valid(int32 input) cil managed
    {
        .locals init ([0] valuetype IntCarrier)
        ldloca.s 0
        initobj IntCarrier
        ldloca.s 0
        ldarg.0
        stfld int32 IntCarrier::Value
        ldloc.0
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.DefaultCarrier_Valid() cil managed
    {
        .locals init ([0] valuetype RefCarrier)
        ldloc.0
        ret
    }

    .method public hidebysig static int32& Return.ByRefCopy_Valid(int32& input) cil managed
    {
        .locals init ([0] int32&)
        ldarg.0
        stloc.0
        ldloc.0
        ret
    }

    .method public hidebysig static int32& Return.ByRefLocalCopy_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] int32&)
        ldloca.s 0
        stloc.1
        ldloc.1
        ret
    }

    .method public hidebysig static int32& Return.ReassignedArgument_Invalid_ReturnPtrToStack(int32& input) cil managed
    {
        .locals init ([0] int32)
        ldloca.s 0
        starg.s 0
        ldarg.0
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.LocalPayloadEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldloca.s 1
        initobj RefCarrier
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.CopiedPayloadEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier, [2] valuetype RefCarrier)
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloc.1
        stloc.2
        ldloc.2
        ret
    }

    .method public hidebysig static void Return.StaticCarrierEscape_Invalid_Unverifiable(valuetype RefCarrier input) cil managed
    {
        ldarg.0
        stsfld valuetype RefCarrier ReturnTestsType::invalidStaticCarrier
        ret
    }

    .method public hidebysig static valuetype InvalidRefCarrier Return.InvalidRefField_Invalid_Unverifiable(valuetype RefCarrier& input) cil managed
    {
        .locals init ([0] valuetype InvalidRefCarrier)
        ldloca.s 0
        ldarg.0
        stfld valuetype RefCarrier& InvalidRefCarrier::Item
        ldloc.0
        ret
    }

    .method public hidebysig static int32& Return.LocalCarrierField_Invalid_ReturnPtrToStack(valuetype IntCarrier input) cil managed
    {
        ldarga.s 0
        ldflda int32 IntCarrier::Value
        ret
    }

    .method public hidebysig static int32& Return.IncomingRefField_Valid(valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static !!T Return.GenericRefLikeIdentity_Valid<byreflike T>(!!T input) cil managed
    {
        ldarg.0
        ret
    }

    .method public hidebysig static !!T Return.GenericScopedEscape_Invalid_ReturnPtrToStack<byreflike T>(!!T input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.ScopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ret
    }

    .method public hidebysig static object Return.BoxGenericRefLike_Invalid_BoxByRef<byreflike T>(!!T input) cil managed
    {
        ldarg.0
        box !!T
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.BuildCarrier_Valid(int32& input) cil managed
    {
        .locals init ([0] valuetype RefCarrier)
        ldloca.s 0
        ldarg.0
        stfld int32& RefCarrier::Item
        ldloc.0
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.CallCarrier_Valid(int32& input) cil managed
    {
        ldarg.0
        call valuetype RefCarrier ReturnTestsType::Return.BuildCarrier_Valid(int32&)
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.CallLocalCarrier_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32)
        ldloca.s 0
        call valuetype RefCarrier ReturnTestsType::Return.BuildCarrier_Valid(int32&)
        ret
    }

    .method public hidebysig static int32& Return.RefIdentity_Valid(int32& input) cil managed
    {
        ldarg.0
        ret
    }

    .method public hidebysig static int32& Return.CallLocalReference_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32)
        ldloca.s 0
        call int32& ReturnTestsType::Return.RefIdentity_Valid(int32&)
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.BranchCarrier_Valid(bool choose, valuetype RefCarrier a, valuetype RefCarrier b) cil managed
    {
        ldarg.0
        brtrue.s take_a
        ldarg.2
        br.s done
    take_a:
        ldarg.1
    done:
        ret
    }

    .method public hidebysig static int32& Return.BranchReference_Invalid_ReturnPtrToStack(bool choose, int32& input) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        brtrue.s take_local
        ldarg.1
        br.s done
    take_local:
        ldloca.s 0
    done:
        ret
    }

    .method public hidebysig static int32& Return.ReverseBranchReference_Invalid_ReturnPtrToStack(bool choose, int32& input) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        brtrue.s take_input
        ldloca.s 0
        br.s done
    take_input:
        ldarg.1
    done:
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.ResetCarrier_Valid() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloca.s 1
        initobj RefCarrier
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.IndirectCarrier_Valid(valuetype RefCarrier input) cil managed
    {
        .locals init ([0] valuetype RefCarrier)
        ldloca.s 0
        ldarg.0
        stobj RefCarrier
        ldloca.s 0
        ldobj RefCarrier
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.CopyObjectEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier, [2] valuetype RefCarrier)
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloca.s 2
        ldloca.s 1
        cpobj RefCarrier
        ldloc.2
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.MutatedArgument_Invalid_ReturnPtrToStack(valuetype RefCarrier input) cil managed
    {
        .locals init ([0] int32)
        ldarga.s 0
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldarg.0
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.CopyObjectTypeMismatch_Invalid_StackUnexpected() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldloca.s 1
        ldloca.s 0
        cpobj RefCarrier
        ldloc.1
        ret
    }

    .method public hidebysig static void Return.StoreCarrier_Valid(valuetype RefCarrier& destination, int32& source) cil managed
    {
        .param [2]
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        stfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.CallMutationEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldloca.s 1
        ldloca.s 0
        call void ReturnTestsType::Return.StoreCarrier_Valid(valuetype RefCarrier&, int32&)
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.DelegateScopeMismatch_Invalid_Unverifiable() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldnull
        ldftn void ReturnTestsType::Return.StoreCarrier_Valid(valuetype RefCarrier&, int32&)
        newobj instance void CaptureDelegate::.ctor(object, native int)
        ldloca.s 1
        ldloca.s 0
        call instance void CaptureDelegate::Invoke(valuetype RefCarrier&, int32&)
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.ConstrainedMutationEscape_Invalid_ReturnPtrToStack(valuetype RefCarrier input) cil managed
    {
        .locals init ([0] int32)
        ldarga.s 0
        ldloca.s 0
        constrained. RefCarrier
        callvirt instance void IRefMutator::Capture(int32&)
        ldarg.0
        ret
    }

    .method public hidebysig static !!T Return.OpenConstrainedDispatch_Invalid_Unverifiable<byreflike (IRefMutator) T>(!!T& input, int32& source) cil managed
    {
        .param [2]
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        constrained. !!T
        callvirt instance void IRefMutator::Capture(int32&)
        ldarg.0
        ldobj !!T
        ret
    }

    .method public hidebysig static int32& Return.ScopedEscape_Invalid_ReturnPtrToStack(int32& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.ScopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.ScopedValueEscape_Invalid_ReturnPtrToStack(valuetype RefCarrier input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.ScopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ret
    }

    .method public hidebysig static valuetype [System.Runtime]System.Span`1<int32> Return.ArraySpan_Valid(int32[] input) cil managed
    {
        ldarg.0
        newobj instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(!0[])
        ret
    }

    .method public hidebysig static valuetype [System.Runtime]System.Span`1<int32> Return.SliceSpan_Valid(valuetype [System.Runtime]System.Span`1<int32> input) cil managed
    {
        ldarga.s 0
        ldc.i4.0
        call instance valuetype [System.Runtime]System.Span`1<!0> valuetype [System.Runtime]System.Span`1<int32>::Slice(int32)
        ret
    }

    .method public hidebysig static valuetype [System.Runtime]System.ReadOnlySpan`1<int32> Return.ReadOnlySpan_Valid(valuetype [System.Runtime]System.Span`1<int32> input) cil managed
    {
        ldarg.0
        call valuetype [System.Runtime]System.ReadOnlySpan`1<!0> valuetype [System.Runtime]System.Span`1<int32>::op_Implicit(valuetype [System.Runtime]System.Span`1<!0>)
        ret
    }

    .method public hidebysig static string Return.SpanToString_Valid(valuetype [System.Runtime]System.Span`1<int32> input) cil managed
    {
        ldarga.s 0
        call instance string valuetype [System.Runtime]System.Span`1<int32>::ToString()
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.FinallyEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        .try
        {
            leave.s done
        }
        finally
        {
            ldloca.s 1
            ldloca.s 0
            stfld int32& RefCarrier::Item
            endfinally
        }
    done:
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.CatchCarrier_Valid(valuetype RefCarrier input) cil managed
    {
        .try
        {
            leave.s done
        }
        catch [System.Runtime]System.Exception
        {
            pop
            leave.s done
        }
    done:
        ldarg.0
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.AliasCarrier_Valid(valuetype RefCarrier input) cil managed
    {
        .locals init ([0] valuetype RefCarrier, [1] valuetype RefCarrier&)
        ldloca.s 0
        stloc.1
        ldloc.1
        ldarg.0
        stobj RefCarrier
        ldloc.0
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.AliasEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier, [2] valuetype RefCarrier&)
        ldloca.s 1
        stloc.2
        ldloc.2
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.UnknownAliasEscape_Invalid_ReturnPtrToStack(bool choose) cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier, [2] valuetype RefCarrier)
        ldarg.0
        brtrue.s take_first
        ldloca.s 2
        br.s store
    take_first:
        ldloca.s 1
    store:
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier& Return.SelectCarrierReference_Valid(valuetype RefCarrier& first, valuetype RefCarrier& second, bool choose) cil managed
    {
        ldarg.2
        brtrue.s take_first
        ldarg.1
        ret
    take_first:
        ldarg.0
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.UnknownReadAfterStoreEscape_Invalid_ReturnPtrToStack(bool choose) cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier, [2] valuetype RefCarrier, [3] valuetype RefCarrier&)
        ldloca.s 1
        ldloca.s 2
        ldarg.0
        call valuetype RefCarrier& ReturnTestsType::Return.SelectCarrierReference_Valid(valuetype RefCarrier&, valuetype RefCarrier&, bool)
        stloc.3
        ldloc.3
        ldobj RefCarrier
        pop
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloc.3
        ldobj RefCarrier
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.UnknownReadAfterReset_Valid(bool choose) cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier, [2] valuetype RefCarrier, [3] valuetype RefCarrier&)
        ldloca.s 1
        ldloca.s 2
        ldarg.0
        call valuetype RefCarrier& ReturnTestsType::Return.SelectCarrierReference_Valid(valuetype RefCarrier&, valuetype RefCarrier&, bool)
        stloc.3
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloc.3
        ldobj RefCarrier
        pop
        ldloca.s 1
        initobj RefCarrier
        ldloca.s 2
        initobj RefCarrier
        ldloc.3
        ldobj RefCarrier
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.UnknownReadAcrossBlocksEscape_Invalid_ReturnPtrToStack(bool choose, bool keep) cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier, [2] valuetype RefCarrier, [3] valuetype RefCarrier&)
        ldloca.s 1
        ldloca.s 2
        ldarg.0
        call valuetype RefCarrier& ReturnTestsType::Return.SelectCarrierReference_Valid(valuetype RefCarrier&, valuetype RefCarrier&, bool)
        stloc.3
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldarg.1
        brtrue.s tainted
        ldloca.s 1
        initobj RefCarrier
        ldloca.s 2
        initobj RefCarrier
        ldloc.3
        ldobj RefCarrier
        ret
    tainted:
        ldloc.3
        ldobj RefCarrier
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.UnknownLocalAliasWithOutput_Valid(bool choose, [out] valuetype RefCarrier& output) cil managed
    {
        .locals init ([0] valuetype RefCarrier, [1] valuetype RefCarrier)
        ldloca.s 0
        ldloca.s 1
        ldarg.0
        call valuetype RefCarrier& ReturnTestsType::Return.SelectCarrierReference_Valid(valuetype RefCarrier&, valuetype RefCarrier&, bool)
        ldobj RefCarrier
        ldarg.1
        initobj RefCarrier
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.SelfLoopEscape_Invalid_ReturnPtrToStack(bool repeat, valuetype RefCarrier input) cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier, [2] valuetype RefCarrier)
        ldarg.1
        stloc.1
    again:
        ldloc.1
        stloc.2
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldarg.0
        brtrue.s again
        ldloc.2
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.EntryLoopEscape_Invalid_ReturnPtrToStack(bool repeat, valuetype RefCarrier input) cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
    again:
        ldarg.1
        stloc.1
        ldarga.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldarg.0
        brtrue.s again
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.ScopedCarrierValue_Valid(valuetype RefCarrier& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.ScopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldobj RefCarrier
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.CallScopedCarrier_Valid(valuetype RefCarrier input) cil managed
    {
        ldarga.s 0
        call valuetype RefCarrier ReturnTestsType::Return.ScopedCarrierValue_Valid(valuetype RefCarrier&)
        ret
    }

    .method public hidebysig static valuetype RefCarrier& Return.CarrierReference_Valid(valuetype RefCarrier& input) cil managed
    {
        ldarg.0
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.ResetThroughCall_Valid() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldloca.s 1
        call valuetype RefCarrier& ReturnTestsType::Return.CarrierReference_Valid(valuetype RefCarrier&)
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloca.s 1
        initobj RefCarrier
        ldloc.1
        ret
    }

    .method public hidebysig static void Return.OutCarrier_Valid([out] valuetype RefCarrier& destination, int32& source) cil managed
    {
        ldarg.0
        ldarg.1
        stfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static void Return.OutReadBeforeWrite_Invalid_Unverifiable([out] valuetype RefCarrier& destination) cil managed
    {
        ldarg.0
        ldfld int32& RefCarrier::Item
        pop
        ldarg.0
        initobj RefCarrier
        ret
    }

    .method public hidebysig static void Return.OutPair_Valid([out] valuetype RefPairCarrier& destination, int32& first, int32& second) cil managed
    {
        ldarg.0
        ldarg.1
        stfld int32& RefPairCarrier::First
        ldarg.0
        ldarg.2
        stfld int32& RefPairCarrier::Second
        ret
    }

    .method public hidebysig static void Return.PartialOutPair_Invalid_Unverifiable([out] valuetype RefPairCarrier& destination, int32& first) cil managed
    {
        ldarg.0
        ldarg.1
        stfld int32& RefPairCarrier::First
        ret
    }

    .method public hidebysig static void Return.BranchOutPair_Invalid_Unverifiable(bool choose, [out] valuetype RefPairCarrier& destination, int32& source) cil managed
    {
        ldarg.0
        brtrue.s first
        ldarg.1
        ldarg.2
        stfld int32& RefPairCarrier::Second
        br.s done
    first:
        ldarg.1
        ldarg.2
        stfld int32& RefPairCarrier::First
    done:
        ret
    }

    .method public hidebysig static void Return.MalformedScoped_Invalid_Unverifiable(int32& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.ScopedRefAttribute::.ctor() = ( 01 00 01 00 )
        ret
    }

    .method public hidebysig static void Return.StoreThenThrow_Valid(valuetype RefCarrier& destination, int32& source) cil managed
    {
        .param [2]
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        stfld int32& RefCarrier::Item
        newobj instance void [System.Runtime]System.Exception::.ctor()
        throw
    }

    .method public hidebysig static valuetype RefCarrier Return.ExceptionalMutationEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        .try
        {
            ldloca.s 1
            ldloca.s 0
            call void ReturnTestsType::Return.StoreThenThrow_Valid(valuetype RefCarrier&, int32&)
            leave.s done
        }
        catch [System.Runtime]System.Exception
        {
            pop
            leave.s done
        }
    done:
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.IncomingAliasEscape_Invalid_Unverifiable.ReturnPtrToStack(
        valuetype RefCarrier& a, valuetype RefCarrier& b) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldarg.1
        ldobj RefCarrier
        ret
    }

    .method public hidebysig instance void Return.Void_Valid() cil managed
    {
        ldarg.0
        pop
        ret
    }

    .method public hidebysig instance void Return.VoidNonEmptyStack_Invalid_ReturnVoid() cil managed
    {
        ldarg.0
        ret
    }

    .method public hidebysig instance int32 Return.Int32_Valid() cil managed
    {
        ldc.i4.0
        ret
    }

    .method public hidebysig instance int32 Return.Int32EmptyStack_Invalid_ReturnMissing() cil managed
    {
        ret
    }

    .method public hidebysig instance int32 Return.Int32StackLeft_Invalid_ReturnEmpty() cil managed
    {
        ldc.i4.0
        ldc.i4.0
        ret
    }

    .method public hidebysig instance void Return.FromTry_Invalid_ReturnFromTry() cil managed
    {
        .try
        {
            ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s     lbl_ret
        }

    lbl_ret:
        ret
    }

    .method public hidebysig instance void Return.FromCatch_Invalid_ReturnFromHandler() cil managed
    {
        .try
        {
            leave.s     lbl_ret
        }
        catch [System.Runtime]System.Object
        {
            pop
            ret
        }

    lbl_ret:
        ret
    }

    .method public hidebysig instance void Return.FromFilter_Invalid_ReturnFromFilter() cil managed
    {
        .try
        {
            leave.s     lbl_ret
        }
        filter
        {
            pop
            ret

            endfilter
        }
        {
            pop
            leave.s     lbl_ret
        }

    lbl_ret:
        ret
    }

    .method public hidebysig instance int32 Return.ObjectForInt32_Invalid_StackUnexpected(object o) cil managed
    {
        ldarg.1
        ret
    }

    .method public hidebysig instance int32& Return.LocalArgByRef_Invalid_ReturnPtrToStack(int32 i) cil managed
    {
        ldarga.s    i
        ret
    }

    .method public hidebysig instance int32& Return.InstanceFieldByRef_Valid(int32 i) cil managed
    {
        ldarg.0
        ldflda      int32 ReturnTestsType::instanceField
        ret
    }

    .method public hidebysig instance int32& Return.StaticFieldByRef_Valid(int32 i) cil managed
    {
        ldsflda      int32 ReturnTestsType::staticField
        ret
    }

    .method public hidebysig instance int32& Return.LocalStructFieldByRef_Invalid_ReturnPtrToStack(valuetype Struct s) cil managed
    {
        ldarg.1
        ldflda      int32 Struct::instanceField
        ret
    }

    .method public hidebysig instance int32& Return.FieldStructFieldByRef_Valid() cil managed
    {
        ldarg.0
        ldflda      valuetype Struct ReturnTestsType::instanceStruct
        ldflda      int32 Struct::instanceField
        ret
    }

    .method public hidebysig instance void MissingReturn_Invalid_MethodFallthrough() cil managed
    {
        ldarg.0
    }

    .method public hidebysig instance void 'special.ReturnAfterBaseCtor..ctor_Valid'() cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed 
    {
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig instance void 'special.ReturnBeforeBaseCtor..ctor_Invalid_ThisUninitReturn'(int32) cil managed { ret }
    .method public hidebysig specialname rtspecialname instance void .ctor(int32) cil managed 
    {
        ret
    }
}
