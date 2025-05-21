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
        private bool _disposed;

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

        public LifetimeTrackingHttpMessageHandler Handler { get; private set; }

        public TimeSpan Lifetime { get; }

        public string Name { get; }

        public IServiceScope? Scope { get; private set; }

        public void StartExpiryTimer(TimerCallback callback)
        {
            if (_disposed)
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
                if (Volatile.Read(ref _timerInitialized) || _disposed)
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
            Debug.Assert(_timer != null);

            lock (_lock)
            {
                if (_timer != null && !_disposed)
                {
                    _timer.Dispose();
                    _timer = null;

                    _callback(this);
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
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            StopTimer();

            // When this entry is converted to an expired entry, we don't dispose anything
            // to avoid a race with active clients. Let the ExpiredHandlerTrackingEntry handle disposal.
            
            if (Handler != null)
            {
                if (Handler.InnerHandler != null)
                {
                    Handler.InnerHandler.Dispose();
                }
                
                Handler = null!;
            }
            
            if (Scope != null)
            {
                Scope.Dispose();
                Scope = null;
            }
        }
    }
}
