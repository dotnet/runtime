// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class Misc
{ //Only append content to this class as the test suite depends on line info
    public static int CreateObject(int foo, int bar)
    {
        var f = new Fancy()
        {
            Foo = foo,
            Bar = bar,
        };

        Console.WriteLine($"{f.Foo} {f.Bar}");
        return f.Foo + f.Bar;
    }
}

public class Fancy
{
    public int Foo;
    public int Bar { get; set; }
    public static void Types()
    {
        double dPI = System.Math.PI;
        float fPI = (float) System.Math.PI;

        int iMax = int.MaxValue;
        int iMin = int.MinValue;
        uint uiMax = uint.MaxValue;
        uint uiMin = uint.MinValue;

        long l = uiMax * (long) 2;
        long lMax = long.MaxValue; // cannot be represented as double
        long lMin = long.MinValue; // cannot be represented as double

        sbyte sbMax = sbyte.MaxValue;
        sbyte sbMin = sbyte.MinValue;
        byte bMax = byte.MaxValue;
        byte bMin = byte.MinValue;

        short sMax = short.MaxValue;
        short sMin = short.MinValue;
        ushort usMin = ushort.MinValue;
        ushort usMax = ushort.MaxValue;

        var d = usMin + usMax;
    }
}