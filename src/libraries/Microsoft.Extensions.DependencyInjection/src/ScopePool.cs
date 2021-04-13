// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class ScopePool
    {
        // Stash the scope state on a thread static
        [ThreadStatic]
        private static State t_tlsScopeState;
        private volatile int _maxScopeSize;

        // Roughly ~3.6KB
        private const int MaxResolvedServicesCount = 100;

        // Below default ~85K LOH threshold, avoid large temporary garbage
        private const int MaxCapacity = 2000;

        public State Rent()
        {
            // Try to get a scope from TLS
            State state = t_tlsScopeState;

            if (state != null)
            {
                // Clear the state
                t_tlsScopeState = null;
            }

            return state ?? new State(this, _maxScopeSize);
        }

        public bool Return(State state)
        {
            var size = state.Size;

            if (size <= MaxResolvedServicesCount)
            {
                // Stash the state back in TLS
                state.Clear();
                t_tlsScopeState = state;

                return true;
            }

            // Clamp the size between the min and max
            _maxScopeSize = Math.Min(MaxCapacity, Math.Max(size, _maxScopeSize));

            return false;
        }

        public class State
        {
            private readonly ScopePool _pool;

            public IDictionary<ServiceCacheKey, object> ResolvedServices { get; }
            public List<object> Disposables { get; set; }

            public int Size => ((Dictionary<ServiceCacheKey, object>)ResolvedServices).Count;

            public State(ScopePool pool = null, int initialCapacity = 0)
            {
                _pool = pool;
                // When pool is null, we're in the global scope which doesn't need pooling.
                // To reduce lock contention for singletons upon resolve we use a concurrent dictionary.
                ResolvedServices = pool == null ? new ConcurrentDictionary<ServiceCacheKey, object>() : new Dictionary<ServiceCacheKey, object>(initialCapacity);
            }

            internal void Clear()
            {
                // This should only get called from the pool
                Debug.Assert(_pool != null);
                // REVIEW: Should we trim excess here as well?
                ((Dictionary<ServiceCacheKey, object>)ResolvedServices).Clear();
                Disposables?.Clear();
            }

            public bool Return()
            {
                return _pool?.Return(this) ?? false;
            }
        }
    }
}
