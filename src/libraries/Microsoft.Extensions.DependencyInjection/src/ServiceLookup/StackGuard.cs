// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class StackGuard
    {
        private const int MaxExecutionStackCount = 1024;

        private int _executionStackCount;


        public bool TryEnterOnCurrentStack()
        {
#if NETCOREAPP2_0
            if (RuntimeHelpers.TryEnsureSufficientExecutionStack())
            {
                return true;
            }
#else
            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
                return true;
            }
            catch (InsufficientExecutionStackException)
            {
            }
#endif

            if (_executionStackCount < MaxExecutionStackCount)
            {
                return false;
            }

            throw new InsufficientExecutionStackException();
        }

        public TR RunOnEmptyStack<T1, T2, TR>(Func<T1, T2, TR> action, T1 arg1, T2 arg2)
        {
            return RunOnEmptyStackCore(s =>
            {
                var t = (Tuple<Func<T1, T2, TR>, T1, T2>)s;
                return t.Item1(t.Item2, t.Item3);
            }, Tuple.Create(action, arg1, arg2));
        }

        private R RunOnEmptyStackCore<R>(Func<object, R> action, object state)
        {
            _executionStackCount++;

            try
            {
                // Using default scheduler rather than picking up the current scheduler.
                Task<R> task = Task.Factory.StartNew(action, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

                TaskAwaiter<R> awaiter = task.GetAwaiter();

                // Avoid AsyncWaitHandle lazy allocation of ManualResetEvent in the rare case we finish quickly.
                if (!awaiter.IsCompleted)
                {
                    // Task.Wait has the potential of inlining the task's execution on the current thread; avoid this.
                    ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
                }

                // Using awaiter here to unwrap AggregateException.
                return awaiter.GetResult();
            }
            finally
            {
                _executionStackCount--;
            }
        }
    }
}
