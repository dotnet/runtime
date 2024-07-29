// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern System.Runtime.Extensions {}
.assembly extern System.Console {}
.assembly Runtime_75312 {}

.class public abstract auto ansi sealed beforefieldinit Runtime_75312
       extends [System.Runtime]System.Object
{
    .method public hidebysig static 
        int32 Main () cil managed 
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .maxstack 2
        .locals init (
            [0] int64 a
        )
        ldc.i8 1234605616436508552
        stloc.0
        ldc.i4 1146447579
        ldloca.s 0
        conv.u
        newobj instance void [System.Runtime]System.IntPtr::.ctor(void*)
        call int32 [System.Runtime]System.IntPtr::op_Explicit(native int)
        call int32 Runtime_75312::Test(int32)
        sub
        ret
    }

    .method private hidebysig static int32 Test (int32 lcl) cil managed noinlining nooptimization
    {
        .maxstack 8
        
        // return *(int*)(arg0 + ((3 * 0) << 2) + 1);
        // to avoid constant folding in Roslyn (even for Debug) it's written in IL

        ldarg.0
        ldc.i4.3
        ldc.i4.0
        mul
        ldc.i4.2
        shl
        add
        ldc.i4.1
        add
        conv.i
        ldind.i4
        ret
    }
}
