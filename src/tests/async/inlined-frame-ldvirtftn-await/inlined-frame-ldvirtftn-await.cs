// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

// Awaits that the EE asks to be dispatched via ldvirtftn, such as calls to generic virtual
// methods, go through their own argument insertion path. An await inside an inlined async
// frame uses that frame's own contexts, and the ldvirtftn path must not hand it the
// caller's instead: doing so leaves the inlined frame's own resumed indicator unset, so
// after a suspension its context restore runs as if the frame had never resumed, over
// state that the resumption skipped capturing.
public class Async2InlinedFrameLdvirtftnAwait
{
    private class Dispatcher
    {
        // Generic virtual methods are dispatched via ldvirtftn.
        public virtual async Task<T> SuspendAndReturnAsync<T>(T value)
        {
            await Task.Yield();
            return value;
        }

        public virtual async Task<T> SuspendAndThrowAsync<T>(T value)
        {
            await Task.Yield();
            throw new InvalidOperationException("boom");
        }
    }

    private static readonly Dispatcher s_dispatcher = new Dispatcher();

    // Aggressively inlined so that the awaits below end up in an inlined async frame with
    // its own contexts. Neither await is in tail position.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task<int> InlinedReturningAsync()
    {
        string result = await s_dispatcher.SuspendAndReturnAsync("value");
        return result.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task<int> InlinedThrowingAsync()
    {
        string result = await s_dispatcher.SuspendAndThrowAsync("value");
        return result.Length;
    }

    private static async Task<int> ReturningAsync() => await InlinedReturningAsync();

    private static async Task<int> ThrowingAsync() => await InlinedThrowingAsync();

    [Fact]
    public static void LdvirtftnAwaitInInlinedFrameReturns()
    {
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(5, ReturningAsync().GetAwaiter().GetResult());
        }
    }

    [Fact]
    public static void LdvirtftnAwaitInInlinedFramePropagatesException()
    {
        for (int i = 0; i < 10; i++)
        {
            InvalidOperationException e =
                Assert.Throws<InvalidOperationException>(() => ThrowingAsync().GetAwaiter().GetResult());
            Assert.Equal("boom", e.Message);
        }
    }
}
