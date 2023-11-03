// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MainApp
{
    private static int s_c = 1;

    public static int Bar_Inline(int v)
    {
        Console.WriteLine("Entering Bar_Inline: v=");
        int ret = v * 2;
        return ret;
    }

    public static int Foo_Inline(int v)
    {
        int ret = Bar_Inline(v + 1) * 4;

        Console.WriteLine(ret);
        return ret;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Foo_Inline(s_c);
            Console.WriteLine(Foo_Inline(s_c));
            Console.WriteLine(Foo_Inline(s_c) + 1 + Foo_Inline(s_c));
            Console.WriteLine(Foo_Inline(s_c) + 1 + Foo_Inline(s_c) + 2 + Foo_Inline(s_c));
            Console.WriteLine("Test Passed");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine("Test failed: " + e.Message);
            return 101;
        }
    }
}


