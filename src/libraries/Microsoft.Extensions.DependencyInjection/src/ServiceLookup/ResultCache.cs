// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal struct ResultCache
    {
        public static ResultCache None { get; } = new ResultCache(CallSiteResultCacheLocation.None, ServiceCacheKey.Empty);

        internal ResultCache(CallSiteResultCacheLocation lifetime, ServiceCacheKey cacheKey)
        {
            Location = lifetime;
            Key = cacheKey;
        }

        public ResultCache(ServiceLifetime lifetime, Type? type, int slot)
        {
            Debug.Assert(lifetime == ServiceLifetime.Transient || type != null);

            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    Location = CallSiteResultCacheLocation.Root;
                    break;
                case ServiceLifetime.Scoped:
                    Location = CallSiteResultCacheLocation.Scope;
                    break;
                case ServiceLifetime.Transient:
                    Location = CallSiteResultCacheLocation.Dispose;
                    break;
                default:
                    Location = CallSiteResultCacheLocation.None;
                    break;
            }
            Key = new ServiceCacheKey(type, slot);
        }

        public CallSiteResultCacheLocation Location { get; set; }

        public ServiceCacheKey Key { get; set; }
    }
}
