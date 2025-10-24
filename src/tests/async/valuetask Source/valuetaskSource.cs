// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Xunit;

public class Async2ValueTaskSource
{
    static string trace;

    [Fact]
    static void TestEntry()
    {
        ValueTask vtUseContext = new ValueTask(new Source(true), 1);

        AwaitConfigDefault(vtUseContext).GetAwaiter().GetResult();
        Assert.Equal("UseSchedulingContext", trace);
        AwaitConfigTrue(vtUseContext).GetAwaiter().GetResult();
        Assert.Equal("UseSchedulingContext", trace);
        AwaitConfigFalse(vtUseContext).GetAwaiter().GetResult();
        Assert.Equal("None", trace);

        ValueTask vtIgnoreContext = new ValueTask(new Source(true), 1);
        AwaitConfigDefault(vtIgnoreContext).GetAwaiter().GetResult();
        Assert.Equal("UseSchedulingContext", trace);
        AwaitConfigTrue(vtIgnoreContext).GetAwaiter().GetResult();
        Assert.Equal("UseSchedulingContext", trace);
        AwaitConfigFalse(vtIgnoreContext).GetAwaiter().GetResult();
        Assert.Equal("None", trace);

        Console.WriteLine();
        SynchronizationContext.SetSynchronizationContext(new SideeffectingContext());

        AwaitConfigDefault(vtUseContext).GetAwaiter().GetResult();
        Assert.Equal("UseSchedulingContext Posted", trace);
        AwaitConfigTrue(vtUseContext).GetAwaiter().GetResult();
        Assert.Equal("UseSchedulingContext Posted", trace);
        AwaitConfigFalse(vtUseContext).GetAwaiter().GetResult();
        Assert.Equal("None Posted", trace);

        AwaitConfigDefault(vtIgnoreContext).GetAwaiter().GetResult();
        Assert.Equal("UseSchedulingContext Posted", trace);
        AwaitConfigTrue(vtIgnoreContext).GetAwaiter().GetResult();
        Assert.Equal("UseSchedulingContext Posted", trace);
        AwaitConfigFalse(vtIgnoreContext).GetAwaiter().GetResult();
        Assert.Equal("None Posted", trace);
    }

    class SideeffectingContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            trace += " Posted";
            base.Post(d, state);
        }
    }

    static async ValueTask AwaitConfigDefault(ValueTask vt)
    {
        await (vt);
    }

    static async ValueTask AwaitConfigTrue(ValueTask vt)
    {
        await (vt).ConfigureAwait(true);
    }

    static async ValueTask AwaitConfigFalse(ValueTask vt)
    {
        await (vt).ConfigureAwait(false);
    }

    class Source : IValueTaskSource
    {
        private readonly bool _useContext;
        public Source(bool useContext)
        {
            _useContext = useContext;
        }

        public void GetResult(short token)
        {
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            return ValueTaskSourceStatus.Pending;
        }

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            trace = flags.ToString();

            var schedulingContext = SynchronizationContext.Current;
            if (_useContext && schedulingContext != null)
            {
                schedulingContext.Post((obj) => continuation(obj), state);
            }
            else
            {
                ThreadPool.QueueUserWorkItem((obj) => continuation(obj), state);
            }
        }
    }
}
