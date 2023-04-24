// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace RNG
{
    public class Test
    {
        [Fact]
        public static int TestEntryPoint()
        {
            return foo(10, 20, 30, 40, 50);
        }

        private static int foo(int a, int b, int c, int d, int e)
        {
            int[] x = new int[100];
            int[] y = new int[100];
            int length = 1;
            int i, j, k, l = 0;
            k = 0;

            do
            {
                l = 0;
                do
                {
                    y[l] = 5;
                    l++;
                }
                while (l < b);
                e++;
                k++;
            } while (k < a);

            for (i = 0; i < c; i++)
            {
                for (j = 0; j < d; j++)
                {
                    // these two should be the same
                    x[j] = k + i;
                    y[j] = i + k;
                }
            }
            for (k = 0; k < 100; k++)
            {
                Console.WriteLine(x[k]);
                Console.WriteLine(y[k]);
                if (x[k] != y[k])
                {
                    Console.WriteLine("Array elements do not match!");
                    return 0;
                }
            }

            Console.WriteLine("Passed");
            return 100;
        }
    }
}
