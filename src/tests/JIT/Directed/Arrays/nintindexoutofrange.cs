// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
using System;
using System.Runtime.CompilerServices;
using Xunit;

public class NintIndexOutOfRangeTest
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Stelem_Ref(object[] arr, nint i, Object value)
        => arr[i] = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void LdElemATestHelper(ref object nothingOfInterest)
    {}

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void LdElemA(object[] arr, nint i)
    {
        LdElemATestHelper(ref arr[i]);
    }

    [Fact]
    public static unsafe int TestEntryPoint()
    {
        long longIndex = ((long)1) << 32;
        nint index = (nint)longIndex;
        bool failed = false;

        // On a 32bit platform, just succeed.
        if (sizeof(long) != sizeof(nint))
            return 100;

        var arr = new Object[10];
        // Try store to invalid index with null
        try
        {
            Stelem_Ref(arr, index, null);
            failed = true;
            Console.WriteLine("Failed to throw IndexOutOfRange when storing null");
        }
        catch (IndexOutOfRangeException) {}

        // Try store to invalid index with actual value
        try
        {
            Stelem_Ref(arr, index, new object());
            failed = true;
            Console.WriteLine("Failed to throw IndexOutOfRange when storing object");
        }
        catch (IndexOutOfRangeException) {}

        // Try to load element address
        try
        {
            LdElemA(arr, index);
            failed = true;
            Console.WriteLine("Failed to throw IndexOutOfRange when accessing element");
        }
        catch (IndexOutOfRangeException) {}

        if (failed)
            return 1;
        else
            return 100;
    }
}
