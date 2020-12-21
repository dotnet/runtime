// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Fakes;
using Microsoft.Extensions.DependencyInjection.Specification;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Microsoft.Extensions.DependencyInjection.Tests.Fakes;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public abstract class ServiceProviderContainerTests : DependencyInjectionSpecificationTests
    {
        [Fact]
        public void RethrowOriginalExceptionFromConstructor()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<ClassWithThrowingEmptyCtor>();
            serviceCollection.AddTransient<ClassWithThrowingCtor>();
            serviceCollection.AddTransient<IFakeService, FakeService>();

            var provider = CreateServiceProvider(serviceCollection);

            var ex1 = Assert.Throws<Exception>(() => provider.GetService<ClassWithThrowingEmptyCtor>());
            Assert.Equal(nameof(ClassWithThrowingEmptyCtor), ex1.Message);

            var ex2 = Assert.Throws<Exception>(() => provider.GetService<ClassWithThrowingCtor>());
            Assert.Equal(nameof(ClassWithThrowingCtor), ex2.Message);
        }

        [Fact]
        public void DependencyWithPrivateConstructorIsIdentifiedAsPartOfException()
        {
            // Arrange
            var expectedMessage = $"A suitable constructor for type '{typeof(ClassWithPrivateCtor).FullName}' could not be located. "
                + "Ensure the type is concrete and services are registered for all parameters of a public constructor.";
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<ClassWithPrivateCtor>();
            serviceCollection.AddTransient<ClassDependsOnPrivateConstructorClass>();
            var serviceProvider = CreateServiceProvider(serviceCollection);

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetServices<ClassDependsOnPrivateConstructorClass>());
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void AttemptingToResolveNonexistentServiceIndirectlyThrows()
        {
            // Arrange
            var collection = new ServiceCollection();
            collection.AddTransient<DependOnNonexistentService>();
            var provider = CreateServiceProvider(collection);

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.GetService<DependOnNonexistentService>());
            Assert.Equal($"Unable to resolve service for type '{typeof(IFakeService)}' while attempting to activate " +
                $"'{typeof(DependOnNonexistentService)}'.", ex.Message);
        }

        [Fact]
        public void AttemptingToIEnumerableResolveNonexistentServiceIndirectlyThrows()
        {
            // Arrange
            var collection = new ServiceCollection();
            collection.AddTransient<DependOnNonexistentService>();
            var provider = CreateServiceProvider(collection);

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                provider.GetService<IEnumerable<DependOnNonexistentService>>());
            Assert.Equal($"Unable to resolve service for type '{typeof(IFakeService)}' while attempting to activate " +
                $"'{typeof(DependOnNonexistentService)}'.", ex.Message);
        }

        [Theory]
        // GenericTypeDefintion, Abstract GenericTypeDefintion
        [InlineData(typeof(IFakeOpenGenericService<>), typeof(AbstractFakeOpenGenericService<>))]
        // GenericTypeDefintion, Interface GenericTypeDefintion
        [InlineData(typeof(ICollection<>), typeof(IList<>))]
        // Implementation type is GenericTypeDefintion
        [InlineData(typeof(IList<int>), typeof(List<>))]
        // Implementation type is Abstract
        [InlineData(typeof(IFakeService), typeof(AbstractClass))]
        // Implementation type is Interface
        [InlineData(typeof(IFakeEveryService), typeof(IFakeService))]
        public void CreatingServiceProviderWithUnresolvableTypesThrows(Type serviceType, Type implementationType)
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient(serviceType, implementationType);

            // Act and Assert
            var exception = Assert.Throws<ArgumentException>(() => serviceCollection.BuildServiceProvider());
            Assert.Equal(
                $"Cannot instantiate implementation type '{implementationType}' for service type '{serviceType}'.",
                exception.Message);
        }

        [Fact]
        public void DoesNotDisposeSingletonInstances()
        {
            var disposable = new Disposable();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(disposable);

            var provider = CreateServiceProvider(serviceCollection);
            provider.GetService<Disposable>();

            ((IDisposable)provider).Dispose();

            Assert.False(disposable.Disposed);
        }

        [Fact]
        public void ResolvesServiceMixedServiceAndOptionalStructConstructorArguments()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IFakeService, FakeService>();
            serviceCollection.AddSingleton<ClassWithServiceAndOptionalArgsCtorWithStructs>();

            var provider = CreateServiceProvider(serviceCollection);
            var service = provider.GetService<ClassWithServiceAndOptionalArgsCtorWithStructs>();
            Assert.NotNull(service);
        }

        [Fact]
        public void ResolvesServiceMixedServiceAndOptionalStructConstructorArgumentsReliably()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IFakeService, FakeService>();
            serviceCollection.AddTransient<ClassWithServiceAndOptionalArgsCtorWithStructs>();

            var provider = CreateServiceProvider(serviceCollection);

            // Repeatedly resolve and re-check to ensure dynamically generated code properly initializes the values types.
            for (int i = 0; i < 100; i++)
            {
                var service = provider.GetService<ClassWithServiceAndOptionalArgsCtorWithStructs>();

                Assert.NotNull(service);
                Assert.Equal(new DateTime(), service.DateTime);
                Assert.Equal(default(DateTime), service.DateTimeDefault);
                Assert.Equal(new TimeSpan(), service.TimeSpan);
                Assert.Equal(default(TimeSpan), service.TimeSpanDefault);
                Assert.Equal(new DateTimeOffset(), service.DateTimeOffset);
                Assert.Equal(default(DateTimeOffset), service.DateTimeOffsetDefault);
                Assert.Equal(new Guid(), service.Guid);
                Assert.Equal(default(Guid), service.GuidDefault);
                Assert.Equal(new ClassWithServiceAndOptionalArgsCtorWithStructs.CustomStruct(), service.CustomStructValue);
                Assert.Equal(default(ClassWithServiceAndOptionalArgsCtorWithStructs.CustomStruct), service.CustomStructDefault);
            }
        }

        public enum TheEnum
        {
            HelloWorld = -1,
            NiceWorld = 0,
            GoodByeWorld = 1,
        }

        [Theory]
        [InlineData(ServiceLifetime.Transient)]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Singleton)]
        public void ResolvesConstantValueTypeServicesCorrectly(ServiceLifetime lifetime)
        {
            var serviceCollection = new ServiceCollection();
            if (lifetime == ServiceLifetime.Transient)
            {
                serviceCollection.AddTransient(typeof(int), _ => 4);
                serviceCollection.AddTransient(typeof(DateTime), _ => new DateTime());
                serviceCollection.AddTransient(typeof(TheEnum), _ => TheEnum.HelloWorld);

                serviceCollection.AddTransient(typeof(TimeSpan), _ => TimeSpan.Zero);
                serviceCollection.AddTransient(typeof(TimeSpan), _ => new TimeSpan(1, 2, 3));
            }
            else if (lifetime == ServiceLifetime.Scoped)
            {
                serviceCollection.AddScoped(typeof(int), _ => 4);
                serviceCollection.AddScoped(typeof(DateTime), _ => new DateTime());
                serviceCollection.AddScoped(typeof(TheEnum), _ => TheEnum.HelloWorld);

                serviceCollection.AddScoped(typeof(TimeSpan), _ => TimeSpan.Zero);
                serviceCollection.AddScoped(typeof(TimeSpan), _ => new TimeSpan(1, 2, 3));
            }
            else if (lifetime == ServiceLifetime.Singleton)
            {
                serviceCollection.AddSingleton(typeof(int), 4);
                serviceCollection.AddSingleton(typeof(DateTime), new DateTime());
                serviceCollection.AddSingleton(typeof(TheEnum), TheEnum.HelloWorld);

                serviceCollection.AddSingleton(typeof(TimeSpan), TimeSpan.Zero);
                serviceCollection.AddSingleton(typeof(TimeSpan), _ => new TimeSpan(1, 2, 3));
            }

            var provider = CreateServiceProvider(serviceCollection);

            int i = provider.GetService<int>();
            Assert.Equal(4, i);

            DateTime d = provider.GetService<DateTime>();
            Assert.Equal(new DateTime(), d);

            TheEnum e = provider.GetService<TheEnum>();
            Assert.Equal(TheEnum.HelloWorld, e);

            IEnumerable<TimeSpan> times = provider.GetServices<TimeSpan>();
            Assert.Equal(new[] { TimeSpan.Zero, new TimeSpan(1, 2, 3) }, times);
        }

        [Fact]
        public void RootProviderDispose_PreventsServiceResolution()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IFakeService, FakeService>();

            var provider = CreateServiceProvider(serviceCollection);
            ((IDisposable)provider).Dispose();

            Assert.Throws<ObjectDisposedException>(() => provider.GetService<IFakeService>());
        }

        [Fact]
        public void RootProviderDispose_PreventsScopeCreation()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IFakeService, FakeService>();

            var provider = CreateServiceProvider(serviceCollection);
            ((IDisposable)provider).Dispose();

            Assert.Throws<ObjectDisposedException>(() => provider.CreateScope());
        }

        [Fact]
        public void RootProviderDispose_PreventsServiceResolution_InChildScope()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IFakeService, FakeService>();

            var provider = CreateServiceProvider(serviceCollection);
            var scope = provider.CreateScope();
            ((IDisposable)provider).Dispose();

            Assert.Throws<ObjectDisposedException>(() => scope.ServiceProvider.GetService<IFakeService>());
        }

        [Fact]
        public void ScopeDispose_PreventsServiceResolution()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IFakeService, FakeService>();

            var provider = CreateServiceProvider(serviceCollection);
            var scope = provider.CreateScope();
            scope.Dispose();

            Assert.Throws<ObjectDisposedException>(() => scope.ServiceProvider.GetService<IFakeService>());
            //Check that resolution from root works
            Assert.NotNull(provider.CreateScope());
        }

        [Fact]
        public void GetService_DisposeOnSameThread_Throws()
        {
            var services = new ServiceCollection();
            services.AddSingleton<DisposeServiceProviderInCtor>();
            IServiceProvider sp = services.BuildServiceProvider();
            Assert.Throws<ObjectDisposedException>(() =>
            {
                // ctor disposes ServiceProvider
                var service = sp.GetRequiredService<DisposeServiceProviderInCtor>();
            });
        }

        [Fact]
        public void GetAsyncService_DisposeAsyncOnSameThread_ThrowsAndDoesNotHangAndDisposeAsyncGetsCalled()
        {
            // Arrange
            var services = new ServiceCollection();
            var asyncDisposableResource = new AsyncDisposable();
            services.AddSingleton<DisposeServiceProviderInCtorAsyncDisposable>(sp =>
                new DisposeServiceProviderInCtorAsyncDisposable(asyncDisposableResource, sp));

            var sp = services.BuildServiceProvider();
            bool doesNotHang = Task.Run(() =>
            {
                SingleThreadedSynchronizationContext.Run(() =>
                {
                    // Act
                    Assert.Throws<ObjectDisposedException>(() =>
                    {
                        // ctor disposes ServiceProvider
                        var service = sp.GetRequiredService<DisposeServiceProviderInCtorAsyncDisposable>();
                    });
                });
            }).Wait(TimeSpan.FromSeconds(10));

            Assert.True(doesNotHang);
            Assert.True(asyncDisposableResource.DisposeAsyncCalled);
        }

        [Fact]
        public void GetService_DisposeOnSameThread_ThrowsAndDoesNotHangAndDisposeGetsCalled()
        {
            // Arrange
            var services = new ServiceCollection();
            var disposableResource = new Disposable();
            services.AddSingleton<DisposeServiceProviderInCtorDisposable>(sp =>
                new DisposeServiceProviderInCtorDisposable(disposableResource, sp));

            var sp = services.BuildServiceProvider();
            bool doesNotHang = Task.Run(() =>
            {
                SingleThreadedSynchronizationContext.Run(() =>
                {
                    // Act
                    Assert.Throws<ObjectDisposedException>(() =>
                    {
                        // ctor disposes ServiceProvider
                        var service = sp.GetRequiredService<DisposeServiceProviderInCtorDisposable>();
                    });
                });
            }).Wait(TimeSpan.FromSeconds(10));

            Assert.True(doesNotHang);
            Assert.True(disposableResource.Disposed);
        }

        private class DisposeServiceProviderInCtor : IDisposable
        {
            public DisposeServiceProviderInCtor(IServiceProvider sp)
            {
                (sp as IDisposable).Dispose();
            }
            public void Dispose() { }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task AddDisposablesAndAsyncDisposables_DisposeAsync_AllDisposed(bool includeDelayedAsyncDisposable)
        {
            var services = new ServiceCollection();
            services.AddSingleton<AsyncDisposable>();
            services.AddSingleton<Disposable>();
            if (includeDelayedAsyncDisposable)
            {
                //forces Dispose ValueTask to be asynchronous and not be immediately completed
                services.AddSingleton<DelayedAsyncDisposableService>();
            }
            ServiceProvider sp = services.BuildServiceProvider();
            var disposable = sp.GetRequiredService<Disposable>();
            var asyncDisposable = sp.GetRequiredService<AsyncDisposable>();
            DelayedAsyncDisposableService delayedAsyncDisposableService = null;
            if (includeDelayedAsyncDisposable)
            {
                delayedAsyncDisposableService = sp.GetRequiredService<DelayedAsyncDisposableService>();
            }

            await sp.DisposeAsync();
            
            Assert.True(disposable.Disposed);
            Assert.True(asyncDisposable.DisposeAsyncCalled);
            if (includeDelayedAsyncDisposable)
            {
                Assert.Equal(1, delayedAsyncDisposableService.DisposeCount);
            }
        }

        private class DisposeServiceProviderInCtorAsyncDisposable : IFakeService, IAsyncDisposable
        {
            private readonly AsyncDisposable _asyncDisposable;

            public DisposeServiceProviderInCtorAsyncDisposable(AsyncDisposable asyncDisposable, IServiceProvider sp)
            {
                _asyncDisposable = asyncDisposable;
                (sp as IAsyncDisposable).DisposeAsync();
            }
            public async ValueTask DisposeAsync()
            {
                await _asyncDisposable.DisposeAsync();
                await Task.Yield();
            }
        }

        private class DisposeServiceProviderInCtorDisposable : IFakeService, IDisposable
        {
            private readonly Disposable _disposable;

            public DisposeServiceProviderInCtorDisposable(Disposable disposable, IServiceProvider sp)
            {
                _disposable = disposable;
                (sp as IDisposable).Dispose();
            }
            public void Dispose()
            {
                _disposable.Dispose();
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/42160")] // We don't support value task services currently
        [Theory]
        [InlineData(ServiceLifetime.Transient)]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Singleton)]
        public void WorksWithStructServices(ServiceLifetime lifetime)
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.Add(new ServiceDescriptor(typeof(IFakeService), typeof(StructFakeService), lifetime));
            serviceCollection.Add(new ServiceDescriptor(typeof(StructService), typeof(StructService), lifetime));
            serviceCollection.Add(new ServiceDescriptor(typeof(IFakeMultipleService), typeof(StructFakeMultipleService), lifetime));

            var provider = CreateServiceProvider(serviceCollection);
            var service = provider.GetService<IFakeMultipleService>();
            Assert.NotNull(service);
        }

        [Fact]
        public void WorksWithWideScopedTrees()
        {
            var serviceCollection = new ServiceCollection();
            for (int i = 0; i < 20; i++)
            {
                serviceCollection.AddScoped<IFakeOuterService, FakeOuterService>();
                serviceCollection.AddScoped<IFakeMultipleService, FakeMultipleServiceWithIEnumerableDependency>();
                serviceCollection.AddScoped<IFakeService, FakeService>();
            }

            var service = CreateServiceProvider(serviceCollection).GetService<IEnumerable<IFakeOuterService>>();
        }

        [Fact]
        public void GenericIEnumerableItemCachedInTheRightSlot()
        {
            var services = new ServiceCollection();
            // It's important that this service is generic, it hits a different codepath when resolved inside IEnumerable
            services.AddSingleton<IFakeOpenGenericService<PocoClass>, FakeService>();
            // Doesn't matter what this services is, we just want something in the collection after generic registration
            services.AddSingleton<FakeService>();

            var serviceProvider = services.BuildServiceProvider();

            var serviceRef1 = serviceProvider.GetRequiredService<IFakeOpenGenericService<PocoClass>>();
            var servicesRef1 = serviceProvider.GetServices<IFakeOpenGenericService<PocoClass>>().Single();

            Assert.Same(serviceRef1, servicesRef1);
        }


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

        [Fact]
        public void SingletonServiceCreatedFromFactoryIsDisposedWhenContainerIsDisposed()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(_ => new FakeDisposable());
            var serviceProvider = CreateServiceProvider(serviceCollection);

            // Act
            var service = serviceProvider.GetService<FakeDisposable>();
            ((IDisposable)serviceProvider).Dispose();

            // Assert
            Assert.True(service.IsDisposed);
        }

        [Fact]
        public void SingletonServiceCreatedFromInstanceIsNotDisposedWhenContainerIsDisposed()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(new FakeDisposable());
            var serviceProvider = CreateServiceProvider(serviceCollection);

            // Act
            var service = serviceProvider.GetService<FakeDisposable>();
            ((IDisposable)serviceProvider).Dispose();

            // Assert
            Assert.False(service.IsDisposed);
        }

        [Fact]
        public async Task ProviderDisposeAsyncCallsDisposeAsyncOnceOnServices()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<DelayedAsyncDisposableService>();

            var serviceProvider = CreateServiceProvider(serviceCollection);
            var disposable = serviceProvider.GetService<DelayedAsyncDisposableService>();

            await (serviceProvider as IAsyncDisposable).DisposeAsync();

            Assert.Equal(1, disposable.DisposeCount);
        }

        private class FakeDisposable : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        private class FakeMultipleServiceWithIEnumerableDependency: IFakeMultipleService
        {
            public FakeMultipleServiceWithIEnumerableDependency(IEnumerable<IFakeService> fakeServices)
            {
            }
        }

        private abstract class AbstractFakeOpenGenericService<T> : IFakeOpenGenericService<T>
        {
            public abstract T Value { get; }
        }

        private class Disposable : IDisposable
        {
            public bool Disposed { get; set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private class AsyncDisposable : IFakeService, IAsyncDisposable
        {
            public bool DisposeAsyncCalled { get; private set; }

            public ValueTask DisposeAsync()
            {
                DisposeAsyncCalled = true;
                return new ValueTask(Task.CompletedTask);
            }
        }

        private class SyncAsyncDisposable : IFakeService, IAsyncDisposable, IDisposable
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

        private class DelayedAsyncDisposableService : IAsyncDisposable
        {
            public int DisposeCount { get; private set; }
            public async ValueTask DisposeAsync()
            {
                //forces ValueTask to be asynchronous and not be immediately completed
                await Task.Yield();
                DisposeCount++;
            }
        }
    }
}
