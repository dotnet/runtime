// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//unit test for null check assertion propagation.

using System;

internal class Sample6
{
    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int func(int[] a1)
    {
        int h;

        h = a1[1];

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
            int h = func(new int[3]);
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
