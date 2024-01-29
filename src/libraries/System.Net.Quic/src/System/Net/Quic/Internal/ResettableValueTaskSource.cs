// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Quic;

internal sealed class ResettableValueTaskSource : IValueTaskSource
{
    // None -> [TryGetValueTask] -> Awaiting -> [TrySetResult|TrySetException(final: false)] -> Ready -> [GetResult] -> None
    // None -> [TrySetResult|TrySetException(final: false)] -> Ready -> [TryGetValueTask] -> [GetResult] -> None
    // None|Awaiting -> [TrySetResult|TrySetException(final: true)] -> Completed(never leaves this state)
    // Ready -> [GetResult: TrySet*(final: true) was called] -> Completed(never leaves this state)
    private enum State
    {
        None,
        Awaiting,
        Ready,
        Completed
    }

    private State _state;
    private bool _hasWaiter;
    private ManualResetValueTaskSourceCore<bool> _valueTaskSource;
    private CancellationTokenRegistration _cancellationRegistration;
    private CancellationToken _cancelledToken;
    private Action<object?>? _cancellationAction;
    private GCHandle _keepAlive;
    private FinalTaskSource _finalTaskSource;

    public ResettableValueTaskSource()
    {
        _state = State.None;
        _hasWaiter = false;
        _valueTaskSource = new ManualResetValueTaskSourceCore<bool>() { RunContinuationsAsynchronously = true };
        _cancellationRegistration = default;
        _cancelledToken = default;
        _keepAlive = default;
        _finalTaskSource = new FinalTaskSource();
    }

    /// <summary>
    /// Allows setting additional cancellation action to be called if token passed to <see cref="TryGetValueTask(out ValueTask, object?, CancellationToken)"/> fires off.
    /// The argument for the action is the <c>keepAlive</c> object from the same <see cref="TryGetValueTask(out ValueTask, object?, CancellationToken)"/> call.
    /// </summary>
    public Action<object?> CancellationAction { init { _cancellationAction = value; } }

    /// <summary>
    /// Returns <c>true</c> is this task source has entered its final state, i.e. <see cref="TrySetResult(bool)"/> or <see cref="TrySetException(Exception, bool)"/>
    /// was called with <c>final</c> set to <c>true</c> and the result was propagated.
    /// </summary>
    public bool IsCompleted => (State)Volatile.Read(ref Unsafe.As<State, byte>(ref _state)) == State.Completed;

    /// <summary>
    /// Tries to get a value task representing this task source. If this task source is <see cref="State.None"/>, it'll also transition it into <see cref="State.Awaiting"/> state.
    /// It prevents concurrent operations from being invoked since it'll return <c>false</c> if the task source was already in <see cref="State.Awaiting"/> state.
    /// In other states, it'll return a value task representing this task source without any other work. So to determine whether to invoke a P/Invoke operation or not,
    /// the state of <paramref name="valueTask"/> must also be checked.
    /// </summary>
    /// <param name="valueTask">A value task representing the result. Only meaningful in case this method returns <c>true</c>. Might already be completed.</param>
    /// <param name="keepAlive">An object to hold during a P/Invoke call. It'll get release with setting the result/exception.</param>
    /// <param name="cancellationToken">A cancellation token which might cancel the value task.</param>
    /// <returns><c>true</c> if this is not an overlapping call (task source transitioned or was already set); otherwise, <c>false</c>.</returns>
    public bool TryGetValueTask(out ValueTask valueTask, object? keepAlive = null, CancellationToken cancellationToken = default)
    {
        lock (this)
        {
            // Cancellation might kick off synchronously, re-entering the lock and changing the state to completed.
            if (_state == State.None)
            {
                // Register cancellation if the token can be cancelled and the task is not completed yet.
                if (cancellationToken.CanBeCanceled)
                {
                    _cancellationRegistration = cancellationToken.UnsafeRegister(static (obj, cancellationToken) =>
                    {
                        (ResettableValueTaskSource thisRef, object? target) = ((ResettableValueTaskSource, object?))obj!;
                        lock (thisRef)
                        {
                            thisRef._cancelledToken = cancellationToken;
                        }
                        thisRef._cancellationAction?.Invoke(target);
                    }, (this, keepAlive));
                }
            }

            State state = _state;

            // None: prepare for the actual operation happening and transition to Awaiting.
            if (state == State.None)
            {
                // Keep alive the caller object until the result is read from the task.
                // Used for keeping caller alive during async interop calls.
                if (keepAlive is not null)
                {
                    Debug.Assert(!_keepAlive.IsAllocated);
                    _keepAlive = GCHandle.Alloc(keepAlive);
                }

                _state = State.Awaiting;
            }
            // None, Ready, Completed: return the current task.
            if (state is State.None or State.Ready or State.Completed)
            {
                // Remember that the value task with the current version is being given out.
                _hasWaiter = true;
                valueTask = new ValueTask(this, _valueTaskSource.Version);
                return true;
            }

            // Awaiting: forbidden concurrent call.
            valueTask = default;
            return false;
        }
    }

    /// <summary>
    /// Gets a <see cref="Task"/> that will transition to a completed state with the last transition of this source, i.e. into <see cref="State.Completed"/>.
    /// </summary>
    /// <returns>The <see cref="Task"/> that will transition to a completed state with the last transition of this source.</returns>
    public Task GetFinalTask(object? keepAlive)
    {
        lock (this)
        {
            return _finalTaskSource.GetTask(keepAlive);
        }
    }

    private bool TryComplete(Exception? exception, bool final)
    {
        // Dispose the cancellation registration before completing the task, so that it cannot run after the awaiting method returned.
        // Dispose must be done outside of lock since it will wait on pending cancellation callbacks that can hold the lock from another thread.
        CancellationTokenRegistration cancellationRegistration = default;
        lock (this)
        {
            cancellationRegistration = _cancellationRegistration;
            _cancellationRegistration = default;
        }
        cancellationRegistration.Dispose();

        lock (this)
        {
            try
            {
                State state = _state;

                // Completed: nothing to do.
                if (state == State.Completed)
                {
                    return false;
                }

                // The task was non-finally completed without having anyone awaiting on it.
                // In such case, discard the temporary result and replace it with this final completion.
                if (state == State.Ready && !_hasWaiter && final)
                {
                    _valueTaskSource.Reset();
                    state = State.None;
                }

                // If the _valueTaskSource has already been set, we don't want to lose the result by overwriting it.
                // So keep it as is and store the result in _finalTaskSource.
                if (state is State.None or State.Awaiting)
                {
                    _state = final ? State.Completed : State.Ready;
                }

                // Unblock the current task source and in case of a final also the final task source.
                if (exception is not null)
                {
                    // Set up the exception stack trace for the caller.
                    exception = exception.StackTrace is null ? ExceptionDispatchInfo.SetCurrentStackTrace(exception) : exception;
                    if (state is State.None or State.Awaiting)
                    {
                        _valueTaskSource.SetException(exception);
                    }
                }
                else
                {
                    if (state is State.None or State.Awaiting)
                    {
                        _valueTaskSource.SetResult(final);
                    }
                }
                if (final)
                {
                    if (_finalTaskSource.TryComplete(exception))
                    {
                        // Signal the final task only if we don't have another result in the value task source.
                        // In that case, the final task will be signalled after the value task result is retrieved.
                        if (state != State.Ready)
                        {
                            _finalTaskSource.TrySignal(out _);
                        }
                        return true;
                    }
                    return false;
                }
                return state != State.Ready;
            }
            finally
            {
                // Un-root the the kept alive object in all cases.
                if (_keepAlive.IsAllocated)
                {
                    _keepAlive.Free();
                }
            }
        }
    }

    /// <summary>
    /// Tries to transition from <see cref="State.Awaiting"/> to either <see cref="State.Ready"/> or <see cref="State.Completed"/>, depending on the value of <paramref name="final"/>.
    /// Only the first call (with either value for <paramref name="final"/>) is able to do that. I.e.: <c>TrySetResult()</c> followed by <c>TrySetResult(true)</c> will both return <c>true</c>.
    /// </summary>
    /// <param name="final">Whether this is the final transition to <see cref="State.Completed" /> or just a transition into <see cref="State.Ready"/> from which the task source can be reset back to <see cref="State.None"/>.</param>
    /// <returns><c>true</c> if this is the first call that set the result; otherwise, <c>false</c>.</returns>
    public bool TrySetResult(bool final = false)
    {
        return TryComplete(null, final);
    }

    /// <summary>
    /// Tries to transition from <see cref="State.Awaiting"/> to either <see cref="State.Ready"/> or <see cref="State.Completed"/>, depending on the value of <paramref name="final"/>.
    /// Only the first call is able to do that with the exception of <c>TrySetResult()</c> followed by <c>TrySetResult(true)</c>, which will both return <c>true</c>.
    /// </summary>
    /// <param name="final">Whether this is the final transition to <see cref="State.Completed" /> or just a transition into <see cref="State.Ready"/> from which the task source can be reset back to <see cref="State.None"/>.</param>
    /// <param name="exception">The exception to set as a result of the value task.</param>
    /// <returns><c>true</c> if this is the first call that set the result; otherwise, <c>false</c>.</returns>
    public bool TrySetException(Exception exception, bool final = false)
    {
        return TryComplete(exception, final);
    }

    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
        => _valueTaskSource.GetStatus(token);

    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => _valueTaskSource.OnCompleted(continuation, state, token, flags);

    void IValueTaskSource.GetResult(short token)
    {
        try
        {
            _cancelledToken.ThrowIfCancellationRequested();
            _valueTaskSource.GetResult(token);
        }
        finally
        {
            lock (this)
            {
                State state = _state;

                _hasWaiter = false;
                _cancelledToken = default;

                if (state == State.Ready)
                {
                    _valueTaskSource.Reset();
                    _state = State.None;

                    // Propagate the _finalTaskSource result into _valueTaskSource if completed.
                    if (_finalTaskSource.TrySignal(out Exception? exception))
                    {
                        _state = State.Completed;

                        if (exception is not null)
                        {
                            _valueTaskSource.SetException(exception);
                        }
                        else
                        {
                            _valueTaskSource.SetResult(true);
                        }
                    }
                    else
                    {
                        _state = State.None;
                    }
                }
            }
        }
    }

    /// <summary>
    /// It remembers the result from <see cref="TryComplete"/> and propagates it to <see cref="_finalTaskSource"/> only after <see cref="TrySignal"/> is called.
    /// Effectively allowing to separate setting of the result from task completion, which is necessary when the resettable portion of the value task source needs to consumed first.
    /// </summary>
    private struct FinalTaskSource
    {
        private TaskCompletionSource? _finalTaskSource;
        private bool _isCompleted;
        private bool _isSignaled;
        private Exception? _exception;

        public FinalTaskSource()
        {
            _finalTaskSource = null;
            _isCompleted = false;
            _isSignaled = false;
            _exception = null;
        }

        public Task GetTask(object? keepAlive)
        {
            if (_finalTaskSource is null)
            {
                _finalTaskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                if (!_isCompleted)
                {
                    GCHandle handle = GCHandle.Alloc(keepAlive);
                    _finalTaskSource.Task.ContinueWith(static (_, state) =>
                    {
                        ((GCHandle)state!).Free();
                    }, handle, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
                if (_isSignaled)
                {
                    TrySignal(out _);
                }
            }
            return _finalTaskSource.Task;
        }

        public bool TryComplete(Exception? exception = null)
        {
            if (_isCompleted)
            {
                return false;
            }

            _exception = exception;
            _isCompleted = true;
            return true;
        }

        public bool TrySignal(out Exception? exception)
        {
            if (!_isCompleted)
            {
                exception = default;
                return false;
            }

            if (_exception is not null)
            {
                _finalTaskSource?.SetException(_exception);
            }
            else
            {
                _finalTaskSource?.SetResult();
            }

            exception = _exception;
            _isSignaled = true;
            return true;
        }
    }
}
