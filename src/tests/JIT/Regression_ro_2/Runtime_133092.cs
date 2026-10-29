// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/133092
//
// The importer's constant-folding path for BitOperations.TrailingZeroCount
// built a TYP_LONG node for a 64-bit operand, then set `baseType = retType`,
// which suppressed the LONG->INT reconciliation at the end of
// impPrimitiveNamedIntrinsic. On 32-bit targets that produced an
// InvalidProgramException at runtime (and an importer assert in a checked JIT).
//
// Note on constant selection: the malformed node was produced for *every*
// constant form, but only constants that Roslyn must encode as `ldc.i8`
// escalate to a failure on a release JIT. Constants encoded as
// `ldc.i4* + conv.i8` -- such as 0UL, or 0xFFFFFFFFFFFFFFFF via ldc.i4.m1 --
// returned correct values even on the unfixed compiler. The 1<<32, 1<<63 and
// 0x100000001 cases below are the ones that actually fail without the fix;
// the others are kept because they still exercise the importer assert on a
// checked JIT.

namespace Runtime_133092;

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133092
{
    [Fact]
    public static void TestEntryPoint()
    {
        // Constants requiring ldc.i8. These threw InvalidProgramException
        // on x86 and arm before the fix.
        Assert.Equal(32, Tzc_OneShl32());
        Assert.Equal(63, Tzc_OneShl63());
        Assert.Equal(0, Tzc_LowBitSetAboveWord());

        // Constants encoded as ldc.i4* + conv.i8.
        Assert.Equal(64, Tzc_Zero());
        Assert.Equal(0, Tzc_AllOnes());

        // Controls: a non-constant operand takes a different path, and the
        // sibling intrinsics never had the defect.
        Assert.Equal(32, Tzc_Var(0x100000000UL));
        Assert.Equal(31, Lzc_Const());
        Assert.Equal(1, PopCount_Const());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Tzc_OneShl32() => BitOperations.TrailingZeroCount(0x100000000UL);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Tzc_OneShl63() => BitOperations.TrailingZeroCount(0x8000000000000000UL);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Tzc_LowBitSetAboveWord() => BitOperations.TrailingZeroCount(0x100000001UL);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Tzc_Zero() => BitOperations.TrailingZeroCount(0UL);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Tzc_AllOnes() => BitOperations.TrailingZeroCount(0xFFFFFFFFFFFFFFFFUL);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Tzc_Var(ulong v) => BitOperations.TrailingZeroCount(v);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Lzc_Const() => BitOperations.LeadingZeroCount(0x100000000UL);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int PopCount_Const() => BitOperations.PopCount(0x100000000UL);
}
