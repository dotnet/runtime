// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * Test reading localloc variable with function call.
 */

using System;
using LocallocTesting;
using Xunit;

namespace Test_call01_cs
{
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
        Int32* intArray2 = stackalloc Int32[1];
        s_locallocSize = 1;
#elif LOCALLOC_LARGE
		Int32* intArray1 = stackalloc Int32[0x1000];
		Int32* intArray2 = stackalloc Int32[0x1000];
		locallocSize = 0x1000;
#else
		Int32* intArray1 = stackalloc Int32[Global.stackAllocSize];
		Int32* intArray2 = stackalloc Int32[Global.stackAllocSize];
		locallocSize = Global.stackAllocSize;
#endif
        Global.initializeStack(intArray1, s_locallocSize, 1000);
        Global.initializeStack(intArray2, s_locallocSize, 2000);

        if (!func1(1, 2, 3, 4, 5, 6, 7, 8, intArray1, intArray2))
            return 1;

        if (!Global.verifyStack("intArray1", intArray1, s_locallocSize, 1000))
        {
            return 1;
        }
        if (!Global.verifyStack("intArray2", intArray2, s_locallocSize, 2000))
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

    private static unsafe bool func1(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, Int32* ar1, Int32* ar2)
    {
#if LOCALLOC_SMALL
        Int32* intArray1 = stackalloc Int32[1];
        Int32* intArray2 = stackalloc Int32[1];
#elif LOCALLOC_LARGE
		Int32* intArray1 = stackalloc Int32[0x1000];
		Int32* intArray2 = stackalloc Int32[0x1000];
#else
		Int32* intArray1 = stackalloc Int32[Global.stackAllocSize];
		Int32* intArray2 = stackalloc Int32[Global.stackAllocSize];
#endif
        Global.initializeStack(intArray1, s_locallocSize, 3000);
        Global.initializeStack(intArray2, s_locallocSize, 4000);

        if (!Global.verifyStack("ar1", ar1, s_locallocSize, 1000))
        {
            return false;
        }
        if (!Global.verifyStack("ar2", ar2, s_locallocSize, 2000))
        {
            return false;
        }
        if (!Global.verifyStack("intArray1", intArray1, s_locallocSize, 3000))
        {
            return false;
        }
        if (!Global.verifyStack("intArray2", intArray2, s_locallocSize, 4000))
        {
            return false;
        }
        return true;
    }
}
}
