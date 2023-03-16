// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Xunit;

public class BindHandleInvalid
{
    [Fact]
    public static int TestEntryPoint()
    {
        return (new BindHandleInvalid().RunTest());
    }

    int RunTest()
    {
        SafeFileHandle sfh = new SafeFileHandle(IntPtr.Zero, false);

        try
        {
            ThreadPool.BindHandle(sfh);
        }
        catch (Exception ex)
        {
            if ((uint)ex.HResult == (uint)0x80070006) // E_HANDLE, we can't access hresult
            {
                Console.WriteLine("Test passed");
                return (100);
            }
            else
            {
                Console.WriteLine($"Got wrong error - HResult: 0x{ex.HResult:x}, Exception: {ex}");
            }
        }
        Console.WriteLine("Didn't get argument null exception");
        return (99);
    }

    
}
