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
