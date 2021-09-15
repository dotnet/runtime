// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly FunctionPointerTests
{
}

.class public auto ansi beforefieldinit C
        extends [System.Runtime]System.Object
{
    .method private hidebysig instance void MyDispatcher(method void *(int32) f) cil managed
    {
        .locals init (
            [0] method void *(int32)
        )

        ldarg.1
        stloc.0
        ldc.i4.s 42
        ldloc.0
        calli void(int32)
        ret
    }

    .method private hidebysig static void StaticMethod(int32 x) cil managed
    {
        ldarg.0
        ldc.i4 571
        add
        starg.s x
        ret
    }

    .method public hidebysig instance void Call.Dispatcher_Valid() cil managed
    {
        newobj instance void C::.ctor()
        ldftn void C::StaticMethod(int32)
        call instance void C::MyDispatcher(method void *(int32))
        ret
    }

    .method public hidebysig instance void Call.Dispatcher_Invalid_MethodAccess() cil managed
    {
        newobj instance void C::.ctor()
        ldftn void D::PrivateStaticMethodOfOtherType(int32)
        call instance void C::MyDispatcher(method void *(int32))
        ret
    }

    .method private hidebysig instance void ReceiverOfPrivateType(method class D/D_Private *(int32) ptr) cil managed
    {
        ret
    }

    .method public hidebysig instance void Call.SendFunctionPointerWithInaccessibleReturnType_Invalid_MethodAccess.MethodAccess() cil managed
    {
        newobj instance void C::.ctor()
        ldftn class D/D_Private D::StaticMethodReturningPrivateTypeInstance(int32)
        call instance void C::ReceiverOfPrivateType(method class D/D_Private *(int32))
        ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit D
        extends [System.Runtime]System.Object
{
    .method private hidebysig static void PrivateStaticMethodOfOtherType(int32 x) cil managed
    {
        ldarg.0
        ldc.i4 571
        add
        starg.s x
        ret
    }

    .method public hidebysig static class D/D_Private StaticMethodReturningPrivateTypeInstance(int32 x) cil managed
    {
        newobj instance void D/D_Private::.ctor()
        ret
    }

    .class nested private auto ansi beforefieldinit D_Private
        extends [System.Runtime]System.Object
    {
        .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
        {
            ldarg.0
            call instance void [System.Runtime]System.Object::.ctor()
            ret
        }
    }
}
