// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
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

        public class CountAsyncDisposableService : IAsyncDisposable
        {
            public int DisposeCount { get; private set; }
            public async ValueTask DisposeAsync()
            {
                await Task.Delay(1);
                DisposeCount++;
            }
        }

        [Fact]
        public async Task DisposeAsync_IsCalledOnce()
        {
            var asyncDisposableService = new CountAsyncDisposableService();
            var serviceProviderEngineScope = new ServiceProviderEngineScope(null);
            serviceProviderEngineScope.CaptureDisposable(asyncDisposableService);
            await serviceProviderEngineScope.DisposeAsync();

            Assert.Equal(1, asyncDisposableService.DisposeCount);
        }
    }
}
