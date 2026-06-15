// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SharedSingletonResolvedAsMultipleServices_IsDisposedOnce(bool useAsyncDispose)
        {
            var services = new ServiceCollection();
            services.AddSingleton<MultipleServiceImpl>();
            services.AddSingleton<IMultipleService1>(sp => sp.GetRequiredService<MultipleServiceImpl>());
            services.AddSingleton<IMultipleService2>(sp => sp.GetRequiredService<MultipleServiceImpl>());

            var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetRequiredService<MultipleServiceImpl>();

            Assert.Same(service, serviceProvider.GetRequiredService<IMultipleService1>());
            Assert.Same(service, serviceProvider.GetRequiredService<IMultipleService2>());

            await DisposeAsync(serviceProvider, useAsyncDispose);

            Assert.Equal(1, service.DisposeCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SharedSingletonIsDisposedAfterDependents(bool useAsyncDispose)
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

            await DisposeAsync(serviceProvider, useAsyncDispose);

            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public void SharedSingletonWithManyAliases_IsDisposedOnce()
        {
            // Captures well over MaxDisposablesForLinearDedup (16) entries so BeginDispose takes the HashSet path.
            const int AliasCount = 20;

            var services = new ServiceCollection();
            services.AddSingleton<MultipleServiceImpl>();
            for (int i = 0; i < AliasCount; i++)
            {
                services.AddSingleton<IMultipleService1>(sp => sp.GetRequiredService<MultipleServiceImpl>());
            }

            var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetRequiredService<MultipleServiceImpl>();
            var aliases = serviceProvider.GetServices<IMultipleService1>();

            int aliasCount = 0;
            foreach (var alias in aliases)
            {
                Assert.Same(service, alias);
                aliasCount++;
            }
            Assert.Equal(AliasCount, aliasCount);

            serviceProvider.Dispose();

            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public async Task DisposeAsync_SkipsNulledDuplicatesInAsyncContinuation()
        {
            // Last-captured disposable returns an incomplete ValueTask so DisposeAsync transitions
            // into the static `Await` continuation. Earlier slots are duplicate captures of the
            // shared singleton, which the dedup pass nulls out. The continuation must skip the
            // nulled slots while still disposing the surviving singleton exactly once.
            var services = new ServiceCollection();
            services.AddSingleton<MultipleServiceImpl>();
            services.AddSingleton<IMultipleService1>(sp => sp.GetRequiredService<MultipleServiceImpl>());
            services.AddSingleton<IMultipleService2>(sp => sp.GetRequiredService<MultipleServiceImpl>());
            services.AddSingleton<AsyncBlockingDisposable>();

            var serviceProvider = services.BuildServiceProvider();

            var service = serviceProvider.GetRequiredService<MultipleServiceImpl>();
            _ = serviceProvider.GetRequiredService<IMultipleService1>();
            _ = serviceProvider.GetRequiredService<IMultipleService2>();
            var blocker = serviceProvider.GetRequiredService<AsyncBlockingDisposable>();

            await ((IAsyncDisposable)serviceProvider).DisposeAsync();

            Assert.Equal(1, service.DisposeCount);
            Assert.True(blocker.IsDisposed);
        }

        private static async ValueTask DisposeAsync(ServiceProvider serviceProvider, bool useAsyncDispose)
        {
            if (useAsyncDispose)
            {
                await ((IAsyncDisposable)serviceProvider).DisposeAsync();
            }
            else
            {
                serviceProvider.Dispose();
            }
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
            int DisposeCount { get; }
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
            private readonly IMultipleService1 _service;

            public DependsOnMultipleService(IMultipleService1 service)
            {
                _service = service;
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

        private sealed class AsyncBlockingDisposable : IAsyncDisposable
        {
            public bool IsDisposed { get; private set; }

            public ValueTask DisposeAsync() => new ValueTask(DisposeAsyncCore());

            private async Task DisposeAsyncCore()
            {
                await Task.Yield();
                IsDisposed = true;
            }
        }
    }
}
