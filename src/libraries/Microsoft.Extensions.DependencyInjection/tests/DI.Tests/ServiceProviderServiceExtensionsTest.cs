// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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

            return serviceCollection.BuildServiceProvider();
        }

        public interface IFoo { }

        public class Foo1 : IFoo { }

        public class Foo2 : IFoo { }

        public interface IBar { }

        public class Bar1 : IBar { }

        public class Bar2 : IBar { }

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
