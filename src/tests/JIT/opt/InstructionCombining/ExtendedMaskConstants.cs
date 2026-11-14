// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

public class ExtendedMaskConstants
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong SignedDiv3()
    {
        //ARM64-FULL-LINE: mov {{x[0-9]+}}, #{{0xAAAAAAAAAAAAAAAA|0xD1FFAB1E}}
        //ARM64-FULL-LINE: movk {{x[0-9]+}}, #{{0xAAAB|0xD1FFAB1E}}
        //ARM64-FULL-LINE: movk {{x[0-9]+}}, #{{0x2AAA|0xD1FFAB1E}} LSL #48
        //ARM64-NOT: movk
        return 0x2AAAAAAAAAAAAAABUL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong HighOnly()
    {
        //ARM64-FULL-LINE: mov {{x[0-9]+}}, #{{0xAAAAAAAAAAAAAAAA|0xD1FFAB1E}}
        //ARM64-FULL-LINE: movk {{x[0-9]+}}, #{{0x1234|0xD1FFAB1E}} LSL #48
        //ARM64-NOT: movk
        return 0x1234AAAAAAAAAAAAUL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong DifferentOuter()
    {
        //ARM64-FULL-LINE: mov {{x[0-9]+}}, #{{0xAAAAAAAAAAAAAAAA|0xD1FFAB1E}}
        //ARM64-FULL-LINE: movk {{x[0-9]+}}, #{{0x5678|0xD1FFAB1E}}
        //ARM64-FULL-LINE: movk {{x[0-9]+}}, #{{0x1234|0xD1FFAB1E}} LSL #48
        //ARM64-NOT: movk
        return 0x1234AAAAAAAA5678UL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Existing()
    {
        return 0xAAAAAAAAAAAAAAABUL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong TopZero()
    {
        return 0x0000AAAAAAAAAAABUL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong TopOnes()
    {
        return 0xFFFFAAAAAAAAAAABUL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong LowZero()
    {
        return 0x1234AAAAAAAA0000UL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong LowOnes()
    {
        return 0x1234AAAAAAAAFFFFUL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong NotLogical()
    {
        return 0x1234123412345678UL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong DifferentMiddle()
    {
        return 0x1234AAAABBBB5678UL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong SingleMov()
    {
        return 0xAAAAAAAAAAAAAAAAUL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong SmallLong()
    {
        return 0x00000000FFFF1234UL;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // Parse expected values so the tested materialization cannot affect both sides.
        if (SignedDiv3() != ulong.Parse("2AAAAAAAAAAAAAAB", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 101;
        }

        if (HighOnly() != ulong.Parse("1234AAAAAAAAAAAA", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 102;
        }

        if (DifferentOuter() != ulong.Parse("1234AAAAAAAA5678", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 103;
        }

        if (Existing() != ulong.Parse("AAAAAAAAAAAAAAAB", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 104;
        }

        if (TopZero() != ulong.Parse("0000AAAAAAAAAAAB", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 105;
        }

        if (TopOnes() != ulong.Parse("FFFFAAAAAAAAAAAB", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 106;
        }

        if (LowZero() != ulong.Parse("1234AAAAAAAA0000", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 107;
        }

        if (LowOnes() != ulong.Parse("1234AAAAAAAAFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 108;
        }

        if (NotLogical() != ulong.Parse("1234123412345678", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 109;
        }

        if (DifferentMiddle() != ulong.Parse("1234AAAABBBB5678", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 110;
        }

        if (SingleMov() != ulong.Parse("AAAAAAAAAAAAAAAA", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 111;
        }

        if (SmallLong() != ulong.Parse("00000000FFFF1234", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 112;
        }

        return 100;
    }
}
