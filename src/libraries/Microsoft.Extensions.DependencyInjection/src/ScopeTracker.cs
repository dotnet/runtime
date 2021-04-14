// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class ScopeTracker
    {
        // We keep track of the number of the maximum scoped services resolved
        private volatile int _maxResolvedServices;
        private volatile int _maxDisposableServices;

        // Below default ~85K LOH threshold, avoid large temporary garbage
        private const int MaxCapacity = 2000;

        public State Allocate() => new State(this, _maxResolvedServices, _maxDisposableServices);

        private bool Track(State state)
        {
            // Clamp the size between the min and max
            _maxResolvedServices = Math.Min(MaxCapacity, Math.Max(state.ResolvedServicesSize, _maxResolvedServices));
            _maxDisposableServices = Math.Min(MaxCapacity, Math.Max(state.Disposables.Count, _maxDisposableServices));

            return false;
        }

        public class State
        {
            private readonly ScopeTracker _tracker;

            public IDictionary<ServiceCacheKey, object> ResolvedServices { get; }
            public List<object> Disposables { get; set; }

            public int ResolvedServicesSize => ((Dictionary<ServiceCacheKey, object>)ResolvedServices).Count;

            public State(ScopeTracker tracker = null, int initialCapacity = 0, int initialDisposableCapacity = 0)
            {
                _tracker = tracker;

                // When tracker is null, we're not tracking the number of resolved services. Also
                // to reduce lock contention for singletons upon resolve we use a concurrent dictionary.

                ResolvedServices = tracker == null ? new ConcurrentDictionary<ServiceCacheKey, object>() : new Dictionary<ServiceCacheKey, object>(initialCapacity);

                if (initialDisposableCapacity > 0)
                {
                    Disposables ??= new List<object>(initialDisposableCapacity);
                }
            }

            public void Track()
            {
                _tracker?.Track(this);
            }
        }
    }
}
