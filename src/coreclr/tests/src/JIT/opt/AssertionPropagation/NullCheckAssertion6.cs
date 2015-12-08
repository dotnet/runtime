// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//Unit test for null check assertion.

using System;

internal class Sample8
{
    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int func(int[,] a1)
    {
        int h;

        h = a1[1, 1];

        if (a1 == null)
        {
            throw new Exception();
        }

        return h;
    }

    private static int Main(string[] args)
    {
        try
        {
            int h = func(new int[3, 3]);
            if (h == 0)
            {
                Console.WriteLine("Passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Failed");
                return 101;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}
