// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ServiceScopeFactoryCallSite : ServiceCallSite
    {
        public ServiceScopeFactoryCallSite() : base(ResultCache.None)
        {
        }

        public override Type ServiceType { get; } = typeof(IServiceScopeFactory);
        public override Type ImplementationType { get; } = typeof(ServiceProviderEngine);
        public override CallSiteKind Kind { get; } = CallSiteKind.ServiceScopeFactory;
    }
}
