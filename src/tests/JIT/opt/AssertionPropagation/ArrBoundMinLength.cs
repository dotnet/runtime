// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class Program
{
    private static int returnCode = 100;

    private static int[] arr = new int[6];

    public static int Main(string[] args)
    {
        RunTestThrows(Tests.GreaterOutOfBound);
        RunTestThrows(Tests.GreaterEqualOutOfBound);
        RunTestThrows(Tests.LessOutOfBound);
        RunTestThrows(Tests.LessEqualsOutOfBound);
        RunTestThrows(Tests.EqualsOutOfBound);
        RunTestThrows(Tests.EqualsReversedOutOfBound);
        RunTestThrows(Tests.NotEqualsOutOfBound);
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
        RunTestNoThrow(TestsEarlyReturn.GreaterInBound);
        RunTestNoThrow(TestsEarlyReturn.GreaterEqualInBound);
        RunTestNoThrow(TestsEarlyReturn.LessInBound);
        RunTestNoThrow(TestsEarlyReturn.LessEqualsInBound);
        RunTestNoThrow(TestsEarlyReturn.NotEqualsInBound);
        RunTestNoThrow(TestsEarlyReturn.ZeroInBounds);

        Tests.ModInBounds(arr, 11);
        try { Tests.ModOutOfBounds(arr, 6); returnCode--; } catch {}

        arr = new int[0];
        RunTestThrows(Tests.ZeroOutOfBounds);
        RunTestThrows(TestsEarlyReturn.ZeroOutOfBounds);

        arr = new int[1];
        RunTestThrows(Tests.OneOutOfBounds);
        RunTestThrows(Tests.OneEqualsOutOfBounds);

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
}
