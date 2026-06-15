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
        public void DoubleDisposeWorks()
        {
            var provider = new ServiceProvider(new ServiceCollection(), ServiceProviderOptions.Default);
            var serviceProviderEngineScope = new ServiceProviderEngineScope(provider, isRootScope: true);
            serviceProviderEngineScope.ResolvedServices.Add(new ServiceCacheKey(ServiceIdentifier.FromServiceType(typeof(IFakeService)), 0), null);
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

        [Fact]
        public void ServiceProviderEngineScope_ImplementsAllServiceProviderInterfaces()
        {
            var engineScopeInterfaces = typeof(ServiceProviderEngineScope).GetInterfaces();
            foreach (var serviceProviderInterface in typeof(ServiceProvider).GetInterfaces())
            {
                Assert.Contains(serviceProviderInterface, engineScopeInterfaces);
            }
        }

        [Fact]
        public void Dispose_ServiceThrows_DisposesAllAndThrows()
        {
            var services = new ServiceCollection();
            services.AddKeyedTransient("throws", (_, _) => new TestDisposable(true));
            services.AddKeyedTransient("doesnotthrow", (_, _) => new TestDisposable(false));

            var scope = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider;

            var disposables = new TestDisposable[]
            {
                scope.GetRequiredKeyedService<TestDisposable>("throws"),
                scope.GetRequiredKeyedService<TestDisposable>("doesnotthrow")
            };

            var exception = Assert.Throws<InvalidOperationException>(() => ((IDisposable)scope).Dispose());
            Assert.Equal(TestDisposable.ErrorMessage, exception.Message);
            Assert.All(disposables, disposable => Assert.True(disposable.IsDisposed));
        }

        [Fact]
        public void Dispose_TwoServicesThrows_DisposesAllAndThrowsAggregateException()
        {
            var services = new ServiceCollection();
            services.AddKeyedTransient("throws", (_, _) => new TestDisposable(true));
            services.AddKeyedTransient("doesnotthrow", (_, _) => new TestDisposable(false));

            var scope = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider;

            var disposables = new TestDisposable[]
            {
                scope.GetRequiredKeyedService<TestDisposable>("throws"),
                scope.GetRequiredKeyedService<TestDisposable>("doesnotthrow"),
                scope.GetRequiredKeyedService<TestDisposable>("throws"),
                scope.GetRequiredKeyedService<TestDisposable>("doesnotthrow"),
            };

            var exception = Assert.Throws<AggregateException>(() => ((IDisposable)scope).Dispose());
            Assert.Equal(2, exception.InnerExceptions.Count);
            Assert.All(exception.InnerExceptions, ex => Assert.IsType<InvalidOperationException>(ex));
            Assert.All(disposables, disposable => Assert.True(disposable.IsDisposed));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DisposeAsync_ServiceThrows_DisposesAllAndThrows(bool synchronous)
        {
            var services = new ServiceCollection();
            services.AddKeyedTransient("throws", (_, _) => new TestDisposable(true, synchronous));
            services.AddKeyedTransient("doesnotthrow", (_, _) => new TestDisposable(false, synchronous));

            var scope = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider;

            var disposables = new TestDisposable[]
            {
                scope.GetRequiredKeyedService<TestDisposable>("throws"),
                scope.GetRequiredKeyedService<TestDisposable>("doesnotthrow")
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await ((IAsyncDisposable)scope).DisposeAsync());
            Assert.Equal(TestDisposable.ErrorMessage, exception.Message);
            Assert.All(disposables, disposable => Assert.True(disposable.IsDisposed));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DisposeAsync_TwoServicesThrows_DisposesAllAndThrowsAggregateException(bool synchronous)
        {
            var services = new ServiceCollection();
            services.AddKeyedTransient("throws", (_, _) => new TestDisposable(true, synchronous));
            services.AddKeyedTransient("doesnotthrow", (_, _) => new TestDisposable(false, synchronous));

            var scope = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider;

            var disposables = new TestDisposable[]
            {
                scope.GetRequiredKeyedService<TestDisposable>("throws"),
                scope.GetRequiredKeyedService<TestDisposable>("doesnotthrow"),
                scope.GetRequiredKeyedService<TestDisposable>("throws"),
                scope.GetRequiredKeyedService<TestDisposable>("doesnotthrow"),
            };

            var exception = await Assert.ThrowsAsync<AggregateException>(async () => await ((IAsyncDisposable)scope).DisposeAsync());
            Assert.Equal(2, exception.InnerExceptions.Count);
            Assert.All(exception.InnerExceptions, ex => Assert.IsType<InvalidOperationException>(ex));
            Assert.All(disposables, disposable => Assert.True(disposable.IsDisposed));
        }

        [Fact]
        public void Dispose_DoesNotDisposeSameSingletonInstanceResolvedAsMultipleServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<MultipleServiceImpl>();
            services.AddSingleton<IMultipleService1>(sp => sp.GetRequiredService<MultipleServiceImpl>());
            services.AddSingleton<IMultipleService2>(sp => sp.GetRequiredService<MultipleServiceImpl>());

            var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetRequiredService<MultipleServiceImpl>();

            Assert.Same(service, serviceProvider.GetRequiredService<IMultipleService1>());
            Assert.Same(service, serviceProvider.GetRequiredService<IMultipleService2>());

            serviceProvider.Dispose();

            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public async Task DisposeAsync_DoesNotDisposeSameSingletonInstanceResolvedAsMultipleServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<MultipleServiceImpl>();
            services.AddSingleton<IMultipleService1>(sp => sp.GetRequiredService<MultipleServiceImpl>());
            services.AddSingleton<IMultipleService2>(sp => sp.GetRequiredService<MultipleServiceImpl>());

            var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetRequiredService<MultipleServiceImpl>();

            Assert.Same(service, serviceProvider.GetRequiredService<IMultipleService1>());
            Assert.Same(service, serviceProvider.GetRequiredService<IMultipleService2>());

            await serviceProvider.DisposeAsync();

            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public void Dispose_DisposesSharedSingletonAfterDependents()
        {
            var services = new ServiceCollection();
            services.AddSingleton<MultipleServiceImpl>();
            services.AddSingleton<IMultipleService1>(sp => sp.GetRequiredService<MultipleServiceImpl>());
            services.AddSingleton<IMultipleService2>(sp => sp.GetRequiredService<MultipleServiceImpl>());
            services.AddSingleton<DependsOnMultipleService>();

            var serviceProvider = services.BuildServiceProvider();

            _ = serviceProvider.GetRequiredService<DependsOnMultipleService>();
            _ = serviceProvider.GetRequiredService<IMultipleService2>();
            var service = serviceProvider.GetRequiredService<MultipleServiceImpl>();

            serviceProvider.Dispose();

            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public async Task DisposeAsync_DisposesSharedSingletonAfterDependents()
        {
            var services = new ServiceCollection();
            services.AddSingleton<MultipleServiceImpl>();
            services.AddSingleton<IMultipleService1>(sp => sp.GetRequiredService<MultipleServiceImpl>());
            services.AddSingleton<IMultipleService2>(sp => sp.GetRequiredService<MultipleServiceImpl>());
            services.AddSingleton<DependsOnMultipleService>();

            var serviceProvider = services.BuildServiceProvider();

            _ = serviceProvider.GetRequiredService<DependsOnMultipleService>();
            _ = serviceProvider.GetRequiredService<IMultipleService2>();
            var service = serviceProvider.GetRequiredService<MultipleServiceImpl>();

            await serviceProvider.DisposeAsync();

            Assert.Equal(1, service.DisposeCount);
        }

        private class TestDisposable : IDisposable, IAsyncDisposable
        {
            public const string ErrorMessage = "Dispose failed.";

            private readonly bool _throwsOnDispose;
            private readonly bool _synchronous;

            public bool IsDisposed { get; private set; }

            public TestDisposable(bool throwsOnDispose = false, bool synchronous = false)
            {
                _throwsOnDispose = throwsOnDispose;
                _synchronous = synchronous;
            }

            public void Dispose()
            {
                IsDisposed = true;

                if (_throwsOnDispose)
                {
                    throw new InvalidOperationException(ErrorMessage);
                }
            }

            public ValueTask DisposeAsync()
            {
                if (_synchronous)
                {
                    Dispose();
                    return default;
                }

                return new ValueTask(DisposeAsyncInternal());

                async Task DisposeAsyncInternal()
                {
                    await Task.Yield();
                    Dispose();
                }
            }
        }

        private interface IMultipleService1
        {
        }

        private interface IMultipleService2
        {
        }

        private sealed class MultipleServiceImpl : IMultipleService1, IMultipleService2, IDisposable, IAsyncDisposable
        {
            public int DisposeCount { get; private set; }

            public void Dispose() => DisposeCount++;

            public ValueTask DisposeAsync()
            {
                DisposeCount++;
                return default;
            }
        }

        private sealed class DependsOnMultipleService : IDisposable, IAsyncDisposable
        {
            private readonly MultipleServiceImpl _service;

            public DependsOnMultipleService(IMultipleService1 service)
            {
                _service = (MultipleServiceImpl)service;
            }

            public void Dispose()
            {
                if (_service.DisposeCount != 0)
                {
                    throw new InvalidOperationException("Shared service should be disposed after dependents.");
                }
            }

            public ValueTask DisposeAsync()
            {
                if (_service.DisposeCount != 0)
                {
                    throw new InvalidOperationException("Shared service should be disposed after dependents.");
                }
                return default;
            }
        }
    }
}
