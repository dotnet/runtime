// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    public class ServiceProviderEngineScopeTests
    {
        [Fact]
        public void Dispose_DoesntClearResolvedServices()
        {
            var serviceProviderEngineScope = new ServiceProviderEngineScope(null);
            serviceProviderEngineScope.ResolvedServices.Add(new ServiceCacheKey(typeof(IFakeService), 0), null);
            serviceProviderEngineScope.Dispose();

            Assert.Single(serviceProviderEngineScope.ResolvedServices);
        }
    }
}
