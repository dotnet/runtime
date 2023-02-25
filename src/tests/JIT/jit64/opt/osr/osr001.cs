// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class TestCase
{
    private const int ARRAY_MULTIPLIER = 3;
    private const int ARRAY_UNITSIZE = 10;
    private const int ARRAY_SIZE = ARRAY_UNITSIZE * ARRAY_MULTIPLIER;

    [Fact]
    public static int TestEntryPoint()
    {
        int rc = 0;
        int[] array = new int[ARRAY_SIZE];
        int j;
        int i = ARRAY_MULTIPLIER - 1;

        while (i >= 0)
        {
            for (j = (i * ARRAY_UNITSIZE); j < ((i + 1) * ARRAY_UNITSIZE); j++)
            {
                array[j] = j + i;
            }
            i--;
        }

        // Check for values of array elements
        int nErrors = ARRAY_SIZE;
        for (int k = 0; k < ARRAY_SIZE; k++)
        {
            if (array[k] != (k + k / ARRAY_UNITSIZE))
            {
                Console.WriteLine("[k = {0}]\texpected = {1}\tactual={2}", k, k + (k / ARRAY_UNITSIZE), array[k]);
            }
            else
            {
                nErrors--;
            }
        }
        if (nErrors == 0)
        {
            Console.WriteLine("Passed");
            rc = 100;
        }
        else
        {
            Console.WriteLine("Failed");
            rc = 1;
        }

        return rc;
    }
}

