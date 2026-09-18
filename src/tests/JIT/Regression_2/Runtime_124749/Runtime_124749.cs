// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

// fgOptimizeHWIntrinsic transforms "(-v1) + v2" to "v2 - v1", reversing the
// evaluation order of the operands. When CSE has planted a store to a local in
// v1 that is read by v2, and assertion propagation re-morphs the tree, the swap
// schedules the read before its def, tripping a use-before-def assert during
// Rationalization.

public class Runtime_124749
{
    public static byte[] s_2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void M0()
    {
        var vr1 = Vector256.CreateScalar(1U);
        var vr3 = Vector256.Create<uint>(1);
        var vr5 = Vector256.Create<uint>(0);
        var vr7 = Vector256.Create<uint>(1);
        var vr6 = Avx512CD.VL.LeadingZeroCount(vr7);
        var vr4 = Avx2.CompareEqual(vr5, vr6);
        var vr2 = Avx2.Subtract(vr3, vr4);
        if (Avx.TestZ(vr1, vr2))
        {
            var vr0 = s_2[0];
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        if (!Avx512CD.VL.IsSupported)
        {
            return;
        }

        try
        {
            M0();
        }
        catch (NullReferenceException)
        {
            // Expected if the branch reading the uninitialized "s_2" is taken.
        }
    }
}
