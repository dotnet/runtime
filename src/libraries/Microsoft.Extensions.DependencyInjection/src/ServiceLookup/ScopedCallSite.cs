// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ScopedCallSite : IServiceCallSite
    {
        internal IServiceCallSite ServiceCallSite { get; }
        public object CacheKey { get; }

        public ScopedCallSite(IServiceCallSite serviceCallSite, object cacheKey)
        {
            ServiceCallSite = serviceCallSite;
            CacheKey = cacheKey;
        }

        public Type ServiceType => ServiceCallSite.ServiceType;
        public Type ImplementationType => ServiceCallSite.ImplementationType;
        public virtual CallSiteKind Kind { get; } = CallSiteKind.Scope;
    }
}