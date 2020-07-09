// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly ReturnTests
{
}

.class public sequential ansi sealed beforefieldinit Struct
        extends [System.Runtime]System.ValueType
{
    .field public int32 instanceField
}

.class public auto ansi beforefieldinit ReturnTestsType
        extends [System.Runtime]System.Object
{
    .field private int32 instanceField
    .field private static int32 staticField

    .field private valuetype Struct instanceStruct

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
