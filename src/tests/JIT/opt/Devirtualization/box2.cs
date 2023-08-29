// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Program
{
    static async Task TestTask()
    {
        for (int i = 0; i < 10; i++)
        {
            // In the associated AwaitUnsafeOnCompleted, the 
            // jit should devirtualize, remove the box,
            // and change to call unboxed entry, passing
            // extra context argument.
            await new ValueTask<string>(Task.Delay(1).ContinueWith(_ => default(string))).ConfigureAwait(false);
        }
    }

    [Fact]
    public static void TestEntryPoint() => Task.Run(TestTask).Wait();
}
