// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// Repro for https://github.com/dotnet/coreclr/issues/22820.
// On x86 we need to report enclosed handler
// live-in locals as live into any enclosing filter.
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
    
    [Fact]
    public static int TestEntryPoint()
    {
        int result = 0;

        try
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
        catch (Exception e2)
        {
            Console.WriteLine("In catch 2");
            result = 100;
        }

        return result;
    }
}
