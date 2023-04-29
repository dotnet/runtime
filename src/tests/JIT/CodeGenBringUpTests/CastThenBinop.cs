// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
using System;
using System.Runtime.CompilerServices;
using Xunit;

// Test for https://github.com/dotnet/runtime/issues/13816
public class Test_CastThenBinop
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int DowncastOr(int a, int b)
    {
        return (byte)a | (byte)b;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long UpcastAnd(int a, int b)
    {
        return (long)a & (long)b;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long UpcastAnd_ComplexExpression(int a, int b)
    {
        return (long)(a - 2) & (long)(b + 1);
    }
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long UpcastAnd_SideEffect(int a, int b, out int a1, out int b1)
    {
        return (long)(a1 = a) & (long)(b1 = b);
    }
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int DowncastAnd_Overflow(int a, int b)
    {
        checked
        {
            return (byte)a & (byte)b;
        }
    }

    // github issue 60597
    // https://github.com/dotnet/runtime/issues/60597
    //
    // I can only seem to reproduce the bug when _xorLeft and _xorRight are fields
    static sbyte _xorLeft = 0;
    static sbyte _xorRight = -1;
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static ushort UpcastXor_SignExtension()
    {
        return (ushort)(_xorLeft ^ (long)_xorRight);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        const int Pass = 100;
        const int Fail = -1;

        if (DowncastOr(0x0F, 0xF0) != 0xFF)
        {
            return Fail;
        }
        if (UpcastAnd(0x0FF, 0xFF0) != 0xF0)
        {
            return Fail;
        }

        try
        {
            DowncastAnd_Overflow(0x100, 0xFF);
            // should throw
            return Fail;
        }
        catch (OverflowException)
        {
            // expected
        }

        {
            var result = UpcastAnd_ComplexExpression(0x0FF, 0xFF0);
            if (result != 0xF1)
            {
                return Fail;
            }
        }
        {
            var result = UpcastAnd_SideEffect(0x0FF, 0xFF0, out var out1, out var out2);
            if (result != 0xF0 || out1 != 0x0FF || out2 != 0xFF0)
            {
                return Fail;
            }
        }

        {
            var result = UpcastXor_SignExtension();
            if (result != 65535)
            {
                return Fail;
            }
        }

        return Pass;
    }
}
