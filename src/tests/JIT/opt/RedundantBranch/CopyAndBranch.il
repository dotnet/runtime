// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime {}
.assembly extern xunit.core {}
.assembly CopyAndBranch {}

.class public auto ansi CopyAndBranch extends [System.Runtime]System.Object
{
    // Small parameters normalize on load. The constants stored into them deliberately
    // exceed their ranges, and the copied value remains live after the branch.
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

    .method private static int32 CopyFromSByte(int8 input, bool choose) cil managed noinlining
    {
        .maxstack 2
        .locals init (int32 copy)
        ldarg.1
        brtrue.s NegativeValue
        ldc.i4 256
        starg.s input
        br.s Join
    NegativeValue:
        ldc.i4 255
        starg.s input
    Join:
        ldarg.0
        stloc.0
        ldloc.0
        ldc.i4.0
        blt.s Negative
        ldc.i4.s 100
        ret
    Negative:
        ldloc.0
        ret
    }

    .method private static int32 CopyToByte(uint8 input, bool choose) cil managed noinlining
    {
        .maxstack 2
        .locals init (int32 source)
        ldarg.1
        brtrue.s One
        ldc.i4 256
        stloc.0
        br.s Join
    One:
        ldc.i4 257
        stloc.0
    Join:
        ldloc.0
        starg.s input
        ldarg.0
        brtrue.s Nonzero
        ldc.i4.s 100
        ret
    Nonzero:
        ldarg.0
        ret
    }

    .method private static int32 CopyToSByte(int8 input, bool choose) cil managed noinlining
    {
        .maxstack 2
        .locals init (int32 source)
        ldarg.1
        brtrue.s NegativeValue
        ldc.i4 256
        stloc.0
        br.s Join
    NegativeValue:
        ldc.i4 255
        stloc.0
    Join:
        ldloc.0
        starg.s input
        ldarg.0
        ldc.i4.0
        blt.s Negative
        ldc.i4.s 100
        ret
    Negative:
        ldarg.0
        ret
    }

    .method private static int32 CopySByteToUShort(int8 source, uint16 copy, bool choose) cil managed noinlining
    {
        .maxstack 2
        ldarg.2
        brtrue.s NegativeValue
        ldc.i4 256
        starg.s source
        br.s Join
    NegativeValue:
        ldc.i4 255
        starg.s source
    Join:
        ldarg.0
        starg.s copy
        ldarg.1
        ldc.i4 255
        bgt.s Large
        ldc.i4.s 100
        ret
    Large:
        ldarg.1
        ret
    }

    .method public static int32 TestEntryPoint() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (01 00 00 00)
        .entrypoint
        .maxstack 3

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

        ldc.i4.0
        ldc.i4.0
        call int32 CopyAndBranch::CopyFromSByte(int8, bool)
        ldc.i4.s 100
        bne.un Fail
        ldc.i4.0
        ldc.i4.1
        call int32 CopyAndBranch::CopyFromSByte(int8, bool)
        ldc.i4.m1
        bne.un Fail

        ldc.i4.0
        ldc.i4.0
        call int32 CopyAndBranch::CopyToByte(uint8, bool)
        ldc.i4.s 100
        bne.un Fail
        ldc.i4.0
        ldc.i4.1
        call int32 CopyAndBranch::CopyToByte(uint8, bool)
        ldc.i4.1
        bne.un Fail

        ldc.i4.0
        ldc.i4.0
        call int32 CopyAndBranch::CopyToSByte(int8, bool)
        ldc.i4.s 100
        bne.un Fail
        ldc.i4.0
        ldc.i4.1
        call int32 CopyAndBranch::CopyToSByte(int8, bool)
        ldc.i4.m1
        bne.un Fail

        ldc.i4.0
        ldc.i4.0
        ldc.i4.0
        call int32 CopyAndBranch::CopySByteToUShort(int8, uint16, bool)
        ldc.i4.s 100
        bne.un Fail
        ldc.i4.0
        ldc.i4.0
        ldc.i4.1
        call int32 CopyAndBranch::CopySByteToUShort(int8, uint16, bool)
        ldc.i4 65535
        bne.un Fail

        ldc.i4.s 100
        ret
    Fail:
        ldc.i4.m1
        ret
    }
}
