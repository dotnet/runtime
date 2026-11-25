// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly InitLocalsTests
{
}

// localsinit is required only for verifiable methods (III.1.8.1.1)
.class public auto ansi beforefieldinit InitLocalsTestsType
       extends [System.Runtime]System.Object
{
    .method public hidebysig static void InitLocals.LoadLocal_Invalid_InitLocals_Validation_Valid() cil managed
    {
        .locals (int32 V_0)
        ldloc.0
        pop
        ret
    }

    .method public hidebysig static void InitLocals.LoadLocalAddress_Invalid_InitLocals_Validation_Valid() cil managed
    {
        .locals (int32 V_0)
        ldloca.s V_0
        pop
        ret
    }
}
