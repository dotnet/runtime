// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_126414;

public readonly struct Endpoint
{
    public Endpoint(Endpoint e)
    {
        A = e.A;
        B = e.B;
        C = e.C;
    }

    public Endpoint(int a, int b, int c) { A = a; B = b; C = c; }

    public int A { get; }
    public int B { get; }
    public int C { get; }
}

public sealed class Range
{
    private readonly Endpoint m_start;
    private readonly Endpoint m_end;

    // Using AggressiveOptimization to force reproduction of the bug which would eventually occur when tiered compilation optimizes the constructor
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public Range(Endpoint start, Endpoint end)
    {
        m_start = new Endpoint(start);  // Copy via copy constructor
        m_end = new Endpoint(end);      // Copy via copy constructor
    }

    public int Length => m_end.A - m_start.A;
}

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        const int expected = 5759;

        var r = new Range(new Endpoint(100, 0, 0), new Endpoint(100 + expected, 0, 0));

        if (r.Length != expected)
        {
            Console.WriteLine($"FAIL: Length={r.Length} (expected {expected}), hex=0x{r.Length:X}");
            return 101;
        }

        Console.WriteLine($"PASS: Length = {expected}");
        return 100;
    }
}
