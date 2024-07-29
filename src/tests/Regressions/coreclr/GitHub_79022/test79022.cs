// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    // This is a regression test for https://github.com/dotnet/runtime/issues/79022
    static ulong[,] s_1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test()
    {
        try
        {
            ushort vr10 = default(ushort);
            bool vr11 = 0 < ((s_1[0, 0] * (uint)(0 / vr10)) % 1);
        }
        catch 
        {
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Test();
        }
        catch
        {
            return -1;
        }

        return 100;
    }
}
