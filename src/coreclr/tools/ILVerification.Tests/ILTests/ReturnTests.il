// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly extern System.Private.CoreLib
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

    .method public hidebysig instance void Return.Round5706ReadonlyStructWrite_Invalid_StackUnexpected() cil managed
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldc.i4.1
        stfld int32 Struct::instanceField
        ret
    }
}

.class public sequential ansi sealed beforefieldinit IntCarrier
        extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public int32 Value

    .method public hidebysig instance int32 Return.Round5706ReadonlyScalarForward_Valid() cil managed
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldflda int32 IntCarrier::Value
        call int32 ReturnTestsType::Return.Round5706ReadScalar_Valid(int32&)
        ret
    }
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

    .method public hidebysig specialname instance int32& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) get_Item(int32 index) cil managed
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        .param [0]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldfld int32& RefCarrier::Item
        ret
    }

    .property instance int32& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) ItemAt(int32)
    {
        .get instance int32& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) RefCarrier::get_Item(int32)
    }

    .method public hidebysig instance int32& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) 'special.Round5706ReadonlyIndexer.get_Item_Valid'(int32 index) cil managed { ret }

    .method public hidebysig instance void Return.Followup5705ReadonlyReceiverToRefReadonly_Valid() cil managed
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        call void ReturnTestsType::Return.Followup5705RefReadonlyRead_Valid(valuetype RefCarrier&)
        ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor(int32& input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig instance void 'special.CarrierConstructor..ctor_Valid'(int32& input) cil managed { ret }

    .method public hidebysig instance void Return.TypedReferenceThisEscape_Invalid_Unverifiable() cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        mkrefany RefCarrier
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call void ReturnTestsType::Return.StoreTypedCarrier_Invalid_Unverifiable(typedref, valuetype RefCarrier)
        ret
    }

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

    .method public hidebysig instance void Return.Review5705CopySecondToFirst_Valid() cil managed
    {
        ldarg.0
        ldarg.0
        ldfld int32& RefPairCarrier::Second
        stfld int32& RefPairCarrier::First
        ret
    }

    .method public hidebysig instance void Return.Review5705ReadonlyReceiverWrite_Invalid_StackUnexpected(int32& input) cil managed
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        .param [1]
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        stfld int32& RefPairCarrier::First
        ret
    }

    .method public hidebysig instance void Return.Review5705ReadonlyReceiverMutableCall_Invalid_ReadOnlyIllegalWrite() cil managed
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        call instance void RefPairCarrier::Return.Review5705CopySecondToFirst_Valid()
        ret
    }
}

.class public sequential ansi sealed beforefieldinit OuterCarrier
        extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public valuetype RefCarrier A
    .field public valuetype RefCarrier B

    .method public hidebysig specialname rtspecialname instance void .ctor(valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld valuetype RefCarrier OuterCarrier::A
        ldarg.0
        ldarg.0
        ldfld valuetype RefCarrier OuterCarrier::A
        stfld valuetype RefCarrier OuterCarrier::B
        ret
    }

    .method public hidebysig instance void 'special.CopyingConstructor..ctor_Valid'(valuetype RefCarrier input) cil managed { ret }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        initobj RefCarrier
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::B
        initobj RefCarrier
        ret
    }

    .method public hidebysig instance void 'special.Review5705FieldInitConstructor..ctor_Valid'() cil managed { ret }

    .method public hidebysig specialname rtspecialname instance void .ctor(int32 mode) cil managed
    {
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        initobj RefCarrier
        ret
    }

    .method public hidebysig instance void 'special.Review5705FieldInitConstructorSibling..ctor_Invalid_Unverifiable'(int32 mode) cil managed { ret }
}

.class public sequential ansi sealed beforefieldinit NestedOuterCarrier
        extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public valuetype OuterCarrier Value
}

.class public sequential ansi sealed beforefieldinit ReadonlyCarrier
        extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .field public int32& Item

    .method public hidebysig instance void Return.Review5705ReadonlyOwnerWrite_Invalid_StackUnexpected(int32& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        stfld int32& ReadonlyCarrier::Item
        ret
    }
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

.class public sequential ansi sealed beforefieldinit SpanPairCarrier
        extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public valuetype [System.Runtime]System.Span`1<int32> A
    .field public valuetype [System.Runtime]System.Span`1<int32> B

    .method public hidebysig specialname rtspecialname instance void .ctor(valuetype [System.Runtime]System.Span`1<int32> input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld valuetype [System.Runtime]System.Span`1<int32> SpanPairCarrier::A
        ldarg.0
        ldarg.0
        ldflda valuetype [System.Runtime]System.Span`1<int32> SpanPairCarrier::A
        ldc.i4.1
        call instance valuetype [System.Runtime]System.Span`1<!0> valuetype [System.Runtime]System.Span`1<int32>::Slice(int32)
        stfld valuetype [System.Runtime]System.Span`1<int32> SpanPairCarrier::B
        ret
    }

    .method public hidebysig instance void 'special.SliceAssignedConstructorField..ctor_Valid'(valuetype [System.Runtime]System.Span`1<int32> input) cil managed { ret }
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

.class interface public abstract auto ansi ITypedReferenceConsumer
{
    .method public hidebysig newslot abstract virtual instance void Consume(typedref input) cil managed {}
}

.class public auto ansi sealed TypedReferenceDelegate extends [System.Runtime]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor(object target, native int fn) runtime managed {}
    .method public hidebysig newslot virtual instance void Invoke(typedref input) runtime managed {}
}

.class interface public abstract auto ansi IFollowupReader
{
    .method public hidebysig newslot abstract virtual instance void Read([in] valuetype RefCarrier& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = ( 01 00 00 00 )
    }
}

.class public auto ansi sealed FollowupReader extends [System.Runtime]System.Object
{
    .method public hidebysig newslot virtual final instance void Read([in] valuetype RefCarrier& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) input) cil managed
    {
        .override method instance void IFollowupReader::Read(valuetype RefCarrier& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute))
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.1
        call void ReturnTestsType::Return.Followup5705RefReadonlyRead_Valid(valuetype RefCarrier&)
        ret
    }

    .method public hidebysig instance void 'special.Followup5705VirtualRefReadonlyForward.Read_Valid'([in] valuetype RefCarrier& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) input) cil managed { ret }
}

.class public sequential ansi sealed beforefieldinit FollowupSetterCarrier extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public valuetype RefCarrier A
    .field public valuetype RefCarrier B
    .field public bool Flag

    .method public hidebysig instance void SetA(valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld valuetype RefCarrier FollowupSetterCarrier::A
        ret
    }

    .method public hidebysig instance void SetB(valuetype RefCarrier input) cil managed
    {
        nop
        ldarg.0
        ldarg.1
        stfld valuetype RefCarrier FollowupSetterCarrier::B
        ret
    }

    .method public hidebysig instance void SetFlag(bool input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld bool FollowupSetterCarrier::Flag
        ret
    }

    .method public hidebysig instance void SetWrongA(valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld valuetype RefCarrier FollowupSetterCarrier::B
        ret
    }

    .method public hidebysig instance void SetNontrivial(valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld valuetype RefCarrier FollowupSetterCarrier::A
        ldarg.0
        ldfld valuetype RefCarrier FollowupSetterCarrier::B
        pop
        ret
    }

    .method public hidebysig instance void SetReadingOther(valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldarg.0
        ldfld valuetype RefCarrier FollowupSetterCarrier::B
        stfld valuetype RefCarrier FollowupSetterCarrier::A
        ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor(valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldc.i4.1
        call instance void FollowupSetterCarrier::SetFlag(bool)
        ldarg.0
        ldarg.1
        call instance void FollowupSetterCarrier::SetA(valuetype RefCarrier)
        ldarg.0
        ldarg.1
        call instance void FollowupSetterCarrier::SetB(valuetype RefCarrier)
        ret
    }

    .method public hidebysig instance void 'special.Followup5705SetterConstructor..ctor_Valid'(valuetype RefCarrier input) cil managed { ret }
}

.class public sequential ansi sealed beforefieldinit FollowupGenericSetterCarrier`1<byreflike T> extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public !T Value

    .method public hidebysig instance void Set(!T input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld !T valuetype FollowupGenericSetterCarrier`1<!T>::Value
        ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor(!T input) cil managed
    {
        ldarg.0
        ldarg.1
        call instance void valuetype FollowupGenericSetterCarrier`1<!T>::Set(!0)
        ret
    }

    .method public hidebysig instance void 'special.Followup5705GenericSetterConstructor..ctor_Valid'(!T input) cil managed { ret }
}

.class public sequential ansi sealed beforefieldinit FollowupDisposableCarrier`1<byreflike T>
        extends [System.Runtime]System.ValueType implements [System.Runtime]System.IDisposable
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public valuetype RefCarrier Item

    .method public hidebysig newslot virtual final instance void Dispose() cil managed
    {
        .override method instance void [System.Runtime]System.IDisposable::Dispose()
        ret
    }
}

.class interface public abstract auto ansi IFollowupChoice`1<T>
{
    .method public hidebysig newslot abstract virtual instance void Clear() cil managed {}
}

.class public sequential ansi sealed beforefieldinit FollowupAmbiguousCarrier`2<T, U>
        extends [System.Runtime]System.ValueType
        implements class IFollowupChoice`1<!T>, class IFollowupChoice`1<!U>
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public valuetype RefCarrier Item

    .method private hidebysig newslot virtual final instance void ClearFirst() cil managed
    {
        .override method instance void class IFollowupChoice`1<!T>::Clear()
        ret
    }

    .method private hidebysig newslot virtual final instance void ClearSecond() cil managed
    {
        .override method instance void class IFollowupChoice`1<!U>::Clear()
        ret
    }
}

.class public sequential ansi sealed beforefieldinit Round5706InitCarrier extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    .field public initonly valuetype RefCarrier Value

    .method public hidebysig specialname instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) set_Value(valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld valuetype RefCarrier Round5706InitCarrier::Value
        ldarg.0
        ldfld valuetype RefCarrier Round5706InitCarrier::Value
        pop
        ret
    }

    .method public hidebysig instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) 'special.Round5706InitAccessorBody.set_Value_Valid'(valuetype RefCarrier input) cil managed { ret }
}

.class public sequential ansi sealed beforefieldinit Round5706ReadonlyFieldCarrier extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public int32& Item
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )

    .method public hidebysig specialname rtspecialname instance void .ctor([in] int32& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        stfld int32& Round5706ReadonlyFieldCarrier::Item
        ret
    }

    .method public hidebysig instance void 'special.Round5706ReadonlyRefFieldConstructor..ctor_Valid'([in] int32& input) cil managed { ret }
}

.class interface public abstract auto ansi IRound5706ScalarReader
{
    .method public hidebysig newslot abstract virtual instance int32 Read([in] int32& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
    }
}

.class public auto ansi sealed Round5706ScalarReader extends [System.Runtime]System.Object implements IRound5706ScalarReader
{
    .method public hidebysig newslot virtual final instance int32 Read([in] int32& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) input) cil managed
    {
        .override method instance int32 IRound5706ScalarReader::Read(int32& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute))
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.1
        ldind.i4
        ret
    }
}

.class public auto ansi beforefieldinit Round776WriterBase extends [System.Runtime]System.Object
{
    .method public hidebysig newslot virtual instance valuetype [System.Runtime]System.Span`1<int32> GetSpan() cil managed
    {
        .locals init ([0] valuetype [System.Runtime]System.Span`1<int32>)
        ldloc.0
        ret
    }
}

.class public auto ansi sealed Round776SealedWriter extends Round776WriterBase
{
    .method public hidebysig virtual instance valuetype [System.Runtime]System.Span`1<int32> GetSpan() cil managed
    {
        .locals init ([0] valuetype [System.Runtime]System.Span`1<int32>)
        ldloc.0
        ret
    }
}

.class public auto ansi beforefieldinit ReturnTestsType
        extends [System.Runtime]System.Object
{
    .field private int32 instanceField
    .field private static int32 staticField
    .field private static valuetype RefCarrier invalidStaticCarrier

    .field private valuetype Struct instanceStruct

    .method private hidebysig static void Round776StringPair(string left, string right) cil managed
    {
        ret
    }

    .method public hidebysig static void Return.Round776DuplicateArgumentErrors_Invalid_StackUnexpected.StackUnexpected() cil managed
    {
        ldc.i4.0
        ldc.i4.0
        call void ReturnTestsType::Round776StringPair(string, string)
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.Round776DuplicateArgumentRevisit_Invalid_StackUnexpected.StackUnexpected.ReturnPtrToStack(bool repeat, valuetype RefCarrier input, int32 source) cil managed
    {
    again:
        ldarg.0
        brtrue.s mutate
        ldc.i4.0
        ldc.i4.0
        call void ReturnTestsType::Round776StringPair(string, string)
        ldarg.1
        ret
    mutate:
        ldarga.s 1
        ldarga.s 2
        stfld int32& RefCarrier::Item
        br.s again
    }

    .method public hidebysig static valuetype [System.Runtime]System.Span`1<int32> Return.Round776SealedVirtualResult_Valid(class Round776SealedWriter writer) cil managed
    {
        ldarg.0
        callvirt instance valuetype [System.Runtime]System.Span`1<int32> Round776SealedWriter::GetSpan()
        ret
    }

    .method public hidebysig static valuetype [System.Runtime]System.Span`1<int32> Return.Round776BaseDeclaredVirtual_Invalid_Unverifiable(class Round776SealedWriter writer) cil managed
    {
        ldarg.0
        callvirt instance valuetype [System.Runtime]System.Span`1<int32> Round776WriterBase::GetSpan()
        ret
    }

    .method public hidebysig static void Return.Round5706ReadPair_Valid([in] valuetype RefPairCarrier& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldfld int32& RefPairCarrier::Second
        pop
        ret
    }

    .method public hidebysig static void Return.Round5706MergedOutputReadonlyRead_Invalid_Unverifiable(bool choose, [out] valuetype RefPairCarrier& first, [out] valuetype RefPairCarrier& second) cil managed
    {
        ldarg.0
        brtrue.s takeFirst
        ldarg.2
        br.s selected
    takeFirst:
        ldarg.1
    selected:
        call void ReturnTestsType::Return.Round5706ReadPair_Valid(valuetype RefPairCarrier&)
        ldarg.1
        initobj RefPairCarrier
        ldarg.2
        initobj RefPairCarrier
        ret
    }

    .method public hidebysig static void Return.Round5706MergedInitializedReadonlyRead_Valid(bool choose, [out] valuetype RefPairCarrier& first, [out] valuetype RefPairCarrier& second) cil managed
    {
        ldarg.1
        initobj RefPairCarrier
        ldarg.2
        initobj RefPairCarrier
        ldarg.0
        brtrue.s takeFirst
        ldarg.2
        br.s selected
    takeFirst:
        ldarg.1
    selected:
        call void ReturnTestsType::Return.Round5706ReadPair_Valid(valuetype RefPairCarrier&)
        ret
    }

    .method public hidebysig static void Return.Round5706WeakMergedOutputReadonlyRead_Invalid_Unverifiable(bool choose, [out] valuetype RefPairCarrier& first, [out] valuetype RefPairCarrier& second) cil managed
    {
        .locals init ([0] valuetype RefPairCarrier&)
        .try
        {
            ldarg.0
            brtrue.s takeFirst
            ldarg.2
            br.s selected
        takeFirst:
            ldarg.1
        selected:
            stloc.0
            ldloc.0
            call void ReturnTestsType::Return.Round5706ReadPair_Valid(valuetype RefPairCarrier&)
            leave.s done
        }
        finally
        {
            endfinally
        }
    done:
        ldarg.1
        initobj RefPairCarrier
        ldarg.2
        initobj RefPairCarrier
        ret
    }

    .method public hidebysig static void Return.Round5706ReadonlyObjectWrite_Invalid_ReadOnlyIllegalWrite([in] object& destination, object replacement, valuetype RefCarrier& other) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        stind.ref
        ret
    }

    .method public hidebysig static void Return.Round5706CovariantReadonlyCaller_Valid(object[] input) cil managed
    {
        .locals init ([0] valuetype RefCarrier)
        ldarg.0
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Object
        newobj instance void [System.Runtime]System.Object::.ctor()
        ldloca.s 0
        call void ReturnTestsType::Return.Round5706ReadonlyObjectWrite_Invalid_ReadOnlyIllegalWrite(object&, object, valuetype RefCarrier&)
        ret
    }

    .method public hidebysig static void Return.Round5706ReadonlyScalarWrite_Invalid_ReadOnlyIllegalWrite([in] int32& destination) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldc.i4.1
        stind.i4
        ret
    }

    .method public hidebysig static void Return.Round5706ReadonlyScalarAliasWrite_Invalid_ReadOnlyIllegalWrite([in] int32& destination) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = ( 01 00 00 00 )
        .locals init ([0] int32&)
        ldarg.0
        stloc.0
        ldloc.0
        ldc.i4.1
        stind.i4
        ret
    }

    .method public hidebysig static void Return.Round5706ReboundReadonlyWrite_Invalid_ReadOnlyIllegalWrite([in] object& destination, object& other) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.1
        starg.s 0
        ldarg.0
        ldnull
        stind.ref
        ret
    }

    .method public hidebysig static void Return.Round5706ReadonlyParameterAddress_Invalid_ByrefOfByref([in] int32& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarga.s 0
        pop
        ret
    }

    .method public hidebysig static void Return.Round5706BareInScalarWrite_Valid([in] int32& destination) cil managed
    {
        ldarg.0
        ldc.i4.1
        stind.i4
        ret
    }

    .method public hidebysig static valuetype Round5706InitCarrier Return.Round5706InitCallerLocalEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype Round5706InitCarrier)
        ldloca.s 1
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call instance void modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit) Round5706InitCarrier::set_Value(valuetype RefCarrier)
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier& Return.Round5706ReadonlyIdentity_Valid([in] valuetype RefCarrier& input) cil managed
    {
        .param [0]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ret
    }

    .method public hidebysig static valuetype RefCarrier& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) Return.Round5706ReadonlyModifierIdentity_Valid([in] valuetype RefCarrier& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ret
    }

    .method public hidebysig static int32& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) Return.Round5706ReadonlyScalarIdentity_Valid([in] int32& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ret
    }

    .method public hidebysig static valuetype RefCarrier& Return.Round5706WritableReturnFromReadonly_Invalid_StackUnexpected([in] valuetype RefCarrier& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ret
    }

    .method public hidebysig static void Return.Round5706ReadonlyResultWrite_Invalid_ReadOnlyIllegalWrite(valuetype RefCarrier& input, valuetype RefCarrier replacement) cil managed
    {
        ldarg.0
        call valuetype RefCarrier& ReturnTestsType::Return.Round5706ReadonlyIdentity_Valid(valuetype RefCarrier&)
        ldarg.1
        stobj RefCarrier
        ret
    }

    .method public hidebysig static void Return.Round5706ReadonlyScalarResultWrite_Invalid_ReadOnlyIllegalWrite(int32& input) cil managed
    {
        ldarg.0
        call int32& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) ReturnTestsType::Return.Round5706ReadonlyScalarIdentity_Valid(int32&)
        ldc.i4.1
        stind.i4
        ret
    }

    .method public hidebysig static void Return.Round5706ConditionalDefaultThenFields_Valid(bool condition, [out] valuetype RefPairCarrier& output, int32& input) cil managed
    {
        ldarg.0
        brfalse.s assigned
        ldarg.1
        initobj RefPairCarrier
    assigned:
        ldarg.1
        ldarg.2
        stfld int32& RefPairCarrier::First
        ldarg.1
        ldarg.2
        stfld int32& RefPairCarrier::Second
        ret
    }

    .method public hidebysig static void Return.Round5706ConditionalDefaultLocalEscape_Invalid_Unverifiable.Unverifiable(bool condition, [out] valuetype RefPairCarrier& output, int32& input) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        brfalse.s safe
        ldarg.1
        initobj RefPairCarrier
        ldarg.1
        ldloca.s 0
        stfld int32& RefPairCarrier::Second
        br.s assigned
    safe:
        ldarg.1
        ldarg.2
        stfld int32& RefPairCarrier::Second
    assigned:
        ldarg.1
        ldarg.2
        stfld int32& RefPairCarrier::First
        ret
    }

    .method public hidebysig static int32 Return.Round5706ReadScalar_Valid([in] int32& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldind.i4
        ret
    }

    .method public hidebysig static int32 Return.Round5706ReadonlyPrefixScalarForward_Valid(int32[] input) cil managed
    {
        ldarg.0
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int32
        call int32 ReturnTestsType::Return.Round5706ReadScalar_Valid(int32&)
        ret
    }

    .method public hidebysig static void Return.Round5706ReadonlyRefFieldWrite_Invalid_ReadOnlyIllegalWrite(valuetype Round5706ReadonlyFieldCarrier input) cil managed
    {
        ldarg.0
        ldfld int32& Round5706ReadonlyFieldCarrier::Item
        ldc.i4.1
        stind.i4
        ret
    }

    .method public hidebysig static void Return.Round5706ReadonlyIndexerResultWrite_Invalid_ReadOnlyIllegalWrite(valuetype RefCarrier& input) cil managed
    {
        ldarg.0
        ldc.i4.0
        call instance int32& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) RefCarrier::get_Item(int32)
        ldc.i4.1
        stind.i4
        ret
    }

    .method public hidebysig static int32 Return.Round5706ScalarVirtualPrefix_Valid(class Round5706ScalarReader reader, int32[] input) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int32
        callvirt instance int32 Round5706ScalarReader::Read(int32& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute))
        ret
    }

    .method public hidebysig static void Return.Followup5705RefReadonlyRead_Valid([in] valuetype RefCarrier& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldobj RefCarrier
        pop
        ret
    }

    .method public hidebysig static void Return.Followup5705InToRefReadonly_Valid([in] valuetype RefCarrier& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        call void ReturnTestsType::Return.Followup5705RefReadonlyRead_Valid(valuetype RefCarrier&)
        ret
    }

    .method public hidebysig static void Return.Followup5705RefReadonlyForward_Valid([in] valuetype RefCarrier& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        call void ReturnTestsType::Return.Followup5705RefReadonlyRead_Valid(valuetype RefCarrier&)
        ret
    }

    .method public hidebysig static void Return.Followup5705RefReadonlyVirtualCaller_Valid(class FollowupReader reader, [in] valuetype RefCarrier& input) cil managed
    {
        .param [2]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        callvirt instance void FollowupReader::Read(valuetype RefCarrier& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute))
        ret
    }

    .method public hidebysig static void Return.Followup5705RefReadonlyWrite_Invalid_StackUnexpected([in] valuetype RefCarrier& destination, int32& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = ( 01 00 00 00 )
        .param [2]
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        stfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static void Return.Followup5705BareInWrite_Valid([in] valuetype RefCarrier& destination, int32& input) cil managed
    {
        .param [2]
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        stfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static void Return.Followup5705BareInCaptureEscape_Invalid_Unverifiable(valuetype RefCarrier& destination) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        ldloca.s 0
        call void ReturnTestsType::Return.Followup5705BareInWrite_Valid(valuetype RefCarrier&, int32&)
        ret
    }

    .method public hidebysig static void Return.Followup5705SetterOutput_Valid([out] valuetype FollowupSetterCarrier& output, valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldarg.1
        call instance void FollowupSetterCarrier::SetA(valuetype RefCarrier)
        ldarg.0
        ldarg.1
        call instance void FollowupSetterCarrier::SetB(valuetype RefCarrier)
        ret
    }

    .method public hidebysig static void Return.Followup5705WrongFieldSetterOutput_Invalid_Unverifiable([out] valuetype FollowupSetterCarrier& output, valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldarg.1
        call instance void FollowupSetterCarrier::SetWrongA(valuetype RefCarrier)
        ldarg.0
        ldarg.1
        stfld valuetype RefCarrier FollowupSetterCarrier::B
        ret
    }

    .method public hidebysig static void Return.Followup5705NontrivialSetterOutput_Invalid_Unverifiable.Unverifiable([out] valuetype FollowupSetterCarrier& output, valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldarg.1
        call instance void FollowupSetterCarrier::SetNontrivial(valuetype RefCarrier)
        ret
    }

    .method public hidebysig static void Return.Followup5705ReadingSetterOutput_Invalid_Unverifiable.Unverifiable([out] valuetype FollowupSetterCarrier& output, valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld valuetype RefCarrier FollowupSetterCarrier::B
        ldarg.0
        ldarg.1
        call instance void FollowupSetterCarrier::SetReadingOther(valuetype RefCarrier)
        ret
    }

    .method public hidebysig static valuetype FollowupSetterCarrier Return.Followup5705SetterLocalEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype FollowupSetterCarrier)
        ldloca.s 1
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call instance void FollowupSetterCarrier::SetA(valuetype RefCarrier)
        ldloc.1
        ret
    }

    .method public hidebysig static void Return.Followup5705ConcreteConstrainedDispose_Valid(valuetype FollowupDisposableCarrier`1<int32>& input) cil managed
    {
        ldarg.0
        constrained. valuetype FollowupDisposableCarrier`1<int32>
        callvirt instance void [System.Runtime]System.IDisposable::Dispose()
        ret
    }

    .method public hidebysig static void Return.Followup5705GenericConstrainedDispose_Valid<byreflike T>(valuetype FollowupDisposableCarrier`1<!!T>& input) cil managed
    {
        ldarg.0
        constrained. valuetype FollowupDisposableCarrier`1<!!T>
        callvirt instance void [System.Runtime]System.IDisposable::Dispose()
        ret
    }

    .method public hidebysig static void Return.Followup5705SplitEnumeratorFinally_Valid() cil managed
    {
        .locals init ([0] valuetype [System.Private.CoreLib]System.MemoryExtensions/SpanSplitEnumerator`1<char>)
        .try
        {
            leave.s done
        }
        finally
        {
            ldloca.s 0
            constrained. valuetype [System.Private.CoreLib]System.MemoryExtensions/SpanSplitEnumerator`1<char>
            callvirt instance void [System.Runtime]System.IDisposable::Dispose()
            endfinally
        }
    done:
        ret
    }

    .method public hidebysig static void Return.Followup5705AmbiguousConstrained_Invalid_Unverifiable(valuetype FollowupAmbiguousCarrier`2<int32, int32>& input) cil managed
    {
        ldarg.0
        constrained. valuetype FollowupAmbiguousCarrier`2<int32, int32>
        callvirt instance void class IFollowupChoice`1<int32>::Clear()
        ret
    }

    .method public hidebysig static void Return.Followup5705AmbiguousOpenConstrained_Invalid_Unverifiable<T>(valuetype FollowupAmbiguousCarrier`2<!!T, int32>& input) cil managed
    {
        ldarg.0
        constrained. valuetype FollowupAmbiguousCarrier`2<!!T, int32>
        callvirt instance void class IFollowupChoice`1<!!T>::Clear()
        ret
    }

    .method public hidebysig static void Return.Followup5705DistinctConstrained_Valid(valuetype FollowupAmbiguousCarrier`2<int32, string>& input) cil managed
    {
        ldarg.0
        constrained. valuetype FollowupAmbiguousCarrier`2<int32, string>
        callvirt instance void class IFollowupChoice`1<int32>::Clear()
        ret
    }

    .method public hidebysig static valuetype RefPairCarrier Return.Review5705ReceiverOnlyPartialOutputEscape_Invalid_Unverifiable.Unverifiable.Unverifiable.Unverifiable.Unverifiable.ReturnPtrToStack([out] valuetype RefPairCarrier& output, int32& input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld int32& RefPairCarrier::First
        ldarg.0
        call instance void RefPairCarrier::Return.Review5705CopySecondToFirst_Valid()
        ldarg.0
        ldarg.0
        ldfld int32& RefPairCarrier::First
        stfld int32& RefPairCarrier::Second
        ldarg.0
        ldobj RefPairCarrier
        ret
    }

    .method public hidebysig static void Return.Review5705ReceiverOnlyInitialized_Valid(valuetype RefPairCarrier input) cil managed
    {
        ldarga.s 0
        call instance void RefPairCarrier::Return.Review5705CopySecondToFirst_Valid()
        ret
    }

    .method public hidebysig static valuetype RefPairCarrier Return.Review5705ReceiverOnlyCallerLocalEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefPairCarrier)
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefPairCarrier::Second
        ldloca.s 1
        call instance void RefPairCarrier::Return.Review5705CopySecondToFirst_Valid()
        ldloca.s 1
        ldloca.s 1
        ldfld int32& RefPairCarrier::First
        stfld int32& RefPairCarrier::Second
        ldloc.1
        ret
    }

    .method public hidebysig static int32 Return.Review5705SpanReceiverOnly_Valid(valuetype [System.Runtime]System.Span`1<int32> input) cil managed
    {
        ldarga.s 0
        call instance void valuetype [System.Runtime]System.Span`1<int32>::Clear()
        ldarga.s 0
        call instance int32 valuetype [System.Runtime]System.Span`1<int32>::get_Length()
        ret
    }

    .method public hidebysig static valuetype [System.Runtime]System.Span`1<int32> Return.Review5705RefLocalFinally_Valid(valuetype [System.Runtime]System.Span`1<int32> input) cil managed
    {
        .locals init ([0] valuetype [System.Runtime]System.Span`1<int32>, [1] valuetype [System.Runtime]System.Span`1<int32>&)
        .try
        {
            ldarg.0
            stloc.0
            ldloca.s 0
            stloc.1
            ldloc.1
            ldobj valuetype [System.Runtime]System.Span`1<int32>
            stloc.0
            leave.s done
        }
        finally
        {
            endfinally
        }
    done:
        ldloc.0
        ret
    }

    .method public hidebysig static valuetype [System.Runtime]System.Span`1<int32> Return.Review5705RefLocalFinallyEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] valuetype [System.Runtime]System.Span`1<int32>, [1] valuetype [System.Runtime]System.Span`1<int32>&, [2] int32)
        .try
        {
            ldloca.s 2
            newobj instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(!0&)
            stloc.0
            ldloca.s 0
            stloc.1
            ldloc.1
            ldobj valuetype [System.Runtime]System.Span`1<int32>
            stloc.0
            leave.s done
        }
        finally
        {
            endfinally
        }
    done:
        ldloc.0
        ret
    }

    .method public hidebysig static void Return.Review5705FieldInitOutput_Valid([out] valuetype OuterCarrier& output) cil managed
    {
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        initobj RefCarrier
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::B
        initobj RefCarrier
        ret
    }

    .method public hidebysig static void Return.Review5705FieldStoreOutput_Valid([out] valuetype OuterCarrier& output, valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        ldarg.1
        stobj RefCarrier
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::B
        ldarg.1
        stobj RefCarrier
        ret
    }

    .method public hidebysig static void Return.Review5705FieldAddressLocalOutput_Valid([out] valuetype OuterCarrier& output) cil managed
    {
        .locals init ([0] valuetype RefCarrier&)
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        stloc.0
        ldloc.0
        initobj RefCarrier
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::B
        initobj RefCarrier
        ret
    }

    .method public hidebysig static void Return.Review5705FieldCopyOutput_Valid([out] valuetype OuterCarrier& output, valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        ldarga.s 1
        cpobj RefCarrier
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::B
        ldarga.s 1
        cpobj RefCarrier
        ret
    }

    .method public hidebysig static void Return.Review5705FieldConstructorOutput_Valid([out] valuetype OuterCarrier& output, int32& input) cil managed
    {
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        ldarg.1
        call instance void RefCarrier::.ctor(int32&)
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::B
        ldarg.1
        call instance void RefCarrier::.ctor(int32&)
        ret
    }

    .method public hidebysig static void Return.Review5705FieldInitSiblingOutput_Invalid_Unverifiable([out] valuetype OuterCarrier& output) cil managed
    {
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        initobj RefCarrier
        ret
    }

    .method public hidebysig static void Return.Review5705NestedFieldInitOutput_Invalid_Unverifiable([out] valuetype NestedOuterCarrier& output) cil managed
    {
        ldarg.0
        ldflda valuetype OuterCarrier NestedOuterCarrier::Value
        ldflda valuetype RefCarrier OuterCarrier::A
        initobj RefCarrier
        ret
    }

    .method public hidebysig static void Return.Review5705MergedFieldInitOutput_Invalid_Unverifiable(bool choose, [out] valuetype OuterCarrier& output) cil managed
    {
        ldarg.1
        ldarg.0
        brtrue.s first
        ldflda valuetype RefCarrier OuterCarrier::B
        br.s joined
    first:
        ldflda valuetype RefCarrier OuterCarrier::A
    joined:
        initobj RefCarrier
        ret
    }

    .method public hidebysig static int32& Return.Review5705TypedReferencePreservesRef_Valid(typedref input, int32& reference) cil managed
    {
        ldarga.s 0
        call instance int32 [System.Runtime]System.TypedReference::GetHashCode()
        pop
        ldarg.1
        ret
    }

    .method public hidebysig static int32& Return.Review5705CalliPreservesRef_Invalid_Unverifiable(method void *() target, int32& reference) cil managed
    {
        ldarg.0
        calli void()
        ldarg.1
        ret
    }

    .method public hidebysig static void Return.Review5705SpanCopyTo_Valid(valuetype [System.Runtime]System.Span`1<int32>& source) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        ldloca.s 0
        newobj instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(!0&)
        call instance void valuetype [System.Runtime]System.Span`1<int32>::CopyTo(valuetype [System.Runtime]System.Span`1<!0>)
        ret
    }

    .method public hidebysig static bool Return.Review5705SpanTryCopyTo_Valid(valuetype [System.Runtime]System.Span`1<int32>& source) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        ldloca.s 0
        newobj instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(!0&)
        call instance bool valuetype [System.Runtime]System.Span`1<int32>::TryCopyTo(valuetype [System.Runtime]System.Span`1<!0>)
        ret
    }

    .method public hidebysig static void Return.Review5705InParameterWrite_Invalid_StackUnexpected([in] valuetype RefCarrier& destination, int32& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        .param [2]
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        stfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static void Return.Review5705InParameterIndirectWrite_Invalid_ReadOnlyIllegalWrite([in] valuetype RefCarrier& destination, valuetype RefCarrier input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        stobj RefCarrier
        ret
    }

    .method public hidebysig static void Return.Review5705InParameterAliasWrite_Invalid_StackUnexpected([in] valuetype RefCarrier& destination, int32& input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        .param [2]
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        .locals init ([0] valuetype RefCarrier&)
        ldarg.0
        stloc.0
        ldloc.0
        ldarg.1
        stfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static void Return.Review5705InParameterInteriorRead_Valid([in] valuetype OuterCarrier& source) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        ldobj RefCarrier
        pop
        ret
    }

    .method public hidebysig static void Return.Review5705InParameterModifierWrite_Invalid_StackUnexpected([in] valuetype RefCarrier& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) destination, int32& input) cil managed
    {
        .param [2]
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldarg.1
        stfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static void Return.Review5705InParameterRead_Valid([in] valuetype RefCarrier& source, valuetype RefCarrier other) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldobj RefCarrier
        pop
        ret
    }

    .method public hidebysig static void Return.Review5705InParameterCall_Valid(valuetype RefCarrier& source) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call void ReturnTestsType::Return.Review5705InParameterRead_Valid(valuetype RefCarrier&, valuetype RefCarrier)
        ret
    }

    .method public hidebysig static int32& Return.Review5705FatalPreservesLifetime_Invalid_StackUnderflow.ReturnPtrToStack(bool choose) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        brtrue.s fatal
        ldloca.s 0
        ret
    fatal:
        pop
        ldloca.s 0
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.InOutCarrier_Valid([in][out] valuetype RefCarrier& input) cil managed
    {
        ldarg.0
        ldobj RefCarrier
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.InOutReturnEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloca.s 1
        call valuetype RefCarrier ReturnTestsType::Return.InOutCarrier_Valid(valuetype RefCarrier&)
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.InOutWritebackEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloca.s 1
        call valuetype RefCarrier ReturnTestsType::Return.InOutCarrier_Valid(valuetype RefCarrier&)
        pop
        ldloc.1
        ret
    }

    .method public hidebysig static int32& Return.TemporaryReturnedAliasEscape_Invalid_Unverifiable() cil managed
    {
        .locals init ([0] int32, [1] valuetype OuterCarrier)
        ldloc.1
        ldflda valuetype RefCarrier OuterCarrier::A
        dup
        call valuetype RefCarrier& ReturnTestsType::Return.CarrierReference_Valid(valuetype RefCarrier&)
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static int32& Return.TemporaryJoinedAliasEscape_Invalid_Unverifiable(bool choose) cil managed
    {
        .locals init ([0] int32, [1] valuetype OuterCarrier, [2] valuetype RefCarrier)
        ldloc.1
        ldflda valuetype RefCarrier OuterCarrier::A
        dup
        ldarg.0
        brtrue.s joined
        pop
        ldloca.s 2
    joined:
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static int32& Return.TemporaryReturnedAliasReset_Valid(valuetype OuterCarrier input) cil managed
    {
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        dup
        call valuetype RefCarrier& ReturnTestsType::Return.CarrierReference_Valid(valuetype RefCarrier&)
        initobj RefCarrier
        ldfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static void Return.ExceptionOutputBeforeTry_Valid([out] valuetype RefCarrier& output, valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldarg.1
        stobj RefCarrier
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
        ret
    }

    .method public hidebysig static void Return.ExceptionOutputInsideTry_Valid([out] valuetype RefCarrier& output, valuetype RefCarrier input) cil managed
    {
        .try
        {
            ldarg.0
            ldarg.1
            stobj RefCarrier
            leave.s done
        }
        finally
        {
            endfinally
        }
    done:
        ret
    }

    .method public hidebysig static void Return.ExceptionOutputFieldsBeforeTry_Valid([out] valuetype RefPairCarrier& output, int32& input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld int32& RefPairCarrier::First
        ldarg.0
        ldarg.1
        stfld int32& RefPairCarrier::Second
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
        ret
    }

    .method public hidebysig static void Return.ExceptionOutputFieldsInsideTry_Valid([out] valuetype RefPairCarrier& output, int32& input) cil managed
    {
        .try
        {
            ldarg.0
            ldarg.1
            stfld int32& RefPairCarrier::First
            ldarg.0
            ldarg.1
            stfld int32& RefPairCarrier::Second
            leave.s done
        }
        finally
        {
            endfinally
        }
    done:
        ret
    }

    .method public hidebysig static void Return.ExceptionOutputConditional_Invalid_Unverifiable(bool choose, [out] valuetype RefCarrier& output) cil managed
    {
        .try
        {
            ldarg.0
            brfalse.s skip
            ldarg.1
            initobj RefCarrier
        skip:
            leave.s done
        }
        finally
        {
            endfinally
        }
    done:
        ret
    }

    .method public hidebysig static void Return.ExceptionOutputHandlerOnly_Invalid_Unverifiable([out] valuetype RefCarrier& output) cil managed
    {
        .try
        {
            leave.s done
        }
        catch [System.Runtime]System.Exception
        {
            pop
            ldarg.0
            initobj RefCarrier
            leave.s done
        }
    done:
        ret
    }

    .method public hidebysig static void Return.ExceptionOutputFinallyMutation_Invalid_Unverifiable.Unverifiable([out] valuetype RefCarrier& output) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        initobj RefCarrier
        .try
        {
            leave.s done
        }
        finally
        {
            ldarg.0
            ldloca.s 0
            stfld int32& RefCarrier::Item
            endfinally
        }
    done:
        ret
    }

    .method public hidebysig static void Return.ExceptionOutputCatchMutation_Invalid_Unverifiable.Unverifiable([out] valuetype RefCarrier& output) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        initobj RefCarrier
        .try
        {
            leave.s done
        }
        catch [System.Runtime]System.Exception
        {
            pop
            ldarg.0
            ldloca.s 0
            stfld int32& RefCarrier::Item
            leave.s done
        }
    done:
        ret
    }

    .method public hidebysig static void Return.MultipleOutputs_Valid([out] valuetype RefCarrier& first, [out] valuetype RefCarrier& second, int32& input) cil managed
    {
        ldarg.0
        ldarg.2
        stfld int32& RefCarrier::Item
        ldarg.1
        ldarg.2
        stfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.MergedDefaultAddresses_Valid(bool choose) cil managed
    {
        .locals init ([0] valuetype RefCarrier, [1] valuetype RefCarrier)
        ldarg.0
        brtrue.s first
        ldloca.s 1
        br.s joined
    first:
        ldloca.s 0
    joined:
        ldobj RefCarrier
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.MergedLocalAddressEscape_Invalid_ReturnPtrToStack(bool choose) cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier, [2] valuetype RefCarrier)
        ldloca.s 1
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldarg.0
        brtrue.s first
        ldloca.s 2
        br.s joined
    first:
        ldloca.s 1
    joined:
        ldobj RefCarrier
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.MergedArgumentAddresses_Valid(bool choose, valuetype RefCarrier first, valuetype RefCarrier second) cil managed
    {
        ldarg.0
        brtrue.s first
        ldarga.s 2
        br.s joined
    first:
        ldarga.s 1
    joined:
        ldobj RefCarrier
        ret
    }

    .method public hidebysig static !!T Return.UnboxObject_Valid<byreflike T>(object input) cil managed
    {
        ldarg.0
        unbox.any !!T
        ret
    }

    .method public hidebysig static !!T Return.UnboxTestedObject_Valid<byreflike T>(object input) cil managed
    {
        ldarg.0
        isinst !!T
        unbox.any !!T
        ret
    }

    .method public hidebysig static void Return.TypedReferenceForward_Valid(typedref destination) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        ldloca.s 0
        newobj instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(!0&)
        call void ReturnTestsType::Return.StoreTypedSpan_Invalid_Unverifiable(typedref, valuetype [System.Runtime]System.Span`1<int32>)
        ret
    }

    .method public hidebysig static void Return.TypedReferenceForwardCaller_Valid() cil managed
    {
        .locals init ([0] valuetype [System.Runtime]System.Span`1<int32>)
        ldloca.s 0
        mkrefany valuetype [System.Runtime]System.Span`1<int32>
        call void ReturnTestsType::Return.TypedReferenceForward_Valid(typedref)
        ldloca.s 0
        call instance void valuetype [System.Runtime]System.Span`1<int32>::Clear()
        ret
    }

    .method public hidebysig static void Return.TypedReferenceStoreDefault_Valid(typedref destination) cil managed
    {
        ldarg.0
        refanyval RefCarrier
        initobj RefCarrier
        ret
    }

    .method public hidebysig static void Return.TypedReferenceStoreHeapSpan_Valid(typedref destination, int32[] input) cil managed
    {
        ldarg.0
        refanyval valuetype [System.Runtime]System.Span`1<int32>
        ldarg.1
        newobj instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(!0[])
        stobj valuetype [System.Runtime]System.Span`1<int32>
        ret
    }

    .method public hidebysig static int32 Return.TypedReferenceReceiver_Valid(typedref input) cil managed
    {
        ldarga.s 0
        call instance int32 [System.Runtime]System.TypedReference::GetHashCode()
        ret
    }

    .method public hidebysig static void Return.AddressUnassignedOutputField_Invalid_Unverifiable([out] valuetype SpanPairCarrier& output, valuetype [System.Runtime]System.Span`1<int32> input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld valuetype [System.Runtime]System.Span`1<int32> SpanPairCarrier::A
        ldarg.0
        ldflda valuetype [System.Runtime]System.Span`1<int32> SpanPairCarrier::B
        ldc.i4.1
        call instance valuetype [System.Runtime]System.Span`1<!0> valuetype [System.Runtime]System.Span`1<int32>::Slice(int32)
        pop
        ldarg.0
        initobj SpanPairCarrier
        ret
    }

    .method public hidebysig static void Return.AddressAssignedOutputField_Valid([out] valuetype OuterCarrier& output, valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld valuetype RefCarrier OuterCarrier::A
        ldarg.0
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        ldobj RefCarrier
        stfld valuetype RefCarrier OuterCarrier::B
        ret
    }

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

    .method public hidebysig static valuetype RefCarrier Return.UntrackedFirstEscape_Invalid_Unverifiable.ReturnPtrToStack(valuetype OuterCarrier input) cil managed
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

    .method public hidebysig static valuetype RefCarrier Return.UntrackedSecondEscape_Invalid_Unverifiable.ReturnPtrToStack(valuetype OuterCarrier input) cil managed
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

    .method public hidebysig static bool Return.GenericNullCheck_Valid<byreflike T>(!!T input) cil managed
    {
        ldarg.0
        box !!T
        brtrue.s not_null
        ldc.i4.1
        ret
    not_null:
        ldc.i4.0
        ret
    }

    .method public hidebysig static bool Return.GenericTypeCheck_Valid<byreflike T>(!!T input) cil managed
    {
        ldarg.0
        box !!T
        isinst [System.Runtime]System.IDisposable
        brfalse.s not_disposable
        ldc.i4.1
        ret
    not_disposable:
        ldc.i4.0
        ret
    }

    .method public hidebysig static !!T Return.GenericBoxRoundTrip_Valid<byreflike T>(!!T input) cil managed
    {
        ldarg.0
        box !!T
        unbox.any !!T
        ret
    }

    .method public hidebysig static !!T Return.GenericBoxTypeRoundTrip_Valid<byreflike T>(!!T input) cil managed
    {
        ldarg.0
        box !!T
        isinst !!T
        unbox.any !!T
        ret
    }

    .method public hidebysig static !!T Return.BoxRoundTripEscape_Invalid_ReturnPtrToStack<byreflike T>(!!T input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.ScopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        box !!T
        unbox.any !!T
        ret
    }

    .method public hidebysig static !!T Return.BoxTypeRoundTripEscape_Invalid_ReturnPtrToStack<byreflike T>(!!T input) cil managed
    {
        .param [1]
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.ScopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        box !!T
        isinst !!T
        unbox.any !!T
        ret
    }

    .method public hidebysig static object Return.GenericBoxWrongUnbox_Invalid_BoxByRef<byreflike T>(!!T input) cil managed
    {
        ldarg.0
        box !!T
        unbox.any [System.Runtime]System.Object
        ret
    }

    .method public hidebysig static !!T Return.GenericBoxWrongTypeTest_Invalid_BoxByRef<byreflike T>(!!T input) cil managed
    {
        ldarg.0
        box !!T
        isinst [System.Runtime]System.Object
        unbox.any !!T
        ret
    }

    .method public hidebysig static void Return.StoreTypedCarrier_Invalid_Unverifiable(typedref destination, valuetype RefCarrier input) cil managed
    {
        ldarg.0
        refanyval RefCarrier
        ldarg.1
        stobj RefCarrier
        ret
    }

    // This chain cannot verify as a whole: StoreTypedCarrier rejects the write through refanyval.
    .method public hidebysig static void Return.TypedReferenceCarrierForward_Valid(typedref destination) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call void ReturnTestsType::Return.StoreTypedCarrier_Invalid_Unverifiable(typedref, valuetype RefCarrier)
        ret
    }

    .method public hidebysig static void Return.TypedReferenceCarrierForwardCaller_Valid() cil managed
    {
        .locals init ([0] valuetype RefCarrier)
        ldloca.s 0
        mkrefany RefCarrier
        call void ReturnTestsType::Return.TypedReferenceCarrierForward_Valid(typedref)
        ret
    }

    .method public hidebysig static void Return.TypedReferenceParameterEscape_Invalid_Unverifiable(valuetype RefCarrier& destination) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        mkrefany RefCarrier
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call void ReturnTestsType::Return.StoreTypedCarrier_Invalid_Unverifiable(typedref, valuetype RefCarrier)
        ret
    }

    .method public hidebysig static void Return.TypedReferenceDirectStoreControl_Invalid_Unverifiable(valuetype RefCarrier& destination) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        stobj RefCarrier
        ret
    }

    .method public hidebysig static void Return.TypedReferenceValueParameter_Valid(valuetype RefCarrier destination) cil managed
    {
        .locals init ([0] int32)
        ldarga.s 0
        mkrefany RefCarrier
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call void ReturnTestsType::Return.StoreTypedCarrier_Invalid_Unverifiable(typedref, valuetype RefCarrier)
        ret
    }

    .method public hidebysig static void Return.TypedReferenceLocal_Valid() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldloca.s 1
        mkrefany RefCarrier
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call void ReturnTestsType::Return.StoreTypedCarrier_Invalid_Unverifiable(typedref, valuetype RefCarrier)
        ret
    }

    .method public hidebysig static int32& Return.TypedReferenceSnapshotEscape_Invalid_Unverifiable() cil managed
    {
        .locals init ([0] int32, [1] valuetype OuterCarrier)
        ldloc.1
        ldflda valuetype RefCarrier OuterCarrier::A
        dup
        mkrefany RefCarrier
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call void ReturnTestsType::Return.StoreTypedCarrier_Invalid_Unverifiable(typedref, valuetype RefCarrier)
        ldfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static int32& Return.TypedReferenceTrackedAliasEscape_Invalid_Unverifiable.ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype OuterCarrier)
        ldloca.s 1
        ldflda valuetype RefCarrier OuterCarrier::A
        dup
        mkrefany RefCarrier
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call void ReturnTestsType::Return.StoreTypedCarrier_Invalid_Unverifiable(typedref, valuetype RefCarrier)
        ldfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.TypedReferenceDeferredEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier, [2] typedref)
        ldloca.s 1
        mkrefany RefCarrier
        stloc.2
        ldloc.2
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call void ReturnTestsType::Return.StoreTypedCarrier_Invalid_Unverifiable(typedref, valuetype RefCarrier)
        ldloc.1
        ret
    }

    .method public hidebysig static void Return.StoreTypedCarrierByRef_Invalid_Unverifiable.Unverifiable(typedref& destination, valuetype RefCarrier input) cil managed
    {
        ldarg.0
        ldobj [System.Runtime]System.TypedReference
        refanyval RefCarrier
        ldarg.1
        stobj RefCarrier
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.TypedReferenceByRefEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier, [2] typedref)
        ldloca.s 1
        mkrefany RefCarrier
        stloc.2
        ldloca.s 2
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call void ReturnTestsType::Return.StoreTypedCarrierByRef_Invalid_Unverifiable.Unverifiable(typedref&, valuetype RefCarrier)
        ldloc.1
        ret
    }

    .method public hidebysig static object Return.VirtualTypedReference_Valid(class [System.Runtime]System.Reflection.FieldInfo info) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        ldloca.s 0
        mkrefany [System.Runtime]System.Int32
        callvirt instance object [System.Runtime]System.Reflection.FieldInfo::GetValueDirect(typedref)
        ret
    }

    .method public hidebysig static void Return.InterfaceTypedReference_Valid(class ITypedReferenceConsumer consumer) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        ldloca.s 0
        mkrefany [System.Runtime]System.Int32
        callvirt instance void ITypedReferenceConsumer::Consume(typedref)
        ret
    }

    .method public hidebysig static void Return.DelegateTypedReference_Valid(class TypedReferenceDelegate consumer) cil managed
    {
        .locals init ([0] int32)
        ldarg.0
        ldloca.s 0
        mkrefany [System.Runtime]System.Int32
        callvirt instance void TypedReferenceDelegate::Invoke(typedref)
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.TypedReferenceEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldloca.s 1
        mkrefany RefCarrier
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call void ReturnTestsType::Return.StoreTypedCarrier_Invalid_Unverifiable(typedref, valuetype RefCarrier)
        ldloc.1
        ret
    }

    .method public hidebysig static valuetype RefCarrier Return.TypedReferenceAfterResetEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype RefCarrier)
        ldloca.s 1
        mkrefany RefCarrier
        ldloca.s 1
        initobj RefCarrier
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        call void ReturnTestsType::Return.StoreTypedCarrier_Invalid_Unverifiable(typedref, valuetype RefCarrier)
        ldloc.1
        ret
    }

    .method public hidebysig static void Return.StoreTypedSpan_Invalid_Unverifiable(typedref destination, valuetype [System.Runtime]System.Span`1<int32> input) cil managed
    {
        ldarg.0
        refanyval valuetype [System.Runtime]System.Span`1<int32>
        ldarg.1
        stobj valuetype [System.Runtime]System.Span`1<int32>
        ret
    }

    .method public hidebysig static valuetype [System.Runtime]System.Span`1<int32> Return.TypedReferenceSpanEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype [System.Runtime]System.Span`1<int32>)
        ldloca.s 1
        mkrefany valuetype [System.Runtime]System.Span`1<int32>
        ldloca.s 0
        newobj instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(!0&)
        call void ReturnTestsType::Return.StoreTypedSpan_Invalid_Unverifiable(typedref, valuetype [System.Runtime]System.Span`1<int32>)
        ldloc.1
        ret
    }

    .method public hidebysig static int32& Return.TemporaryAliasEscape_Invalid_Unverifiable() cil managed
    {
        .locals init ([0] int32, [1] valuetype OuterCarrier)
        ldloc.1
        ldflda valuetype RefCarrier OuterCarrier::A
        dup
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static int32& Return.TemporaryStoredAliasEscape_Invalid_Unverifiable() cil managed
    {
        .locals init ([0] int32, [1] valuetype OuterCarrier, [2] valuetype RefCarrier&)
        ldloc.1
        ldflda valuetype RefCarrier OuterCarrier::A
        dup
        stloc.2
        ldloca.s 0
        stfld int32& RefCarrier::Item
        ldloc.2
        ldfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static int32& Return.TemporaryUnchangedAlias_Valid(valuetype OuterCarrier input) cil managed
    {
        ldarg.0
        ldflda valuetype RefCarrier OuterCarrier::A
        ldfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static int32& Return.TemporaryIndirectAliasEscape_Invalid_Unverifiable() cil managed
    {
        .locals init ([0] int32, [1] valuetype OuterCarrier)
        ldloc.1
        ldflda valuetype RefCarrier OuterCarrier::A
        dup
        ldloca.s 0
        newobj instance void RefCarrier::.ctor(int32&)
        stobj RefCarrier
        ldfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static int32& Return.TemporaryCallAliasEscape_Invalid_Unverifiable() cil managed
    {
        .locals init ([0] int32, [1] valuetype OuterCarrier)
        ldloc.1
        ldflda valuetype RefCarrier OuterCarrier::A
        dup
        ldloca.s 0
        call void ReturnTestsType::Return.StoreCarrier_Valid(valuetype RefCarrier&, int32&)
        ldfld int32& RefCarrier::Item
        ret
    }

    .method public hidebysig static void Return.ReadAssignedOutputField_Valid([out] valuetype RefPairCarrier& output, int32& input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld int32& RefPairCarrier::First
        ldarg.0
        ldarg.0
        ldfld int32& RefPairCarrier::First
        stfld int32& RefPairCarrier::Second
        ret
    }

    .method public hidebysig static void Return.ReadUnassignedOutputField_Invalid_Unverifiable([out] valuetype RefPairCarrier& output, int32& input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld int32& RefPairCarrier::First
        ldarg.0
        ldfld int32& RefPairCarrier::Second
        pop
        ldarg.0
        initobj RefPairCarrier
        ret
    }

    .method public hidebysig static void Return.ReadConditionalOutputField_Invalid_Unverifiable(bool choose, [out] valuetype RefPairCarrier& output, int32& input) cil managed
    {
        ldarg.0
        brtrue.s first
        ldarg.1
        ldarg.2
        stfld int32& RefPairCarrier::Second
        br.s joined
    first:
        ldarg.1
        ldarg.2
        stfld int32& RefPairCarrier::First
    joined:
        ldarg.1
        ldfld int32& RefPairCarrier::First
        pop
        ldarg.1
        initobj RefPairCarrier
        ret
    }

    .method public hidebysig static void Return.RevisitedDiagnostic_Invalid_InitLocals(bool repeat, valuetype RefCarrier input, int32 source) cil managed
    {
        .locals ([0] int32)
    again:
        ldarg.0
        brtrue.s mutate
        ldloc.0
        pop
        ret
    mutate:
        ldarga.s 1
        ldarga.s 2
        stfld int32& RefCarrier::Item
        br.s again
    }

    .method public hidebysig static valuetype RefCarrier Return.RevisitedLifetimeDiagnostic_Invalid_InitLocals.ReturnPtrToStack(bool repeat, valuetype RefCarrier input, int32 source) cil managed
    {
        .locals ([0] int32)
    again:
        ldarg.0
        brtrue.s mutate
        ldloc.0
        pop
        ldarg.1
        ret
    mutate:
        ldarga.s 1
        ldarga.s 2
        stfld int32& RefCarrier::Item
        br.s again
    }

    .method public hidebysig static valuetype RefCarrier Return.RevisitedLifetimeControl_Invalid_ReturnPtrToStack(bool repeat, valuetype RefCarrier input, int32 source) cil managed
    {
        .locals init ([0] int32)
    again:
        ldarg.0
        brtrue.s mutate
        ldloc.0
        pop
        ldarg.1
        ret
    mutate:
        ldarga.s 1
        ldarga.s 2
        stfld int32& RefCarrier::Item
        br.s again
    }

    .method public hidebysig static valuetype RefCarrier Return.RevisitedExceptionalDiagnostic_Invalid_InitLocals.ReturnPtrToStack(valuetype RefCarrier input, int32 source) cil managed
    {
        .locals ([0] int32)
        .try
        {
            leave.s done
        }
        catch [System.Runtime]System.Exception
        {
            pop
            ldarga.s 0
            ldarga.s 1
            stfld int32& RefCarrier::Item
            leave.s done
        }
    done:
        ldloc.0
        pop
        ldarg.0
        ret
    }

    .method public hidebysig static bool Return.GenericBoxBranchBoundary_Invalid_BoxByRef<byreflike T>(!!T input, bool choose) cil managed
    {
        ldarg.1
        brfalse.s other
        ldarg.0
        box !!T
    test:
        brtrue.s yes
        ldc.i4.0
        ret
    yes:
        ldc.i4.1
        ret
    other:
        ldnull
        br.s test
    }

    .method public hidebysig static bool Return.GenericBoxTypeTestBoundary_Invalid_BoxByRef<byreflike T>(!!T input, bool choose) cil managed
    {
        ldarg.1
        brfalse.s other
        ldarg.0
        box !!T
    test:
        isinst [System.Runtime]System.IDisposable
        brfalse.s no
        ldc.i4.1
        ret
    no:
        ldc.i4.0
        ret
    other:
        ldnull
        br.s test
    }

    .method public hidebysig static void Return.GenericBoxUnboxBoundary_Invalid_BoxByRef<byreflike T>(!!T input, bool choose) cil managed
    {
        ldarg.1
        brfalse.s other
        ldarg.0
        box !!T
    test:
        unbox.any !!T
        pop
        ret
    other:
        ldnull
        br.s test
    }

    .method public hidebysig static void Return.GenericBoxTypeUnboxBoundary_Invalid_BoxByRef<byreflike T>(!!T input, bool choose) cil managed
    {
        ldarg.1
        brfalse.s other
        ldarg.0
        box !!T
        isinst !!T
    test:
        unbox.any !!T
        pop
        ret
    other:
        ldnull
        br.s test
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

    .method public hidebysig static void Return.CopyObjectUnsignedSource_Valid(int32& destination, uint32& source) cil managed
    {
        ldarg.0
        ldarg.1
        cpobj [System.Runtime]System.Int32
        ret
    }

    .method public hidebysig static void Return.CopyObjectReferenceSource_Valid(object& destination, string& source) cil managed
    {
        ldarg.0
        ldarg.1
        cpobj [System.Runtime]System.Object
        ret
    }

    .method public hidebysig static void Return.CopyObjectReferenceDestination_Valid(object& destination, string& source) cil managed
    {
        ldarg.0
        ldarg.1
        cpobj [System.Runtime]System.String
        ret
    }

    .method public hidebysig static void Return.CopyObjectReferenceSourceMismatch_Invalid_StackUnexpected(object& destination, object& source) cil managed
    {
        ldarg.0
        ldarg.1
        cpobj [System.Runtime]System.String
        ret
    }

    .method public hidebysig static void Return.CopyObjectReferenceDestinationMismatch_Invalid_StackUnexpected(string& destination, string& source) cil managed
    {
        ldarg.0
        ldarg.1
        cpobj [System.Runtime]System.Object
        ret
    }

    .method public hidebysig static void Return.CopyObjectReadOnlyDestination_Invalid_ReadOnlyIllegalWrite(int32[] destination, int32& source) cil managed
    {
        ldarg.0
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int32
        ldarg.1
        cpobj [System.Runtime]System.Int32
        ret
    }

    .method public hidebysig static void Return.CopyObjectReadOnlySource_Valid(int32& destination, int32[] source) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.i4.0
        readonly.
        ldelema [System.Runtime]System.Int32
        cpobj [System.Runtime]System.Int32
        ret
    }

    .method public hidebysig static void Return.CopyObjectNonByRefSource_Invalid_StackByRef(int32& destination) cil managed
    {
        ldarg.0
        ldc.i4.0
        cpobj [System.Runtime]System.Int32
        ret
    }

    .method public hidebysig static void Return.CopyObjectNonByRefDestination_Invalid_StackByRef(int32& source) cil managed
    {
        ldc.i4.0
        ldarg.0
        cpobj [System.Runtime]System.Int32
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
