// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

// A smoke test for all DOTNET_TieredPGO strategies
class Program : IDisposable
{
    static int Main()
    {
        Program p = new();
        for (int i = 0; i < 100; i++)
        {
            HotLoop(p);
            Thread.Sleep(40); // cold loop
        }
        return 100;
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