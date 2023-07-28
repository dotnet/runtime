// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public struct AA
{
    public static char[, , , ,][, ,] Static1(char param1, int param2, bool[,] param3,
        sbyte[,][][, , ,][,][] param4, ref  int param5, ref byte[,][, , ,] param6)
    {
        float local1 = 10.0f;
        short local2 = ((short)(47.0f));
        for (local1--; ((param2 * 117u) == ((long)(local2))); local2++) ;

        return (new char[99u, 88u, 97u, 120u, 72u][, ,]);
    }
}

public class App
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            AA.Static1(
                '\x69',
                92,
                (new bool[49u, 76u]),
                (new sbyte[24u, ((uint)(51))][][,,,][,][]),
                ref App.m1,
                ref App.m2);
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        Console.WriteLine("Passed.");
        return 100;
    }
    public static int m1;
    public static byte[,][, , ,] m2;
}
