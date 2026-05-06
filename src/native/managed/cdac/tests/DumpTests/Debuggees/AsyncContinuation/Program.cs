// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

/// <summary>
/// Debuggee for cDAC dump test — exercises async continuations.
/// Uses the runtime-async=on compiler feature to generate async2 methods,
/// which cause the JIT/runtime to create continuation MethodTables
/// (setting g_pContinuationClassIfSubTypeCreated).
///
/// FailFast is called from within the resumed inner async method
/// while DispatchContinuations is still on the call stack, ensuring
/// AsyncDispatcherInfo.t_current points to a live continuation chain.
/// </summary>
internal static class Program
{
    internal static async Task<int> InnerAsync(int value)
    {
        await Task.Delay(1);

        // Crash while still inside Resume — t_current is set on this thread
        // and NextContinuation points to OuterAsync's continuation.
        Environment.FailFast("cDAC dump test: AsyncContinuation debuggee intentional crash");

        return value + 1;
    }

    internal static async Task<int> OuterAsync(int value)
    {
        return await InnerAsync(value);
    }

    private static void Main()
    {
        OuterAsync(41).GetAwaiter().GetResult();
    }
}
