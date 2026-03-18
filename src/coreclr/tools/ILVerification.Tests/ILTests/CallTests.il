// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
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

.class public auto ansi beforefieldinit CallTestsType
        extends [System.Runtime]System.Object
{
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
}
