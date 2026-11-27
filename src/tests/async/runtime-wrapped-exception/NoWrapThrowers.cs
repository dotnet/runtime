// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// This assembly opts OUT of wrapping non-Exception throws, in contrast to the
// C# default (WrapNonExceptionThrows = true) used by the main test assembly and
// by CoreLib. The async methods below are otherwise identical to the ones in the
// main test assembly; keeping them in a separate assembly is the only way to
// exercise the WrapNonExceptionThrows = false configuration, since the setting is
// assembly-scoped and decided by the frame that catches the exception.
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = false)]

public static class NoWrapThrowers
{
    // Runtime async (async2) throwing a non-Exception after suspending.
    public static async Task ThrowAfterYieldAsync2()
    {
        await Task.Yield();
        NonExceptionThrower.ThrowNonException();
    }

    // Compiler state machine (async1) throwing a non-Exception after suspending.
    [RuntimeAsyncMethodGeneration(false)]
    public static async Task ThrowAfterYieldAsync1()
    {
        await Task.Yield();
        NonExceptionThrower.ThrowNonException();
    }

    // Runtime async (async2) throwing a non-Exception before suspending.
    public static async Task ThrowBeforeYieldAsync2()
    {
        NonExceptionThrower.ThrowNonException();
        await Task.Yield();
    }

    // Compiler state machine (async1) throwing a non-Exception before suspending.
    [RuntimeAsyncMethodGeneration(false)]
    public static async Task ThrowBeforeYieldAsync1()
    {
        NonExceptionThrower.ThrowNonException();
        await Task.Yield();
    }
}
