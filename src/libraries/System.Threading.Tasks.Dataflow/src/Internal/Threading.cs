// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Dataflow.Internal.Threading
{
    internal delegate void TimerCallback(object state);

    internal sealed class Timer : CancellationTokenSource, IDisposable
    {
        internal Timer(TimerCallback callback, object state, int dueTime, int period)
        {
            Debug.Assert(period == -1, "This stub implementation only supports dueTime.");
            Task.Delay(dueTime, Token).ContinueWith((t, s) =>
            {
                var tuple = (Tuple<TimerCallback, object>)s;
                tuple.Item1(tuple.Item2);
            }, Tuple.Create(callback, state), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        }

        public new void Dispose() { base.Cancel(); }
    }

    internal sealed class Thread
    {
        internal static bool Yield() { return true; }
    }

    internal delegate void WaitCallback(object state);

    internal sealed class ThreadPool
    {
        private static readonly SynchronizationContext _ctx = new SynchronizationContext();

        internal static void QueueUserWorkItem(WaitCallback callback, object state)
        {
            _ctx.Post(s =>
            {
                var tuple = (Tuple<WaitCallback, object>)s;
                tuple.Item1(tuple.Item2);
            }, Tuple.Create(callback, state));
        }
    }
}
