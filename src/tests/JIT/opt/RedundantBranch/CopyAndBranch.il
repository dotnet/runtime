// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly CopyAndBranch {}

.class public auto ansi CopyAndBranch extends [System.Runtime]System.Object
{
    // Exercise source normalization when fgFoldSimpleCondByForwardSub substitutes
    // a constant through the adjacent copy. The byte source must truncate 256/257
    // to 0/1 before folding the branch, and the copied value remains live afterward.
    .method private static int32 CopyFromByte(uint8 input, bool choose) cil managed noinlining
    {
        .maxstack 2
        .locals init (int32 copy)
        ldarg.1
        brtrue.s One
        ldc.i4 256
        starg.s input
        br.s Join
    One:
        ldc.i4 257
        starg.s input
    Join:
        ldarg.0
        stloc.0
        ldloc.0
        brtrue.s Nonzero
        ldc.i4.s 100
        ret
    Nonzero:
        ldloc.0
        ret
    }

    .method public static int32 TestEntryPoint() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (01 00 00 00)
        .entrypoint
        .maxstack 2

        ldc.i4.0
        ldc.i4.0
        call int32 CopyAndBranch::CopyFromByte(uint8, bool)
        ldc.i4.s 100
        bne.un Fail
        ldc.i4.0
        ldc.i4.1
        call int32 CopyAndBranch::CopyFromByte(uint8, bool)
        ldc.i4.1
        bne.un Fail

        ldc.i4.s 100
        ret
    Fail:
        ldc.i4.m1
        ret
    }
}
