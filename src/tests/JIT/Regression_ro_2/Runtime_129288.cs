// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// GenTree::SupportsSettingZeroFlag treated GT_ROL/GT_ROR as members of the
// shift group on xarch, which let lowering drop the TEST/CMP before a branch
// that compares the rotate's result to zero. ROL/ROR only modify CF (and OF
// for the 1-bit form) on xarch -- SF/ZF/AF/PF are not affected. So the
// branch read stale ZF from whatever flag-setting instruction the JIT had
// scheduled immediately before the rotate (a bounds check, in the original
// repro) and went the wrong direction.
//
// Each test below places a bounds check immediately before a rotate-then-
// branch-on-zero pattern. With x == 0 the rotate yields 0 and the
// (rotate == 0) branch must be taken. Pre-fix, the buggy code reads the
// bounds-check's ZF (zero == "indices equal", which they aren't) and falls
// through.

namespace Runtime_129288;

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_129288
{
    private static readonly int[] s_sink = new int[16];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RorUlongConst(ulong x, int idx)
    {
        s_sink[idx] = 1;                              // bounds check sets ZF
        ulong r = BitOperations.RotateRight(x, 1);    // ROR does not touch ZF
        if (r == 0) return 0;                         // pre-fix: stale ZF -> wrong branch
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RolUlongConst(ulong x, int idx)
    {
        s_sink[idx] = 1;
        ulong r = BitOperations.RotateLeft(x, 1);
        if (r == 0) return 0;
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RorUintConst(uint x, int idx)
    {
        s_sink[idx] = 1;
        uint r = BitOperations.RotateRight(x, 1);
        if (r == 0) return 0;
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RolUintConst(uint x, int idx)
    {
        s_sink[idx] = 1;
        uint r = BitOperations.RotateLeft(x, 1);
        if (r == 0) return 0;
        return 1;
    }

    // Exercise the GT_LT-against-1 pattern from the original repro, which
    // the JIT lowers to the same branch-on-rotate-zero shape for unsigned.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RorUlongLessThanOne(ulong x, int idx)
    {
        s_sink[idx] = 1;
        ulong r = BitOperations.RotateRight(x, 1);
        if (r < 1UL) return 0;
        return 1;
    }

    // Same shape with !=, which exercises the opposite branch polarity.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RorUlongConstNeZero(ulong x, int idx)
    {
        s_sink[idx] = 1;
        ulong r = BitOperations.RotateRight(x, 1);
        if (r != 0) return 1;
        return 0;
    }

    private static int Drive(ulong u64, uint u32, int idx, int passingValue)
    {
        int errors = 0;
        if (RorUlongConst(u64, idx)         != passingValue) errors++;
        if (RolUlongConst(u64, idx)         != passingValue) errors++;
        if (RorUintConst(u32, idx)          != passingValue) errors++;
        if (RolUintConst(u32, idx)          != passingValue) errors++;
        if (RorUlongLessThanOne(u64, idx)   != passingValue) errors++;
        if (RorUlongConstNeZero(u64, idx)   != passingValue) errors++;
        return errors;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // CI runs JIT regression tests with tiering disabled, so the first
        // call is FullOpts and the buggy lowering decision is exercised
        // immediately.
        int errors = 0;

        // x == 0: rotate yields 0, "== 0" branch must be taken.
        errors += Drive(0UL, 0U, 0, 0);

        // Sanity: with a non-zero input the rotate is non-zero and the
        // opposite branch must be taken.
        errors += Drive(1UL, 1U, 1, 1);

        return errors == 0 ? 100 : 101 + errors;
    }
}
