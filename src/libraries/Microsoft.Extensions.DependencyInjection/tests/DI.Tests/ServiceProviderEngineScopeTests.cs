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
            var provider = new ServiceProvider(new ServiceCollection(), ServiceProviderOptions.Default);
            var serviceProviderEngineScope = new ServiceProviderEngineScope(provider, isRootScope: true);
            serviceProviderEngineScope.ResolvedServices.Add(new ServiceCacheKey(typeof(IFakeService), 0), null);
            serviceProviderEngineScope.Dispose();
            serviceProviderEngineScope.Dispose();
        }

        [Fact]
        public void RootEngineScopeDisposeTest()
        {
            var services = new ServiceCollection();
            ServiceProvider sp = services.BuildServiceProvider();
            var s = sp.GetRequiredService<IServiceProvider>();
            ((IDisposable)s).Dispose();

            Assert.Throws<ObjectDisposedException>(() => sp.GetRequiredService<IServiceProvider>());
        }
    }
}
