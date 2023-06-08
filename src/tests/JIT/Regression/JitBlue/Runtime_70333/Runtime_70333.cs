// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        Test();

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Test()
    {
        M1(0);
    }

    internal static void M1(byte arg0)
    {
        long var6 = default(long);
        arg0 = (byte)(~(ulong)var6 % 3545460779U);
        System.Console.WriteLine(arg0);
    }
}
