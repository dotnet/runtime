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
        public State Allocate() => new State(this);

        private void Track(State state)
        {
            DependencyInjectionEventSource.Log.ScopeDisposed(this, state);
        }

        public class State
        {
            private readonly ScopeTracker _tracker;

            public IDictionary<ServiceCacheKey, object> ResolvedServices { get; }
            public List<object> Disposables { get; set; }

            public int DisposableServicesCount => Disposables?.Count ?? 0;
            public int ResolvedServicesCount => ((Dictionary<ServiceCacheKey, object>)ResolvedServices).Count;

            public State(ScopeTracker tracker = null)
            {
                _tracker = tracker;

                // When tracker is null, we're not tracking the number of resolved services. Also
                // to reduce lock contention for singletons upon resolve we use a concurrent dictionary.

                ResolvedServices = tracker == null ? new ConcurrentDictionary<ServiceCacheKey, object>() : new Dictionary<ServiceCacheKey, object>();
            }

            public void Track()
            {
                _tracker?.Track(this);
            }
        }
    }
}
