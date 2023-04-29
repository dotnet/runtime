// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    private static int returnCode = 100;

    private static int[] arr = new int[6];

    [Fact]
    public static int TestEntryPoint()
    {
        RunTestThrows(Tests.GreaterOutOfBound);
        RunTestThrows(Tests.GreaterEqualOutOfBound);
        RunTestThrows(Tests.LessOutOfBound);
        RunTestThrows(Tests.LessEqualsOutOfBound);
        RunTestThrows(Tests.EqualsOutOfBound);
        RunTestThrows(Tests.EqualsReversedOutOfBound);
        RunTestThrows(Tests.NotEqualsOutOfBound);
        RunTestThrows(Tests.NegativeIndexesThrows);
        RunTestThrows(TestsEarlyReturn.GreaterOutOfBound);
        RunTestThrows(TestsEarlyReturn.GreaterEqualOutOfBound);
        RunTestThrows(TestsEarlyReturn.LessOutOfBound);
        RunTestThrows(TestsEarlyReturn.LessEqualsOutOfBound);
        RunTestThrows(TestsEarlyReturn.NotEqualsOutOfBound);
        RunTestThrows(TestsEarlyReturn.EqualsOutOfBound);

        RunTestNoThrow(Tests.GreaterInBound);
        RunTestNoThrow(Tests.GreaterEqualInBound);
        RunTestNoThrow(Tests.LessInBound);
        RunTestNoThrow(Tests.EqualsInBound);
        RunTestNoThrow(Tests.LessEqualsInBound);
        RunTestNoThrow(Tests.EqualsInBound);
        RunTestNoThrow(Tests.EqualsReversedInBound);
        RunTestNoThrow(Tests.ZeroInBounds);  
        RunTestNoThrow(Tests.CompareAgainstLong);
        RunTestNoThrow(TestsEarlyReturn.GreaterInBound);
        RunTestNoThrow(TestsEarlyReturn.GreaterEqualInBound);
        RunTestNoThrow(TestsEarlyReturn.LessInBound);
        RunTestNoThrow(TestsEarlyReturn.LessEqualsInBound);
        RunTestNoThrow(TestsEarlyReturn.NotEqualsInBound);
        RunTestNoThrow(TestsEarlyReturn.ZeroInBounds);
        RunTestNoThrow((int[] arr) => Tests.EqualsAgainstBoundFiveIndex(arr, 6));
        RunTestNoThrow((int[] arr) => TestsEarlyReturn.NotEqualsAgainstBoundFiveIndex(arr, 6));


        Tests.ModInBounds(arr, 11);
        try { Tests.ModOutOfBounds(arr, 6); returnCode--; } catch {}

        arr = new int[0];
        RunTestThrows(Tests.ZeroOutOfBounds);
        RunTestThrows(TestsEarlyReturn.ZeroOutOfBounds);
        RunTestThrows((int[] arr) => Tests.EqualsAgainstBoundZeroIndex(arr, 0));
        RunTestThrows((int[] arr) => TestsEarlyReturn.EqualsAgainstBoundZeroIndexOutOfBound(arr, 1));
        RunTestThrows((int[] arr) => TestsEarlyReturn.EqualsAgainstBoundZeroIndex(arr, 0));
        RunTestThrows((int[] arr) => TestsEarlyReturn.NotEqualsAgainstBoundFiveIndex(arr, 0));
        RunTestThrows((int[] arr) => Tests.NotEqualsAgainstBoundZeroIndex(arr, 1));
        RunTestNoThrow((int[] arr) => TestsEarlyReturn.NotEqualsAgainstBoundFiveIndex(arr, 6));

        arr = new int[1];
        RunTestThrows(Tests.OneOutOfBounds);
        RunTestThrows(Tests.OneEqualsOutOfBounds);
        RunTestThrows((int[] arr) => TestsEarlyReturn.EqualsFiveIndexOutOfBound(arr, 6));
        RunTestThrows((int[] arr) => Tests.NotEqualsAgainstBoundFiveIndex(arr, 0));
        RunTestThrows((int[] arr) => Tests.NotEqualsAgainstBoundFiveIndex(arr, 5));
        RunTestNoThrow((int[] arr) => TestsEarlyReturn.EqualsAgainstBoundZeroIndex(arr, 1));
        RunTestNoThrow((int[] arr) => Tests.EqualsAgainstBoundZeroIndex(arr, 1));
        RunTestNoThrow((int[] arr) => Tests.NotEqualsAgainstBoundZeroIndex(arr, 0));

        return returnCode;
    }

    private static void RunTestThrows(Action<int[]> action)
    {
        try
        {
            action(arr);
            Console.WriteLine("failed " + action.Method.Name);
            returnCode--;
        }
        catch (Exception)
        {

        }
    }

    private static void RunTestNoThrow(Action<int[]> action)
    {
        try
        {
            action(arr);
        }
        catch (Exception)
        {
            Console.WriteLine("failed " + action.Method.Name);
            returnCode--;
        }
    }
}

public static class Tests
{
    public static void GreaterInBound(int[] arr)
    {
        if (arr.Length > 5)
        {
            arr[5] = 1;
        }
    }

    public static void GreaterOutOfBound(int[] arr)
    {
        if (arr.Length > 5)
        {
            arr[6] = 1;
        }
    }

    public static void GreaterEqualInBound(int[] arr)
    {
        if (arr.Length >= 6)
        {
            arr[5] = 1;
        }
    }

    public static void GreaterEqualOutOfBound(int[] arr)
    {
        if (arr.Length >= 5)
        {
            arr[6] = 1;
        }
    }

    public static void LessInBound(int[] arr)
    {
        if (5 < arr.Length)
        {
            arr[5] = 1;
        }
    }

    public static void LessOutOfBound(int[] arr)
    {
        if (5 < arr.Length)
        {
            arr[6] = 1;
        }
    }

    public static void LessEqualsInBound(int[] arr)
    {
        if (5 <= arr.Length)
        {
            arr[4] = 1;
        }
    }

    public static void LessEqualsOutOfBound(int[] arr)
    {
        if (5 <= arr.Length)
        {
            arr[6] = 1;
        }
    }

    public static void EqualsInBound(int[] arr)
    {
        if (arr.Length == 6)
        {
            arr[5] = 1;
        }
    }

    public static void EqualsReversedInBound(int[] arr)
    {
        if (6 == arr.Length)
        {
            arr[5] = 1;
        }
    }

    public static void EqualsOutOfBound(int[] arr)
    {
        if (arr.Length == 6)
        {
            arr[6] = 1;
        }
    }

    public static void EqualsReversedOutOfBound(int[] arr)
    {
        if (6 == arr.Length)
        {
            arr[6] = 1;
        }
    }

    public static void NotEqualsOutOfBound(int[] arr)
    {
        if (arr.Length != 5)
        {
            arr[6] = 1;
        }
    }

    public static void ZeroInBounds(int[] arr)
    {
        if (arr.Length != 0)
        {
            arr[0] = 0;
        }
    }

    public static void ZeroOutOfBounds(int[] arr)
    {
        if (arr.Length == 0)
        {
            arr[0] = 0;
        }

        return;
    }

    public static void OneOutOfBounds(int[] arr)
    {
        if (arr.Length != 0)
        {
            arr[1] = 0;
        }
    }

    public static void OneEqualsOutOfBounds(int[] arr)
    {
        if (arr.Length == 0)
        {
            return;
        }

        arr[1] = 0;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void ModInBounds(int[] arr, uint i)
    {
        if (arr.Length == 6)
        {
            arr[(int)(i % 6)] = 0;
        }
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void ModOutOfBounds(int[] arr, uint i)
    {
        if (arr.Length == 6)
        {
            arr[(int)(i % 7)] = 0;
        }
    }

    public static void CompareAgainstLong(int[] arr)
    {
        long len = (1L << 32) + 1;
        if (arr.Length == len)
            arr[0] = 0;
    }

    public static void NegativeIndexesThrows(int[] arr)
    {
        if (arr.Length > 5)
        {
            arr[-1] = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EqualsAgainstBoundFiveIndex(int[] arr, int bound)
    {
        if (arr.Length == bound)
        {
            arr[5] = 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotEqualsAgainstBoundFiveIndex(int[] arr, int bound)
    {
        if (arr.Length != bound)
        {
            arr[5] = 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EqualsAgainstBoundZeroIndex(int[] arr, int bound)
    {
        if (arr.Length == bound)
        {
            arr[0] = 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotEqualsAgainstBoundZeroIndex(int[] arr, int bound)
    {
        if (arr.Length != bound)
        {
            arr[0] = 1;
        }
    }
}

public static class TestsEarlyReturn
{
    public static void GreaterInBound(int[] arr)
    {
        if (arr.Length <= 5)
        {
            return;
        }

        arr[5] = 1;
    }

    public static void GreaterOutOfBound(int[] arr)
    {
        if (arr.Length <= 5)
        {
            return;
        }

        arr[6] = 1;
    }

    public static void GreaterEqualInBound(int[] arr)
    {
        if (arr.Length < 5)
        {
            return;
        }

        arr[4] = 1;
    }

    public static void GreaterEqualOutOfBound(int[] arr)
    {
        if (arr.Length < 5)
        {
            return;
        }

        arr[6] = 1;
    }

    public static void LessInBound(int[] arr)
    {
        if (5 >= arr.Length)
        {
            return;
        }

        arr[5] = 1;
    }

    public static void LessOutOfBound(int[] arr)
    {
        if (5 >= arr.Length)
        {
            return;
        }

        arr[6] = 1;
    }

    public static void LessEqualsInBound(int[] arr)
    {
        if (5 > arr.Length)
        {
            return;
        }

        arr[4] = 1;
    }

    public static void LessEqualsOutOfBound(int[] arr)
    {
        if (5 > arr.Length)
        {
            return;
        }

        arr[6] = 1;
    }

    public static void NotEqualsInBound(int[] arr)
    {
        if (arr.Length != 6)
        {
            return;
        }

        arr[5] = 1;
    }

    public static void NotEqualsOutOfBound(int[] arr)
    {
        if (arr.Length != 6)
        {
            return;
        }

        arr[6] = 1;
    }

    public static void EqualsOutOfBound(int[] arr)
    {
        if (arr.Length == 5)
        {
            return;
        }

        arr[6] = 1;
    }

    public static void ZeroInBounds(int[] arr)
    {
        if (arr.Length == 0)
        {
            return;
        }

        arr[0] = 0;
    }

    public static void ZeroOutOfBounds(int[] arr)
    {
        if (arr.Length != 0)
        {
            return;
        }

        arr[0] = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotEqualsAgainstBoundFiveIndex(int[] arr, int bound)
    {
        if (arr.Length != bound)
        {
            return;
        }

        arr[5] = 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EqualsAgainstBoundZeroIndex(int[] arr, int bound)
    {
        if (arr.Length != bound)
        {
            return;
        }

        arr[0] = 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EqualsFiveIndexOutOfBound(int[] arr, int bound)
    {
        if (arr.Length == bound)
        {
            return;
        }

        arr[5] = 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EqualsAgainstBoundZeroIndexOutOfBound(int[] arr, int bound)
    {
        if (arr.Length == bound)
        {
           return;
        }

        arr[0] = 1;
    }
}
