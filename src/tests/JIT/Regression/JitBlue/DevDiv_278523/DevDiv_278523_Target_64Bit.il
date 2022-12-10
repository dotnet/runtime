// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly 'DevDiv_278523_64' {}
.assembly extern xunit.core {}

// This test originally triggered an assert when computing the value number for a block assignment. In particular, the
// VN framework expected any block assignments to a tracked lclVar to have a destination address of the form
// `(addr (lclVar))` or `(addr (lclFld))`. The check that it was using to determine whether or not a block assignment
// targets a lclVar also admitted addresses formed by some combination of adds of constants to these patterns (e.g.
// `(add (const 4) (add (addr lclVar) (const 4)))`. The bits of IL that trigger the assert are called out in the method
// bodies below. They differ for 32- and 64-bit targets because on 64-bit targets, the importer will insert an
// int->long conversion when adding a constant int to any long. Due to the cast, the resulting IR is not considered to
// be an add of a constant and a lclVar address. In order to repro the bug on a 64-bit target, the input IL must
// directly produce a long constant.

.class private sequential ansi sealed beforefieldinit S extends [mscorlib]System.ValueType
{
    .field public uint8 m_fld
    .field public uint8 m_fld1
    .field public uint8 m_fld2
    .field public uint8 m_fld3
    .field public uint8 m_fld4
    .field public uint8 m_fld5
    .field public uint8 m_fld6
}

.class private sequential ansi sealed beforefieldinit T extends [mscorlib]System.ValueType
{
    .field public int32 m_int
    .field public valuetype S m_fld
}

.class private abstract auto ansi sealed beforefieldinit C extends [mscorlib]System.Object
{
    .method private static int32 Test64Bit(int32 i) noinlining
    {
        .locals init (valuetype S V_0, valuetype T V_1)

        ldloca.s V_0
        ldarg.0
        conv.u1
        stfld uint8 S::m_fld6

        // This sequence of IL repros the issue. Note that the `ldc.i8` is necessary (rather than an `ldc.i4` that is
        // implicitly converted to a long by the `add`).
        // This sequence is incorrect IL, because it adds native int + long, that is BADCODE according to ECMA.
        // However, 64bits jit can compile and execute it. See CoreCLR issue #11476.
        ldloca.s V_1
        ldc.i8 4
        add
        ldloc.0
        stobj S

        ldloca.s V_1
        ldfld valuetype S T::m_fld
        ldfld uint8 S::m_fld6
        conv.i4
        ret
    }

    .method private static int32 Main()
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .locals init (int32 V_0)

        sizeof [mscorlib]System.IntPtr
        ldc.i4 8
        beq.s _64bit

        // This file is for 64bit-ness platforms only.
        br.s fail

_64bit:
        ldc.i4 100
        dup
        call int32 C::Test64Bit(int32)
        bne.un.s fail

success:
        ldc.i4 100
        ret

fail:
        ldc.i4 101
        ret
    }
}
