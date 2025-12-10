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
    [Fact]
    public static void AwaitValueTaskDefaultContext()
    {
        SynchronizationContext currentCtx = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            Source sUseContext = new Source(true);

            AwaitConfigDefault(sUseContext).GetAwaiter().GetResult();
            // If we have no scheduling context or it is the default context,
            // then "UseSchedulingContext" works the same as "None".
            // However, what is passed to the VTS is optimization-specific:
            // - in unoptimized case ValueTask awaiters will pass "UseSchedulingContext",
            //   unless configured to "false".
            // - in optimized (as in JitOptimizeAwait=1, which is the default) case
            //   we do not distinguish "false" config vs. having default/no scheduling context,
            //   and "None" is passed in either case.
            Assert.True("UseSchedulingContext" == sUseContext.trace || "None" == sUseContext.trace);
            AwaitConfigTrue(sUseContext).GetAwaiter().GetResult();
            // Either value is ok. See comment above.
            Assert.True("UseSchedulingContext" == sUseContext.trace || "None" == sUseContext.trace);
            AwaitConfigFalse(sUseContext).GetAwaiter().GetResult();
            Assert.Equal("None", sUseContext.trace);

            Source sIgnoreContext = new Source(false);

            AwaitConfigDefault(sIgnoreContext).GetAwaiter().GetResult();
            // Either value is ok. See comment above.
            Assert.True("UseSchedulingContext" == sUseContext.trace || "None" == sUseContext.trace);
            AwaitConfigTrue(sIgnoreContext).GetAwaiter().GetResult();
            // Either value is ok. See comment above.
            Assert.True("UseSchedulingContext" == sUseContext.trace || "None" == sUseContext.trace);
            AwaitConfigFalse(sIgnoreContext).GetAwaiter().GetResult();
            Assert.Equal("None", sIgnoreContext.trace);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(currentCtx);
        }
    }

    [Fact]
    public static void AwaitValueTaskCustomContext()
    {
        SynchronizationContext currentCtx = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new CustomContext());
        try
        {
            Source sUseContext = new Source(true);

            AwaitConfigDefault(sUseContext).GetAwaiter().GetResult();
            Assert.Equal("UseSchedulingContext Posted", sUseContext.trace);
            AwaitConfigTrue(sUseContext).GetAwaiter().GetResult();
            Assert.Equal("UseSchedulingContext Posted", sUseContext.trace);
            AwaitConfigFalse(sUseContext).GetAwaiter().GetResult();
            Assert.Equal("None Posted", sUseContext.trace);

            Source sIgnoreContext = new Source(false);

            AwaitConfigDefault(sIgnoreContext).GetAwaiter().GetResult();
            Assert.Equal("UseSchedulingContext", sIgnoreContext.trace);
            AwaitConfigTrue(sIgnoreContext).GetAwaiter().GetResult();
            Assert.Equal("UseSchedulingContext", sIgnoreContext.trace);
            AwaitConfigFalse(sIgnoreContext).GetAwaiter().GetResult();
            Assert.Equal("None", sIgnoreContext.trace);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(currentCtx);
        }
    }

    [Fact]
    public static void AwaitValueTaskCustomContextExtraCall()
    {
        SynchronizationContext currentCtx = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new CustomContext());
        try
        {
            Source sUseContext = new Source(true);

            AwaitConfigDefaultExtraCall(sUseContext).GetAwaiter().GetResult();
            Assert.Equal("UseSchedulingContext Posted", sUseContext.trace);
            AwaitConfigTrueExtraCall(sUseContext).GetAwaiter().GetResult();
            Assert.Equal("UseSchedulingContext Posted", sUseContext.trace);
            AwaitConfigFalseExtraCall(sUseContext).GetAwaiter().GetResult();
            Assert.Equal("UseSchedulingContext Posted", sUseContext.trace);

            Source sIgnoreContext = new Source(false);

            AwaitConfigDefaultExtraCall(sIgnoreContext).GetAwaiter().GetResult();
            Assert.Equal("UseSchedulingContext", sIgnoreContext.trace);
            AwaitConfigTrueExtraCall(sIgnoreContext).GetAwaiter().GetResult();
            Assert.Equal("UseSchedulingContext", sIgnoreContext.trace);
            AwaitConfigFalseExtraCall(sIgnoreContext).GetAwaiter().GetResult();
            Assert.Equal("UseSchedulingContext", sIgnoreContext.trace);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(currentCtx);
        }
    }

    static bool IsDefaultContext()
    {
        return SynchronizationContext.Current == null ||
            SynchronizationContext.Current.GetType() == typeof(SynchronizationContext);
    }

    static async ValueTask AwaitConfigDefault(Source s)
    {
        bool inDefaultContext = IsDefaultContext();
        await s.Awaitable();

        if (inDefaultContext || !s._useContext)
        {
            Assert.True(IsDefaultContext());
        }
        else
        {
            Assert.False(IsDefaultContext());
        }
    }

    static async ValueTask AwaitConfigTrue(Source s)
    {
        bool inDefaultContext = IsDefaultContext();
        await s.Awaitable().ConfigureAwait(true);

        if (inDefaultContext || !s._useContext)
        {
            Assert.True(IsDefaultContext());
        }
        else
        {
            Assert.False(IsDefaultContext());
        }
    }

    static async ValueTask AwaitConfigFalse(Source s)
    {
        bool inDefaultContext = IsDefaultContext();
        await s.Awaitable().ConfigureAwait(false);

        if (inDefaultContext || !s._useContext)
        {
            Assert.True(IsDefaultContext());
        }
        else
        {
            Assert.False(IsDefaultContext());
        }
    }

    static async ValueTask AwaitConfigDefaultExtraCall(Source s)
    {
        bool inDefaultContext = IsDefaultContext();
        await s.ExtraCall();

        // regardless of source the context should be preserved.
        Assert.Equal(inDefaultContext, IsDefaultContext());
    }

    static async ValueTask AwaitConfigTrueExtraCall(Source s)
    {
        bool inDefaultContext = IsDefaultContext();
        await s.ExtraCall().ConfigureAwait(true);

        // regardless of source the context should be preserved.
        Assert.Equal(inDefaultContext, IsDefaultContext());
    }

    static async ValueTask AwaitConfigFalseExtraCall(Source s)
    {
        bool inDefaultContext = IsDefaultContext();
        await s.ExtraCall().ConfigureAwait(false);

        // TODO: With ConfigureAwait(false) the following should behave as if ExtraCall continuation
        //       runs transparently. That is not a case right now. We may consider matching the baseline behavior.
        //
        //       When a continuation requires no context (i.e. configured "false" or had not context),
        //       it can run on notifiers's context. See Task.SetContinuationForAwait and Task.RunContinuations
        //       Note that this "inlining" is optional. There is also a check for enough stack.

        //if (inDefaultContext || !s._useContext)
        //{
        //    Assert.True(IsDefaultContext());
        //}
        //else
        //{
        //    Assert.False(IsDefaultContext());
        //}
    }

    class CustomContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            SynchronizationContext currentCtx = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(this);
            try
            {
                d(state);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(currentCtx);
            }
        }
    }

    class Source : IValueTaskSource
    {
        public readonly bool _useContext;
        public string trace;

        public async ValueTask ExtraCall()
        {
            // this should not be a tailcall
            await Awaitable();
        }

        public ValueTask Awaitable()
        {
            return new ValueTask(this, 1);
        }

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
                trace += " Posted";
                schedulingContext.Post((obj) => continuation(obj), state);
            }
            else
            {
                ThreadPool.QueueUserWorkItem((obj) => continuation(obj), state);
            }
        }
    }
}
