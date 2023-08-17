// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class FactoryCallSite : ServiceCallSite
    {
        public Func<IServiceProvider, object> Factory { get; }

        public FactoryCallSite(ResultCache cache, Type serviceType, Func<IServiceProvider, object> factory) : base(cache)
        {
            Factory = factory;
            ServiceType = serviceType;
        }

        public FactoryCallSite(ResultCache cache, Type serviceType, object serviceKey, Func<IServiceProvider, object, object> factory) : base(cache)
        {
            Factory = sp => factory(sp, serviceKey);
            ServiceType = serviceType;
        }

        public override Type ServiceType { get; }
        public override Type? ImplementationType => null;

        public override CallSiteKind Kind { get; } = CallSiteKind.Factory;
    }
}
