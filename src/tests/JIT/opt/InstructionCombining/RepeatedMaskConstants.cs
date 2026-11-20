// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

public class RepeatedMaskConstants
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Div3()
    {
        //ARM64-FULL-LINE: mov {{x[0-9]+}}, #{{0xAAAAAAAAAAAAAAAA|0xD1FFAB1E}}
        //ARM64-FULL-LINE: movk {{x[0-9]+}}, #{{0xAAAB|0xD1FFAB1E}}
        //ARM64-NOT: movk
        return 0xAAAAAAAAAAAAAAABUL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Div5()
    {
        //ARM64-FULL-LINE: mov {{x[0-9]+}}, #{{0xCCCCCCCCCCCCCCCC|0xD1FFAB1E}}
        //ARM64-FULL-LINE: movk {{x[0-9]+}}, #{{0xCCCD|0xD1FFAB1E}}
        //ARM64-NOT: movk
        return 0xCCCCCCCCCCCCCCCDUL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ByteMask()
    {
        return 0xFF00FF00FF001234UL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ZeroLow()
    {
        return 0x8000800080000000UL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong OnesLow()
    {
        return 0xFF00FF00FF00FFFFUL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong SingleBitMask()
    {
        return 0x0001000100011234UL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AllRepeated()
    {
        return 0xAAAAAAAAAAAAAAAAUL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ZerosAbove()
    {
        return 0x0000000000001234UL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong OnesAbove()
    {
        return 0xFFFFFFFFFFFF1234UL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong NotEncodable()
    {
        return 0x1234123412345678UL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong MiddleDiffers()
    {
        return 0xAAAAAAAABBBBAAABUL;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Arbitrary()
    {
        return 0x123456789ABCDEF0UL;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // Parse expected values at runtime so a faulty constant materialization cannot
        // affect both sides of a comparison and hide a failure.
        if (Div3() != ulong.Parse("AAAAAAAAAAAAAAAB", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 101;
        }

        if (Div5() != ulong.Parse("CCCCCCCCCCCCCCCD", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 102;
        }

        if (ByteMask() != ulong.Parse("FF00FF00FF001234", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 103;
        }

        if (ZeroLow() != ulong.Parse("8000800080000000", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 104;
        }

        if (OnesLow() != ulong.Parse("FF00FF00FF00FFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 105;
        }

        if (SingleBitMask() != ulong.Parse("0001000100011234", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 106;
        }

        if (AllRepeated() != ulong.Parse("AAAAAAAAAAAAAAAA", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 107;
        }

        if (ZerosAbove() != ulong.Parse("0000000000001234", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 108;
        }

        if (OnesAbove() != ulong.Parse("FFFFFFFFFFFF1234", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 109;
        }

        if (NotEncodable() != ulong.Parse("1234123412345678", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 110;
        }

        if (MiddleDiffers() != ulong.Parse("AAAAAAAABBBBAAAB", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 111;
        }

        if (Arbitrary() != ulong.Parse("123456789ABCDEF0", NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        {
            return 112;
        }

        return 100;
    }
}
