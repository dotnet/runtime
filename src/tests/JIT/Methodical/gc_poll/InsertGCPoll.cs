// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

static class GCPollNative
{
    // Simple function that can be marked as SuppressGCTransition which will
    // result in a GCPoll insertion.
    [DllImport(nameof(GCPollNative))]
    [SuppressGCTransition]
    public static extern uint NextUInt32(uint n);

    // Simple function that can be marked as SuppressGCTransition which will
    // result in a GCPoll insertion.
    [DllImport(nameof(GCPollNative))]
    [SuppressGCTransition]
    public static extern ulong NextUInt64(ulong n);
}

public class InsertGCPoll
{
    private static int PropNextInt32 => (int)GCPollNative.NextUInt32(0);
    private static long PropNextInt64 => (long)GCPollNative.NextUInt64(0);

    private static void AccessAsProperty32()
    {
        int a = PropNextInt32;
        int b = PropNextInt32;
        DisplayValues(a, b);
    }

    private static void AccessAsProperty64()
    {
        long a = PropNextInt64;
        long b = PropNextInt64;
        DisplayValues(a, b);
    }

    private static void DisplayValues<T>(T a, T b)
    {
        Console.WriteLine($"{a} {b}");
    }

    private static void BranchOnProperty32()
    {
        if (-1 == PropNextInt32)
        {
            Console.WriteLine("");
        }
    }

    private static void BranchOnProperty64()
    {
        if (-1 == PropNextInt64)
        {
            Console.WriteLine("");
        }
    }

    private static void CompoundStatementBranchOnProperty()
    {
        if (-1 == (PropNextInt64 + PropNextInt32 - PropNextInt64 + PropNextInt64 - PropNextInt32))
        {
            Console.WriteLine("");
        }
    }

    private static void LoopOn32()
    {
        uint i = 0;
        for (int j = 0; j < 10 || i < 32; ++j)
        {
            i += GCPollNative.NextUInt32(1);
        }
    }

    private static void LoopOn64()
    {
        ulong i = 0;
        for (int j = 0; j < 10 || i < 32; ++j)
        {
            i += GCPollNative.NextUInt64(1);
        }
    }
    
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            AccessAsProperty32();
            AccessAsProperty64();
            BranchOnProperty32();
            BranchOnProperty64();
            CompoundStatementBranchOnProperty();
            LoopOn32();
            LoopOn64();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return 101;
        }
        return 100;
    }
}
