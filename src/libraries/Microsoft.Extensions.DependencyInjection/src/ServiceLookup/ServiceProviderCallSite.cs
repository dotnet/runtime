// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class ServiceProviderCallSite : ServiceCallSite
    {
        public ServiceProviderCallSite() : base(ResultCache.None)
        {
        }

        public override Type ServiceType { get; } = typeof(IServiceProvider);
        public override Type ImplementationType { get; } = typeof(ServiceProvider);
        public override CallSiteKind Kind { get; } = CallSiteKind.ServiceProvider;
    }
}
