// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http.LifetimeProcessing.Handlers.Entries.Base;

namespace Microsoft.Extensions.Http.LifetimeProcessing.Handlers.Entries
{
    // Thread-safety: This class is immutable
    internal sealed class ExpiredHandlerTrackingEntry : HandlerTrackingEntryBase
    {
        private readonly WeakReference _livenessTracker;

        // IMPORTANT: don't cache a reference to `other` or `other.Handler` here.
        // We need to allow it to be GC'ed.
        public ExpiredHandlerTrackingEntry(ActiveHandlerTrackingEntry other)
            : base(other.Name, other.Scope)
        {
            _livenessTracker = new WeakReference(other.Handler);
            InnerHandler = other.Handler.InnerHandler!;
        }

        public bool CanDispose => !_livenessTracker.IsAlive;

        public HttpMessageHandler InnerHandler { get; }
    }
}
