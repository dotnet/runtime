// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

