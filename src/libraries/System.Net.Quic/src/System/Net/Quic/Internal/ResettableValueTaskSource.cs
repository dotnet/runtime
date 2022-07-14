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
    // None|Awaiting -> [TrySetResult|TrySetException(final: true)] -> Final(never leaves this state)
    private enum State
    {
        None,
        Awaiting,
        Ready,
        Completed
    }

    private State _state;
    private ManualResetValueTaskSourceCore<bool> _valueTaskSource;
    private CancellationTokenRegistration _cancellationRegistration;
    private Action<object?>? _cancellationAction;
    private GCHandle _keepAlive;

    private FinalTaskSource _finalTaskSource;

    public ResettableValueTaskSource(bool runContinuationsAsynchronously = true)
    {
        _state = State.None;
        _valueTaskSource = new ManualResetValueTaskSourceCore<bool>() { RunContinuationsAsynchronously = runContinuationsAsynchronously };
        _cancellationRegistration = default;
        _keepAlive = default;

        _finalTaskSource = new FinalTaskSource(runContinuationsAsynchronously);
    }

    /// <summary>
    /// Allows setting additional cancellation action to be called if token passed to <see cref="TryGetValueTask(out ValueTask, object?, CancellationToken)"/> fires off.
    /// The argument for the action is the <c>keepAlive</c> object from the same <see cref="TryGetValueTask(out ValueTask, object?, CancellationToken)"/> call.
    /// </summary>
    public Action<object?> CancellationAction { init { _cancellationAction = value; } }

    /// <summary>
    /// Returns <c>true</c> is this task source has entered its final state, i.e. <see cref="TryComplete(Exception?, bool)"/> or <see cref="TrySetException(Exception, bool)"/>
    /// was called with <c>final</c> set to <c>true</c> and the result was propagated.
    /// </summary>
    public bool IsCompleted => (State)Volatile.Read(ref Unsafe.As<State, byte>(ref _state)) == State.Completed;

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
                        (ResettableValueTaskSource parent, object? target) = ((ResettableValueTaskSource, object?))obj!;
                        if (parent.TrySetException(new OperationCanceledException(cancellationToken)))
                        {
                            parent._cancellationAction?.Invoke(target);
                        }
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
            // None, Completed, Final: return the current task.
            if (state == State.None ||
                state == State.Ready ||
                state == State.Completed)
            {
                valueTask = new ValueTask(this, _valueTaskSource.Version);
                return true;
            }

            // Awaiting: forbidden concurrent call.
            valueTask = default;
            return false;
        }
    }

    public Task GetFinalTask() => _finalTaskSource.Task;

    private bool TryComplete(Exception? exception, bool final)
    {
        CancellationTokenRegistration cancellationRegistration = default;
        try
        {
            lock (this)
            {
                try
                {
                    State state = _state;

                    // None,Awaiting: clean up and finish the task source.
                    if (state == State.Awaiting ||
                        state == State.None)
                    {
                        _state = final ? State.Completed : State.Ready;

                        // Swap the cancellation registration so the one that's been registered gets eventually Disposed.
                        // Ideally, we would dispose it here, but if the callbacks kicks in, it tries to take the lock held by this thread leading to deadlock.
                        cancellationRegistration = _cancellationRegistration;
                        _cancellationRegistration = default;

                        // Unblock the current task source and in case of a final also the final task source.
                        if (exception is not null)
                        {
                            // Set up the exception stack strace for the caller.
                            exception = exception.StackTrace is null ? ExceptionDispatchInfo.SetCurrentStackTrace(exception) : exception;
                            _valueTaskSource.SetException(exception);
                        }
                        else
                        {
                            _valueTaskSource.SetResult(final);
                        }

                        if (final)
                        {
                            _finalTaskSource.TryComplete(exception);
                            _finalTaskSource.TrySignal(out _);
                        }

                        return true;
                    }

                    // Final: remember the first final result to set it once the current non-final result gets retrieved.
                    if (final)
                    {
                        return _finalTaskSource.TryComplete(exception);
                    }

                    return false;
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
        finally
        {
            // Dispose the cancellation if registered.
            // Must be done outside of lock since Dispose will wait on pending cancellation callbacks which require taking the lock.
            cancellationRegistration.Dispose();
        }
    }

    public bool TrySetResult(bool final = false)
    {
        return TryComplete(null, final);
    }

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
            _valueTaskSource.GetResult(token);
        }
        finally
        {
            lock (this)
            {
                State state = _state;

                if (state == State.Ready)
                {
                    _valueTaskSource.Reset();
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

    private struct FinalTaskSource
    {
        private TaskCompletionSource _finalTaskSource;
        private bool _isCompleted;
        private Exception? _exception;

        public FinalTaskSource(bool runContinuationsAsynchronously = true)
        {
            // TODO: defer instantiation only after Task is retrieved
            _finalTaskSource = new TaskCompletionSource(runContinuationsAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);
            _isCompleted = false;
            _exception = null;
        }

        public Task Task => _finalTaskSource.Task;

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
                _finalTaskSource.SetException(_exception);
            }
            else
            {
                _finalTaskSource.SetResult();
            }

            exception = _exception;
            return true;
        }
    }
}
