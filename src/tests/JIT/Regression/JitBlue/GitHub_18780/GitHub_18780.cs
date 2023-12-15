// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_18780
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool ok = true;
        ok &= M1(0);
        ok &= M2();
        ok &= M3();
        return ok ? 100 : -1;
    }

    // The multiplication by uint.MaxValue was optimized to a NEG
    // which was typed as the second operand, giving a byte NEG node.
    // With x86/ARM32 RyuJIT the cast to ulong would then treat vr13
    // as a byte instead of a uint and zero extend from 8 bits.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool M1(byte arg2)
    {
        byte vr23 = arg2++;
        return (ulong)(uint.MaxValue * arg2) == uint.MaxValue;
    }

    // Like above, the -1 multiplication was turned into a byte NEG node.
    // The byte cast was then removed, but since the NEG byte node still
    // produces a wide result this meant (byte)val was -1.
    static byte s_1 = 1;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool M2()
    {
        return (byte)(-1 * s_1) == 255;
    }

    // Exactly the same as above, but this tests the optimization for
    // transforming (0 - expr) into -expr.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool M3()
    {
        return (byte)(0 - s_1) == 255;
    }
}
