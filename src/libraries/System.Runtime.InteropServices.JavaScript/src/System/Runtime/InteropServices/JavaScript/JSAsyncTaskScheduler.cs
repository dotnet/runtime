// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    // executes all tasks thru queue, never inline
    internal sealed class JSAsyncTaskScheduler : TaskScheduler
    {
        private readonly JSSynchronizationContext m_synchronizationContext;

        internal JSAsyncTaskScheduler(JSSynchronizationContext synchronizationContext)
        {
            m_synchronizationContext = synchronizationContext;
        }

        protected override void QueueTask(Task task)
        {
            m_synchronizationContext.Post((_) =>
            {
                if (!TryExecuteTask(task))
                {
                    Environment.FailFast("Unexpected failure in JSAsyncTaskScheduler" + Environment.CurrentManagedThreadId);
                }
            }, null);
        }

        // this is the main difference from the SynchronizationContextTaskScheduler
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        protected override IEnumerable<Task>? GetScheduledTasks()
        {
            return null;
        }

        public override int MaximumConcurrencyLevel => 1;
    }
}
