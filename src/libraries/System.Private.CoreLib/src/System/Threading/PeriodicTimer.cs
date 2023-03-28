// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Threading
{
    /// <summary>Provides a periodic timer that enables waiting asynchronously for timer ticks.</summary>
    /// <remarks>
    /// This timer is intended to be used only by a single consumer at a time: only one call to <see cref="WaitForNextTickAsync" />
    /// may be in flight at any given moment.  <see cref="Dispose"/> may be used concurrently with an active <see cref="WaitForNextTickAsync"/>
    /// to interrupt it and cause it to return false. Similarly, <see cref="Period"/> may be used concurrently with a consumer accessing
    /// <see cref="WaitForNextTickAsync"/> in order to change the timer's period.
    /// </remarks>
    public sealed class PeriodicTimer : IDisposable
    {
        /// <summary>The underlying timer.</summary>
        private readonly ITimer _timer;
        /// <summary>All state other than the _timer, so that the rooted timer's callback doesn't indirectly root itself by referring to _timer.</summary>
        private readonly State _state;
        /// <summary>The timer's current period.</summary>
        private TimeSpan _period;

        /// <summary>Initializes the timer.</summary>
        /// <param name="period">The period between ticks</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> must be <see cref="Timeout.InfiniteTimeSpan"/> or represent a number of milliseconds equal to or larger than 1 and smaller than <see cref="uint.MaxValue"/>.</exception>
        public PeriodicTimer(TimeSpan period)
        {
            if (!TryGetMilliseconds(period, out uint ms))
            {
                GC.SuppressFinalize(this);
                throw new ArgumentOutOfRangeException(nameof(period));
            }

            _period = period;
            _state = new State();

            _timer = new TimerQueueTimer(s => ((State)s!).Signal(), _state, ms, ms, flowExecutionContext: false);
        }

        /// <summary>Initializes the timer.</summary>
        /// <param name="period">The period between ticks</param>
        /// <param name="timeProvider">The <see cref="TimeProvider"/> used to interpret <paramref name="period"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> must be <see cref="Timeout.InfiniteTimeSpan"/> or represent a number of milliseconds equal to or larger than 1 and smaller than <see cref="uint.MaxValue"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is null</exception>
        public PeriodicTimer(TimeSpan period, TimeProvider timeProvider)
        {
            if (!TryGetMilliseconds(period, out uint ms))
            {
                GC.SuppressFinalize(this);
                throw new ArgumentOutOfRangeException(nameof(period));
            }

            if (timeProvider is null)
            {
                GC.SuppressFinalize(this);
                throw new ArgumentNullException(nameof(timeProvider));
            }

            _period = period;
            _state = new State();
            TimerCallback callback = s => ((State)s!).Signal();

            if (timeProvider == TimeProvider.System)
            {
                _timer = new TimerQueueTimer(callback, _state, ms, ms, flowExecutionContext: false);
            }
            else
            {
                using (ExecutionContext.SuppressFlow())
                {
                    _timer = timeProvider.CreateTimer(callback, _state, period, period);
                }
            }
        }

        /// <summary>Gets or sets the period between ticks.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> must be <see cref="Timeout.InfiniteTimeSpan"/> or represent a number of milliseconds equal to or larger than 1 and smaller than <see cref="uint.MaxValue"/>.</exception>
        /// <remarks>
        /// All prior ticks of the timer, including any that may be waiting to be consumed by <see cref="WaitForNextTickAsync"/>,
        /// are unaffected by changes to <see cref="Period"/>. Setting <see cref="Period"/> affects only and all subsequent times
        /// at which the timer will tick.
        /// </remarks>
        public TimeSpan Period
        {
            get => _period;
            set
            {
                if (!TryGetMilliseconds(value, out _))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _period = value;
                if (!_timer.Change(value, value))
                {
                    ThrowHelper.ThrowObjectDisposedException(this);
                }
            }
        }

        /// <summary>Tries to extract the number of milliseconds from <paramref name="value"/>.</summary>
        /// <returns>
        /// true if the number of milliseconds is extracted and stored into <paramref name="milliseconds"/>;
        /// false if the number of milliseconds would be out of range of a timer.
        /// </returns>
        private static bool TryGetMilliseconds(TimeSpan value, out uint milliseconds)
        {
            long ms = (long)value.TotalMilliseconds;
            if ((ms >= 1 && ms <= Timer.MaxSupportedTimeout) || value == Timeout.InfiniteTimeSpan)
            {
                milliseconds = (uint)ms;
                return true;
            }

            milliseconds = 0;
            return false;
        }

        /// <summary>Wait for the next tick of the timer, or for the timer to be stopped.</summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> to use to cancel the asynchronous wait. If cancellation is requested, it affects only the single wait operation;
        /// the underlying timer continues firing.
        /// </param>
        /// <returns>A task that will be completed due to the timer firing, <see cref="Dispose"/> being called to stop the timer, or cancellation being requested.</returns>
        /// <remarks>
        /// The <see cref="PeriodicTimer"/> behaves like an auto-reset event, in that multiple ticks are coalesced into a single tick if they occur between
        /// calls to <see cref="WaitForNextTickAsync"/>.  Similarly, a call to <see cref="Dispose"/> will void any tick not yet consumed. <see cref="WaitForNextTickAsync"/>
        /// may only be used by one consumer at a time, and may be used concurrently with a single call to <see cref="Dispose"/>.
        /// </remarks>
        public ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken = default) =>
            _state.WaitForNextTickAsync(this, cancellationToken);

        /// <summary>Stops the timer and releases associated managed resources.</summary>
        /// <remarks>
        /// <see cref="Dispose"/> will cause an active wait with <see cref="WaitForNextTickAsync"/> to complete with a value of false.
        /// All subsequent <see cref="WaitForNextTickAsync"/> invocations will produce a value of false.
        /// </remarks>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _timer.Dispose();
            _state.Signal(stopping: true);
        }

        /// <summary>Ensures that resources are freed and other cleanup operations are performed when the garbage collector reclaims the <see cref="PeriodicTimer" /> object.</summary>
        ~PeriodicTimer() => Dispose();

        /// <summary>Core implementation for the periodic timer.</summary>
        private sealed class State : IValueTaskSource<bool>
        {
            /// <summary>The associated <see cref="PeriodicTimer"/>.</summary>
            /// <remarks>
            /// This should refer to the parent instance only when there's an active waiter, and be null when there
            /// isn't. The TimerQueueTimer in the PeriodicTimer strongly roots itself, and it references this State
            /// object:
            ///     PeriodicTimer (finalizable) --ref--> TimerQueueTimer (rooted) --ref--> State --ref--> null
            /// If this State object then references the PeriodicTimer, it creates a strongly-rooted cycle that prevents anything from
            /// being GC'd:
            ///     PeriodicTimer (finalizable) --ref--> TimerQueueTimer (rooted) --ref--> State --v
            ///           ^--ref-------------------------------------------------------------------|
            /// When this field is null, the cycle is broken, and dropping all references to the PeriodicTimer allows the
            /// PeriodicTimer to be finalized and unroot the TimerQueueTimer. Thus, we keep this field set during<see cref="WaitForNextTickAsync"/>
            /// so that the timer roots any async continuation chain awaiting it, and then keep it unset otherwise so that everything
            /// can be GC'd appropriately.
            ///
            /// Note that if the period is set to infinite, even when there's an active waiter the PeriodicTimer won't
            /// be rooted because TimerQueueTimer won't be rooted via the static linked list.  That's fine, as the timer
            /// will never tick in such a case, and for the timer's period to be changed, the user's code would need
            /// some other reference to PeriodicTimer keeping it alive, anyway.
            /// </remarks>
            private PeriodicTimer? _owner;
            /// <summary>Core of the <see cref="IValueTaskSource{TResult}"/> implementation.</summary>
            private ManualResetValueTaskSourceCore<bool> _mrvtsc;
            /// <summary>Cancellation registration for any active <see cref="WaitForNextTickAsync"/> call.</summary>
            private CancellationTokenRegistration _ctr;
            /// <summary>Whether the timer has been stopped.</summary>
            private bool _stopped;
            /// <summary>Whether there's a pending notification to be received.  This could be due to the timer firing, the timer being stopped, or cancellation being requested.</summary>
            private bool _signaled;
            /// <summary>Whether there's a <see cref="WaitForNextTickAsync"/> call in flight.</summary>
            private bool _activeWait;

            /// <summary>Wait for the next tick of the timer, or for the timer to be stopped.</summary>
            public ValueTask<bool> WaitForNextTickAsync(PeriodicTimer owner, CancellationToken cancellationToken)
            {
                lock (this)
                {
                    if (_activeWait)
                    {
                        // WaitForNextTickAsync should only be used by one consumer at a time.  Failing to do so is an error.
                        ThrowHelper.ThrowInvalidOperationException();
                    }

                    // If cancellation has already been requested, short-circuit.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return ValueTask.FromCanceled<bool>(cancellationToken);
                    }

                    // If the timer has a pending tick or has been stopped, we can complete synchronously.
                    if (_signaled)
                    {
                        // Reset the signal for subsequent consumers, but only if we're not stopped. Since.
                        // stopping the timer is one way, any subsequent calls should also complete synchronously
                        // with false, and thus we leave _signaled pinned at true.
                        if (!_stopped)
                        {
                            _signaled = false;
                        }

                        return new ValueTask<bool>(!_stopped);
                    }

                    Debug.Assert(!_stopped, "Unexpectedly stopped without _signaled being true.");

                    // Set up for the wait and return a task that will be signaled when the
                    // timer fires, stop is called, or cancellation is requested.
                    _owner = owner;
                    _activeWait = true;
                    _ctr = cancellationToken.UnsafeRegister(static (state, cancellationToken) => ((State)state!).Signal(cancellationToken: cancellationToken), this);

                    return new ValueTask<bool>(this, _mrvtsc.Version);
                }
            }

            /// <summary>Signal that the timer has either fired or been stopped.</summary>
            public void Signal(bool stopping = false, CancellationToken cancellationToken = default)
            {
                bool completeTask = false;

                lock (this)
                {
                    _stopped |= stopping;
                    if (!_signaled)
                    {
                        _signaled = true;
                        completeTask = _activeWait;
                    }
                }

                if (completeTask)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        // If cancellation is requested just before the UnsafeRegister call, it's possible this will end up being invoked
                        // as part of the WaitForNextTickAsync call and thus as part of holding the lock.  The goal of completeTask
                        // was to escape that lock, so that we don't invoke any synchronous continuations from the ValueTask as part
                        // of completing _mrvtsc.  However, in that case, we also haven't returned the ValueTask to the caller, so there
                        // won't be any continuations yet, which makes this safe.
                        _mrvtsc.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(cancellationToken)));
                    }
                    else
                    {
                        Debug.Assert(!Monitor.IsEntered(this));
                        _mrvtsc.SetResult(true);
                    }
                }
            }

            /// <inheritdoc/>
            bool IValueTaskSource<bool>.GetResult(short token)
            {
                // Dispose of the cancellation registration.  This is done outside of the below lock in order
                // to avoid a potential deadlock due to waiting for a concurrent cancellation callback that might
                // in turn try to take the lock.  For valid usage, GetResult is only called once _ctr has been
                // successfully initialized before WaitForNextTickAsync returns to its synchronous caller, and
                // there should be no race conditions accessing it, as concurrent consumption is invalid. If there
                // is invalid usage, with GetResult used erroneously/concurrently, the worst that happens is cancellation
                // may not take effect for the in-flight operation, with its registration erroneously disposed.
                // Note we use Dispose rather than Unregister (which wouldn't risk deadlock) so that we know that thecancellation callback associated with this operation
                // won't potentially still fire after we've completed this GetResult and a new operation
                // has potentially started.
                _ctr.Dispose();

                lock (this)
                {
                    try
                    {
                        _mrvtsc.GetResult(token);
                    }
                    finally
                    {
                        _mrvtsc.Reset();
                        _ctr = default;
                        _activeWait = false;
                        _owner = null;
                        if (!_stopped)
                        {
                            _signaled = false;
                        }
                    }

                    return !_stopped;
                }
            }

            /// <inheritdoc/>
            ValueTaskSourceStatus IValueTaskSource<bool>.GetStatus(short token) => _mrvtsc.GetStatus(token);

            /// <inheritdoc/>
            void IValueTaskSource<bool>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
                _mrvtsc.OnCompleted(continuation, state, token, flags);
        }
    }
}
