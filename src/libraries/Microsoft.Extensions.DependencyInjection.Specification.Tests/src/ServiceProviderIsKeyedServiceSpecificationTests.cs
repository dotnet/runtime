// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public abstract partial class KeyedDependencyInjectionSpecificationTests
    {
        public virtual bool SupportsIServiceProviderIsKeyedService => true;

        [Fact]
        public void ExplicitServiceRegistrationWithIsKeyedService()
        {
            if (!SupportsIServiceProviderIsKeyedService)
            {
                return;
            }

            // Arrange
            var key = new object();
            var collection = new TestServiceCollection();
            collection.AddKeyedTransient(typeof(IFakeService), key, typeof(FakeService));
            var provider = CreateServiceProvider(collection);

            // Act
            var serviceProviderIsService = provider.GetService<IServiceProviderIsKeyedService>();

            // Assert
            Assert.NotNull(serviceProviderIsService);
            Assert.True(serviceProviderIsService.IsKeyedService(typeof(IFakeService), key));
            Assert.False(serviceProviderIsService.IsKeyedService(typeof(FakeService), new object()));
        }

        [Fact]
        public void OpenGenericsWithIsKeyedService()
        {
            if (!SupportsIServiceProviderIsKeyedService)
            {
                return;
            }

            // Arrange
            var key = new object();
            var collection = new TestServiceCollection();
            collection.AddKeyedTransient(typeof(IFakeOpenGenericService<>), key, typeof(FakeOpenGenericService<>));
            var provider = CreateServiceProvider(collection);

            // Act
            var serviceProviderIsService = provider.GetService<IServiceProviderIsKeyedService>();

            // Assert
            Assert.NotNull(serviceProviderIsService);
            Assert.True(serviceProviderIsService.IsKeyedService(typeof(IFakeOpenGenericService<IFakeService>), key));
            Assert.False(serviceProviderIsService.IsKeyedService(typeof(IFakeOpenGenericService<>), new object()));
        }

        [Fact]
        public void ClosedGenericsWithIsKeyedService()
        {
            if (!SupportsIServiceProviderIsKeyedService)
            {
                return;
            }

            // Arrange
            var key = new object();
            var collection = new TestServiceCollection();
            collection.AddKeyedTransient(typeof(IFakeOpenGenericService<IFakeService>), key, typeof(FakeOpenGenericService<IFakeService>));
            var provider = CreateServiceProvider(collection);

            // Act
            var serviceProviderIsService = provider.GetService<IServiceProviderIsKeyedService>();

            // Assert
            Assert.NotNull(serviceProviderIsService);
            Assert.True(serviceProviderIsService.IsKeyedService(typeof(IFakeOpenGenericService<IFakeService>), key));
        }

        [Fact]
        public void IEnumerableWithIsKeyedServiceAlwaysReturnsTrue()
        {
            if (!SupportsIServiceProviderIsKeyedService)
            {
                return;
            }

            // Arrange
            var key = new object();
            var collection = new TestServiceCollection();
            collection.AddKeyedTransient(typeof(IFakeService), key, typeof(FakeService));
            var provider = CreateServiceProvider(collection);

            // Act
            var serviceProviderIsService = provider.GetService<IServiceProviderIsKeyedService>();

            // Assert
            Assert.NotNull(serviceProviderIsService);
            Assert.True(serviceProviderIsService.IsKeyedService(typeof(IEnumerable<IFakeService>), key));
            Assert.True(serviceProviderIsService.IsKeyedService(typeof(IEnumerable<FakeService>), key));
            Assert.False(serviceProviderIsService.IsKeyedService(typeof(IEnumerable<>), new object()));
        }

        [Fact]
        public void NonKeyedServiceWithIsKeyedService()
        {
            if (!SupportsIServiceProviderIsKeyedService)
            {
                return;
            }

            // Arrange
            var collection = new TestServiceCollection();
            collection.AddKeyedTransient(typeof(IFakeService), null, typeof(FakeService));
            var provider = CreateServiceProvider(collection);

            // Act
            var serviceProviderIsService = provider.GetService<IServiceProviderIsKeyedService>();

            // Assert
            Assert.NotNull(serviceProviderIsService);
            Assert.True(serviceProviderIsService.IsKeyedService(typeof(IFakeService), null));
        }
    }
}
