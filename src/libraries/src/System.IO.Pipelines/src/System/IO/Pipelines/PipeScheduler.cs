// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Pipelines
{
    /// <summary>
    /// Abstraction for running <see cref="PipeReader"/> and <see cref="PipeWriter"/> callbacks and continuations
    /// </summary>
    public abstract class PipeScheduler
    {
        private static readonly ThreadPoolScheduler _threadPoolScheduler = new ThreadPoolScheduler();
        private static readonly InlineScheduler _inlineScheduler = new InlineScheduler();

        /// <summary>
        /// The <see cref="PipeScheduler"/> implementation that queues callbacks to thread pool
        /// </summary>
        public static PipeScheduler ThreadPool => _threadPoolScheduler;

        /// <summary>
        /// The <see cref="PipeScheduler"/> implementation that runs callbacks inline
        /// </summary>
        public static PipeScheduler Inline => _inlineScheduler;

        /// <summary>
        /// Requests <see cref="action"/> to be run on scheduler
        /// </summary>
        public abstract void Schedule(Action action);

        /// <summary>
        /// Requests <see cref="action"/> to be run on scheduler with <see cref="state"/> being passed in
        /// </summary>
        public abstract void Schedule(Action<object> action, object state);
    }
}
