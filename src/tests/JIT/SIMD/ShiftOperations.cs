// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Xunit;

public class Test
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static R Shlx64bit<T, R>(T x, int y)
    {
        switch (x)
        {
            case ulong a:
                ulong resUlong = ((ulong)a) << y;
                return (R)Convert.ChangeType(resUlong, typeof(R));
            case uint b:
                uint resUint = ((uint)b) << y;
                return (R)Convert.ChangeType(resUint, typeof(R));
            case ushort c:
                int resInt = ((ushort)c) << y;
                return (R)Convert.ChangeType(resInt, typeof(R));
            default:
                Console.WriteLine("Unsupported type.");
                return default(R);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static R Sarx64bit<T, R>(T x, int y)
    {
        int resInt = 0;
        switch (x)
        {
            case long a:
                long resLong = ((long)a) >> y;
                return (R)Convert.ChangeType(resLong, typeof(R));
            case int b:
                resInt = ((int)b) >> y;
                return (R)Convert.ChangeType(resInt, typeof(R));
            case short c:
                Console.WriteLine($"Before: {Convert.ToString((short)c, toBase: 2)}");
                resInt = ((short)c) >> y;
                Console.WriteLine($"After: {Convert.ToString(resInt, toBase: 2)}");
                return (R)Convert.ChangeType(resInt, typeof(R));
            default:
                Console.WriteLine("Unsupported type.");
                return default(R);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static R Shrx64bit<T, R>(T x, int y)
    {
        switch (x)
        {
            case ulong a:
                ulong resUlong = ((ulong)a) >> y;
                return (R)Convert.ChangeType(resUlong, typeof(R));
            case uint b:
                uint resUint = ((uint)b) >> y;
                return (R)Convert.ChangeType(resUint, typeof(R));
            case ushort c:
                int resInt = ((ushort)c) >> y;
                return (R)Convert.ChangeType(resInt, typeof(R));
            default:
                Console.WriteLine("Unsupported type.");
                return default(R);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe ulong ShrxRef64bit(ulong* x, int y) => *x >> y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe uint ShrxRef64bit(uint* x, int y) => *x >> y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int ShrxRef64bit(ushort* x, int y) => *x >> y;

    [Fact]
    public static unsafe int TestEntryPoint()
    {
        const int PASS = 100;
        const int FAIL = 101;
        int returnCode = PASS;

        try
        {
            //
            // Shlx64bit tests
            //

            // ulong
            int MOD64 = 64;

            Console.WriteLine();
            Console.WriteLine("### UnitTest: Shlx64bit (ulong) ###############");
            ulong[] valUlong = new ulong[] { 0, 8, 1, 1, 0xFFFFFFFFFFFFFFFF };
            int[] shiftBy = new int[] { 1, 1, 63, 65, 1 };
            for (int idx = 0; idx < valUlong.Length; idx++)
            {
                ulong resULong = (ulong)Shlx64bit<ulong, ulong>(valUlong[idx], shiftBy[idx]);
                ulong expectedUlong = (ulong)(valUlong[idx] << (shiftBy[idx] % MOD64));
                if (!Validate<ulong, ulong>(valUlong[idx], shiftBy[idx], resULong, expectedUlong))
                {
                    returnCode = FAIL;
                }
            }

            // uint
            int MOD32 = 32;

            Console.WriteLine();
            Console.WriteLine("### UnitTest: Shlx64bit (uint) ###############");
            uint[] valUint = new uint[] { 0, 8, 1, 1, 0xFFFFFFFF };
            shiftBy = new int[] { 1, 1, 32, 33, 1 };
            for (int idx = 0; idx < valUint.Length; idx++)
            {
                uint resUint = (uint)Shlx64bit<uint, uint>(valUint[idx], shiftBy[idx]);
                uint expectedUint = (uint)(valUint[idx] << (shiftBy[idx] % MOD32));
                if (!Validate<uint, uint>(valUint[idx], shiftBy[idx], resUint, expectedUint))
                {
                    returnCode = FAIL;
                }
            }

            // ushort
            Console.WriteLine();
            Console.WriteLine("### UnitTest: Shlx64bit (ushort) ###############");
            ushort[] valUshort = new ushort[] { 0, 8, 1, 1, 0b_0111_0001_1000_0010 };
            shiftBy = new int[] { 1, 1, 16, 18, 16 };
            for (int idx = 0; idx < valUshort.Length; idx++)
            {
                int resInt = (int)Shlx64bit<ushort, int>(valUshort[idx], shiftBy[idx]);
                int expectedInt = (int)(((int)valUshort[idx]) << (shiftBy[idx] % MOD32));
                if (!Validate<ushort, int>(valUshort[idx], shiftBy[idx], resInt, expectedInt))
                {
                    returnCode = FAIL;
                }
            }

            //
            // Sarx64bit tests
            //

            // long
            Console.WriteLine();
            Console.WriteLine("### UnitTest: Sarx64bit (long) ###############");
            long[] valLong = new long[] { 1, -8, -8, 0x7FFFFFFFFFFFFFFF };
            shiftBy = new int[] { 1, 1, 65, 63 };
            for (int idx = 0; idx < valLong.Length; idx++)
            {
                long resLong = (long)Sarx64bit<long, long>(valLong[idx], shiftBy[idx]);
                long expectedLong = (long)(valLong[idx] >> (shiftBy[idx] % MOD64));
                if (!Validate<long, long>(valLong[idx], shiftBy[idx], resLong, expectedLong))
                {
                    returnCode = FAIL;
                }
            }

            // int
            Console.WriteLine();
            Console.WriteLine("### UnitTest: Sarx64bit (int) ###############");
            int[] valInt = new int[] { 1, -8, -8, 0x7FFFFFFF };
            shiftBy = new int[] { 1, 1, 32, 33 };
            for (int idx = 0; idx < valInt.Length; idx++)
            {
                int resInt = (int)Sarx64bit<int, int>(valInt[idx], shiftBy[idx]);
                int expectedInt = (int)(valInt[idx] >> (shiftBy[idx] % MOD32));
                if (!Validate<int, int>(valInt[idx], shiftBy[idx], resInt, expectedInt))
                {
                    returnCode = FAIL;
                }
            }

            // short
            Console.WriteLine();
            Console.WriteLine("### UnitTest: Sarx64bit (short) ###############");
            short[] valShort = new short[] { 1, -8, -8, 0b_0111_0001_1000_0010 };
            shiftBy = new int[] { 1, 1, 16, 18 };
            for (int idx = 0; idx < valShort.Length; idx++)
            {
                int resInt = (int)Sarx64bit<short, int>(valShort[idx], shiftBy[idx]);
                int expectedInt = (int)valShort[idx] >> (shiftBy[idx] % MOD32);
                if (!Validate<short, int>(valShort[idx], shiftBy[idx], resInt, expectedInt))
                {
                    returnCode = FAIL;
                }
            }

            //
            // Shrx64bit tests
            //

            // ulong
            Console.WriteLine();
            Console.WriteLine("### UnitTest: Shrx64bit (ulong) ###############");
            valUlong = new ulong[] { 1, 8, 8, 0xFFFFFFFFFFFFFFFF, 0x7FFFFFFFFFFFFFFF };
            shiftBy = new int[] { 1, 2, 65, 63, 65 };
            for (int idx = 0; idx < valUlong.Length; idx++)
            {
                ulong resULong = (ulong)Shrx64bit<ulong, ulong>(valUlong[idx], shiftBy[idx]);
                ulong expectedUlong = (ulong)(valUlong[idx] >> (shiftBy[idx] % MOD64));
                if (!Validate<ulong, ulong>(valUlong[idx], shiftBy[idx], resULong, expectedUlong))
                {
                    returnCode = FAIL;
                }
            }

            // uint
            Console.WriteLine();
            Console.WriteLine("### UnitTest: Shrx64bit (uint) ###############");
            valUint = new uint[] { 1, 8, 8, 0xFFFFFFFF };
            shiftBy = new int[] { 1, 1, 32, 33 };
            for (int idx = 0; idx < valUint.Length; idx++)
            {
                uint resUint = (uint)Shrx64bit<uint, uint>(valUint[idx], shiftBy[idx]);
                uint expectedUint = (uint)(valUint[idx] >> (shiftBy[idx] % MOD32));
                if (!Validate<uint, uint>(valUint[idx], shiftBy[idx], resUint, expectedUint))
                {
                    returnCode = FAIL;
                }
            }

            // ushort
            Console.WriteLine();
            Console.WriteLine("### UnitTest: Shrx64bit (ushort) ###############");
            valUshort = new ushort[] { 0, 8, 0b_1000_0000_0000_0000, 0b_1000_0000_0000_0000, 0b_1111_0001_1000_0010 };
            shiftBy = new int[] { 1, 1, 15, 18, 40 };
            for (int idx = 0; idx < valUshort.Length; idx++)
            {
                int resInt = (int)Shrx64bit<ushort, int>(valUshort[idx], shiftBy[idx]);
                int expectedInt = (int)(((int)valUshort[idx]) >> (shiftBy[idx] % MOD32));
                if (!Validate<ushort, int>(valUshort[idx], shiftBy[idx], resInt, expectedInt))
                {
                    returnCode = FAIL;
                }
            }

            //
            // ShrxRef64bit
            //

            // ulong
            Console.WriteLine();
            Console.WriteLine("### UnitTest: ShrxRef64bit (ulong) ###############");
            ulong valUlongRef = 8;
            int shiftByRef = 1;
            ulong resUlongRef = ShrxRef64bit(&valUlongRef, shiftByRef);
            ulong expectedULongRef = (ulong)(valUlongRef >> (shiftByRef % MOD64));
            if (!Validate<ulong, ulong>(valUlongRef, shiftByRef, resUlongRef, expectedULongRef))
            {
                returnCode = FAIL;
            }

            // uint
            Console.WriteLine();
            Console.WriteLine("### UnitTest: ShrxRef64bit (uint) ###############");
            uint valUintRef = 0xFFFFFFFF;
            shiftByRef = 1;
            uint resUintRef = ShrxRef64bit(&valUintRef, shiftByRef);
            uint expectedUintRef = (uint)(valUintRef >> (shiftByRef % MOD32));
            if (!Validate<uint, uint>(valUintRef, shiftByRef, resUintRef, expectedUintRef))
            {
                returnCode = FAIL;
            }

            // ushort
            Console.WriteLine();
            Console.WriteLine("### UnitTest: ShrxRef64bit (ushort) ###############");
            ushort valUshortRef = 0xFFFF;
            shiftByRef = 15;
            int resUshortRef = ShrxRef64bit(&valUshortRef, shiftByRef);
            int expectedUshortRef = (int)((uint)valUshortRef >> (shiftByRef % MOD32));
            if (!Validate<ushort, int>(valUshortRef, shiftByRef, resUshortRef, expectedUshortRef))
            {
                returnCode = FAIL;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return FAIL;
        }

        Console.WriteLine();
        if (returnCode == PASS)
        {
            Console.WriteLine("PASSED.");
        }
        else
        {
            Console.WriteLine("FAILED.");
        }
        return returnCode;
    }

    private static bool Validate<TValue, TResult>(TValue value, int shiftBy, TResult actual, TResult expected)
    {
        Console.Write("(value, shiftBy) ({0},{1}): {2}", value, shiftBy, actual);
        if (EqualityComparer<TResult>.Default.Equals(actual, expected))
        {
            Console.WriteLine(" == {0}   ==> Passed.", expected);
            return true;
        }
        else
        {
            Console.WriteLine(" != {0}    ==> Failed.", expected);
            return false;
        }
    }
}
