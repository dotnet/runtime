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

internal sealed class ValueTaskSource : IValueTaskSource
{
    // None -> [TryInitialize] -> Awaiting -> [TrySetResult|TrySetException] -> Completed
    // None -> [TrySetResult|TrySetException] -> Completed
    private enum State : byte
    {
        None,
        Awaiting,
        Completed
    }

    private State _state;
    private ManualResetValueTaskSourceCore<bool> _valueTaskSource;
    private CancellationTokenRegistration _cancellationRegistration;
    private GCHandle _keepAlive;

    public ValueTaskSource()
    {
        _state = State.None;
        _valueTaskSource = new ManualResetValueTaskSourceCore<bool>() { RunContinuationsAsynchronously = true };
        _cancellationRegistration = default;
        _keepAlive = default;
    }

    /// <summary>
    /// Returns <c>true</c> is this task source was completed, i.e. <see cref="TrySetResult"/> or <see cref="TrySetException(Exception)"/> was called.
    /// </summary>
    public bool IsCompleted => (State)Volatile.Read(ref Unsafe.As<State, byte>(ref _state)) == State.Completed;
    /// <summary>
    /// Returns <c>true</c> is this task source was completed successfully, i.e. <see cref="TrySetResult"/> was called and set the result.
    /// </summary>
    public bool IsCompletedSuccessfully => IsCompleted && _valueTaskSource.GetStatus(_valueTaskSource.Version) == ValueTaskSourceStatus.Succeeded;

    /// <summary>
    /// Tries to transition from <see cref="State.None"/> to <see cref="State.Awaiting"/>. Only the first call is able to do that so the result can be used to determine whether to invoke an operation or not.
    /// </summary>
    /// <param name="valueTask">A value task representing the result. In case this method returns <c>false</c>, it'll still contain a value task that'll get set with the original operation.</param>
    /// <param name="keepAlive">An object to hold during a P/Invoke call. It'll get release with setting the result/exception.</param>
    /// <param name="cancellationToken">A cancellation token which might cancel the value task.</param>
    /// <returns><c>true</c> if this is the first call; otherwise, <c>false</c>.</returns>
    public bool TryInitialize(out ValueTask valueTask, object? keepAlive = null, CancellationToken cancellationToken = default)
    {
        lock (this)
        {
            // Set up value task either way, so the the caller can get the result even if they do not start the operation.
            valueTask = new ValueTask(this, _valueTaskSource.Version);

            // Cancellation might kick off synchronously, re-entering the lock and changing the state to completed.
            if (_state == State.None)
            {
                // Register cancellation if the token can be cancelled and the task is not completed yet.
                if (cancellationToken.CanBeCanceled)
                {
                    _cancellationRegistration = cancellationToken.UnsafeRegister(static (obj, cancellationToken) =>
                    {
                        ValueTaskSource thisRef = (ValueTaskSource)obj!;
                        thisRef.TrySetException(new OperationCanceledException(cancellationToken));
                    }, this);
                }
            }

            State state = _state;

            // If we're the first here, we will return true.
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
                return true;
            }

            return false;
        }
    }

    private bool TryComplete(Exception? exception)
    {
        CancellationTokenRegistration cancellationRegistration = default;
        try
        {
            lock (this)
            {
                try
                {
                    State state = _state;

                    if (state != State.Completed)
                    {
                        _state = State.Completed;

                        // Swap the cancellation registration so the one that's been registered gets eventually Disposed.
                        // Ideally, we would dispose it here, but if the callbacks kicks in, it tries to take the lock held by this thread leading to deadlock.
                        cancellationRegistration = _cancellationRegistration;
                        _cancellationRegistration = default;

                        if (exception is not null)
                        {
                            // Set up the exception stack trace for the caller.
                            exception = exception.StackTrace is null ? ExceptionDispatchInfo.SetCurrentStackTrace(exception) : exception;
                            _valueTaskSource.SetException(exception);
                        }
                        else
                        {
                            _valueTaskSource.SetResult(true);
                        }

                        return true;
                    }

                    return false;
                }
                finally
                {
                    // Un-root the kept alive object in all cases.
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

    /// <summary>
    /// Tries to transition from <see cref="State.Awaiting"/> to <see cref="State.Completed"/>. Only the first call is able to do that.
    /// </summary>
    /// <returns><c>true</c> if this is the first call that set the result; otherwise, <c>false</c>.</returns>
    public bool TrySetResult()
    {
        return TryComplete(null);
    }

    /// <summary>
    /// Tries to transition from <see cref="State.Awaiting"/> to <see cref="State.Completed"/>. Only the first call is able to do that.
    /// </summary>
    /// <param name="exception">The exception to set as a result of the value task.</param>
    /// <returns><c>true</c> if this is the first call that set the result; otherwise, <c>false</c>.</returns>
    public bool TrySetException(Exception exception)
    {
        return TryComplete(exception);
    }

    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
        => _valueTaskSource.GetStatus(token);

    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => _valueTaskSource.OnCompleted(continuation, state, token, flags);

    void IValueTaskSource.GetResult(short token)
        => _valueTaskSource.GetResult(token);
}
