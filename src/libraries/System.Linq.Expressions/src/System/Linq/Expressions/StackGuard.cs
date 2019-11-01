// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Expressions
{
    internal sealed class StackGuard
    {
        private const int MaxExecutionStackCount = 1024;

        private int _executionStackCount;

        public bool TryEnterOnCurrentStack()
        {
            if (RuntimeHelpers.TryEnsureSufficientExecutionStack())
            {
                return true;
            }

            if (_executionStackCount < MaxExecutionStackCount)
            {
                return false;
            }

            throw new InsufficientExecutionStackException();
        }

        public void RunOnEmptyStack<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            RunOnEmptyStackCore(s =>
            {
                var t = (Tuple<Action<T1, T2>, T1, T2>)s;
                t.Item1(t.Item2, t.Item3);
                return default(object);
            }, Tuple.Create(action, arg1, arg2));
        }

        public void RunOnEmptyStack<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            RunOnEmptyStackCore(s =>
            {
                var t = (Tuple<Action<T1, T2, T3>, T1, T2, T3>)s;
                t.Item1(t.Item2, t.Item3, t.Item4);
                return default(object);
            }, Tuple.Create(action, arg1, arg2, arg3));
        }

        public R RunOnEmptyStack<T1, T2, R>(Func<T1, T2, R> action, T1 arg1, T2 arg2)
        {
            return RunOnEmptyStackCore(s =>
            {
                var t = (Tuple<Func<T1, T2, R>, T1, T2>)s;
                return t.Item1(t.Item2, t.Item3);
            }, Tuple.Create(action, arg1, arg2));
        }

        public R RunOnEmptyStack<T1, T2, T3, R>(Func<T1, T2, T3, R> action, T1 arg1, T2 arg2, T3 arg3)
        {
            return RunOnEmptyStackCore(s =>
            {
                var t = (Tuple<Func<T1, T2, T3, R>, T1, T2, T3>)s;
                return t.Item1(t.Item2, t.Item3, t.Item4);
            }, Tuple.Create(action, arg1, arg2, arg3));
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
