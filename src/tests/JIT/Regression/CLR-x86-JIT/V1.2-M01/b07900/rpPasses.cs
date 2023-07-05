// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public struct AA
{
    public static byte[, ,][] f()
    {
        for ((new long[58u, 97u, 118u])[122, 61, Math.Sign(41)] = ((long)(Math.Max(82.0
            , 69.0))); ((bool)((new object[42u])[54])); (new byte[33u, 119u])[60, (new
            int[46u, 48u])[55, 81]] = (new byte[8u][, ,])[82][48, (new int[126u, 109u, 120u
            , 12u])[48, 49, 33, 16], Math.Min(68, 43)])
        {
            for (new byte[] { }[64] /= (new byte[44u, 81u, 16u, 52u, 20u])[(new int[58u, 45u
                ])[125, 36], Math.Max(22, 90), 8, ((int)(69.0)), Math.Sign(22)]; new bool[]{
				false }[(new int[55u])[71]]; new int[] { 18, 117, 73 }[((int)(93.0f))] /= new
                int[][] { new int[] { 6 }, new int[] { 103, 28, 52, 112, 31 } }[85][(new int[76u,
                48u, 105u])[86, 24, 7]])
            {
            }
            try
            {
            }
            catch (IndexOutOfRangeException)
            {
            }
        }
        return ((new byte[40u, 107u, 4u][, ,][])[107, 69, 93] = new byte[][, ,][] { }[70]
            );
    }
}

public class App
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            AA.f();
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        Console.WriteLine("Passed.");
        return 100;
    }
}
