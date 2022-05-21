// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
    private GCHandle _keepAlive;

    private FinalValueTaskSource _finalTaskSource;

    public ResettableValueTaskSource(bool runContinuationsAsynchronously = true)
    {
        _state = State.None;
        _valueTaskSource = new ManualResetValueTaskSourceCore<bool>() { RunContinuationsAsynchronously = runContinuationsAsynchronously };
        _cancellationRegistration = default;
        _keepAlive = default;

        _finalTaskSource = new FinalValueTaskSource(runContinuationsAsynchronously);

        // Reset the value task source so that its version doesn't overlap with the final task source.
        if (_valueTaskSource.Version == _finalTaskSource.Version)
        {
            _valueTaskSource.Reset();
        }
    }

    public bool IsCompleted
    {
        get
        {
            lock (this)
            {
                return _state == State.Completed;
            }
        }
    }

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
                        var parent = (ResettableValueTaskSource)obj!;
                        parent.TrySetException(new OperationCanceledException(cancellationToken));
                    }, this);
                }
            }

            var state = _state;

            // None: prepare for the actual operation happening and transition to Awaiting.
            if (state == State.None)
            {
                // Root the object to keep alive if requested.
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

    private bool TryComplete(Exception? exception, bool final)
    {
        // Dispose the cancellation if registered.
        // Must be done outside of lock since Dispose will wait on pending cancellation callbacks which requires taking the lock.
        _cancellationRegistration.Dispose();
        _cancellationRegistration = default;

        lock (this)
        {
            try
            {
                var state = _state;

                // None,Awaiting: clean up and finish the task source.
                if (state == State.Awaiting ||
                    state == State.None)
                {
                    _state = final ? State.Completed : State.Ready;

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

    public bool TrySetResult(bool final = false)
    {
        return TryComplete(null, final);
    }

    public bool TrySetException(Exception exception, bool final = false)
    {
        return TryComplete(exception, final);
    }

    public ValueTask GetFinalTask()
        => new ValueTask(this, _finalTaskSource.Version);

    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
    {
        if (token == _finalTaskSource.Version)
        {
            return _finalTaskSource.GetStatus(token);
        }
        else
        {
            return _valueTaskSource.GetStatus(token);
        }
    }
    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        if (token == _finalTaskSource.Version)
        {
            _finalTaskSource.OnCompleted(continuation, state, token, flags);

        }
        else
        {
            _valueTaskSource.OnCompleted(continuation, state, token, flags);
        }
    }
    void IValueTaskSource.GetResult(short token)
    {
        if (token == _finalTaskSource.Version)
        {
            _finalTaskSource.GetResult(token);
        }
        else
        {
            try
            {
                _valueTaskSource.GetResult(token);
            }
            finally
            {
                lock (this)
                {
                    var state = _state;

                    if (state == State.Ready)
                    {
                        // Reset the value task source so that its version doesn't overlap with the final task source.
                        _valueTaskSource.Reset();
                        if (_valueTaskSource.Version == _finalTaskSource.Version)
                        {
                            _valueTaskSource.Reset();
                        }
                        if (_finalTaskSource.TrySignal(out var exception))
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
    }

    private struct FinalValueTaskSource
    {
        private ManualResetValueTaskSourceCore<bool> _valueTaskSource;
        private bool _isCompleted;
        private Exception? _exception;

        public FinalValueTaskSource(bool runContinuationsAsynchronously = true)
        {
            _valueTaskSource = new ManualResetValueTaskSourceCore<bool>() { RunContinuationsAsynchronously = runContinuationsAsynchronously };
            _isCompleted = false;
            _exception = null;
        }

        public short Version => _valueTaskSource.Version;

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
                _valueTaskSource.SetException(_exception);
            }
            else
            {
                _valueTaskSource.SetResult(true);
            }

            exception = _exception;
            return true;
        }

        public ValueTaskSourceStatus GetStatus(short token)
            => _valueTaskSource.GetStatus(_valueTaskSource.Version);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _valueTaskSource.OnCompleted(continuation, state, token, flags);

        public void GetResult(short token)
            => _valueTaskSource.GetResult(_valueTaskSource.Version);
    }
}
