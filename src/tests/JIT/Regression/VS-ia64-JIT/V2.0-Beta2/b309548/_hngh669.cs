// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections;
using System.Runtime.InteropServices;
using Xunit;
public enum TestEnum
{
    red = 1,
    green = 2,
    blue = 4,
}


[StructLayout(LayoutKind.Sequential)]
public class AA
{
    public static float[][, , , ,] m_afStatic1;
    public static uint[,] Static3(ushort param1)
    {
        byte local14 = ((byte)(Math.Min(((ulong)(36.0)), ((ulong)(124.0)))));
        uint local15 = 26u;
        for (local14 /= (local14 *= local14); ('\x20' != ((char)(((int)(local15))
            ))); local15 -= 95u)
        {
            for (local15++; (118u == local15); param1 = (param1 /= (param1 -= (
                param1 /= param1))))
            {
            }
        }
        return ((uint[,])(((Array)(null))));
    }
}

public class App
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Console.WriteLine("Testing AA::Static3");
            AA.Static3(Math.Min(((ushort)(((byte)(3u)))), ((ushort)(((sbyte)(72))))));
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }

        Console.WriteLine("Passed.");
        return 100;
    }

}
