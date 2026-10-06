// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135234
{
    private const int WarmupIterations = 10_000;
    private const int TestIterations = 50_000;
    private const long MaxMemoryGrowth = 64 * 1024 * 1024;

    private sealed class Instance
    {
        public int Value;
    }

    private static class FailingInitializer
    {
        public static int Value;

        static FailingInitializer()
        {
            throw new InvalidOperationException("Initialization failed");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void TestEntryPoint(bool hardwareException)
    {
        using Process process = Process.GetCurrentProcess();
        RunIterations(WarmupIterations, hardwareException);
        long before = GetWorkingSet(process);

        RunIterations(TestIterations, hardwareException);
        long growth = GetWorkingSet(process) - before;

        string exceptionName = hardwareException ? nameof(NullReferenceException) : nameof(TypeInitializationException);
        Console.WriteLine($"Working set growth after {TestIterations} {exceptionName}s: {growth} bytes");
        Assert.True(growth < MaxMemoryGrowth, $"Working set grew by {growth} bytes; limit is {MaxMemoryGrowth} bytes.");
    }

    private static void RunIterations(int count, bool hardwareException)
    {
        for (int i = 0; i < count; i++)
        {
            if (hardwareException)
            {
                CheckHardwareException();
            }
            else
            {
                CheckException();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckException()
    {
        try
        {
            _ = FailingInitializer.Value;
        }
        catch (TypeInitializationException exception)
        {
            Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.Equal("Initialization failed", exception.InnerException.Message);
            return;
        }

        Assert.Fail("Expected a TypeInitializationException.");
    }

    private static void CheckHardwareException()
    {
        try
        {
            _ = ReadField(null);
        }
        catch (NullReferenceException)
        {
            return;
        }

        Assert.Fail("Expected a NullReferenceException.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadField(Instance instance)
    {
        return instance.Value;
    }

    private static long GetWorkingSet(Process process)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        process.Refresh();
        return process.WorkingSet64;
    }
}
