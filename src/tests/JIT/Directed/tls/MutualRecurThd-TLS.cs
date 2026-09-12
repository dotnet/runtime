// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class MutualRecurThdTLS
{
    [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/133538", RuntimeConfiguration.Checked)]
    [Fact]
    public static void TestEntryPoint()
    {
        Thread[] threads = new Thread[10];

        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() => CallRun(null));
            threads[i].Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

    }

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Run")]
    extern static void CallRun([UnsafeAccessorType("Thread_EA, MutualRecurThdTLSNative")] object? a);
}
