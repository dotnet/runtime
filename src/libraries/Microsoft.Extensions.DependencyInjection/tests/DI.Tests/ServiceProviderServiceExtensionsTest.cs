// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ServiceProviderExtensionsTest
    {
        [Fact]
        public void GetService_Returns_CorrectService()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(1);

            // Act
            var service = serviceProvider.GetService<IFoo>();

            // Assert
            Assert.IsType<Foo1>(service);
        }

        [Fact]
        public void ISupportRequiredService_GetRequiredService_Returns_CorrectService()
        {
            // Arrange
            var serviceProvider = new RequiredServiceSupportingProvider();

            // Act
            var service = serviceProvider.GetRequiredService<IBar>();

            // Assert
            Assert.IsType<Bar1>(service);
        }

        [Fact]
        public void GetRequiredService_Throws_WhenNoServiceRegistered()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(0);

            // Act + Assert
            AssertExtensions.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<IFoo>(),
                $"No service for type '{typeof(IFoo)}' has been registered.");
        }

        [Fact]
        public void ISupportRequiredService_GetRequiredService_Throws_WhenNoServiceRegistered()
        {
            // Arrange
            var serviceProvider = new RequiredServiceSupportingProvider();

            // Act + Assert
            AssertExtensions.Throws<RankException>(() => serviceProvider.GetRequiredService<IFoo>());
        }

        [Fact]
        public void NonGeneric_GetRequiredService_Throws_WhenNoServiceRegistered()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(0);

            // Act + Assert
            AssertExtensions.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService(typeof(IFoo)),
                $"No service for type '{typeof(IFoo)}' has been registered.");
        }

        [Fact]
        public void ISupportRequiredService_NonGeneric_GetRequiredService_Throws_WhenNoServiceRegistered()
        {
            // Arrange
            var serviceProvider = new RequiredServiceSupportingProvider();

            // Act + Assert
            AssertExtensions.Throws<RankException>(() => serviceProvider.GetRequiredService(typeof(IFoo)));
        }

        [Fact]
        public void GetServices_Returns_AllServices()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(2);

            // Act
            var services = serviceProvider.GetServices<IFoo>();

            // Assert
            Assert.Contains(services, item => item is Foo1);
            Assert.Contains(services, item => item is Foo2);
            Assert.Equal(2, services.Count());
        }

        [Fact]
        public void NonGeneric_GetServices_Returns_AllServices()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(2);

            // Act
            var services = serviceProvider.GetServices(typeof(IFoo));

            // Assert
            Assert.Contains(services, item => item is Foo1);
            Assert.Contains(services, item => item is Foo2);
            Assert.Equal(2, services.Count());
        }

        [Fact]
        public void GetServices_Returns_SingleService()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(1);

            // Act
            var services = serviceProvider.GetServices<IFoo>();

            // Assert
            var item = Assert.Single(services);
            Assert.IsType<Foo1>(item);
        }

        [Fact]
        public void NonGeneric_GetServices_Returns_SingleService()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(1);

            // Act
            var services = serviceProvider.GetServices(typeof(IFoo));

            // Assert
            var item = Assert.Single(services);
            Assert.IsType<Foo1>(item);
        }

        [Fact]
        public void GetServices_Returns_CorrectTypes()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(4);

            // Act
            var services = serviceProvider.GetServices(typeof(IBar));

            // Assert
            foreach (var service in services)
            {
                Assert.IsAssignableFrom<IBar>(service);
            }
            Assert.Equal(2, services.Count());
        }

        [Fact]
        public void GetServices_Returns_EmptyArray_WhenNoServicesAvailable()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(0);

            // Act
            var services = serviceProvider.GetServices<IFoo>();

            // Assert
            Assert.Empty(services);
            Assert.IsType<IFoo[]>(services);
        }

        [Fact]
        public void NonGeneric_GetServices_Returns_EmptyArray_WhenNoServicesAvailable()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(0);

            // Act
            var services = serviceProvider.GetServices(typeof(IFoo));

            // Assert
            Assert.Empty(services);
            Assert.IsType<IFoo[]>(services);
        }

        [Fact]
        public void GetServices_WithBuildServiceProvider_Returns_EmptyList_WhenNoServicesAvailable()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IEnumerable<IFoo>>(new List<IFoo>());
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Act
            var services = serviceProvider.GetServices<IFoo>();

            // Assert
            Assert.Empty(services);
            Assert.IsType<List<IFoo>>(services);
        }

        [Fact]
        public void NonGeneric_GetServices_WithBuildServiceProvider_Returns_EmptyList_WhenNoServicesAvailable()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IEnumerable<IFoo>>(new List<IFoo>());
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Act
            var services = serviceProvider.GetServices(typeof(IFoo));

            // Assert
            Assert.Empty(services);
            Assert.IsType<List<IFoo>>(services);
        }

        [Fact]
        public async Task CreateAsyncScope_Returns_AsyncServiceScope_Wrapping_ServiceScope()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IFoo, Foo1>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            await using var scope = serviceProvider.CreateAsyncScope();

            // Act
            var service = scope.ServiceProvider.GetService<IFoo>();

            // Assert
            Assert.IsType<Foo1>(service);
        }

        [Fact]
        public async Task CreateAsyncScope_Returns_AsyncServiceScope_Wrapping_ServiceScope_For_IServiceScopeFactory()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<IFoo, Foo1>();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var factory = serviceProvider.GetService<IServiceScopeFactory>();

            await using var scope = factory.CreateAsyncScope();

            // Act
            var service = scope.ServiceProvider.GetService<IFoo>();

            // Assert
            Assert.IsType<Foo1>(service);
        }

        [Fact]
        public void GetServices_Returns_AllServices_After_Closed_Generic_Is_Cached()
        {
            // Arrange
            var serviceProvider = CreateTestServiceProvider(8);

            // Act
            var service = serviceProvider.GetService<IOpenGenericFoo<IFoo>>();
            var services = serviceProvider.GetServices<IOpenGenericFoo<IFoo>>();

            // Assert
            Assert.True(service is ClosedGenericFoo2);
            Assert.Contains(services, item => item is ClosedGenericFoo1);
            Assert.Contains(services, item => item is ClosedGenericFoo2);
            Assert.Contains(services, item => item is OpenGenericFoo1<IFoo>);
            Assert.Contains(services, item => item is OpenGenericFoo2<IFoo>);
            Assert.Equal(4, services.Count());
        }

        private static IServiceProvider CreateTestServiceProvider(int count)
        {
            var serviceCollection = new ServiceCollection();

            if (count > 0)
            {
                serviceCollection.AddTransient<IFoo, Foo1>();
            }

            if (count > 1)
            {
                serviceCollection.AddTransient<IFoo, Foo2>();
            }

            if (count > 2)
            {
                serviceCollection.AddTransient<IBar, Bar1>();
            }

            if (count > 3)
            {
                serviceCollection.AddTransient<IBar, Bar2>();
            }

            // Note that ClosedGenericFoos are registered before OpenGenericFoos to test the inverse order lookup of
            // descriptors for resolving enumerables
            if (count > 4)
            {
                serviceCollection.AddTransient<IOpenGenericFoo<IFoo>, ClosedGenericFoo1>();
            }

            if (count > 5)
            {
                serviceCollection.AddTransient<IOpenGenericFoo<IFoo>, ClosedGenericFoo2>();
            }

            if (count > 6)
            {
                serviceCollection.AddTransient(typeof(IOpenGenericFoo<>), typeof(OpenGenericFoo1<>));
            }

            if (count > 7)
            {
                serviceCollection.AddTransient(typeof(IOpenGenericFoo<>), typeof(OpenGenericFoo2<>));
            }

            return serviceCollection.BuildServiceProvider();
        }

        public interface IFoo { }

        public class Foo1 : IFoo { }

        public class Foo2 : IFoo { }

        public interface IBar { }

        public class Bar1 : IBar { }

        public class Bar2 : IBar { }

        public interface IOpenGenericFoo<T> where T : IFoo { }

        public class OpenGenericFoo1<T> : IOpenGenericFoo<T> where T : IFoo { }

        public class OpenGenericFoo2<T> : IOpenGenericFoo<T> where T : IFoo { }

        public class ClosedGenericFoo1 : IOpenGenericFoo<IFoo> { }

        public class ClosedGenericFoo2 : IOpenGenericFoo<IFoo> { }

        private class RequiredServiceSupportingProvider : IServiceProvider, ISupportRequiredService
        {
            object ISupportRequiredService.GetRequiredService(Type serviceType)
            {
                if (serviceType == typeof(IBar))
                {
                    return new Bar1();
                }

                throw new RankException();
            }

            object IServiceProvider.GetService(Type serviceType)
            {
                throw new NotSupportedException();
            }
        }
    }
}
