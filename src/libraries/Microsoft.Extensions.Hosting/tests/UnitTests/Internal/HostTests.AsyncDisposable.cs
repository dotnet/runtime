// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Hosting.Internal
{
    public partial class HostTests
    {
        [Fact]
        public async Task HostCallsDisposeAsyncOnServiceProvider()
        {
            using (var host = CreateBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<AsyncDisposableService>();
                })
                .Build())
            {
                await host.StartAsync();

                var asyncDisposableService = host.Services.GetService<AsyncDisposableService>();

                Assert.False(asyncDisposableService.DisposeAsyncCalled);

                await host.StopAsync();

                Assert.False(asyncDisposableService.DisposeAsyncCalled);

                host.Dispose();

                Assert.True(asyncDisposableService.DisposeAsyncCalled);
            }
        }

        [Fact]
        public async Task HostCallsDisposeAsyncOnServiceProviderWhenDisposeAsyncCalled()
        {
            using (var host = CreateBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<AsyncDisposableService>();
                })
                .Build())
            {
                await host.StartAsync();

                var asyncDisposableService = host.Services.GetService<AsyncDisposableService>();

                Assert.False(asyncDisposableService.DisposeAsyncCalled);

                await host.StopAsync();

                Assert.False(asyncDisposableService.DisposeAsyncCalled);

                await ((IAsyncDisposable)host).DisposeAsync();

                Assert.True(asyncDisposableService.DisposeAsyncCalled);
            }
        }

        [Fact]
        public async Task DisposeAsync_DisposesAppConfigurationProviders()
        {
            var providerMock = new Mock<ConfigurationProvider>().As<IDisposable>();
            providerMock.Setup(d => d.Dispose());

            var sourceMock = new Mock<IConfigurationSource>();
            sourceMock.Setup(s => s.Build(It.IsAny<IConfigurationBuilder>()))
                .Returns((ConfigurationProvider)providerMock.Object);

            var host = CreateBuilder()
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.Add(sourceMock.Object);
                })
                .Build();

            providerMock.Verify(c => c.Dispose(), Times.Never);

            await ((IAsyncDisposable)host).DisposeAsync();

            providerMock.Verify(c => c.Dispose(), Times.AtLeastOnce());
        }

        [Fact]
        public async Task DisposeAsync_DisposesHostConfigurationProviders()
        {
            var providerMock = new Mock<ConfigurationProvider>().As<IDisposable>();
            providerMock.Setup(d => d.Dispose());

            var sourceMock = new Mock<IConfigurationSource>();
            sourceMock.Setup(s => s.Build(It.IsAny<IConfigurationBuilder>()))
                .Returns((ConfigurationProvider)providerMock.Object);

            var host = CreateBuilder()
                .ConfigureHostConfiguration(configuration =>
                {
                    configuration.Add(sourceMock.Object);
                })
                .Build();

            providerMock.Verify(c => c.Dispose(), Times.Never);

            await ((IAsyncDisposable)host).DisposeAsync();

            providerMock.Verify(c => c.Dispose(), Times.AtLeastOnce());
        }

        private class AsyncDisposableService: IAsyncDisposable
        {
            public bool DisposeAsyncCalled { get; set; }

            public ValueTask DisposeAsync()
            {
                DisposeAsyncCalled = true;
                return default;
            }
        }
    }
}
#endif
