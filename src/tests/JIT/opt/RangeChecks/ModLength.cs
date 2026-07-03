// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ModLength
{
        [Fact]
    public static void TestEntryPoint()
    {
        Throws<DivideByZeroException>(() => Test1(new int[0], 0));
        Throws<DivideByZeroException>(() => Test2(new int[0], 1));
        Throws<DivideByZeroException>(() => Test3(new int[0], int.MaxValue));
        Throws<DivideByZeroException>(() => Test4(new int[0], 0));
        Throws<DivideByZeroException>(() => Test5(new int[0], 0));
        Throws<DivideByZeroException>(() => Test6(new int[0], 0));
        Throws<DivideByZeroException>(() => Test7(new int[0], 0));
        Throws<DivideByZeroException>(() => Test8(new int[0], 0));
        Throws<DivideByZeroException>(() => Test9(new int[0], 0));
        Test1(new int[1], 1);
        Test2(new int[1], 1);
        Throws<IndexOutOfRangeException>(() => Test9(new int[1], 2));
        Test3(new int[1], int.MaxValue);
        Throws<IndexOutOfRangeException>(() => Test4(new int[1], int.MinValue));
        Test5(new int[1], -1);
        Test6(new int[1], 1);
        Test7(new int[1], 1);
        Test8(new int[1], 1);
        Test1(new int[10], 10);
        Test2(new int[10], 11);
        Test3(new int[10], int.MaxValue);
        Throws<IndexOutOfRangeException>(() => Test4(new int[10], int.MinValue));
        Throws<IndexOutOfRangeException>(() => Test5(new int[10], -1));
        Test6(new int[10], 0);
        Test7(new int[10], 0);
        Throws<DivideByZeroException>(() => Test8(new int[10], 0));

        Test10(new int[10], 9U);
        Throws<DivideByZeroException>(() => Test10(new int[10], 0U));

        Test11(new int[10], 0);
        Test11(new int[10], 100);
        Throws<DivideByZeroException>(() => Test11(new int[0], 10));

        Test12(new int[10], 9);
        Throws<DivideByZeroException>(() => Test12(new int[10], 0));

        Test13(new int[10], 9U);
        Test13(new int[10], 10U);
        Throws<DivideByZeroException>(() => Test13(new int[10], 0U));

        Test14(new int[10], 9);
        Test14(new int[10], 10);
        Throws<DivideByZeroException>(() => Test14(new int[10], 0));

        Test15(new int[10], 9U);
        Throws<DivideByZeroException>(() => Test15(new int[10], 0U));

        Test16(new int[10], 9);
        Throws<DivideByZeroException>(() => Test16(new int[10], 0));

        Test17(new int[10], 9U);
        Test17(new int[10], 10U);
        Throws<DivideByZeroException>(() => Test17(new int[10], 0U));

        Test18(new int[10], 9);
        Test18(new int[10], 10);
        Throws<DivideByZeroException>(() => Test18(new int[10], 0));
    }

    static void Throws<T>(Action action, [CallerLineNumber] int line = 0)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            if (e is T)
            {
                return;
            }
            throw new InvalidOperationException($"{typeof(T)} exception was expected, actual: {e.GetType()}");
        }
        throw new InvalidOperationException($"{typeof(T)} exception was expected");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test1(int[] arr, int index) => arr[index % arr.Length];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test2(int[] arr, int index) => arr[(int)index % (int)arr.Length];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test3(int[] arr, int index) => arr[(int)((uint)index % (uint)arr.Length)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test4(int[] arr, int index) => arr[arr.Length % index];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test5(int[] arr, int index)
    {
        var span = arr.AsSpan();
        return arr[index % arr.Length];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test6(int[] arr, int index)
    {
        var span = arr.AsSpan();
        return span[(int)index % (int)span.Length];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test7(int[] arr, int index)
    {
        var span = arr.AsSpan();
        return span[(int)((uint)index % (uint)span.Length)];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test8(int[] arr, int index)
    {
        var span = arr.AsSpan();
        return span[span.Length % index];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test9(int[] arr, int index) => arr[index / arr.Length];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test10(int[] arr, uint index)
    {
        if (arr.Length > 0 && arr.Length > index)
        {
            return arr[(uint)arr.Length % index];
        }

        return 1234;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test11(int[] arr, int index)
    {
        if (index >= 0)
        {
            return arr[index % arr.Length];
        }

        return 1234;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test12(int[] arr, int index)
    {
        if (arr.Length > 0 && arr.Length > index)
        {
            return arr[arr.Length % index];
        }

        return 1234;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test13(int[] arr, uint index)
    {
        if (arr.Length > 0 && arr.Length >= index)
        {
            return arr[(uint)arr.Length % index];
        }

        return 1234;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test14(int[] arr, int index)
    {
        if (arr.Length > 0 && arr.Length >= index)
        {
            return arr[arr.Length % index];
        }

        return 1234;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test15(int[] arr, uint index)
    {
        if (arr.Length > 0 && index < arr.Length)
        {
            return arr[(uint)arr.Length % index];
        }

        return 1234;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test16(int[] arr, int index)
    {
        if (arr.Length > 0 && index < arr.Length)
        {
            return arr[arr.Length % index];
        }

        return 1234;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test17(int[] arr, uint index)
    {
        if (arr.Length > 0 && index <= arr.Length)
        {
            return arr[(uint)arr.Length % index];
        }

        return 1234;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test18(int[] arr, int index)
    {
        if (arr.Length > 0 && index <= arr.Length)
        {
            return arr[arr.Length % index];
        }

        return 1234;
    }
}
