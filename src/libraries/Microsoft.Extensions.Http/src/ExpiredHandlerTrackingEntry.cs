// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http
{
    // Thread-safety: This class is immutable
    internal sealed class ExpiredHandlerTrackingEntry : IDisposable
    {
        private readonly WeakReference _livenessTracker;

        // IMPORTANT: don't cache a reference to `other` or `other.Handler` here.
        // We need to allow it to be GC'ed.
        public ExpiredHandlerTrackingEntry(ActiveHandlerTrackingEntry other)
        {
            Name = other.Name;
            Scope = other.Scope;

            _livenessTracker = new WeakReference(other.Handler);
            InnerHandler = other.Handler.InnerHandler!;
        }

        // Used during normal cleanup cycles
        public bool CanDispose => !_livenessTracker.IsAlive;

        public HttpMessageHandler InnerHandler { get; }

        public string Name { get; }

        public IServiceScope? Scope { get; }

        public void Dispose()
        {
            try
            {
                InnerHandler.Dispose();
            }
            finally
            {
                if (!_livenessTracker.IsAlive)
                {
                    Scope?.Dispose();
                }
                // If IsAlive is true, it means the handler is still in use
                // Don't dispose the scope as it's still being used with the handler
            }
        }
    }
}
