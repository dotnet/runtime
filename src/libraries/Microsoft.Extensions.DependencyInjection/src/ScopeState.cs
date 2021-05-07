// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class ScopeState
    {
        public Dictionary<ServiceCacheKey, object> ResolvedServices { get; }
        public List<object> Disposables { get; set; }

        public int DisposableServicesCount => Disposables?.Count ?? 0;
        public int ResolvedServicesCount => ResolvedServices.Count;

        public ScopeState()
        {
            ResolvedServices = new Dictionary<ServiceCacheKey, object>();
        }

        public void Track(ServiceProviderEngine engine)
        {
            DependencyInjectionEventSource.Log.ScopeDisposed(engine, this);
        }
    }
}
