// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections;
using Xunit;

public enum TestEnum
{
    red = 1,
    green = 2,
    blue = 4,
}

public class AA
{
    public bool[, ,] m_abField1;
    public static Array Static1()
    {
        byte local1 = (new byte[12u, 37u, 25u, 121u][, , ,])[46, 37, 101, 94][52, 25, 19
            , Math.Max(121, 1)];
        local1 = (new byte[26u, 74u, 72u, 42u])[new int[] { 96 }[60], (local1 ^ local1)
            , (0), Math.Min(123, 31)];
        return (new Array[29u, 70u, 67u][, , ,])[16, 69, 1][75, Math.Sign(30), Math.Sign
            (7), Math.Max(71, 35)];
    }
    public static byte[,] Static2()
    {
        AA.Static1();
        while ((new bool[2u, 2u, 54u, 97u][,])[32, 9, 82, 40][new int[] { }[31],
            Math.Sign(7)])
        {
            do
            {
                sbyte local2 = ((sbyte)((new int[29u, 115u])[6, 114]));
                local2 = (new sbyte[122u, 103u])[(new int[52u, 120u])[18, 122], Math.Sign(
                    109)];
                do
                {
                    break;
                }
#pragma warning disable 0162
                while ((new uint[] { 47u, 48u, 35u, 86u }[72] != (new uint[95u, 92u, 5u])[15,
                    (0), 101]));
#pragma warning restore 0162
            }
            while (((new AA[106u, 122u])[114, 86] != (new object[101u, 59u])[40, (0)]));
            AA.Static1();
            if (((bool)(new object[] { null }[65])))
                AA.Static1();
            for ((new sbyte[101u, 64u][])[122, 88][75] += Math.Min((new sbyte[91u, 27u, 86u
                ])[48, 111, 62], (new sbyte[57u, 83u])[122, 57]); (new float[]{93.0f, 67.0f
				, 46.0f, 61.0f, 70.0f }[34] == ((float)(68.0))); (new ulong[93u, 100u, 123u
                , 105u][])[110, 106, 35, 16][((int)(47.0f))] *= Math.Max((new ulong[115u, 35u
                , 113u, 82u])[1, 91, 62, 42], (new ulong[32u])[108]))
            {
                try
                {
                }
                catch (Exception)
                {
                }
            }
        }
        try
        {
        }
        catch (InvalidOperationException)
        {
        }
        do
        {
        }
        while (((bool)((new object[74u, 111u])[97, 72])));
        return (new byte[100u, 17u, 75u, 30u][][,])[11, 65, 105, 83][Math.Sign(66)];
    }
}

public class App
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Console.WriteLine("Testing AA::Static1");
            AA.Static1();
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        try
        {
            Console.WriteLine("Testing AA::Static2");
            AA.Static2();
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        Console.WriteLine("Passed.");
        return 100;
    }
}
