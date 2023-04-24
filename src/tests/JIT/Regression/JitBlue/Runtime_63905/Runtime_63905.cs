// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_63905
{
    [Fact]
    public static int TestEntryPoint()
    {
        C c = GetNull();
        int i = GetOne();
        try
        {
            Foo(c.Field, i / 0);
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("PASS: Caught NullReferenceException in first argument");
            return 100;
        }
        catch (DivideByZeroException)
        {
            Console.WriteLine("FAIL: Arguments were reordered incorrectly");
            return -1;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static C GetNull() => null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetOne() => 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Foo(object o, int val)
    {
    }

    private class C
    {
        public object Field;
    }
}
