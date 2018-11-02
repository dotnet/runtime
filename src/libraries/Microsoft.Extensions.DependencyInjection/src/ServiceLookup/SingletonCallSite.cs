// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class SingletonCallSite : ScopedCallSite
    {
        public SingletonCallSite(IServiceCallSite serviceCallSite, object cacheKey) : base(serviceCallSite, cacheKey)
        {
        }

        public override CallSiteKind Kind { get; } = CallSiteKind.Singleton;
    }
}