// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Runtime
{
}

.assembly TestLocalScopes3
{
}

.class public abstract auto ansi sealed beforefieldinit TestLocalScopesType
       extends [System.Runtime]System.Object
{
    .field public static int32 GLOBAL_1

    .method public hidebysig static void Foo() cil managed
    {
    IL_0000:    nop
    IL_0001:    ldc.i4.s   10
    IL_0003:    stsfld     int32 TestLocalScopesType::GLOBAL_1
    IL_0008:    nop

                {
                  .locals init([0] int32 LOCAL_0, [1] int32 LOCAL_1)
    IL_0009:        nop
    IL_000a:        ldc.i4.s   2
    IL_000c:        stloc.0
    IL_000d:        ldsfld     int32 TestLocalScopesType::GLOBAL_1
    IL_0012:        ldloc.1
    IL_0013:        add
    IL_0014:        stloc.1
    IL_0015:        nop
    IL_0016:        ldloc.0
    IL_0017:        ldloc.1
    IL_0018:        add
    IL_0019:        stsfld     int32 TestLocalScopesType::GLOBAL_1
    IL_001e:        nop
                }

    IL_001f:    nop
    IL_0020:    ret
    }
}
