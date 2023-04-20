// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test that loop cloning won't consider a[i].struct_field[j] to be
// a jagged array a[i][j].

using System;
using Xunit;

public class Runtime_66254
{
    internal static void t1()
    {
        var a = new ValueTuple<int[], int>[]
        {
            new (new[] { 0 }, 1),
            new (new[] { 2 }, 3)
        };

        for (var i1 = 0; i1 < a.Length; i1++)
        {
            for (var i2 = 0; i2 < a[i1].Item1.Length; i2++)
            {
                var elem = a[i1].Item1[i2];
                Console.WriteLine(elem);
            }
        }
    }

    internal static void t2()
    {
        var a = new ValueTuple<int[], int>[]
        {
            new (new[] { 0 }, 1),
            new (new[] { 2 }, 3)
        };

        for (var i1 = 0; i1 < a.Length; i1++)
        {
            var length = a[i1].Item1.Length;
            for (var i2 = 0; i2 < length; i2++)
            {
                var elem = a[i1].Item1[i2];
                Console.WriteLine(elem);
            }
        }
    }

    internal static void t3()
    {
        var a = new ValueTuple<int, int[]>[]
        {
            new (1, new[] { 2 }),
            new (3, new[] { 4 })
        };

        for (var i1 = 0; i1 < a.Length; i1++)
        {
            var length = a[i1].Item2.Length;
            for (var i2 = 0; i2 < length; i2++)
            {
                var elem = a[i1].Item2[i2];
                Console.WriteLine(elem);
            }
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int result = 100;

        try
        {
            t1();
        }
        catch (Exception e)
        {
            Console.WriteLine($"t1 failed: {e}");
            result = 101;
        }

        try
        {
            t2();
        }
        catch (Exception e)
        {
            Console.WriteLine($"t2 failed: {e}");
            result = 101;
        }

        try
        {
            t3();
        }
        catch (Exception e)
        {
            Console.WriteLine($"t3 failed: {e}");
            result = 101;
        }

        Console.WriteLine((result == 100) ? "PASS" : "FAIL");
        return result;
    }
}
