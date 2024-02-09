// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

namespace test87879;

public class test87879
{
    [Fact, SkipLocalsInit]
    public static void TestEntryPoint()
    {
        //determine the expected available stack size 1.5MB, minus a little bit (384kB) for overhead.
        var expectedSize = 0x180000 - 0x60000;

        //allocate on the stack as specified above
        Span<byte> bytes = stackalloc byte[expectedSize];
        Consume(bytes);
        Console.WriteLine("Main thread succeeded.");

        //repeat on a secondary thread
        Thread t = new Thread([SkipLocalsInit] () =>
        {
            Span<byte> bytes = stackalloc byte[expectedSize];
            Consume(bytes);
        });
        t.Start();
        t.Join();
        Console.WriteLine("Secondary thread succeeded.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(Span<byte> bytes)
    {
    }
}
