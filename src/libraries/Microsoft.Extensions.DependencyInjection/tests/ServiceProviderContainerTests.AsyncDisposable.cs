// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
#if NETCOREAPP
    public abstract partial class ServiceProviderContainerTests
    {
        [Fact]
        public async Task ProviderDisposeAsyncCallsDisposeAsyncOnServices()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<AsyncDisposable>();

            var serviceProvider = CreateServiceProvider(serviceCollection);
            var disposable = serviceProvider.GetService<AsyncDisposable>();

            await (serviceProvider as IAsyncDisposable).DisposeAsync();

            Assert.True(disposable.DisposeAsyncCalled);
        }

        [Fact]
        public async Task ProviderDisposeAsyncPrefersDisposeAsyncOnServices()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<SyncAsyncDisposable>();

            var serviceProvider = CreateServiceProvider(serviceCollection);
            var disposable = serviceProvider.GetService<SyncAsyncDisposable>();

            await (serviceProvider as IAsyncDisposable).DisposeAsync();

            Assert.True(disposable.DisposeAsyncCalled);
        }

        [Fact]
        public void ProviderDisposePrefersServiceDispose()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<SyncAsyncDisposable>();

            var serviceProvider = CreateServiceProvider(serviceCollection);
            var disposable = serviceProvider.GetService<SyncAsyncDisposable>();

            (serviceProvider as IDisposable).Dispose();

            Assert.True(disposable.DisposeCalled);
        }

        [Fact]
        public void ProviderDisposeThrowsWhenOnlyDisposeAsyncImplemented()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<AsyncDisposable>();

            var serviceProvider = CreateServiceProvider(serviceCollection);
            var disposable = serviceProvider.GetService<AsyncDisposable>();

            var exception = Assert.Throws<InvalidOperationException>(() => (serviceProvider as IDisposable).Dispose());
            Assert.Equal(
                "'Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderContainerTests+AsyncDisposable' type only implements IAsyncDisposable. Use DisposeAsync to dispose the container.",
                exception.Message);
        }

        [Fact]
        public async Task ProviderScopeDisposeAsyncCallsDisposeAsyncOnServices()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<AsyncDisposable>();

            var serviceProvider = CreateServiceProvider(serviceCollection);
            var scope = serviceProvider.CreateScope();
            var disposable = scope.ServiceProvider.GetService<AsyncDisposable>();

            await (scope as IAsyncDisposable).DisposeAsync();

            Assert.True(disposable.DisposeAsyncCalled);
        }

        [Fact]
        public async Task ProviderScopeDisposeAsyncPrefersDisposeAsyncOnServices()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<SyncAsyncDisposable>();

            var serviceProvider = CreateServiceProvider(serviceCollection);
            var scope = serviceProvider.CreateScope();
            var disposable = scope.ServiceProvider.GetService<SyncAsyncDisposable>();

            await (scope as IAsyncDisposable).DisposeAsync();

            Assert.True(disposable.DisposeAsyncCalled);
        }

        [Fact]
        public void ProviderScopeDisposePrefersServiceDispose()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<SyncAsyncDisposable>();

            var serviceProvider = CreateServiceProvider(serviceCollection);
            var scope = serviceProvider.CreateScope();
            var disposable = scope.ServiceProvider.GetService<SyncAsyncDisposable>();

            (scope as IDisposable).Dispose();

            Assert.True(disposable.DisposeCalled);
        }

        [Fact]
        public void ProviderScopeDisposeThrowsWhenOnlyDisposeAsyncImplemented()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<AsyncDisposable>();

            var serviceProvider = CreateServiceProvider(serviceCollection);
            var scope = serviceProvider.CreateScope();
            var disposable = scope.ServiceProvider.GetService<AsyncDisposable>();

            var exception = Assert.Throws<InvalidOperationException>(() => (scope as IDisposable).Dispose());
            Assert.Equal(
                "'Microsoft.Extensions.DependencyInjection.Tests.ServiceProviderContainerTests+AsyncDisposable' type only implements IAsyncDisposable. Use DisposeAsync to dispose the container.",
                exception.Message);
        }

        private class AsyncDisposable: IFakeService, IAsyncDisposable
        {
            public bool DisposeAsyncCalled { get; private set; }

            public ValueTask DisposeAsync()
            {
                DisposeAsyncCalled = true;
                return new ValueTask(Task.CompletedTask);
            }
        }

        private class SyncAsyncDisposable: IFakeService, IAsyncDisposable, IDisposable
        {
            public bool DisposeCalled { get; private set; }
            public bool DisposeAsyncCalled { get; private set; }

            public void Dispose()
            {
                DisposeCalled = true;
            }

            public ValueTask DisposeAsync()
            {
                DisposeAsyncCalled = true;
                return new ValueTask(Task.CompletedTask);
            }
        }
    }
#endif
}
