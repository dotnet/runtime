// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

// Verify that the Jit_Patchpoint helper inserted for OSR preserves last error

public class Runtime_75828
{
    [Fact]
    public static int TestEntryPoint()
    {
        Marshal.SetLastSystemError(42);
        
        int expected = 5_000_000 + 42;
        
        int result = 0;
        for (int i = 0; i < 10_000_000; i++)
        {
            result += i % 2;
        }
        
        result += Marshal.GetLastSystemError();
        
        Console.WriteLine($"got {result} expected {expected}");
        
        return result == expected ? 100 : -1;
    }
}

