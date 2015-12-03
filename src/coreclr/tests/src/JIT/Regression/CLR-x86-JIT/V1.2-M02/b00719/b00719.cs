// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal class Test
{
    public static int Main()
    {
        try { f(); return 1; }
        catch (OverflowException) { Console.WriteLine("PASSED"); return 100; }
        return 2;
    }

    public static void f()
    {
        int i = 4;
        i = i - 10;
        int[] a = new int[i];
    }
}

