// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal class MainApp
{
    public static int Main(string[] args)
    {
        int a, prev;

        Console.WriteLine("\n========== Case 1 (wrong result) ==========");

        a = 2044617152;
        Console.WriteLine("a1={0}", a);

        a += 0x12345678;

        if (a < 0) a = -a;
        Console.WriteLine("a2={0}", a);

        prev = a;
        Console.WriteLine("prev={0}, a2={1}", prev, a);

        Console.WriteLine("\n========== Case 2 (right result) ==========");

        a = 2044617152;
        Console.WriteLine("a1={0}", a);

        a += 0x12345678;
        a.ToString();

        if (a < 0) a = -a;
        Console.WriteLine("a2={0}", a);

        Console.WriteLine("prev={0}, a3={1}", prev, a);

        Console.WriteLine("\n========== Test Summary ==========");
        if (a == prev)
        {
            Console.WriteLine("Test SUCCESS");
            return 100;
        }
        else
        {
            Console.WriteLine("Test FAILED");
            return 101;
        }
    }
}

