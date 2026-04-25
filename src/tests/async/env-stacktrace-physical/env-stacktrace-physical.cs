// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

/// <summary>
/// Tests Environment.StackTrace with DOTNET_StackTraceAsyncBehavior=2 (physical only).
/// When set to 2, async continuation stitching is completely disabled.
/// The stack trace shows only the physical call stack — no continuation frames are spliced in.
/// </summary>
public class Async2EnvStackTracePhysical
{
    [Fact]
    public static void TestEntryPoint()
    {
        SyncCaller();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    private static void SyncCaller()
    {
        AsyncEntry().GetAwaiter().GetResult();
    }

    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    private static async Task AsyncEntry()
    {
        (string preAwait, string postAwait) = await OuterMethod();

        // Pre-await: physical stack is intact, so all methods appear.
        Assert.True(
            preAwait.Contains(nameof(MiddleMethod), StringComparison.Ordinal),
            "Pre-await should contain " + nameof(MiddleMethod) + "." + Environment.NewLine + preAwait);
        Assert.True(
            preAwait.Contains(nameof(OuterMethod), StringComparison.Ordinal),
            "Pre-await should contain " + nameof(OuterMethod) + "." + Environment.NewLine + preAwait);
        Assert.True(
            preAwait.Contains(nameof(SyncCaller), StringComparison.Ordinal),
            "Pre-await should contain " + nameof(SyncCaller) + "." + Environment.NewLine + preAwait);

        // Post-await: no continuation stitching, so OuterMethod is NOT on the
        // physical stack (it was a suspended caller). MiddleMethod is the resumed
        // method so it IS on the physical stack.
        Assert.True(
            postAwait.Contains(nameof(MiddleMethod), StringComparison.Ordinal),
            "Post-await should contain " + nameof(MiddleMethod) + "." + Environment.NewLine + postAwait);
        Assert.False(
            postAwait.Contains(nameof(OuterMethod), StringComparison.Ordinal),
            "Post-await should NOT contain " + nameof(OuterMethod) + " with physical-only mode." + Environment.NewLine + postAwait);

        // SyncCaller is also not on the physical stack post-await.
        Assert.False(
            postAwait.Contains(nameof(SyncCaller), StringComparison.Ordinal),
            "Post-await should NOT contain " + nameof(SyncCaller) + " with physical-only mode." + Environment.NewLine + postAwait);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(string, string)> OuterMethod()
    {
        return await MiddleMethod();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(string, string)> MiddleMethod()
    {
        string preAwait = Environment.StackTrace;
        await InnerMethod();
        string postAwait = Environment.StackTrace;
        return (preAwait, postAwait);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task InnerMethod()
    {
        await Task.Delay(1);
    }
}
