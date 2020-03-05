// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class RuntimeServiceProviderEngine : ServiceProviderEngine
    {
        public RuntimeServiceProviderEngine(IEnumerable<ServiceDescriptor> serviceDescriptors, IServiceProviderEngineCallback callback) : base(serviceDescriptors, callback)
        {
        }

        protected override Func<ServiceProviderEngineScope, object> RealizeService(ServiceCallSite callSite)
        {
            return scope =>
            {
                Func<ServiceProviderEngineScope, object> realizedService = p => RuntimeResolver.Resolve(callSite, p);

                RealizedServices[callSite.ServiceType] = realizedService;
                return realizedService(scope);
            };
        }
    }
}