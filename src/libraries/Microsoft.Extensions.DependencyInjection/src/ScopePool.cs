// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class ScopePool
    {
        // Modest number to re-use. We only really care about reuse for short lived scopes
        private const int MaxQueueSize = 128;

        private int _count;
        private readonly ConcurrentQueue<State> _queue = new();

        public State Rent()
        {
            if (_queue.TryDequeue(out State state))
            {
                Interlocked.Decrement(ref _count);
                return state;
            }
            return new State(this);
        }

        public bool Return(State state)
        {
            if (Interlocked.Increment(ref _count) > MaxQueueSize)
            {
                Interlocked.Decrement(ref _count);
                return false;
            }

            state.Clear();
            _queue.Enqueue(state);
            return true;
        }

        public class State
        {
            private readonly ScopePool _pool;

            public IDictionary<ServiceCacheKey, object> ResolvedServices { get; }
            public List<object> Disposables { get; set; }

            public State(ScopePool pool = null)
            {
                _pool = pool;
                // When pool is null, we're in the global scope which doesn't need pooling.
                // To reduce lock contention for singletons upon resolve we use a concurrent dictionary.
                ResolvedServices = pool == null ? new ConcurrentDictionary<ServiceCacheKey, object>() : new Dictionary<ServiceCacheKey, object>();
            }

            internal void Clear()
            {
                // This should only get called from the pool
                Debug.Assert(_pool != null);
                // REVIEW: Should we trim excess here as well?
                ResolvedServices.Clear();
                Disposables?.Clear();
            }

            public bool Return()
            {
                return _pool?.Return(this) ?? false;
            }
        }
    }
}
