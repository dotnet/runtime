// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.ExceptionServices;
using System.Threading.Tasks.Sources;

namespace System.Threading.Tasks.Tests
{
    internal static class ManualResetValueTaskSource
    {
        public static ManualResetValueTaskSource<T> Completed<T>(T result, Exception error = null)
        {
            var vts = new ManualResetValueTaskSource<T>();
            if (error != null)
            {
                vts.SetException(error);
            }
            else
            {
                vts.SetResult(result);
            }
            return vts;
        }

        public static ManualResetValueTaskSource<T> Delay<T>(int delayMs, T result, Exception error = null)
        {
            var vts = new ManualResetValueTaskSource<T>();
            Task.Delay(delayMs).ContinueWith(_ =>
            {
                if (error != null)
                {
                    vts.SetException(error);
                }
                else
                {
                    vts.SetResult(result);
                }
            });
            return vts;
        }
    }

    internal sealed class ManualResetValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
    {
        private static readonly Action<object> s_sentinel = new Action<object>(s => { });
        private Action<object> _continuation;
        private object _continuationState;
        private SynchronizationContext _capturedContext;
        private ExecutionContext _executionContext;
        private bool _completed;
        private T _result;
        private ExceptionDispatchInfo _error;

        public ValueTaskSourceStatus GetStatus(short token) =>
            !_completed ? ValueTaskSourceStatus.Pending :
            _error == null ? ValueTaskSourceStatus.Succeeded :
            _error.SourceException is OperationCanceledException ? ValueTaskSourceStatus.Canceled :
            ValueTaskSourceStatus.Faulted;

        public T GetResult(short token)
        {
            if (!_completed)
            {
                throw new Exception("Not completed");
            }

            _error?.Throw();
            return _result;
        }

        void IValueTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        public void Reset()
        {
            _completed = false;
            _continuation = null;
            _continuationState = null;
            _result = default;
            _error = null;
            _executionContext = null;
            _capturedContext = null;
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                _executionContext = ExecutionContext.Capture();
            }

            if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                _capturedContext = SynchronizationContext.Current;
            }

            _continuationState = state;
            if (Interlocked.CompareExchange(ref _continuation, continuation, null) != null)
            {
                SynchronizationContext sc = _capturedContext;
                if (sc != null)
                {
                    sc.Post(s =>
                    {
                        var tuple = (Tuple<Action<object>, object>)s;
                        tuple.Item1(tuple.Item2);
                    }, Tuple.Create(continuation, state));
                }
                else
                {
                    Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                }
            }
        }

        public void SetResult(T result)
        {
            _result = result;
            SignalCompletion();
        }

        public void SetException(Exception error)
        {
            _error = ExceptionDispatchInfo.Capture(error);
            SignalCompletion();
        }

        private void SignalCompletion()
        {
            _completed = true;
            if (Interlocked.CompareExchange(ref _continuation, s_sentinel, null) != null)
            {
                if (_executionContext != null)
                {
                    ExecutionContext.Run(_executionContext, s => ((ManualResetValueTaskSource<T>)s).InvokeContinuation(), this);
                }
                else
                {
                    InvokeContinuation();
                }
            }
        }

        private void InvokeContinuation()
        {
            SynchronizationContext sc = _capturedContext;
            if (sc != null)
            {
                sc.Post(s =>
                {
                    var thisRef = (ManualResetValueTaskSource<T>)s;
                    thisRef._continuation(thisRef._continuationState);
                }, this);
            }
            else
            {
                _continuation(_continuationState);
            }
        }
    }
}
