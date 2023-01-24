// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class Test_b00719
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

