using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.InteropServices;

public class Test
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint Shlx32bit(uint x, int y) => x<< y;

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
    private static unsafe ulong ShrxRef64bit(ulong *x, int y) => *x >> y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Rorx(ulong x) => BitOperations.RotateRight(x, 2);

    public static unsafe int Main()
    {
        int returnCode = 100;

        try
        {
            uint  valUInt = 0;
            int   valInt = 0;
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
            valUInt = 0;
            shiftBy = 1;
            resUInt = Shlx32bit(valUInt, shiftBy);
            expectedUInt = (uint) (valUInt * Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Shlx32bit({0},{1}): {2}", valUInt, shiftBy, resUInt);
            if (resUInt != expectedUInt)
            {
                Console.Write(" != {0} Failed.\n", expectedUInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedUInt);

            valUInt = 8;
            shiftBy = 1;
            resUInt = Shlx32bit(valUInt, shiftBy);
            expectedUInt = (uint) (valUInt * Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Shlx32bit({0},{1}): {2}", valUInt, shiftBy, resUInt);
            if (resUInt != expectedUInt)
            {
                Console.Write(" != {0} Failed.\n", expectedUInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedUInt);

            valUInt = 1;
            shiftBy = 31;
            resUInt = Shlx32bit(valUInt, shiftBy);
            expectedUInt = (uint) (valUInt * Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Shlx32bit({0},{1}): {2}", valUInt, shiftBy, resUInt);
            if (resUInt != expectedUInt)
            {
                Console.Write(" != {0} Failed.\n", expectedUInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedUInt);

            valUInt = 1;
            shiftBy = 33;
            resUInt = Shlx32bit(valUInt, shiftBy);
            expectedUInt = (uint) (valUInt * Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Shlx32bit({0},{1}): {2}", valUInt, shiftBy, resUInt);
            if (resUInt != expectedUInt)
            {
                Console.Write(" != {0} Failed.\n", expectedUInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedUInt);

            // Test only on x64 and x86. There is a known undefined behavior for Arm64 and Arm.
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64 || RuntimeInformation.ProcessArchitecture == Architecture.X86)
            {
                valUInt = 0xFFFFFFFF;
                shiftBy = 1;
                resUInt = Shlx32bit(valUInt, shiftBy);
                expectedUInt = (uint)(valUInt * Math.Pow(2, (shiftBy % MOD32)));
                Console.Write("UnitTest Shlx32bit({0},{1}): {2}", valUInt, shiftBy, resUInt);
                if (resUInt != expectedUInt)
                {
                    Console.Write(" != {0} Failed.\n", expectedUInt);
                    returnCode = 101;
                }
                Console.Write(" == {0} Passed.\n", expectedUInt);
            }

            //
            // Sarx32bit tests
            //
            valInt = 0;
            shiftBy = 1;
            resInt = Sarx32bit(valInt, shiftBy);
            expectedInt = (int) (valInt / Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Sarx32bit({0},{1}): {2}", valInt, shiftBy, resInt);
            if (resInt != expectedInt)
            {
                Console.Write(" != {0} Failed.\n", expectedInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedInt);

            valInt = -8;
            shiftBy = 1;
            resInt = Sarx32bit(valInt, shiftBy);
            expectedInt = (int) (valInt / Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Sarx32bit({0},{1}): {2}", valInt, shiftBy, resInt);
            if (resInt != expectedInt)
            {
                Console.Write(" != {0} Failed.\n", expectedInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedInt);

            valInt = 1;
            shiftBy = 33;
            resInt = Sarx32bit(valInt, shiftBy);
            expectedInt = (int) (valInt / Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Sarx32bit({0},{1}): {2}", valInt, shiftBy, resInt);
            if (resInt != expectedInt)
            {
                Console.Write(" != {0} Failed.\n", expectedInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedInt);

            valInt = 0x7FFFFFFF;
            shiftBy = 33;
            resInt = Sarx32bit(valInt, shiftBy);
            expectedInt = (int) (valInt / Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Sarx32bit({0},{1}): {2}", valInt, shiftBy, resInt);
            if (resInt != expectedInt)
            {
                Console.Write(" != {0} Failed.\n", expectedInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedInt);

            valInt = 0x7FFFFFFF;
            shiftBy = 30;
            resInt = Sarx32bit(valInt, shiftBy);
            expectedInt = (int) (valInt / Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Sarx32bit({0},{1}): {2}", valInt, shiftBy, resInt);
            if (resInt != expectedInt)
            {
                Console.Write(" != {0} Failed.\n", expectedInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedInt);

            //
            // Shrx32bit tests
            //
            valUInt = 1;
            shiftBy = 1;
            resUInt = Shrx32bit(valUInt, shiftBy);
            expectedUInt = (uint) (valUInt / Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Shrx32bit({0},{1}): {2}", valUInt, shiftBy, resUInt);
            if (resUInt != expectedUInt)
            {
                Console.Write(" != {0} Failed.\n", expectedUInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedUInt);

            valUInt = 8;
            shiftBy = 2;
            resUInt = Shrx32bit(valUInt, shiftBy);
            expectedUInt = (uint) (valUInt / Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Shrx32bit({0},{1}): {2}", valUInt, shiftBy, resUInt);
            if (resUInt != expectedUInt)
            {
                Console.Write(" != {0} Failed.\n", expectedUInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedUInt);

            valUInt = 1;
            shiftBy = 33;
            resUInt = Shrx32bit(valUInt, shiftBy);
            expectedUInt = (uint) (valUInt / Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Shrx32bit({0},{1}): {2}", valUInt, shiftBy, resUInt);
            if (resUInt != expectedUInt)
            {
                Console.Write(" != {0} Failed.\n", expectedUInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedUInt);

            valUInt = 0xFFFFFFFF;
            shiftBy = 31;
            resUInt = Shrx32bit(valUInt, shiftBy);
            expectedUInt = (uint) (valUInt / Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Shrx32bit({0},{1}): {2}", valUInt, shiftBy, resUInt);
            if (resUInt != expectedUInt)
            {
                Console.Write(" != {0} Failed.\n", expectedUInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedUInt);

            valUInt = 0xFFFFFFFF;
            shiftBy = 33;
            resUInt = Shrx32bit(valUInt, shiftBy);
            expectedUInt = (uint) (valUInt / Math.Pow(2, (shiftBy % MOD32)));
            Console.Write("UnitTest Shrx32bit({0},{1}): {2}", valUInt, shiftBy, resUInt);
            if (resUInt != expectedUInt)
            {
                Console.Write(" != {0} Failed.\n", expectedUInt);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedUInt);

            //
            // Ror tests
            //
            valUInt = 0xFF;
            shiftBy = 2;
            resUInt = Ror(valUInt);
            Console.Write("UnitTest Ror({0},{1}): {2}", valUInt, shiftBy, resUInt);
            if (resUInt != 0xC000003F)
            {
                Console.Write(" Failed.\n");
                returnCode = 101;
            }
            Console.Write(" Passed.\n");

            //
            // Shlx64bit tests
            //
            valULong = 0;
            shiftBy = 1;
            resULong = Shlx64bit(valULong, shiftBy);
            expectedULong = (ulong) (valULong * Math.Pow(2, (shiftBy % MOD64)));
            Console.Write("UnitTest Shlx64bit({0},{1}): {2}", valULong, shiftBy, resULong);
            if (resULong != expectedULong)
            {
                Console.Write(" != {0} Failed.\n", expectedULong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedULong);

            valULong = 8;
            shiftBy = 1;
            resULong = Shlx64bit(valULong, shiftBy);
            expectedULong = (ulong) (valULong * Math.Pow(2, (shiftBy % MOD64)));
            Console.Write("UnitTest Shlx64bit({0},{1}): {2}", valULong, shiftBy, resULong);
            if (resULong != expectedULong)
            {
                Console.Write(" != {0} Failed.\n", expectedULong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedULong);

            valULong = 1;
            shiftBy = 63;
            resULong = Shlx64bit(valULong, shiftBy);
            expectedULong = (ulong) (valULong * Math.Pow(2, (shiftBy % MOD64)));
            Console.Write("UnitTest Shlx64bit({0},{1}): {2}", valULong, shiftBy, resULong);
            if (resULong != expectedULong)
            {
                Console.Write(" != {0} Failed.\n", expectedULong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedULong);

            valULong = 1;
            shiftBy = 65;
            resULong = Shlx64bit(valULong, shiftBy);
            expectedULong = (ulong) (valULong * Math.Pow(2, (shiftBy % MOD64)));
            Console.Write("UnitTest Shlx64bit({0},{1}): {2}", valULong, shiftBy, resULong);
            if (resULong != expectedULong)
            {
                Console.Write(" != {0} Failed.\n", expectedULong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedULong);

            valULong = 0xFFFFFFFFFFFFFFFF;
            shiftBy = 1;
            resULong = Shlx64bit(valULong, shiftBy);
            expectedULong = (ulong) (valULong ^ 1);
            Console.Write("UnitTest Shlx64bit({0},{1}): {2}", valULong, shiftBy, resULong);
            if (resULong != expectedULong)
            {
                Console.Write(" != {0} Failed.\n", expectedULong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedULong);

            //
            // Sarx64bit tests
            //
            valLong = 1;
            shiftBy = 1;
            resLong = Sarx64bit(valLong, shiftBy);
            expectedLong = (long) (valLong / Math.Pow(2, (shiftBy % MOD64)));
            Console.Write("UnitTest Sarx64bit({0},{1}): {2}", valLong, shiftBy, resLong);
            if (resLong != expectedLong)
            {
                Console.Write(" != {0} Failed.\n", expectedLong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedLong);

            valLong = -8;
            shiftBy = 1;
            resLong = Sarx64bit(valLong, shiftBy);
            expectedLong = (long) (valLong / Math.Pow(2, (shiftBy % MOD64)));
            Console.Write("UnitTest Sarx64bit({0},{1}): {2}", valLong, shiftBy, resLong);
            if (resLong != expectedLong)
            {
                Console.Write(" != {0} Failed.\n", expectedLong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedLong);

            valLong = -8;
            shiftBy = 65;
            resLong = Sarx64bit(valLong, shiftBy);
            expectedLong = (long) (valLong / Math.Pow(2, (shiftBy % MOD64)));
            Console.Write("UnitTest Sarx64bit({0},{1}): {2}", valLong, shiftBy, resLong);
            if (resLong != expectedLong)
            {
                Console.Write(" != {0} Failed.\n", expectedLong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedLong);

            valLong = 0x7FFFFFFFFFFFFFFF;
            shiftBy = 63;
            resLong = Sarx64bit(valLong, shiftBy);
            expectedLong = 0;
            Console.Write("UnitTest Sarx64bit({0},{1}): {2}", valLong, shiftBy, resLong);
            if (resLong != expectedLong)
            {
                Console.Write(" != {0} Failed.\n", expectedLong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedLong);

            valLong = 0x7FFFFFFFFFFFFFFF;
            shiftBy = 65;
            shiftBy = (int) Math.Pow(2, (shiftBy % MOD64));
            resLong = Sarx64bit(valLong, shiftBy);
            expectedLong = 0x1FFFFFFFFFFFFFFF;
            Console.Write("UnitTest Sarx64bit({0},{1}): {2}", valLong, shiftBy, resLong);
            if (resLong != expectedLong)
            {
                Console.Write(" != {0} Failed.\n", expectedLong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedLong);

            //
            // Shrx64bit tests
            //
            valULong = 1;
            shiftBy = 1;
            resULong = Shrx64bit(valULong, shiftBy);
            expectedULong = (ulong) (valULong / Math.Pow(2, (shiftBy % MOD64)));
            Console.Write("UnitTest Shrx64bit({0},{1}): {2}", valULong, shiftBy, resULong);
            if (resULong != expectedULong)
            {
                Console.Write(" != {0} Failed.\n", expectedULong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedULong);

            valULong = 8;
            shiftBy = 2;
            resULong = Shrx64bit(valULong, shiftBy);
            expectedULong = (ulong) (valULong / Math.Pow(2, (shiftBy % MOD64)));
            Console.Write("UnitTest Shrx64bit({0},{1}): {2}", valULong, shiftBy, resULong);
            if (resULong != expectedULong)
            {
                Console.Write(" != {0} Failed.\n", expectedULong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedULong);

            valULong = 0xFFFFFFFFFFFFFFFF;
            shiftBy = 63;
            resULong = Shrx64bit(valULong, shiftBy);
            expectedULong = 1;
            Console.Write("UnitTest Shrx64bit({0},{1}): {2}", valULong, shiftBy, resULong);
            if (resULong != expectedULong)
            {
                Console.Write(" != {0} Failed.\n", expectedULong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedULong);

            valULong = 0x7FFFFFFFFFFFFFFF;
            shiftBy = 65;
            resULong = Shrx64bit(valULong, shiftBy);
            expectedULong = 0x3FFFFFFFFFFFFFFF;
            Console.Write("UnitTest Shrx64bit({0},{1}): {2}", valULong, shiftBy, resULong);
            if (resULong != expectedULong)
            {
                Console.Write(" != {0} Failed.\n", expectedULong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedULong);

            valULong = 8;
            shiftBy = 65;
            resULong = Shrx64bit(valULong, shiftBy);
            //expectedULong = 4;
            expectedULong = (ulong) (valULong / Math.Pow(2, (shiftBy % MOD64)));
            Console.Write("UnitTest Shrx64bit({0},{1}): {2}", valULong, shiftBy, resULong);
            if (resULong != expectedULong)
            {
                Console.Write(" != {0} Failed.\n", expectedULong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedULong);

            //
            // ShrxRef64bit
            //
            valULong = 8;
            shiftBy = 1;
            resULong = ShrxRef64bit(&valULong, shiftBy);
            expectedULong = (ulong) (valULong / Math.Pow(2, (shiftBy % MOD64)));
            Console.Write("UnitTest ShrxRef64bit({0},{1}): {2}", valULong, shiftBy, resULong);
            if (resULong != expectedULong)
            {
                Console.Write(" != {0} Failed.\n", expectedULong);
                returnCode = 101;
            }
            Console.Write(" == {0} Passed.\n", expectedULong);

            //
            // Rorx tests
            //
            valULong = 0xFF;
            shiftBy = 2;
            resULong = Rorx(valULong);
            Console.Write("UnitTest Rorx({0},{1}): {2}", valULong, shiftBy, resULong);
            if (resULong != 0xC00000000000003F)
            {
                Console.Write(" Failed.\n");
                returnCode = 101;
            }
            Console.Write(" Passed.\n");	
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 101;
        }

        if (returnCode == 101)
        {
            Console.WriteLine("FAILED");
        }
        else if (returnCode == 100)
        {
            Console.WriteLine("PASSED");
        }

        return returnCode;
    }
}
