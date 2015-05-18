// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

internal class Simple2_Array_Test
{
    public static int Main(String[] args)
    {
        Console.WriteLine("Starting...");
        int SIZE = 10;

        Int32[,,,] foo = new Int32[SIZE, SIZE, SIZE, SIZE];
        int i, j, k, l, m;
        Int64 sum = 0;


        for (i = 0; i < SIZE; i++)
            for (j = 0; j < SIZE; j++)
                for (k = 0; k < SIZE; k++)
                    for (l = 0; l < SIZE; l++)
                    {
                        foo[i, j, k, l] = i * j * k * l;
                    }

        for (i = 0; i < SIZE; i++)
            for (j = 0; j < i; j++)
                for (k = 0; k < j; k++)
                    for (l = 0; l < k; l++)
                        for (m = 0; m < l; m++)
                        {
                            sum += foo[i, j, k, l];
                        }

        if (sum == 197163)
        {
            Console.WriteLine("Everything Worked!");
            return 100;
        }
        else
        {
            Console.WriteLine("Something is broken!");
            return 1;
        }
    }
}
