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
    internal sealed class ActiveHandlerTrackingEntry
    {
        // The timer only holds a weak reference to the entry. While the factory is alive, it keeps the entry
        // alive through its collection of active handlers. Once the factory is unreachable, the timer must not
        // keep the entry (and through it the handler pipeline, its scope, and the root service provider) alive.
        private static readonly TimerCallback _timerCallback = (s) =>
        {
            if (((WeakReference<ActiveHandlerTrackingEntry>)s!).TryGetTarget(out ActiveHandlerTrackingEntry? entry))
            {
                entry.Timer_Tick();
            }
        };
        private readonly object _lock;
        private bool _timerInitialized;
        private Timer? _timer;
        private TimerCallback? _callback;

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
                if (Volatile.Read(ref _timerInitialized))
                {
                    return;
                }

                _callback = callback;
                _timer = NonCapturingTimer.Create(_timerCallback, new WeakReference<ActiveHandlerTrackingEntry>(this), Lifetime, Timeout.InfiniteTimeSpan);
                _timerInitialized = true;
            }
        }

        private void Timer_Tick()
        {
            Debug.Assert(_callback != null);
            Debug.Assert(_timer != null);

            lock (_lock)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;

                    _callback(this);
                }
            }
        }
    }
}
