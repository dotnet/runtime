// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

// Exceptions thrown out of an inlined async frame must surface unchanged, including when
// the frame suspended first. A suspension makes the resumption skip the entry code that
// captured the frame's contexts, so the context restores that run while the exception
// unwinds must see that the frame resumed.
public class Async2InlinedFrameExceptions
{
    private static async Task SuspendOnlyAsync() => await Task.Yield();

    private static async Task ThrowOnlyAsync() => throw new InvalidOperationException("boom");

    private static async Task SuspendThenThrowAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("boom");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task InlinedSuspendOnlyAsync() => await SuspendOnlyAsync();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task InlinedThrowOnlyAsync() => await ThrowOnlyAsync();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task InlinedSuspendThenThrowAsync() => await SuspendThenThrowAsync();

    private static async Task<string> CatchAsync(Func<Task> body)
    {
        try
        {
            await body();
            return "no-exception";
        }
        catch (InvalidOperationException e)
        {
            return e.Message;
        }
        catch (Exception e)
        {
            return e.GetType().Name;
        }
    }

    private static async Task<string> SuspendOnlyCaughtAsync() => await CatchAsync(InlinedSuspendOnlyAsync);

    private static async Task<string> ThrowOnlyCaughtAsync() => await CatchAsync(InlinedThrowOnlyAsync);

    private static async Task<string> SuspendThenThrowCaughtAsync() => await CatchAsync(InlinedSuspendThenThrowAsync);

    [Fact]
    public static void ExceptionsSurfaceFromInlinedFrames()
    {
        Assert.Equal("no-exception", SuspendOnlyCaughtAsync().GetAwaiter().GetResult());
        Assert.Equal("boom", ThrowOnlyCaughtAsync().GetAwaiter().GetResult());
        Assert.Equal("boom", SuspendThenThrowCaughtAsync().GetAwaiter().GetResult());
    }

    private static async Task UncaughtAsync() => await InlinedSuspendThenThrowAsync();

    [Fact]
    public static void ExceptionPropagatesOutOfInlinedFrame()
    {
        InvalidOperationException e =
            Assert.Throws<InvalidOperationException>(() => UncaughtAsync().GetAwaiter().GetResult());
        Assert.Equal("boom", e.Message);
    }
}
