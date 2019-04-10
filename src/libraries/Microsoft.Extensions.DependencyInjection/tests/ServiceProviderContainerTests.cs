// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Fakes;
using Microsoft.Extensions.DependencyInjection.Specification;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Microsoft.Extensions.DependencyInjection.Tests.Fakes;
using Moq;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public abstract partial class ServiceProviderContainerTests : DependencyInjectionSpecificationTests
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

        [Theory(Skip = "We don't support value task services currently")]
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
    }
}
