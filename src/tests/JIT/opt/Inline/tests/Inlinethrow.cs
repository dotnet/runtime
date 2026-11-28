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

    private static int s_guardEffects;

    private static void Validate(int value)
    {
        if (value < 0 && value != -1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, string.Format("Expected {0} through {1}", -1, int.MaxValue));
        }
    }

    private static void ValidateAndCount(int value)
    {
        if (value < 0 && value != -1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, string.Format("Expected {0} through {1}", -1, int.MaxValue));
        }
        s_guardEffects += value;
    }

    [Fact]
    public static void ConstantGuard()
    {
        Validate(-1);
        Assert.Throws<ArgumentOutOfRangeException>(() => Validate(-2));

        int before = s_guardEffects;
        ValidateAndCount(5);
        Assert.Equal(before + 5, s_guardEffects);
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


