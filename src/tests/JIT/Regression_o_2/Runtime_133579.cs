// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_133579;

using System.Runtime.CompilerServices;
using System.Threading;
using TestLibrary;
using Xunit;

public class Runtime_133579
{
    private sealed class Box
    {
        public int Value;
        public int Ready;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int ReadAfterAcquire(Box box, ManualResetEventSlim readerStarted)
    {
        readerStarted.Set();

        int value = 0;
        int seen = 0;
        while (seen < 2)
        {
            value = box.Value;
            if (Volatile.Read(ref box.Ready) != 0)
            {
                seen++;
            }
        }

        return value;
    }

    [OuterLoop]
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/41472", typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
    public static void OrdinaryLoadIsNotHoistedAcrossVolatileRead()
    {
        Box box = new();
        using ManualResetEventSlim readerStarted = new();
        int readerResult = -1;
        Thread reader = new(() =>
        {
            readerResult = ReadAfterAcquire(box, readerStarted);
        });
        reader.Start();

        readerStarted.Wait();
        Thread.Sleep(50);
        box.Value = 42;
        Volatile.Write(ref box.Ready, 1);
        reader.Join();

        Assert.Equal(42, readerResult);
    }
}
