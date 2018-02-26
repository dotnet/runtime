// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    internal sealed class ThreadPoolScheduler : PipeScheduler
    {
        public override void Schedule(Action action)
        {
            // Queue to low contention local ThreadPool queue; rather than global queue as per Task
            System.Threading.ThreadPool.QueueUserWorkItem(s_actionCallback, action, preferLocal: true);
        }

        public override void Schedule(Action<object> action, object state)
        {
            // Queue to low contention local ThreadPool queue; rather than global queue as per Task
            System.Threading.ThreadPool.QueueUserWorkItem(action, state, preferLocal: true);
        }

        private static readonly Action<Action> s_actionCallback = state => state();
    }
}
