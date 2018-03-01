// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Threading.Channels
{
    internal abstract class ResettableValueTaskSource
    {
        /// <summary>Sentinel object used in a field to indicate the operation has completed.</summary>
        protected static readonly Action<object> s_completedSentinel = new Action<object>(s => Debug.Fail($"{nameof(ResettableValueTaskSource)}.{nameof(s_completedSentinel)} invoked."));

        /// <summary>Throws an exception indicating that the operation's result was accessed before the operation completed.</summary>
        protected static void ThrowIncompleteOperationException() =>
            throw new InvalidOperationException(SR.InvalidOperation_IncompleteAsyncOperation);

        /// <summary>Throws an exception indicating that multiple continuations can't be set for the same operation.</summary>
        protected static void ThrowMultipleContinuations() =>
            throw new InvalidOperationException(SR.InvalidOperation_MultipleContinuations);

        /// <summary>Throws an exception indicating that the operation was used after it was supposed to be used.</summary>
        protected static void ThrowIncorrectCurrentIdException() =>
            throw new InvalidOperationException(SR.InvalidOperation_IncorrectToken);

        /// <summary>Describes states the operation can be in.</summary>
        public enum States
        {
            /// <summary>The operation has been assigned an owner.  No one else can use it.</summary>
            Owned = 0,
            /// <summary>Completion has been reserved.  Only the reserver is allowed to complete it.</summary>
            CompletionReserved = 1,
            /// <summary>The operation has completed and has had its result or error stored.</summary>
            CompletionSet = 2,
            /// <summary>The operation's result/error has been retrieved.  It's available for reuse.</summary>
            Released = 3
        }
    }

    internal abstract class ResettableValueTaskSource<T> : ResettableValueTaskSource, IValueTaskSource, IValueTaskSource<T>
    {
        private volatile int _state = (int)States.Owned;
        private T _result;
        private ExceptionDispatchInfo _error;
        private Action<object> _continuation;
        private object _continuationState;
        private object _schedulingContext;
        private ExecutionContext _executionContext;
        private short _currentId;

        public ValueTask ValueTask => new ValueTask(this, _currentId);
        public ValueTask<T> ValueTaskOfT => new ValueTask<T>(this, _currentId);

        public bool RunContinutationsAsynchronously { get; protected set; }
        public ValueTaskSourceStatus GetStatus(short token)
        {
            if (_currentId == token)
            {
                switch ((States)_state)
                {
                    case States.Owned:
                    case States.CompletionReserved:
                        return ValueTaskSourceStatus.Pending;

                    case States.CompletionSet:
                        return
                            _error == null ? ValueTaskSourceStatus.Succeeded :
                            _error.SourceException is OperationCanceledException ? ValueTaskSourceStatus.Canceled :
                            ValueTaskSourceStatus.Faulted;
                }
            }

            ThrowIncorrectCurrentIdException();
            return default; // just to satisfy compiler
        }

        public bool IsCompleted => _state >= (int)States.CompletionSet;
        public States UnsafeState { get => (States)_state; set => _state = (int)value; }

        public T GetResult(short token)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            if (!IsCompleted)
            {
                ThrowIncompleteOperationException();
            }

            ExceptionDispatchInfo error = _error;
            T result = _result;
            _currentId++;
            _state = (int)States.Released; // only after fetching all needed data

            error?.Throw();
            return result;
        }

        void IValueTaskSource.GetResult(short token)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            if (!IsCompleted)
            {
                ThrowIncompleteOperationException();
            }

            ExceptionDispatchInfo error = _error;
            _currentId++;
            _state = (int)States.Released; // only after fetching all needed data

            error?.Throw();
        }

        public bool TryOwnAndReset()
        {
            if (Interlocked.CompareExchange(ref _state, (int)States.Owned, (int)States.Released) == (int)States.Released)
            {
                _continuation = null;
                _continuationState = null;
                _result = default;
                _error = null;
                _schedulingContext = null;
                _executionContext = null;
                return true;
            }

            return false;
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (_currentId != token)
            {
                ThrowIncorrectCurrentIdException();
            }

            // We need to store the state before the CompareExchange, so that if it completes immediately
            // after the CompareExchange, it'll find the state already stored.  If someone misuses this
            // and schedules multiple continuations erroneously, we could end up using the wrong state.
            // Make a best-effort attempt to catch such misuse.
            if (_continuationState != null)
            {
                ThrowMultipleContinuations();
            }
            _continuationState = state;

            Debug.Assert(_executionContext == null);
            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                _executionContext = ExecutionContext.Capture();
            }

            Debug.Assert(_schedulingContext == null);
            SynchronizationContext sc = null;
            TaskScheduler ts = null;
            if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                sc = SynchronizationContext.Current;
                if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                {
                    _schedulingContext = sc;
                }
                else
                {
                    ts = TaskScheduler.Current;
                    if (ts != TaskScheduler.Default)
                    {
                        _schedulingContext = ts;
                    }
                }
            }

            Action<object> prevContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
            if (prevContinuation != null)
            {
                if (prevContinuation != s_completedSentinel)
                {
                    ThrowMultipleContinuations();
                }

                Debug.Assert(IsCompleted, $"Expected IsCompleted, got {(States)_state}");
                if (sc != null)
                {
                    sc.Post(s =>
                    {
                        var t = (Tuple<Action<object>, object>)s;
                        t.Item1(t.Item2);
                    }, Tuple.Create(continuation, state));
                }
                else
                {
                    Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts ?? TaskScheduler.Default);
                }
            }
        }

        public bool TrySetResult(T result)
        {
            if (Interlocked.CompareExchange(ref _state, (int)States.CompletionReserved, (int)States.Owned) == (int)States.Owned)
            {
                _result = result;
                SignalCompletion();
                return true;
            }

            return false;
        }

        public bool TrySetException(Exception error)
        {
            if (Interlocked.CompareExchange(ref _state, (int)States.CompletionReserved, (int)States.Owned) == (int)States.Owned)
            {
                _error = ExceptionDispatchInfo.Capture(error);
                SignalCompletion();
                return true;
            }

            return false;
        }

        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref _state, (int)States.CompletionReserved, (int)States.Owned) == (int)States.Owned)
            {
                _error = ExceptionDispatchInfo.Capture(new OperationCanceledException(cancellationToken));
                SignalCompletion();
                return true;
            }

            return false;
        }

        private void SignalCompletion()
        {
            _state = (int)States.CompletionSet;
            if (_continuation != null || Interlocked.CompareExchange(ref _continuation, s_completedSentinel, null) != null)
            {
                ExecutionContext ec = _executionContext;
                if (ec != null)
                {
                    ExecutionContext.Run(ec, s => ((ResettableValueTaskSource<T>)s).InvokeContinuation(), this);
                }
                else
                {
                    InvokeContinuation();
                }
            }
        }

        private void InvokeContinuation()
        {
            Debug.Assert(_continuation != s_completedSentinel, $"The continuation was the completion sentinel. State={(States)_state}.");

            if (_schedulingContext == null)
            {
                if (RunContinutationsAsynchronously)
                {
                    Task.Factory.StartNew(s =>
                    {
                        var vts = (ResettableValueTaskSource<T>)s;
                        vts._continuation(vts._continuationState);
                    }, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                    return;
                }
            }
            else if (_schedulingContext is SynchronizationContext sc)
            {
                if (RunContinutationsAsynchronously || sc != SynchronizationContext.Current)
                {
                    sc.Post(s =>
                    {
                        var vts = (ResettableValueTaskSource<T>)s;
                        vts._continuation(vts._continuationState);
                    }, this);
                    return;
                }
            }
            else
            {
                TaskScheduler ts = (TaskScheduler)_schedulingContext;
                if (RunContinutationsAsynchronously || ts != TaskScheduler.Current)
                {
                    Task.Factory.StartNew(s =>
                    {
                        var vts = (ResettableValueTaskSource<T>)s;
                        vts._continuation(vts._continuationState);
                    }, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                    return;
                }
            }

            _continuation(_continuationState);
        }
    }

    /// <summary>The representation of an asynchronous operation that has a result value.</summary>
    /// <typeparam name="TResult">Specifies the type of the result.  May be <see cref="VoidResult"/>.</typeparam>
    internal class AsyncOperation<TResult> : ResettableValueTaskSource<TResult>
    {
        /// <summary>Registration in <see cref="CancellationToken"/> that should be disposed of when the operation has completed.</summary>
        private CancellationTokenRegistration _registration;

        /// <summary>Initializes the interactor.</summary>
        /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
        public AsyncOperation(bool runContinuationsAsynchronously, CancellationToken cancellationToken = default)
        {
            RunContinutationsAsynchronously = runContinuationsAsynchronously;
            CancellationToken = cancellationToken;
            _registration = cancellationToken.Register(s =>
            {
                var thisRef = (AsyncOperation<TResult>)s;
                thisRef.TrySetCanceled(thisRef.CancellationToken);
            }, this);
        }

        /// <summary>Next operation in the linked list of operations.</summary>
        public AsyncOperation<TResult> Next { get; set; }
        public CancellationToken CancellationToken { get; }

        /// <summary>Completes the interactor with a success state and the specified result.</summary>
        /// <param name="item">The result value.</param>
        /// <returns>true if the interactor could be successfully transitioned to a completed state; false if it was already completed.</returns>
        public bool Success(TResult item)
        {
            UnregisterCancellation();
            return TrySetResult(item);
        }

        /// <summary>Completes the interactor with a failed state and the specified error.</summary>
        /// <param name="exception">The error.</param>
        /// <returns>true if the interactor could be successfully transitioned to a completed state; false if it was already completed.</returns>
        public bool Fail(Exception exception)
        {
            UnregisterCancellation();
            return TrySetException(exception);
        }

        public void UnregisterCancellation() => _registration.Dispose();
    }

    /// <summary>The representation of an asynchronous operation that has a result value and carries additional data with it.</summary>
    /// <typeparam name="TData">Specifies the type of data being written.</typeparam>
    internal sealed class VoidAsyncOperationWithData<TData> : AsyncOperation<VoidResult>
    {
        /// <summary>Initializes the interactor.</summary>
        /// <param name="runContinuationsAsynchronously">true if continuations should be forced to run asynchronously; otherwise, false.</param>
        /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
        public VoidAsyncOperationWithData(bool runContinuationsAsynchronously, CancellationToken cancellationToken = default) :
            base(runContinuationsAsynchronously, cancellationToken)
        {
        }

        /// <summary>The item being written.</summary>
        public TData Item { get; set; }
    }
}
