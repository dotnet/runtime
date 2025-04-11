// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.LifetimeProcessing.Handlers.Entries.Base;
using Microsoft.Extensions.Internal;
using Timer = System.Timers.Timer;

namespace Microsoft.Extensions.Http.LifetimeProcessing.Handlers.Entries
{
    // Thread-safety: We treat this class as immutable except for the timer. Creating a new object
    // for the 'expiry' pool simplifies the threading requirements significantly.
    internal sealed class ActiveHandlerTrackingEntry : HandlerTrackingEntryBase, IDisposable
    {
        private readonly object _lock;
        private bool _isDisposed;
        private Timer? _timer;
        private Action<ActiveHandlerTrackingEntry>? _callback;

        public ActiveHandlerTrackingEntry(
            string name,
            LifetimeTrackingHttpMessageHandler handler,
            IServiceScope? scope,
            TimeSpan lifetime)
            : base(name, scope)
        {
            Handler = handler;
            Lifetime = lifetime;

            _lock = new object();
            _timer = new Timer(lifetime.TotalMilliseconds)
            {
                AutoReset = false
            };
            _timer.Elapsed += (_, _) => Timer_Tick();
        }

        public LifetimeTrackingHttpMessageHandler Handler { get; }

        public TimeSpan Lifetime { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void StartExpiryTimer(Action<ActiveHandlerTrackingEntry> callback)
        {
            if (Lifetime == Timeout.InfiniteTimeSpan)
            {
                return; // never expires.
            }

            _callback = callback;
            _timer!.Start();
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (disposing)
                {
                    _timer?.Dispose();
                    _timer = null;
                }
            }
        }

        private void Timer_Tick()
        {
            Debug.Assert(_callback != null);
            Debug.Assert(_timer != null);

            _callback(this);
        }
    }
}
