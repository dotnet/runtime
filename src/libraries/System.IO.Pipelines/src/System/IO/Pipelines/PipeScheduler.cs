// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.IO.Pipelines
{
    /// <summary>
    /// Abstraction for running <see cref="PipeReader"/> and <see cref="PipeWriter"/> callbacks and continuations
    /// </summary>
    public abstract class PipeScheduler
    {
        private static readonly ThreadPoolScheduler s_threadPoolScheduler = new ThreadPoolScheduler();
        private static readonly InlineScheduler s_inlineScheduler = new InlineScheduler();

        /// <summary>
        /// The <see cref="PipeScheduler"/> implementation that queues callbacks to thread pool
        /// </summary>
        public static PipeScheduler ThreadPool => s_threadPoolScheduler;

        /// <summary>
        /// The <see cref="PipeScheduler"/> implementation that runs callbacks inline
        /// </summary>
        public static PipeScheduler Inline => s_inlineScheduler;

        /// <summary>
        /// Requests <paramref name="action"/> to be run on scheduler with <paramref name="state"/> being passed in
        /// </summary>
        public abstract void Schedule(Action<object> action, object state);

        internal virtual void UnsafeSchedule(Action<object> action, object state)
            => Schedule(action, state);
    }
}
