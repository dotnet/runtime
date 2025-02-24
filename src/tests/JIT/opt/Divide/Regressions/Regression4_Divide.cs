// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Program
{
    private static ushort s_field1;
    private static ulong s_field2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Umod_U4_CharByZero(char c)
    {
        return (uint)c % 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ushort Umod_U2_CharByConst(char c)
    {
        return (ushort)(c % 42);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Umod_I4_CharByConst(char c)
    {
        return c % 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Umod_U4_CharByConst(char c)
    {
        return (uint)c % 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Umod_I8_CharByConst(char c)
    {
        return (long)c % 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong Umod_U8_CharByConst(char c)
    {
        return (ulong)c % 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool TestIsWhiteSpace(char c)
    {
        ReadOnlySpan<char> HashEntries = [' ', ' ', '\u00A0', ' ', ' ', ' ', ' ', ' ', ' ', '\t', '\n', '\v', '\f', '\r', ' ', ' ', '\u2028', '\u2029', ' ', ' ', ' ', ' ', ' ', '\u202F', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '\u3000', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '\u0085', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006', '\u2007', '\u2008', '\u2009', '\u200A', ' ', ' ', ' ', ' ', ' ', '\u205F', '\u1680', ' ', ' ', ' ', ' ', ' ', ' '];
        return HashEntries[c % HashEntries.Length] == c;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ushort Umod_TZC(ulong value)
    {
        return (ushort)(BitOperations.TrailingZeroCount(value) % 42);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ushort Umod_TZC_Intrinsic(ulong value)
    {
        return (ushort)(Bmi1.X64.TrailingZeroCount(value) % 42);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Umod_UInt16Range_ByConst(int value)
    {
        if (value is > 0 and < 1234)
        {
            return value % 123;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void Test1()
    {
        for (int i = 0; i < 2; i++)
        {
            Core();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Core()
        {
            s_field1 = (ushort)(Bmi1.X64.TrailingZeroCount(s_field2) % 42);
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Umod_U4_CharByZero('a');
            return 0;
        }
        catch (DivideByZeroException) { }

        if (Umod_U2_CharByConst('a') != 13)
            return 0;

        if (Umod_I4_CharByConst('a') != 13)
            return 0;

        if (Umod_U4_CharByConst('a') != 13)
            return 0;

        if (Umod_I8_CharByConst('a') != 13)
            return 0;

        if (Umod_U8_CharByConst('a') != 13)
            return 0;

        if (!TestIsWhiteSpace(' '))
            return 0;

        if (!TestIsWhiteSpace('\u2029'))
            return 0;

        if (TestIsWhiteSpace('\0'))
            return 0;

        if (TestIsWhiteSpace('a'))
            return 0;

        if (Umod_TZC(1L << 40) != 40)
            return 0;

        if (Umod_TZC(1L << 50) != 8)
            return 0;

        if (Bmi1.X64.IsSupported)
        {
            if (Umod_TZC_Intrinsic(1L << 40) != 40)
                return 0;

            if (Umod_TZC_Intrinsic(1L << 50) != 8)
                return 0;
        }

        if (Umod_UInt16Range_ByConst(0) != -1)
            return 0;

        if (Umod_UInt16Range_ByConst(42) != 42)
            return 0;

        if (Umod_UInt16Range_ByConst(123) != 0)
            return 0;

        if (Bmi1.X64.IsSupported)
        {
            s_field1 = 1;
            s_field2 = 1L << 50;
            Test1();

            if (s_field1 != 8)
                return 0;
        }

        return 100;
    }
}
