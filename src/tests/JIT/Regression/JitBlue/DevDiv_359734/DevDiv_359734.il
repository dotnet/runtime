// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The original bug for this test was an assertion after the following sequence of events:
// - Assertion propagation constant propagated away the ldlocs, or, and shl in the IL at label "expr"
// - After constant propagation, the resulting expression--`(mod tmp0 cns)`--was remorphed, which
//   replaced the modulus with `(sub tmp0 (mul (div tmp0 cns) cns))` and unset the "exception" flag.
//   Critically, this transformation added a new use of `tmp0` without updating the ref count for `tmp0`.
// - After morphing, the resulting expression was both unsused and side-effect-free, and was removed
// - While removing the lclVar ref count for `tmp0` fell to `0`
// - Later, liveness found a reference to `tmp0` despite the `0` lclVar count and asserted.
//
// The fix was to bump lclVar ref counts after remorphing but before attempting to remove the expression.

.assembly extern mscorlib{}
.assembly 'DevDiv_359734'{}
.assembly extern xunit.core {}
.class ILGEN_CLASS
{
    .method static char ILGEN_METHOD(bool, char, int16, int32)
    {
        .maxstack  65535
        .locals init (int32, int16, int64, int16, unsigned int8, float32, unsigned int8)

        ldc.i8 0x80BBE1AB
        ldc.i8 0x866D9EF3
        or
        ldarg 0x1
        shl
        ldc.i8 0xC09C2177
        sub.ovf.un
        conv.ovf.u8.un
        ldc.i8 0x82B1C678
        or
        ldc.r8 -0.000000
        ldloc 0x5
        ckfinite
        ldloc 0x5
        neg
        neg
        add
        add
        ldc.r8 0.000000
        clt
        stloc.s 0x4
        ldc.i8 0xACB4FC40

    expr:
        ldloc.s 0x3
        ldloc.s 0x0
        or
        shl
        rem

        conv.ovf.i8
        ldloc.s 0x4
        conv.ovf.u
        neg
        brtrue IL_0089
        ldarg 0x0
        conv.ovf.i8
        conv.i8
        ldc.i8 0x2ECADED8
        ldloc 0x2
        shr
        clt
        pop
        ldloc.s 0x1
        conv.ovf.u1.un
        conv.ovf.u.un
        starg 0x0

    IL_0089:
        ldarg.s 0x3
        conv.u8
        bgt IL_0094
        ldloc.s 0x5
        pop

    IL_0094:
        ldc.r8 0.000000
        neg
        conv.i
        ldloc 0x5
        ckfinite
        ldc.i8 0x8BD98F2C
        conv.r4
        ckfinite
        clt
        pop
        neg
        ret
    }

    .method static int32 Main()
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint

        .try
        {
            ldc.i4 0
            dup
            dup
            dup
            call char ILGEN_CLASS::ILGEN_METHOD(bool, char, int16, int32)
            pop
            leave.s done
        }
        catch [mscorlib]System.Exception
        {
            leave.s done
        }

    done:
        ldc.i4 100
        ret
    }
}
