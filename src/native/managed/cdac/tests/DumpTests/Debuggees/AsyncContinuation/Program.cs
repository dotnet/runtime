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
/// The async methods are structured so that InnerAsync has a normal (non-tail)
/// await with locals that survive across the suspension point, forcing the JIT
/// to create a continuation layout via getContinuationType.
///
/// FailFast is called after resuming from the await, while
/// DispatchContinuations is still on the call stack, ensuring
/// AsyncDispatcherInfo.t_current points to a live continuation chain.
/// </summary>
internal static class Program
{
    // Prevent the JIT from treating FailFast as a known noreturn intrinsic
    // by hiding it behind an indirect call.
    private static readonly Action<string> s_failFast = Environment.FailFast;

    internal static async Task<int> InnerAsync(int value)
    {
        // 'value' must survive across this suspension point.
        await Task.Delay(1);

        // Use 'value' after resumption — this forces the JIT to create a
        // continuation object that stores 'value' across the await.
        int result = value + 1;

        // Crash while still inside Resume — t_current is set on this thread
        // and NextContinuation points to OuterAsync's continuation.
        s_failFast("cDAC dump test: AsyncContinuation debuggee intentional crash");

        return result;
    }

    internal static async Task<int> OuterAsync(int value)
    {
        // Non-tail await: capture 'value', await InnerAsync, then use both.
        int inner = await InnerAsync(value);
        return inner + value;
    }

    private static void Main()
    {
        OuterAsync(41).GetAwaiter().GetResult();
    }
}
