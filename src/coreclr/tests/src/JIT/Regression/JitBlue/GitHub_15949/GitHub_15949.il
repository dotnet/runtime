// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly extern System.Console { auto }
.assembly GitHub_15949 { }

// Ensure that shifting a 64 bit value by 64 bits produces the same value. The ECMA spec doesn't specify
// what should happen in this case but it would be preferable to have the same behavior for all shift
// instructions (use only the lowest 6 bits of the shift count).

// This test is intended for 32 bit targets where RyuJIT has 4 different code paths that are of interest:
// helper call, decomposition, constant folding via gtFoldExpr and constant folding via VN's EvalOpIntegral.
// But it also works on the current 64 bit targets (ARM64 & x64) because they too have the same behavior.

.class private auto ansi Program extends [mscorlib]System.Object
{
    .method static int32 Main() cil managed
    {
        .entrypoint
        .maxstack 8
        .locals(int64)

        ldc.i8 0x1020304050607080
        ldc.i4 64
        ldc.i4 1
        call int64 Program::Test_Shl_Helper(int64, int32, bool)
        stloc.0

        ldc.i8 0x1020304050607080
        ldc.i4 64
        ldc.i4 1
        call int64 Program::Test_Shr_Helper(int64, int32, bool)
        ldloc.0
        bne.un FAIL

        ldc.i8 0x1020304050607080
        ldc.i4 64
        ldc.i4 1
        call int64 Program::Test_ShrUn_Helper(int64, int32, bool)
        ldloc.0
        bne.un FAIL

        ldc.i8 0x1020304050607080
        ldc.i4 64
        ldc.i4 1
        call int64 Program::Test_Shl_Decompose(int64, int32, bool)
        ldloc.0
        bne.un FAIL

        ldc.i8 0x1020304050607080
        ldc.i4 64
        ldc.i4 1
        call int64 Program::Test_Shr_Decompose(int64, int32, bool)
        ldloc.0
        bne.un FAIL

        ldc.i8 0x1020304050607080
        ldc.i4 64
        ldc.i4 1
        call int64 Program::Test_ShrUn_Decompose(int64, int32, bool)
        ldloc.0
        bne.un FAIL

        ldc.i8 0x1020304050607080
        ldc.i4 64
        ldc.i4 1
        call int64 Program::Test_Shl_gtFoldExpr(int64, int32, bool)
        ldloc.0
        bne.un FAIL

        ldc.i8 0x1020304050607080
        ldc.i4 64
        ldc.i4 1
        call int64 Program::Test_Shr_gtFoldExpr(int64, int32, bool)
        ldloc.0
        bne.un FAIL

        ldc.i8 0x1020304050607080
        ldc.i4 64
        ldc.i4 1
        call int64 Program::Test_ShrUn_gtFoldExpr(int64, int32, bool)
        ldloc.0
        bne.un FAIL

        ldc.i8 0x1020304050607080
        ldc.i4 64
        ldc.i4 1
        call int64 Program::Test_Shl_EvalOpIntegral(int64, int32, bool)
        ldloc.0
        bne.un FAIL

        ldc.i8 0x1020304050607080
        ldc.i4 64
        ldc.i4 1
        call int64 Program::Test_Shr_EvalOpIntegral(int64, int32, bool)
        ldloc.0
        bne.un FAIL

        ldc.i8 0x1020304050607080
        ldc.i4 64
        ldc.i4 1
        call int64 Program::Test_ShrUn_EvalOpIntegral(int64, int32, bool)
        ldloc.0
        bne.un FAIL

        ldc.i4 100
        ret

    FAIL:
        ldc.i4 1
        ret
    }

    .method static int64 Test_Shl_Helper(int64, int32, bool) cil managed noinlining
    {
        .maxstack 2
        ldarg.0
        ldarg.1
        shl
        ret
    }

    .method static int64 Test_Shr_Helper(int64, int32, bool) cil managed noinlining
    {
        .maxstack 2
        ldarg.0
        ldarg.1
        shr
        ret
    }

    .method static int64 Test_ShrUn_Helper(int64, int32, bool) cil managed noinlining
    {
        .maxstack 2
        ldarg.0
        ldarg.1
        shr.un
        ret
    }

    .method static int64 Test_Shl_Decompose(int64, int32, bool) cil managed noinlining
    {
        .maxstack 2
        ldarg.0
        ldc.i4 64
        shl
        ret
    }

    .method static int64 Test_Shr_Decompose(int64, int32, bool) cil managed noinlining
    {
        .maxstack 2
        ldarg.0
        ldc.i4 64
        shr
        ret
    }

    .method static int64 Test_ShrUn_Decompose(int64, int32, bool) cil managed noinlining
    {
        .maxstack 2
        ldarg.0
        ldc.i4 64
        shr.un
        ret
    }

    .method static int64 Test_Shl_gtFoldExpr(int64, int32, bool) cil managed noinlining
    {
        .maxstack 2
        ldc.i8 0x1020304050607080
        ldc.i4 64
        shl
        ret
    }

    .method static int64 Test_Shr_gtFoldExpr(int64, int32, bool) cil managed noinlining
    {
        .maxstack 2
        ldc.i8 0x1020304050607080
        ldc.i4 64
        shr
        ret
    }

    .method static int64 Test_ShrUn_gtFoldExpr(int64, int32, bool) cil managed noinlining
    {
        .maxstack 2
        ldc.i8 0x1020304050607080
        ldc.i4 64
        shr.un
        ret
    }

    .method static int64 Test_Shl_EvalOpIntegral(int64, int32, bool) cil managed noinlining
    {
        .maxstack 2
        ldc.i8 0x1020304050607080
        ldarg.2
        brtrue L1
        ldc.i4 64
        br L2
    L1: ldc.i4 64
    L2: shl
        ret
    }

    .method static int64 Test_Shr_EvalOpIntegral(int64, int32, bool) cil managed noinlining
    {
        .maxstack 2
        ldc.i8 0x1020304050607080
        ldarg.2
        brtrue L1
        ldc.i4 64
        br L2
    L1: ldc.i4 64
    L2: shr
        ret
    }

    .method static int64 Test_ShrUn_EvalOpIntegral(int64, int32, bool) cil managed noinlining
    {
        .maxstack 2
        ldc.i8 0x1020304050607080
        ldarg.2
        brtrue L1
        ldc.i4 64
        br L2
    L1: ldc.i4 64
    L2: shr.un
        ret
    }
}
