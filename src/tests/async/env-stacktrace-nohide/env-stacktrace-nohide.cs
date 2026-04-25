// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

/// <summary>
/// Tests Environment.StackTrace with DOTNET_StackTraceAsyncBehavior=0 (hiding disabled).
/// When hiding is disabled, the pre-await trace should include non-async callers
/// below the async methods (the full physical call stack).
/// </summary>
public class Async2EnvStackTraceNoHide
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

        // With hiding OFF, MiddleMethod and OuterMethod should appear in both traces.
        Assert.True(
            preAwait.Contains(nameof(MiddleMethod), StringComparison.Ordinal),
            "Pre-await should contain " + nameof(MiddleMethod) + "." + Environment.NewLine + preAwait);
        Assert.True(
            preAwait.Contains(nameof(OuterMethod), StringComparison.Ordinal),
            "Pre-await should contain " + nameof(OuterMethod) + "." + Environment.NewLine + preAwait);

        // With hiding OFF, the sync caller should be VISIBLE in pre-await trace.
        Assert.True(
            preAwait.Contains(nameof(SyncCaller), StringComparison.Ordinal),
            "Pre-await should contain " + nameof(SyncCaller) + " when hiding is disabled." + Environment.NewLine + preAwait);

        // Post-await should still have continuation stitching.
        Assert.True(
            postAwait.Contains(nameof(MiddleMethod), StringComparison.Ordinal),
            "Post-await should contain " + nameof(MiddleMethod) + "." + Environment.NewLine + postAwait);
        Assert.True(
            postAwait.Contains(nameof(OuterMethod), StringComparison.Ordinal),
            "Post-await should contain " + nameof(OuterMethod) + "." + Environment.NewLine + postAwait);
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
