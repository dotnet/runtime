// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_Call1
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    internal static void M() { Console.WriteLine("Hello"); }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    [Fact]
    public static void Call1() => M();
}

