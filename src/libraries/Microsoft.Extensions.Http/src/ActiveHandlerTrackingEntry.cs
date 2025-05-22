// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Http
{
    // Thread-safety: We treat this class as immutable except for the timer. Creating a new object
    // for the 'expiry' pool simplifies the threading requirements significantly.
    internal sealed class ActiveHandlerTrackingEntry : IDisposable
    {
        private static readonly TimerCallback _timerCallback = (s) => ((ActiveHandlerTrackingEntry)s!).Timer_Tick();
        private readonly object _lock;
        private bool _timerInitialized;
        private Timer? _timer;
        private TimerCallback? _callback;
        // States for the handler tracking entry
        private const int NotDisposed = 0;
        private const int Disposed = 1;
        private const int Expired = 2;
        private int _disposed;

        public ActiveHandlerTrackingEntry(
            string name,
            LifetimeTrackingHttpMessageHandler handler,
            IServiceScope? scope,
            TimeSpan lifetime)
        {
            Name = name;
            Handler = handler;
            Scope = scope;
            Lifetime = lifetime;

            _lock = new object();
        }

        public LifetimeTrackingHttpMessageHandler Handler { get; }

        public TimeSpan Lifetime { get; }

        public string Name { get; }

        public IServiceScope? Scope { get; }

        public void StartExpiryTimer(TimerCallback callback)
        {
            if (Interlocked.CompareExchange(ref _disposed, _disposed, _disposed) != NotDisposed)
            {
                return;
            }

            if (Lifetime == Timeout.InfiniteTimeSpan)
            {
                return; // never expires.
            }

            if (Volatile.Read(ref _timerInitialized))
            {
                return;
            }

            StartExpiryTimerSlow(callback);
        }

        private void StartExpiryTimerSlow(TimerCallback callback)
        {
            Debug.Assert(Lifetime != Timeout.InfiniteTimeSpan);

            lock (_lock)
            {
                if (Volatile.Read(ref _timerInitialized) ||
                    Interlocked.CompareExchange(ref _disposed, _disposed, _disposed) != NotDisposed)
                {
                    return;
                }

                _callback = callback;
                _timer = NonCapturingTimer.Create(_timerCallback, this, Lifetime, Timeout.InfiniteTimeSpan);
                _timerInitialized = true;
            }
        }

        private void Timer_Tick()
        {
            Debug.Assert(_callback != null);
            Debug.Assert(_timer != null || _disposed != NotDisposed);

            lock (_lock)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;

                    // Only invoke the callback if we successfully transition from NotDisposed to Expired
                    // This ensures we don't convert to expired if already disposed
                    if (Interlocked.CompareExchange(ref _disposed, Expired, NotDisposed) == NotDisposed)
                    {
                        _callback(this);
                    }
                }
            }
        }

        public void StopTimer()
        {
            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
            }
        }

        public void Dispose()
        {
            // Try to transition from NotDisposed to Disposed state
            // If already in another state (Expired), do nothing further with handlers
            int oldState = Interlocked.CompareExchange(ref _disposed, Disposed, NotDisposed);
            if (oldState != NotDisposed)
            {
                // If the entry was already disposed or expired, exit
                // If it was expired, the timer has already stopped and
                // ExpiredHandlerTrackingEntry now owns both handler and scope
                return;
            }

            StopTimer();

            // When this entry is converted to an expired entry, we don't dispose the handlers
            // to avoid a race with active clients. Let the ExpiredHandlerTrackingEntry handle handler disposal.
            // We only dispose the scope if it exists.
            Scope?.Dispose();
        }
    }
}
