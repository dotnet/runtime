// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class Test
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Shlx32bit(uint x, int y) => x << y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Sarx32bit(int x, int y) => x >> y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Shrx32bit(uint x, int y) => x >> y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Ror(uint x) => BitOperations.RotateRight(x, 2);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Shlx64bit(ulong x, int y) => x << y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Sarx64bit(long x, int y) => x >> y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Shrx64bit(ulong x, int y) => x >> y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe ulong ShrxRef64bit(ulong* x, int y) => *x >> y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Rorx(ulong x) => BitOperations.RotateRight(x, 2);

    public static unsafe int Main()
    {
        const int PASS = 100;
        const int FAIL = 101;
        int returnCode = PASS;

        try
        {
            uint valUInt = 0;
            int valInt = 0;
            ulong valULong = 0;
            long valLong = 0;
            int shiftBy = 0;
            uint resUInt = 0;
            int resInt = 0;
            ulong resULong = 0;
            long resLong = 0;
            uint expectedUInt = 0;
            int expectedInt = 0;
            ulong expectedULong = 0;
            long expectedLong = 0;
            int MOD32 = 32;
            int MOD64 = 64;

            //
            // Shlx32bit tests
            //

            Console.WriteLine("### UnitTest: Shlx32bit ###############");
            uint[] valShlx32bit = new uint[] { 0, 8, 1, 1 };
            int[] shiftByShlx32bit = new int[] { 1, 1, 31, 33 };
            for (int idx = 0; idx < valShlx32bit.Length; idx++)
            {
                valUInt = valShlx32bit[idx];
                shiftBy = shiftByShlx32bit[idx];
                resUInt = Shlx32bit(valUInt, shiftBy);
                expectedUInt = (uint)(valUInt * Math.Pow(2, (shiftBy % MOD32)));
                if (!Validate<uint>(valUInt, shiftBy, resUInt, expectedUInt))
                {
                    returnCode = FAIL;
                }
            }

            // Test only on x64 and x86. There is a known undefined behavior for Arm64 and Arm.
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64 || RuntimeInformation.ProcessArchitecture == Architecture.X86)
            {
                valUInt = 0xFFFFFFFF;
                shiftBy = 1;
                resUInt = Shlx32bit(valUInt, shiftBy);
                expectedUInt = (uint)(valUInt * Math.Pow(2, (shiftBy % MOD32)));
                if (!Validate<uint>(valUInt, shiftBy, resUInt, expectedUInt))
                {
                    returnCode = FAIL;
                }
            }

            //
            // Sarx32bit tests
            //

            Console.WriteLine("### UnitTest: Sarx32bit ###############");
            int[] valSarx32bit = new int[] { 0, -8, 1, 0x7FFFFFFF, 0x7FFFFFFF };
            int[] shiftBySarx32bit = new int[] { 1, 1, 33, 33, 30 };
            for (int idx = 0; idx < valSarx32bit.Length; idx++)
            {
                valInt = valSarx32bit[idx];
                shiftBy = shiftBySarx32bit[idx];
                resInt = Sarx32bit(valInt, shiftBy);
                expectedInt = (int)(valInt / Math.Pow(2, (shiftBy % MOD32)));
                if (!Validate<int>(valInt, shiftBy, resInt, expectedInt))
                {
                    returnCode = FAIL;
                }
            }

            //
            // Shrx32bit tests
            //

            Console.WriteLine("### UnitTest: Shrx32bit ###############");
            uint[] valShrx32bit = new uint[] { 1, 8, 1, 0xFFFFFFFF, 0xFFFFFFFF };
            int[] shiftByShrx32bit = new int[] { 1, 2, 33, 31, 33 };
            for (int idx = 0; idx < valShrx32bit.Length; idx++)
            {
                valUInt = valShrx32bit[idx];
                shiftBy = shiftByShrx32bit[idx];
                resUInt = Shrx32bit(valUInt, shiftBy);
                expectedUInt = (uint)(valUInt / Math.Pow(2, (shiftBy % MOD32)));
                if (!Validate<uint>(valUInt, shiftBy, resUInt, expectedUInt))
                {
                    returnCode = FAIL;
                }
            }

            //
            // Ror tests
            //

            Console.WriteLine("### UnitTest: Ror ###############");
            valUInt = 0xFF;
            shiftBy = 2;
            resUInt = Ror(valUInt);
            expectedUInt = 0xC000003F;
            if (!Validate<uint>(valUInt, shiftBy, resUInt, expectedUInt))
            {
                returnCode = FAIL;
            }

            //
            // Shlx64bit tests
            //

            Console.WriteLine("### UnitTest: Shlx64bit ###############");
            ulong[] valShlx64bit = new ulong[] { 0, 8, 1, 1, 0xFFFFFFFFFFFFFFFF };
            int[] shiftByShlx64bit = new int[] { 1, 1, 63, 65, 1 };
            for (int idx = 0; idx < valShlx64bit.Length; idx++)
            {
                valULong = valShlx64bit[idx];
                shiftBy = shiftByShlx64bit[idx];
                resULong = Shlx64bit(valULong, shiftBy);
                if (idx == 4)
                    expectedULong = (ulong)(valULong ^ 1);
                else
                    expectedULong = (ulong)(valULong * Math.Pow(2, (shiftBy % MOD64)));
                if (!Validate<ulong>(valULong, shiftBy, resULong, expectedULong))
                {
                    returnCode = FAIL;
                }
            }

            //
            // Sarx64bit tests
            //

            Console.WriteLine("### UnitTest: Sarx64bit ###############");
            long[] valSarx64bit = new long[] { 1, -8, -8, 0x7FFFFFFFFFFFFFFF };
            int[] shiftBySarx64bit = new int[] { 1, 1, 65, 63 };
            for (int idx = 0; idx < valSarx64bit.Length; idx++)
            {
                valLong = valSarx64bit[idx];
                shiftBy = shiftBySarx64bit[idx];
                resLong = Sarx64bit(valLong, shiftBy);
                if (idx == 3)
                    expectedLong = 0;
                else
                    expectedLong = (long)(valLong / Math.Pow(2, (shiftBy % MOD64)));
                if (!Validate<long>(valLong, shiftBy, resLong, expectedLong))
                {
                    returnCode = FAIL;
                }
            }

            //
            // Shrx64bit tests
            //

            Console.WriteLine("### UnitTest: Shrx64bit ###############");
            ulong[] valShrx64bit = new ulong[] { 1, 8, 8, 0xFFFFFFFFFFFFFFFF, 0x7FFFFFFFFFFFFFFF };
            int[] shiftByShrx64bit = new int[] { 1, 2, 65, 63, 65 };
            for (int idx = 0; idx < valShrx64bit.Length; idx++)
            {
                valULong = valShrx64bit[idx];
                shiftBy = shiftByShrx64bit[idx];
                resULong = Shrx64bit(valULong, shiftBy);
                if (idx == 3)
                    expectedULong = 1;
                else if (idx == 4)
                    expectedULong = 0x3FFFFFFFFFFFFFFF;
                else
                    expectedULong = (ulong)(valULong / Math.Pow(2, (shiftBy % MOD64)));
                if (!Validate<ulong>(valULong, shiftBy, resULong, expectedULong))
                {
                    returnCode = FAIL;
                }
            }

            //
            // ShrxRef64bit
            //

            Console.WriteLine("### UnitTest: ShrxRef64bit ###############");
            valULong = 8;
            shiftBy = 1;
            resULong = ShrxRef64bit(&valULong, shiftBy);
            expectedULong = (ulong)(valULong / Math.Pow(2, (shiftBy % MOD64)));
            if (!Validate<ulong>(valULong, shiftBy, resULong, expectedULong))
            {
                returnCode = FAIL;
            }

            //
            // Rorx tests
            //

            Console.WriteLine("### UnitTest: Rorx ###############");
            valULong = 0xFF;
            shiftBy = 2;
            resULong = Rorx(valULong);
            expectedULong = 0xC00000000000003F;
            if (!Validate<ulong>(valULong, shiftBy, resULong, expectedULong))
            {
                returnCode = FAIL;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return FAIL;
        }

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

    private static bool Validate<T>(T value, int shiftBy, T actual, T expected)
    {
        Console.Write("(value, shiftBy) ({0},{1}): {2}", value, shiftBy, actual);
        if (EqualityComparer<T>.Default.Equals(actual, expected))
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
