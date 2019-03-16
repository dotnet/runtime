// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal abstract class CompiledServiceProviderEngine : ServiceProviderEngine
    {
#if IL_EMIT
        public ILEmitResolverBuilder ResolverBuilder { get; }
#else
        public ExpressionResolverBuilder ResolverBuilder { get; }
#endif

        public CompiledServiceProviderEngine(IEnumerable<ServiceDescriptor> serviceDescriptors, IServiceProviderEngineCallback callback) : base(serviceDescriptors, callback)
        {
#if IL_EMIT
            ResolverBuilder = new ILEmitResolverBuilder(RuntimeResolver, this, Root);
#else
            ResolverBuilder = new ExpressionResolverBuilder(RuntimeResolver, this, Root);
#endif
        }

        protected override Func<ServiceProviderEngineScope, object> RealizeService(ServiceCallSite callSite)
        {
            var realizedService = ResolverBuilder.Build(callSite);
            RealizedServices[callSite.ServiceType] = realizedService;
            return realizedService;
        }
    }
}
