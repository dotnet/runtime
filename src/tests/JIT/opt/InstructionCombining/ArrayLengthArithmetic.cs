// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ArrayLengthArithmeticTests
{
    private static int returnCode = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        for (int arrayLength = 0; arrayLength < 100; arrayLength++)
        {
            var array = new int[arrayLength];

            if (arrayLength == 0)
            {
                Expect<DivideByZeroException>(() => ArrayLengthDiv_cns0(array));
                Expect<DivideByZeroException>(() => ArrayLengthDiv_var0(array));
                Expect<DivideByZeroException>(() => ArrayLengthMod_cns0(array));
                Expect<DivideByZeroException>(() => ArrayLengthMod_var0(array));
            }

            // Array.Length / cns == Array.Length / ToVar(cns)
            CompareResults(ArrayLengthDiv_cns1(array), ArrayLengthDiv_var1(array));
            CompareResults(ArrayLengthDiv_cns2(array), ArrayLengthDiv_var2(array));
            CompareResults(ArrayLengthDiv_cns3(array), ArrayLengthDiv_var3(array));
            CompareResults(ArrayLengthDiv_cns4(array), ArrayLengthDiv_var4(array));
            CompareResults(ArrayLengthDiv_cns5(array), ArrayLengthDiv_var5(array));
            CompareResults(ArrayLengthDiv_cns8(array), ArrayLengthDiv_var8(array));
            CompareResults(ArrayLengthDiv_cns10(array), ArrayLengthDiv_var10(array));
            CompareResults(ArrayLengthDiv_cnsMaxValuen1(array), ArrayLengthDiv_varMaxValuen1(array));
            CompareResults(ArrayLengthDiv_cnsMaxValue(array), ArrayLengthDiv_varMaxValue(array));
            CompareResults(ArrayLengthDiv_cnsn1(array), ArrayLengthDiv_varn1(array));
            CompareResults(ArrayLengthDiv_cnsn2(array), ArrayLengthDiv_varn2(array));
            CompareResults(ArrayLengthDiv_cnsMinValue(array), ArrayLengthDiv_varMinValue(array));

            // Array.Length % cns == Array.Length % ToVar(cns)
            CompareResults(ArrayLengthMod_cns1(array), ArrayLengthMod_var1(array));
            CompareResults(ArrayLengthMod_cns2(array), ArrayLengthMod_var2(array));
            CompareResults(ArrayLengthMod_cns3(array), ArrayLengthMod_var3(array));
            CompareResults(ArrayLengthMod_cns4(array), ArrayLengthMod_var4(array));
            CompareResults(ArrayLengthMod_cns5(array), ArrayLengthMod_var5(array));
            CompareResults(ArrayLengthMod_cns8(array), ArrayLengthMod_var8(array));
            CompareResults(ArrayLengthMod_cns10(array), ArrayLengthMod_var10(array));
            CompareResults(ArrayLengthMod_cnsMaxValuen1(array), ArrayLengthMod_varMaxValuen1(array));
            CompareResults(ArrayLengthMod_cnsMaxValue(array), ArrayLengthMod_varMaxValue(array));
            CompareResults(ArrayLengthMod_cnsn1(array), ArrayLengthMod_varn1(array));
            CompareResults(ArrayLengthMod_cnsn2(array), ArrayLengthMod_varn2(array));
            CompareResults(ArrayLengthMod_cnsMinValue(array), ArrayLengthMod_varMinValue(array));
        }

        return returnCode;
    }

    // Array.Length / cns
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_cns0(int[] array) => array.Length / 0;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_cns1(int[] array) => array.Length / 1;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_cns2(int[] array) => array.Length / 2;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_cns3(int[] array) => array.Length / 3;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_cns4(int[] array) => array.Length / 4;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_cns5(int[] array) => array.Length / 5;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_cns8(int[] array) => array.Length / 8;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_cns10(int[] array) => array.Length / 10;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_cnsMaxValuen1(int[] array) => array.Length / (int.MaxValue - 1);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_cnsMaxValue(int[] array) => array.Length / int.MaxValue;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_cnsn1(int[] array) => array.Length / -1;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_cnsn2(int[] array) => array.Length / -2;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_cnsMinValue(int[] array) => array.Length / int.MinValue;

    // Array.Length / variable
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_var0(int[] array) => array.Length / ToVar(0);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_var1(int[] array) => array.Length / ToVar(1);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_var2(int[] array) => array.Length / ToVar(2);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_var3(int[] array) => array.Length / ToVar(3);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_var4(int[] array) => array.Length / ToVar(4);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_var5(int[] array) => array.Length / ToVar(5);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_var8(int[] array) => array.Length / ToVar(8);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_var10(int[] array) => array.Length / ToVar(10);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_varMaxValuen1(int[] array) => array.Length / ToVar(int.MaxValue - 1);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_varMaxValue(int[] array) => array.Length / ToVar(int.MaxValue);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_varn1(int[] array) => array.Length / ToVar(-1);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_varn2(int[] array) => array.Length / ToVar(-2);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthDiv_varMinValue(int[] array) => array.Length / ToVar(int.MinValue);

    // Array.Length % cns
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_cns0(int[] array) => array.Length % 0;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_cns1(int[] array) => array.Length % 1;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_cns2(int[] array) => array.Length % 2;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_cns3(int[] array) => array.Length % 3;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_cns4(int[] array) => array.Length % 4;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_cns5(int[] array) => array.Length % 5;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_cns8(int[] array) => array.Length % 8;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_cns10(int[] array) => array.Length % 10;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_cnsMaxValuen1(int[] array) => array.Length % (int.MaxValue - 1);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_cnsMaxValue(int[] array) => array.Length % int.MaxValue;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_cnsn1(int[] array) => array.Length % -1;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_cnsn2(int[] array) => array.Length % -2;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_cnsMinValue(int[] array) => array.Length % int.MinValue;

    // Array.Length % variable
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_var0(int[] array) => array.Length % ToVar(0);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_var1(int[] array) => array.Length % ToVar(1);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_var2(int[] array) => array.Length % ToVar(2);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_var3(int[] array) => array.Length % ToVar(3);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_var4(int[] array) => array.Length % ToVar(4);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_var5(int[] array) => array.Length % ToVar(5);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_var8(int[] array) => array.Length % ToVar(8);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_var10(int[] array) => array.Length % ToVar(10);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_varMaxValuen1(int[] array) => array.Length % ToVar(int.MaxValue - 1);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_varMaxValue(int[] array) => array.Length % ToVar(int.MaxValue);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_varn1(int[] array) => array.Length % ToVar(-1);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_varn2(int[] array) => array.Length % ToVar(-2);
    [MethodImpl(MethodImplOptions.NoInlining)] private static int ArrayLengthMod_varMinValue(int[] array) => array.Length % ToVar(int.MinValue);

    private static void Expect<T>(Action action, [CallerLineNumber] int line = 0) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        Console.WriteLine($"{typeof(T).Name} was expected, L{line}");
        returnCode++;
    }

    private static void CompareResults(int a, int b, [CallerLineNumber] int line = 0)
    {
        if (a != b)
        {
            Console.WriteLine($"{a} != {b}, L{line}");
            returnCode++;
        }
    }

    // cns to var
    [MethodImpl(MethodImplOptions.NoInlining)] private static T ToVar<T>(T t) => t;
}
