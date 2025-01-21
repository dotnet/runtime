// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public static class Test111242
{
    [DllImport("Test111242Lib")]
    static extern unsafe void TestSetJmp(delegate* unmanaged<void*,void> managedCallback);

    [DllImport("Test111242Lib")]
    static extern unsafe void TestLongJmp(void *jmpBuf);

    static bool ExceptionFilter(Exception ex)
    {
        Assert.Fail("Should not call filter for longjmp SEH exception");
        return true;
    }

    static bool wasFinallyInvoked = false;

    [UnmanagedCallersOnly]
    static unsafe void ManagedCallback(void *jmpBuf)
    {
        try
        {
            TestLongJmp(jmpBuf);
        }
        catch (Exception ex) when (ExceptionFilter(ex))
        {
            Assert.Fail("Should not catch longjmp SEH exception via filter");	
        }
        catch
        {
            Assert.Fail("Should not catch longjmp SEH exception via catch-all");	
        }
        finally
        {
            Console.WriteLine("Finally block executed");
            wasFinallyInvoked = true;
        }
        Assert.Fail("Should not reach here");
    }

    [Fact]
    public static unsafe void TestEntryPoint()
    {
        TestSetJmp(&ManagedCallback);
        Assert.True(wasFinallyInvoked);
        Console.WriteLine("Test passed");
    }
}
