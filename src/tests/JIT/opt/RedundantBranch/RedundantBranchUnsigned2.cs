// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

public class RedundantBranchUnsigned2
{
    [Fact]
    public static void TestEntryPoint()
    {
        int[] arr1 = new int[2];
        Test_Span1(arr1, -1);
        Test_Span1(arr1, 0);
        Test_Span1(arr1, 1);
        Test_Span1(arr1, 2);
        if (!arr1.SequenceEqual(new[] {42, 42}))
            throw new Exception("Test_Span1 failed");

        int[] arr2 = new int[2];
        Test_Span2(arr2, -1);
        Test_Span2(arr2, 0);
        Test_Span2(arr2, 1);
        Test_Span2(arr2, 2);
        if (!arr2.SequenceEqual(new[] { 0, 42 }))
            throw new Exception("Test_Span1 failed");

        int[] arr3 = new int[2];
        Test_Span3(arr3, -1);
        Test_Span3(arr3, 0);
        Test_Span3(arr3, 1);
        Throws<IndexOutOfRangeException>(() => Test_Span3(arr3, 2));
        if (!arr3.SequenceEqual(new[] { 0, 42 }))
            throw new Exception("Test_Span1 failed");

        // Should not throw NRE
        Test_Array(arr3, -1);
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new Exception($"Expected {typeof(T)} to be thrown");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test_Span1(Span<int> arr, int i)
    {
        if (i >= 0 && i < arr.Length)
            arr[i] = 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test_Span2(Span<int> arr, int i)
    {
        if (i > 0 && i < arr.Length)
            arr[i] = 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test_Span3(Span<int> arr, int i)
    {
        if (i > 0 && i <= arr.Length)
            arr[i] = 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test_Array(Span<int> arr, int i)
    {
        // Can't be folded into "(uint)i < arr.Length"
        // because arr can be null
        if (i >= 0 && i < arr.Length)
            arr[i] = 42;
    }
}
