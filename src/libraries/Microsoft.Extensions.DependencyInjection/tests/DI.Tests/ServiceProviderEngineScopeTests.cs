// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    public class ServiceProviderEngineScopeTests
    {
        [Fact]
        public void DoubleDisposeWorks()
        {
            var engine = new FakeEngine();
            var serviceProviderEngineScope = new ServiceProviderEngineScope(engine);
            serviceProviderEngineScope.ResolvedServices.Add(new ServiceCacheKey(typeof(IFakeService), 0), null);
            serviceProviderEngineScope.Dispose();
            serviceProviderEngineScope.Dispose();
        }

        private class FakeEngine : ServiceProviderEngine
        {
            public FakeEngine() :
                base(Array.Empty<ServiceDescriptor>())
            {
            }

            protected override Func<ServiceProviderEngineScope, object> RealizeService(ServiceCallSite callSite)
            {
                return scope => null;
            }

        }
    }
}
