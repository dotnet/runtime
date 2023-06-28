// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * Test unwinding from catch block.
 */

using System;
using LocallocTesting;
using Xunit;

public class LocallocTest
{
    private static int s_locallocSize = 0;
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        ulong local1 = Global.INITIAL_VALUE;
        ulong local2 = local1 + 1;
#if LOCALLOC_SMALL
		Int32* intArray1 = stackalloc Int32[1];
		s_locallocSize = 1;
#elif LOCALLOC_LARGE
		Int32* intArray1 = stackalloc Int32[0x1000];
		s_locallocSize = 0x1000;
#else

        Int32* intArray1 = stackalloc Int32[Global.stackAllocSize];
        s_locallocSize = Global.stackAllocSize;
#endif
        try
        {
            Global.initializeStack(intArray1, s_locallocSize, 1000);
            unwindTest1();
            return 1;
        }
        catch
        {
        }
        if (!Global.verifyStack("intArray1", intArray1, s_locallocSize, 1000))
        {
            return 1;
        }
        if (!Global.verifyLocal("local1", local1, Global.INITIAL_VALUE))
        {
            return 1;
        }
        if (!Global.verifyLocal("local2", local2, Global.INITIAL_VALUE + 1))
        {
            return 1;
        }
        Console.WriteLine("Passed\n");
        return 100;
    }

    internal unsafe static void unwindTest1()
    {
#if LOCALLOC_SMALL
		Int32* intArray2 = stackalloc Int32[1];
#elif LOCALLOC_LARGE
		Int32* intArray2 = stackalloc Int32[0x1000];
#else
        Int32* intArray2 = stackalloc Int32[Global.stackAllocSize];
#endif
        Global.initializeStack(intArray2, s_locallocSize, 3000);

        try
        {
            LocallocTest testObj = new LocallocTest();
            testObj.unwindTest2(1, 2, 3, 4, 5, 6, 7, 8, 9);
        }
        catch
        {
            Console.WriteLine("catch block in unwindTest1.\n");
            if (Global.verifyStack("intArray2", intArray2, s_locallocSize, 3000))
            {
                throw;
            }
        }
        return;
    }

    private unsafe void unwindTest2(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9)
    {
#if LOCALLOC_SMALL
		Int32* intArray3 = stackalloc Int32[1];
#elif LOCALLOC_LARGE
		Int32* intArray3 = stackalloc Int32[0x1000];
#else
        Int32* intArray3 = stackalloc Int32[Global.stackAllocSize];
#endif
        Global.initializeStack(intArray3, s_locallocSize, 4000);
        try
        {
            throw new Exception("Test Exception");
        }
        catch
        {
            Console.WriteLine("catch block in unwindTest2.\n");
            if (i9 != 9)
            {
                Console.WriteLine("Parameters on stack is corrupted\n");
                return;
            }

            if (Global.verifyStack("intArray3", intArray3, s_locallocSize, 4000))
            {
                throw;
            }
        }
    }
}
