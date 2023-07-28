// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public struct Yak
{
    public Int32 a;
    private String _foo;
    public Int32 b;
    public void Do_Something()
    {
        _foo = a.ToString();
        b += a;
    }
}


public class Complex2_Array_Test
{
    internal static void test(Yak[,,,,,,] Odd_Variable)
    {
        Console.Write(Odd_Variable.Length);
    }
    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine("Starting...");
        int SIZE = 2;
        Int64 sum = 0;

        Yak[,,,,,,] foo = new Yak[SIZE, SIZE, SIZE, SIZE, SIZE, SIZE, SIZE];
        int i, j, k, l, m, n, o;

        for (i = 0; i < SIZE; i++)
            for (j = 0; j < SIZE; j++)
                for (k = 0; k < SIZE; k++)
                    for (l = 0; l < SIZE; l++)
                        for (m = 0; m < SIZE; m++)
                            for (n = 0; n < SIZE; n++)
                                for (o = 0; o < SIZE; o++)
                                {
                                    foo[i, j, k, l, m, n, o].a = i * j * k * l * m * n * o;
                                    foo[i, j, k, l, m, n, o].b = i + j + k + l + m + n + o;
                                    foo[i, j, k, l, m, n, o].Do_Something();
                                }

        for (i = 0; i < SIZE; i++)
            for (j = 0; j < SIZE; j++)
                for (k = 0; k < SIZE; k++)
                    for (l = 0; l < SIZE; l++)
                        for (m = 0; m < SIZE; m++)
                            for (n = 0; n < SIZE; n++)
                                for (o = 0; o < SIZE; o++)
                                {
                                    sum += foo[i, j, k, l, m, n, o].b;
                                }

        Console.WriteLine("\nTry to get count!");

        test(foo);

        if ((foo.Length == SIZE * SIZE * SIZE * SIZE * SIZE * SIZE * SIZE) && (sum == 449))
        {
            Console.Write("Count is:" + foo.Length.ToString());
            Console.WriteLine("\nEverything Worked!");
            return 100;
        }
        else
        {
            Console.WriteLine("Count is:" + foo.Length.ToString());
            Console.WriteLine("Sum is:" + sum.ToString());
            Console.WriteLine("\nEverything Didnt Work!");
            return 1;
        }
    }
}
