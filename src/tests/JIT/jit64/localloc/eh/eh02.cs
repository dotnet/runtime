// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * Test reading localloc variable from finally block.
 */

using System;
using LocallocTesting;
using Xunit;

public class LocallocTest
{
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        bool testPassed = true;
        ulong local1 = Global.INITIAL_VALUE;
        ulong local2 = local1 + 1;
        int size = 0;
#if LOCALLOC_SMALL
		Int32* intArray1 = stackalloc Int32[1];
		Int32* intArray2 = stackalloc Int32[1];
		size = 1;
#elif LOCALLOC_LARGE
		Int32* intArray1 = stackalloc Int32[0x1000];
		Int32* intArray2 = stackalloc Int32[0x1000];
		size = 0x1000;
#else
        Int32* intArray1 = stackalloc Int32[Global.stackAllocSize];
        Int32* intArray2 = stackalloc Int32[Global.stackAllocSize];
        size = Global.stackAllocSize;
#endif
        try
        {
            try
            {
                Global.initializeStack(intArray1, size, 1000);
                Global.initializeStack(intArray2, size, 2000);
                throw new Exception("Test Exception");
            }
            finally
            {
                if (!Global.verifyStack("intArray1", intArray1, size, 1000))
                {
                    testPassed = false;
                }
                if (!Global.verifyStack("intArray2", intArray2, size, 2000))
                {
                    testPassed = false;
                }
            }
        }
        catch
        {
        }
        if (!testPassed)
            return 1;

        if (!Global.verifyStack("intArray1", intArray1, size, 1000))
        {
            return 1;
        }
        if (!Global.verifyStack("intArray2", intArray2, size, 2000))
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
}
