// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

// Regression tests for https://github.com/dotnet/runtime/issues/123194.
//
// IL allows throwing an object that does not derive from System.Exception. When
// such an object propagates into a catch handler, it is (or is not) wrapped in a
// RuntimeWrappedException depending on the RuntimeCompatibilityAttribute
// WrapNonExceptionThrows setting of the assembly that owns the catch handler.
//
// The goal of runtime async (async2) is to match the traditional compiler
// generated state machine (async1) behavior. These tests exercise both async
// forms (via [RuntimeAsyncMethodGeneration]) throwing a non-Exception object,
// once from an assembly with WrapNonExceptionThrows = true (this assembly, which
// matches the C# default and CoreLib) and once from an assembly with
// WrapNonExceptionThrows = false (NoWrapThrowers), for throws both before and
// after a suspension point.
public class RuntimeAsyncNonExceptionThrows
{
    private const string ThrownObject = "A non-Exception object thrown from IL";

    // WrapNonExceptionThrows = true (this assembly): the non-Exception is observed
    // as a RuntimeWrappedException by the awaiting caller for both async forms.

    [Fact]
    public static void CatchAfterYield_Async2()
        => AssertNonExceptionWrapped(ThrowAfterYieldAsync2);

    [Fact]
    public static void CatchAfterYield_Async1()
        => AssertNonExceptionWrapped(ThrowAfterYieldAsync1);

    [Fact]
    public static void CatchBeforeYield_Async2()
        => AssertNonExceptionWrapped(ThrowBeforeYieldAsync2);

    [Fact]
    public static void CatchBeforeYield_Async1()
        => AssertNonExceptionWrapped(ThrowBeforeYieldAsync1);

    // WrapNonExceptionThrows = false (NoWrapThrowers assembly).

    [Fact]
    public static void CatchAfterYield_NoWrap_Async2()
        => AssertNonExceptionWrapped(NoWrapThrowers.ThrowAfterYieldAsync2);

    // async2 and async1 behave differently for a non-Exception thrown after a
    // suspension point in a WrapNonExceptionThrows = false assembly:
    //   * async2 faults the returned Task with a RuntimeWrappedException, which the
    //     caller observes just like the wrap=true case above.
    //   * async1 lets the raw non-Exception escape the resumed state machine onto
    //     the thread pool where it becomes an unhandled exception and crashes the
    //     process, so it can never be observed by the caller.
    // Enable this test once async1 and async2 agree (see the tracking issue).
    [ActiveIssue("https://github.com/dotnet/runtime/issues/123194")]
    [Fact]
    public static void CatchAfterYield_NoWrap_Async1()
        => AssertNonExceptionWrapped(NoWrapThrowers.ThrowAfterYieldAsync1);

    [Fact]
    public static void CatchBeforeYield_NoWrap_Async2()
        => AssertNonExceptionWrapped(NoWrapThrowers.ThrowBeforeYieldAsync2);

    [Fact]
    public static void CatchBeforeYield_NoWrap_Async1()
        => AssertNonExceptionWrapped(NoWrapThrowers.ThrowBeforeYieldAsync1);

    private static void AssertNonExceptionWrapped(Func<Task> thrower)
    {
        object wrapped = ObserveNonException(thrower).GetAwaiter().GetResult();
        Assert.Equal(ThrownObject, wrapped);
    }

    // Awaits the throwing method and returns the wrapped non-Exception object.
    // The catch handler lives in this WrapNonExceptionThrows = true assembly, so a
    // propagating non-Exception is caught as a RuntimeWrappedException. Returns null
    // if nothing was thrown so the assertion above fails with a clear diff.
    private static async Task<object> ObserveNonException(Func<Task> thrower)
    {
        try
        {
            await thrower();
            return null;
        }
        catch (RuntimeWrappedException e)
        {
            return e.WrappedException;
        }
    }

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
