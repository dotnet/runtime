// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

internal class My
{
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    private static int sum(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13, int a14, int a15, int a16)
    {
        return a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8 + a9 + a10 + a11 + a12 + a13 + a14 + a15 + a16;
    }

    private static int Main()
    {
        Console.WriteLine("A bug was discovered during feature development and is covered by this test.");
        Console.WriteLine("If this test does not crash terribly, it is assumed to have passed... :-/");
        sum(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        Console.WriteLine("PASS");
        return 100;
    }
}
