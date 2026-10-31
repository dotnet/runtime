// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly extern ReturnTests
{
}

.assembly CallTests
{
}

.class public auto ansi beforefieldinit SimpleClass
        extends [System.Runtime]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call    instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig newslot virtual instance void VirtualMethod() cil managed
    {
        ret
    }

    .method public hidebysig static void InObjectMethod([in] object&) cil managed 
    {
        ret
    }
}

.class public auto ansi beforefieldinit DerivedClass
        extends SimpleClass
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call    instance void SimpleClass::.ctor()
        ret
    }

    .method public hidebysig virtual instance void VirtualMethod() cil managed
    {
        ldarg.0
        call    instance void SimpleClass::VirtualMethod()
        ret
    }

    .method public hidebysig instance void Call.BaseMethod_Valid() cil managed
    {
        ldarg.0
        call    instance void SimpleClass::VirtualMethod()
        ret
    }

    .method public hidebysig instance void Call.BaseMethodModThisLdarga_Invalid_ThisMismatch() cil managed
    {
        ldarg.0
        call    instance void SimpleClass::VirtualMethod()
        ldarga  0
        pop
        ret
    }

    .method public hidebysig instance void Call.BaseMethodModThisStarg_Invalid_ThisMismatch() cil managed
    {
        ldarg.0
        call    instance void SimpleClass::VirtualMethod()
        ldarg.0
        starg  0
        ret
    }
}

.class public sequential ansi sealed beforefieldinit LegacyCarrier
        extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public int32 Value
    .field public int32& Item

    .method public hidebysig instance void Legacy.CaptureThis_Invalid_Unverifiable() cil managed
    {
        ldarg.0
        ldarg.0
        ldflda int32 LegacyCarrier::Value
        stfld int32& LegacyCarrier::Item
        ret
    }

    .method public hidebysig instance int32& Legacy.ReturnField_Invalid_ReturnPtrToStack() cil managed
    {
        ldarg.0
        ldflda int32 LegacyCarrier::Value
        ret
    }

    .method public hidebysig instance int32& Legacy.UnscopedThis_Valid() cil managed
    {
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = ( 01 00 00 00 )
        ldarg.0
        ldflda int32 LegacyCarrier::Value
        ret
    }

    .method public hidebysig static valuetype LegacyCarrier Legacy.Identity_Valid(valuetype LegacyCarrier input) cil managed
    {
        ldarg.0
        ret
    }

    .method public hidebysig instance valuetype LegacyCarrier Legacy.Copy_Valid() cil managed
    {
        ldarg.0
        ldobj LegacyCarrier
        ret
    }

    .method public hidebysig static int32& Legacy.ByRefIdentity_Valid(int32& input) cil managed
    {
        ldarg.0
        ret
    }
}

.class public auto ansi beforefieldinit CallTestsType
        extends [System.Runtime]System.Object
{
    .method public hidebysig static valuetype LegacyCarrier Call.LegacyReceiverValue_Valid() cil managed
    {
        .locals init ([0] valuetype LegacyCarrier)
        ldloca.s 0
        call instance valuetype LegacyCarrier LegacyCarrier::Legacy.Copy_Valid()
        ret
    }

    .method public hidebysig static valuetype LegacyCarrier Call.LegacyReceiverContentsEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] int32, [1] valuetype LegacyCarrier)
        ldloca.s 1
        ldloca.s 0
        stfld int32& LegacyCarrier::Item
        ldloca.s 1
        call instance valuetype LegacyCarrier LegacyCarrier::Legacy.Copy_Valid()
        ret
    }

    .method public hidebysig static int32& Call.LegacyUnscopedReceiverEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] valuetype LegacyCarrier)
        ldloca.s 0
        call instance int32& LegacyCarrier::Legacy.UnscopedThis_Valid()
        ret
    }

    .method public hidebysig static int32& Call.PropertyEscape_Invalid_ReturnPtrToStack() cil managed
    {
        .locals init ([0] valuetype [ReturnTests]UnscopedAccessor)
        ldloca.s 0
        call instance int32& [ReturnTests]UnscopedAccessor::get_Value()
        ret
    }

    .method public hidebysig static int32& Call.PropertyReference_Valid(valuetype [ReturnTests]UnscopedAccessor& input) cil managed
    {
        ldarg.0
        call instance int32& [ReturnTests]UnscopedAccessor::get_Value()
        ret
    }

    .method public hidebysig instance void Call.IntToStringManagedPtr_Valid() cil managed
    {
        .locals init (
            [0] int32
        )

        ldc.i4.0
        stloc.0
        ldloca.s    0
        call    instance string [System.Runtime]System.Int32::ToString()
        pop
        ret
    }

    .method public hidebysig instance void Call.IntToStringBoxed_Valid() cil managed
    {
        .locals init (
            [0] int32
        )

        ldc.i4.0
        box     [System.Runtime]System.Int32
        call    instance string [System.Runtime]System.Object::ToString()
        pop
        ret
    }

    .method public hidebysig instance void Callvirt.IntToStringBoxed_Valid() cil managed
    {
        .locals init (
            [0] int32
        )

        ldc.i4.0
        box     [System.Runtime]System.Int32
        callvirt    instance string [System.Runtime]System.Object::ToString()
        pop
        ret
    }

    .method public hidebysig instance void Call.IntToStringUnboxed_Invalid_StackUnexpected() cil managed
    {
        .locals init (
            [0] int32
        )

        ldc.i4.0
        call    instance string [System.Runtime]System.Int32::ToString()
        pop
        ret
    }

    .method public hidebysig instance void Call.ExternBaseMethod_Invalid_ThisMismatch(class DerivedClass c) cil managed
    {
        ldarg.1
        call    instance void SimpleClass::VirtualMethod()
        ret
    }

    .method public hidebysig instance void Call.ReadonlyByRefForInArg_Invalid_StackUnexpected(object[] objectArray) cil managed
    {
        // SimpleClass.InObjectMethod(objectArray[0]);

        ldarg.1
        ldc.i4.0
        readonly.
        ldelema     [System.Runtime]System.Object
        call        void SimpleClass::InObjectMethod(object&)
        ret
    }

    .method public hidebysig instance void Callvirt.StaticMethod_Invalid_CallVirtOnStatic() cil managed
    {
        .locals init (
            [0] object
        )

        // callvirt void SimpleClass::InObjectMethod(object&)
        // ilasm adds 'instance' to the signature of a callvirt target, so the static
        // MethodDef is referenced by token (it is the third method in this file).
        ldloca.s    0
        callvirt    mdtoken(0x06000003)
        ret
    }
}
