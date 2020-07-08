// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class DynamicServiceProviderEngine : CompiledServiceProviderEngine
    {
        public DynamicServiceProviderEngine(IEnumerable<ServiceDescriptor> serviceDescriptors)
            : base(serviceDescriptors)
        {
        }

        protected override Func<ServiceProviderEngineScope, object> RealizeService(ServiceCallSite callSite)
        {
            int callCount = 0;
            return scope =>
            {
                if (Interlocked.Increment(ref callCount) == 2)
                {
                    Task.Run(() => base.RealizeService(callSite));
                }
                return RuntimeResolver.Resolve(callSite, scope);
            };
        }
    }
}
