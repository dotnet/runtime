// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class ScopeState
    {
        public IDictionary<ServiceCacheKey, object> ResolvedServices { get; }
        public List<object> Disposables { get; set; }

        public int DisposableServicesCount => Disposables?.Count ?? 0;
        public int ResolvedServicesCount => ResolvedServices.Count;

        public ScopeState(bool isRoot)
        {
            // When isRoot is true to reduce lock contention for singletons upon resolve we use a concurrent dictionary.
            ResolvedServices = isRoot ? new ConcurrentDictionary<ServiceCacheKey, object>() : new Dictionary<ServiceCacheKey, object>();
        }

        public void Track(ServiceProviderEngine engine)
        {
            DependencyInjectionEventSource.Log.ScopeDisposed(engine, this);
        }
    }
}
