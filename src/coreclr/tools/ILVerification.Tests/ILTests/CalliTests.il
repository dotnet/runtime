// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly CalliTests
{
}

// ILVerify reports every calli as Unverifiable, because the runtime does not track the lifetime of function
// pointers, and then checks the call site.
.class public auto ansi beforefieldinit CalliTests
        extends [System.Runtime]System.Object
{
    .method private hidebysig static void StaticTarget(int32 x) cil managed
    {
        ret
    }

    .method private hidebysig static void ManagedTarget() cil managed
    {
        ret
    }

    .method private hidebysig static int32 ReturnsInt32() cil managed
    {
        ldc.i4.0
        ret
    }

    .method private hidebysig instance void InstanceTarget(int32 x) cil managed
    {
        ret
    }

    .method public hidebysig static void Calli.StaticFromLdftn_Invalid_Unverifiable() cil managed
    {
        ldc.i4.1
        ldftn void CalliTests::StaticTarget(int32)
        calli void(int32)
        ret
    }

    .method public hidebysig static int32 Calli.ReturnValue_Invalid_Unverifiable() cil managed
    {
        ldftn int32 CalliTests::ReturnsInt32()
        calli int32()
        ret
    }

    .method public hidebysig static void Calli.ErrorAfterCalli_Invalid_Unverifiable.StackUnderflow() cil managed
    {
        ldc.i4.1
        ldftn void CalliTests::StaticTarget(int32)
        calli void(int32)
        pop
        ret
    }

    .method public hidebysig static void Calli.FromArgument_Invalid_Unverifiable(method void *(int32) f) cil managed
    {
        ldc.i4.1
        ldarg.0
        calli void(int32)
        ret
    }

    .method public hidebysig static void Calli.ArgumentMismatch_Invalid_Unverifiable.StackUnexpected(method void *(int32) f) cil managed
    {
        ldnull
        ldarg.0
        calli void(int32)
        ret
    }

    .method public hidebysig static void Calli.MethodPointerNotNativeInt_Invalid_Unverifiable.StackMethod() cil managed
    {
        ldc.i4.1
        ldc.i4.0
        calli void(int32)
        ret
    }

    .method public hidebysig instance void Calli.ImplicitThis_Invalid_Unverifiable(method instance void *(int32) f) cil managed
    {
        ldarg.0
        ldc.i4.1
        ldarg.1
        calli instance void(int32)
        ret
    }

    .method public hidebysig instance void Calli.ExplicitThis_Invalid_Unverifiable() cil managed
    {
        ldarg.0
        ldc.i4.1
        ldftn instance void CalliTests::InstanceTarget(int32)
        calli instance explicit void(class CalliTests, int32)
        ret
    }

    // A vararg call site with no extra arguments has no sentinel.
    .method public hidebysig static void Calli.Vararg_Invalid_Unverifiable(method vararg void *(int32) f) cil managed
    {
        ldc.i4.1
        ldarg.0
        calli vararg void(int32)
        ret
    }

    .method public hidebysig static void Calli.TailCall_Invalid_Unverifiable() cil managed
    {
        ldftn void CalliTests::ManagedTarget()
        tail. calli void()
        ret
    }

    .method public hidebysig static void Calli.TailCallByRefArgument_Invalid_Unverifiable.TailByRef(method void *(int32&) f) cil managed
    {
        .locals init (int32 V_0)
        ldloca.s V_0
        ldarg.0
        tail. calli void(int32&)
        ret
    }

    .method public hidebysig static void Calli.TailCallByRefThis_Invalid_Unverifiable.TailByRef(method instance void *() f) cil managed
    {
        .locals init (int32 V_0)
        ldloca.s V_0
        ldarg.0
        tail. calli instance void()
        ret
    }

    .method public hidebysig static void Calli.TailCallNotFollowedByRet_Invalid_Unverifiable.TailRet() cil managed
    {
        ldftn void CalliTests::ManagedTarget()
        tail. calli void()
        nop
        ret
    }

    // The tail call's return checks are shared with call. A void method cannot tail-call a method returning a value;
    // the value then left on the stack also makes the ret report ReturnVoid.
    .method public hidebysig static void Calli.TailCallReturnsValue_Invalid_Unverifiable.TailRetVoid.ReturnVoid() cil managed
    {
        ldftn int32 CalliTests::ReturnsInt32()
        tail. calli int32()
        ret
    }

    .method public hidebysig static int32& Calli.ByRefReturn_Invalid_Unverifiable(method int32& *() f) cil managed
    {
        ldarg.0
        calli int32&()
        ret
    }

    // The calli operand is the TypeDef token of CalliTests (0x02000002), not a StandAloneSig token.
    .method public hidebysig static void Calli.NotSignatureToken_Invalid_Unverifiable.ExpectedSignatureToken() cil managed
    {
        ldftn void CalliTests::ManagedTarget()
        .emitbyte 0x29
        .emitbyte 0x02
        .emitbyte 0x00
        .emitbyte 0x00
        .emitbyte 0x02
        ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit CalliConstructorTests
        extends [System.Runtime]System.Object
{
    // Passing the uninitialized 'this' to a method pointer is not allowed (ECMA-335 III.1.8.1.4).
    .method public hidebysig specialname rtspecialname instance void .ctor(method void *(object) f) cil managed
    {
        ldarg.0
        ldarg.1
        calli void(object)
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig instance void 'special.CalliUninitThis..ctor_Invalid_Unverifiable.UninitStack'(method void *(object) f) cil managed
    {
        ret
    }
}
