// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http
{
    // Thread-safety: This class is immutable
    internal class ExpiredHandlerTrackingEntry
    {
        private readonly WeakReference _livenessTracker;

        // IMPORTANT: don't cache a reference to `other` or `other.Handler` here.
        // We need to allow it to be GC'ed.
        public ExpiredHandlerTrackingEntry(ActiveHandlerTrackingEntry other)
            : this(other.Name, other.Handler, other.Scope)
        {
        }

        // IMPORTANT: don't cache a reference to `handler` here.
        // We need to allow it to be GC'ed.
        internal ExpiredHandlerTrackingEntry(string name, LifetimeTrackingHttpMessageHandler handler, IServiceScope scope)
        {
            Name = name;
            Scope = scope;

            _livenessTracker = new WeakReference(handler);
            InnerHandler = handler.InnerHandler;
        }

        public bool CanDispose => !_livenessTracker.IsAlive;

        public HttpMessageHandler InnerHandler { get; }

        public string Name { get; }

        public IServiceScope Scope { get; }
    }
}
