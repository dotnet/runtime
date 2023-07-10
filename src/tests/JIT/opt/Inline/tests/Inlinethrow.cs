// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MainApp
{
    private static int s_s = 0;

    internal static void MethodThatAlwaysThrows_NoInline()
    {
        Console.WriteLine("In method that always throws");
        throw new Exception("methodthatalwaysthrows");
    }

    internal static void MethodThatMightThrow_Inline()
    {
        if (s_s == 1)
        {
            throw new Exception();
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            MethodThatMightThrow_Inline();
            MethodThatAlwaysThrows_NoInline();
            return 99;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());

            return 100;
        }
    }
}


