// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
