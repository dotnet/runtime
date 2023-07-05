// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    public static IRuntime s_rt;
    public static bool[,] s_32 = new bool[,] { { false } };
    public static sbyte[] s_53 = new sbyte[] { 0 };
    public static ushort s_62;
    [Fact]
    public static int TestEntryPoint()
    {
        s_rt = new Runtime();
        var vr9 = s_32[0, 0];
        var vr10 = M76(ref s_62, vr9);

        if (s_53[0] != 0)
            return 0;

        return 100;
    }

    public static ushort M76(ref ushort arg0, bool arg1)
    {
        byte var17 = default(byte);
        if (!arg1)
        {
            s_rt.WriteLine(0);
            bool[] var14 = new bool[] { false };
            arg1 = var14[0];
            s_rt.WriteLine(var14[0]);
            s_rt.WriteLine(var17);
            if (arg1 && arg1)
            {
                s_53[0] += 1;
            }

            bool[][] var18 = new bool[][] { new bool[] { true }, new bool[] { true }, new bool[] { true } };
        }

        return arg0;
    }
}

public interface IRuntime
{
    void WriteLine<T>(T value);
}

public class Runtime : IRuntime
{
    public void WriteLine<T>(T value)
    {
    }
}
