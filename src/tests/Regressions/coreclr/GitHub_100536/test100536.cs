// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Test100536
{
    [DllImport("__test", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Nonexistent")]
    private static extern IntPtr Nonexistent();

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void GarbleStack()
    {
        Span<byte> local = stackalloc byte[4096];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test()
    {
        try
        {
            Nonexistent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Expected exception {ex} caught");
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Test();
        GarbleStack();
        GC.Collect();
    }
}

