// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly ValueTypeTests
{
}

.class public sequential ansi sealed beforefieldinit ValueTypeTests
       extends [System.Runtime]System.ValueType
{
    .size 1
    .method public instance void CallThis() cil managed
    {
        ret
    }

    .method public instance void ValueType.CallMethod_Valid() cil managed
    {
        ldarg.0
        call instance void ValueTypeTests::CallThis()
        ret
    }
}

.class public sequential ansi sealed beforefieldinit ValueTypeFieldTests
       extends [System.Runtime]System.ValueType
{
    .field public int32 InstanceField

    .method public static int32 ValueType.UnboxLdfld_Valid(object o) cil managed
    {
        ldarg.0
        unbox      ValueTypeFieldTests
        ldfld      int32 ValueTypeFieldTests::InstanceField
        ret
    }
}

.class public sequential ansi sealed beforefieldinit ByRefLikeType
       extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .size 1
}

.class public auto ansi beforefieldinit ByRefLikeTests
       extends [System.Runtime]System.Object
{
    .method public static void ByRefLike.Box_Invalid_BoxByRef(valuetype ByRefLikeType v) cil managed
    {
        ldarg.0
        box        ByRefLikeType
        pop
        ret
    }

    .method public static void ByRefLike.BoxBrtrue_Invalid_BoxByRef(valuetype ByRefLikeType v) cil managed
    {
        ldarg.0
        box        ByRefLikeType
        brtrue.s   DONE
    DONE:
        ret
    }

    .method public static void ByRefLike.BoxIsinstBrfalse_Invalid_BoxByRef(valuetype ByRefLikeType v) cil managed
    {
        ldarg.0
        box        ByRefLikeType
        isinst     [System.Runtime]System.Object
        brfalse.s  DONE
    DONE:
        ret
    }

    .method public static void ByRefLike.BoxUnboxAny_Invalid_BoxByRef(valuetype ByRefLikeType v) cil managed
    {
        ldarg.0
        box        ByRefLikeType
        unbox.any  ByRefLikeType
        pop
        ret
    }

    .method public static void ByRefLike.NewArray_Invalid_ArrayByRef() cil managed
    {
        ldc.i4.1
        newarr     ByRefLikeType
        pop
        ret
    }
}
