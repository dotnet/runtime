// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Runtime
{
}

.assembly TestLocalScopes2
{
}

.class public abstract auto ansi sealed beforefieldinit TestLocalScopesType
       extends [System.Runtime]System.Object
{
    .method public hidebysig static int32 Foo(int32 a) cil managed
    {
      .locals init ([0] int32 LOCAL_0)
    IL_0000:    nop
    IL_0001:    ldloc.0
    IL_0002:    ldarg.0
    IL_0003:    add
    IL_0004:    stloc.0
    IL_0005:    nop

                {
                  .locals ([1] int32 LOCAL_1, [2] int32 LOCAL_2)
    IL_0006:        ldloc.0
    IL_0007:        ldarg.0
    IL_0008:        add
    IL_0009:        stloc.1
    IL_000a:        nop
    IL_000b:        ldc.i4.s   42
    IL_000d:        stloc.2
    IL_000e:        nop
    IL_000f:        ldloc.1
    IL_0010:        ldloc.2
    IL_0011:        add
    IL_0012:        stloc.0
    IL_0013:        nop
                }

    IL_0014:    nop

                {
    IL_0015:        nop
    IL_0016:        ldc.i4.s   7
    IL_0018:        ldloc.0
    IL_0019:        add
    IL_001a:        stloc.0
                }

    IL_001b:    nop
    IL_001c:    ldloc.0
    IL_001d:    ldarg.0
    IL_001e:    add
    IL_001f:    ret
    }
}
