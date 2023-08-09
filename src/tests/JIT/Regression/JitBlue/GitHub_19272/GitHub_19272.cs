// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
// The test showed an incorrect optimization of (int)(long<<32+) when the const 32+ tree
// had side effects.

struct S0
{
    public ulong F0;
    public sbyte F6;
    public S0(sbyte f6): this() { F6 = f6; }
}

public class Program
{
    static S0 s_1 = new S0(127);
    static int result = -1;

    static void SetResult(ulong res)
    {
        result = 100;
    }

    static byte M1()
    {
        SetResult(s_1.F0);
        return 0;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int vr0 = (int)((ulong)M1() << 33) / s_1.F6;
        if (result == 100)
        {
            System.Console.WriteLine("Pass");
        }
        else
        {
            System.Console.WriteLine("Failed");
        }
        return result;
    }
}
