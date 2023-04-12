// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Test_b00719
{
    [Fact]
    public static int TestEntryPoint()
    {
        try { f(); return 1; }
        catch (OverflowException) { Console.WriteLine("PASSED"); return 100; }
        return 2;
    }

    internal static void f()
    {
        int i = 4;
        i = i - 10;
        int[] a = new int[i];
    }
}

