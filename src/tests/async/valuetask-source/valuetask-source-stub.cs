// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Xunit;

public class ValueTaskSourceAndStubs
{
    [Fact]
    public static void EntryPoint()
    {
        SynchronizationContext? original = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new MySyncContext());

        try
        {
            new ValueTaskSourceAndStubs().TestAsync(new C()).GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    private async Task TestAsync(IFace i)
    {
        await i.Foo<string>(0, 1, 2, 3, 4, 5, 6, 7, "value");
    }

    private struct C : IFace
    {
        public ValueTask Foo<T>(int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, T value)
        {
            return new ValueTask(new Source(), 0);
        }

        private class Source : IValueTaskSource
        {
            public void GetResult(short token)
            {
            }

            public ValueTaskSourceStatus GetStatus(short token)
            {
                return ValueTaskSourceStatus.Pending;
            }

            public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
            {
                Assert.Equal(ValueTaskSourceOnCompletedFlags.UseSchedulingContext, flags);
                ThreadPool.UnsafeQueueUserWorkItem(continuation, state, preferLocal: true);
            }
        }
    }

    private interface IFace
    {
        ValueTask Foo<T>(int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, T value);
    }

    private class MySyncContext : SynchronizationContext
    {
    }
}
