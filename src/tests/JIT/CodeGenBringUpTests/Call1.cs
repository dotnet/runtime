// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_Call1
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    internal static void  M() { Console.WriteLine("Hello"); }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    internal static void  Call1()
    {
        M();
    }
    [Fact]
    public static int TestEntryPoint()
    {
        Call1();
        return 100;
    }
}

