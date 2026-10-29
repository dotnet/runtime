// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly TypedReferenceTests
{
}

.class public sequential ansi sealed beforefieldinit RefStruct
        extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public int32 Value
}

.class public auto ansi beforefieldinit TypedReferenceOther
        extends [System.Runtime]System.Object
{
    .class nested private sequential ansi sealed beforefieldinit Hidden
            extends [System.Runtime]System.ValueType
    {
        .field public int32 Value
    }
}

.class public auto ansi beforefieldinit TypedReferenceTests
        extends [System.Runtime]System.Object
{
    .method private hidebysig static void TakesTypedReference(typedref tr) cil managed
    {
        ret
    }

    // ---- mkrefany (ECMA-335 III.4.19) ----

    .method public hidebysig static void Mkrefany.Local_Valid() cil managed
    {
        .locals init (int32 V_0)
        ldloca.s V_0
        mkrefany [System.Runtime]System.Int32
        call void TypedReferenceTests::TakesTypedReference(typedref)
        ret
    }

    .method public hidebysig static void Mkrefany.ErrorAfterMkrefany_Invalid_StackUnderflow() cil managed
    {
        .locals init (int32 V_0)
        ldloca.s V_0
        mkrefany [System.Runtime]System.Int32
        pop
        pop
        ret
    }

    // Correct CIL, but verification requires a managed pointer.
    .method public hidebysig static void Mkrefany.NativeIntPointer_Invalid_StackByRef() cil managed
    {
        ldc.i4.0
        conv.i
        mkrefany [System.Runtime]System.Int32
        pop
        ret
    }

    .method public hidebysig static void Mkrefany.TypeMismatch_Invalid_StackUnexpected() cil managed
    {
        .locals init (int32 V_0)
        ldloca.s V_0
        mkrefany [System.Runtime]System.Int64
        pop
        ret
    }

    // The pointer must point to exactly the type of the class token, not to a derived type.
    .method public hidebysig static void Mkrefany.ReferenceTypeMismatch_Invalid_StackUnexpected() cil managed
    {
        .locals init (string V_0)
        ldloca.s V_0
        mkrefany [System.Runtime]System.Object
        pop
        ret
    }

    .method public hidebysig static void Mkrefany.ReadOnlyPointer_Invalid_ReadOnlyInMkrefany() cil managed
    {
        ldc.i4.1
        newarr [System.Runtime]System.Int32
        ldc.i4.0
        readonly. ldelema [System.Runtime]System.Int32
        mkrefany [System.Runtime]System.Int32
        pop
        ret
    }

    // The ECMA-335 augments allow a managed pointer to a byref-like type, so mkrefany accepts a byref-like class.
    .method public hidebysig static void Mkrefany.TypedReference_Valid() cil managed
    {
        .locals init (typedref V_0)
        ldloca.s V_0
        mkrefany [System.Runtime]System.TypedReference
        pop
        ret
    }

    .method public hidebysig static void Mkrefany.ByRefLikeClass_Valid() cil managed
    {
        .locals init (valuetype RefStruct V_0)
        ldloca.s V_0
        mkrefany RefStruct
        pop
        ret
    }

    .method public hidebysig static void Mkrefany.InaccessibleType_Invalid_TypeAccess() cil managed
    {
        .locals init (valuetype TypedReferenceOther/Hidden V_0)
        ldloca.s V_0
        mkrefany TypedReferenceOther/Hidden
        pop
        ret
    }

    // ---- refanyval (ECMA-335 III.4.23) ----

    .method public hidebysig static void Refanyval.ReadThroughAddress_Valid(typedref tr) cil managed
    {
        ldarg.0
        refanyval [System.Runtime]System.Int32
        ldind.i4
        pop
        ret
    }

    .method public hidebysig static void Refanyval.NotTypedReference_Invalid_StackUnexpected() cil managed
    {
        ldc.i4.0
        refanyval [System.Runtime]System.Int32
        pop
        ret
    }

    .method public hidebysig static void Refanyval.ErrorAfterRefanyval_Invalid_StackUnderflow(typedref tr) cil managed
    {
        ldarg.0
        refanyval [System.Runtime]System.Int32
        pop
        pop
        ret
    }

    // The address in a typed reference can point to a local of a caller.
    .method public hidebysig static int32& Refanyval.ReturnAddress_Invalid_ReturnPtrToStack(typedref tr) cil managed
    {
        ldarg.0
        refanyval [System.Runtime]System.Int32
        ret
    }

    // ---- refanytype (ECMA-335 III.4.22) ----

    .method public hidebysig static void Refanytype.TypeFromHandle_Valid(typedref tr) cil managed
    {
        ldarg.0
        refanytype
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        pop
        ret
    }

    .method public hidebysig static void Refanytype.NotTypedReference_Invalid_StackUnexpected() cil managed
    {
        ldc.i4.0
        refanytype
        pop
        ret
    }

    .method public hidebysig static void Refanytype.ErrorAfterRefanytype_Invalid_StackUnderflow(typedref tr) cil managed
    {
        ldarg.0
        refanytype
        pop
        pop
        ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}
