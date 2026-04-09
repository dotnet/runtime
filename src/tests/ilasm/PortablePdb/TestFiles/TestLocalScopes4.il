// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly TestLocalScopes4
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
                  .locals ([1] int32 LOCAL_11, [3] int32 LOCAL_12)
    IL_0006:        ldloc.0
    IL_0007:        ldarg.0
    IL_0008:        add
    IL_0009:        stloc.1
    IL_000a:        nop

                    {
                      .locals ([2] int32 LOCAL_2)
    IL_000b:            ldloc.1
    IL_000c:            ldarg.0
    IL_000d:            add
    IL_000e:            stloc.2
    IL_000f:            nop

                        {
                          .locals ([3] int32 LOCAL_3)
    IL_0010:                ldloc.2
    IL_0011:                ldarg.0
    IL_0012:                add
    IL_0013:                stloc.3
    IL_0014:                nop
                    }   }

    IL_0015:        nop

                    {
                          .locals ([4] int32 LOCAL_4)
    IL_0016:                ldloc.3
    IL_0017:                ldarg.0
    IL_0018:                add
    IL_0019:                stloc.0
    IL_001a:                nop
                    }

    IL_001b:        nop
    IL_001c:        ldloc.0
    IL_001d:        stloc.0
    IL_001e:        nop
                }

    IL_001f:    nop
    IL_0020:    ldloc.0
    IL_0021:    ldarg.0
    IL_0022:    add
    IL_0023:    ret
    }
}
