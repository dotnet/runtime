// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// Repro for https://github.com/dotnet/runtime/issues/88168.
// Derived from (and similar to) test GitHub_22820 for https://github.com/dotnet/coreclr/issues/22820.
//
// Run with optimized codegen and DOTNET_GCStress=0x4

class DisposableObject : IDisposable
{
    public void Dispose()
    {
        Console.WriteLine("In dispose");
    }
}

public class Program
{
    public static bool IsExpectedException(Exception e)
    {
        Console.WriteLine("In filter");
        GC.Collect();
        return e is OperationCanceledException;
    }
    
    public static IDisposable AllocateObject()
    {
        return new DisposableObject();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void top_level_filter_test()
    {
        try
        {
            using (AllocateObject())
            {
                throw new Exception();
            }
        }
        catch (Exception e1) when (IsExpectedException(e1))
        {
            Console.WriteLine("In catch 1");
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int result = 0;

        try
        {
            top_level_filter_test();
        }
        catch (Exception e2)
        {
            Console.WriteLine("In catch 2");
            result = 100;
        }

        return result;
    }
}