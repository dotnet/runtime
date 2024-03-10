// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

// A smoke test for all DOTNET_TieredPGO strategies
public class Program : IDisposable
{
    [Fact]
    public static void TestEntryPoint()
    {
        Program p = new();
        for (int i = 0; i < 100; i++)
        {
            HotLoop(p);
            Thread.Sleep(40); // cold loop
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void HotLoop(IDisposable d)
    {
        for (int i = 0; i < 100000; i++) // hot loop
            d?.Dispose();
    }

    public void Dispose() => Test();

    [MethodImpl(MethodImplOptions.NoInlining)]
    void Test() { }
}