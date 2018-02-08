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
#if NETCOREAPP2_1
            // Queue to low contention local ThreadPool queue; rather than global queue as per Task
            System.Threading.ThreadPool.QueueUserWorkItem(_actionWaitCallback, action, preferLocal: true);
#elif NETSTANDARD2_0
            System.Threading.ThreadPool.QueueUserWorkItem(_actionWaitCallback, action);
#else
            Task.Factory.StartNew(action);
#endif
        }

        public override void Schedule(Action<object> action, object state)
        {
#if NETCOREAPP2_1
            // Queue to low contention local ThreadPool queue; rather than global queue as per Task
            System.Threading.ThreadPool.QueueUserWorkItem(_actionObjectWaitCallback, new ActionObjectAsWaitCallback(action, state), preferLocal: true);
#elif NETSTANDARD2_0
            System.Threading.ThreadPool.QueueUserWorkItem(_actionObjectWaitCallback, new ActionObjectAsWaitCallback(action, state));
#else
            Task.Factory.StartNew(action, state);
#endif
        }

#if NETCOREAPP2_1 || NETSTANDARD2_0
        private readonly static WaitCallback _actionWaitCallback = state => ((Action)state)();

        private readonly static WaitCallback _actionObjectWaitCallback = state => ((ActionObjectAsWaitCallback)state).Run();

        private sealed class ActionObjectAsWaitCallback
        {
            private Action<object> _action;
            private object _state;

            public ActionObjectAsWaitCallback(Action<object> action, object state)
            {
                _action = action;
                _state = state;
            }

            public void Run() => _action(_state);
        }
#endif
    }
}
