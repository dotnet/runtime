// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public abstract partial class DependencyInjectionSpecificationTests
    {
        public virtual bool SupportsIServiceProviderIsService => true;

        [Fact]
        public void ExplictServiceRegisterationWithIsService()
        {
            if (!SupportsIServiceProviderIsService)
            {
                return;
            }

            // Arrange
            var collection = new TestServiceCollection();
            collection.AddTransient(typeof(IFakeService), typeof(FakeService));
            var provider = CreateServiceProvider(collection);

            // Act
            var serviceProviderIsService = provider.GetService<IServiceProviderIsService>();

            // Assert
            Assert.NotNull(serviceProviderIsService);
            Assert.True(serviceProviderIsService.IsService(typeof(IFakeService)));
            Assert.False(serviceProviderIsService.IsService(typeof(FakeService)));
        }

        [Fact]
        public void OpenGenericsWithIsService()
        {
            if (!SupportsIServiceProviderIsService)
            {
                return;
            }

            // Arrange
            var collection = new TestServiceCollection();
            collection.AddTransient(typeof(IFakeOpenGenericService<>), typeof(FakeOpenGenericService<>));
            var provider = CreateServiceProvider(collection);

            // Act
            var serviceProviderIsService = provider.GetService<IServiceProviderIsService>();

            // Assert
            Assert.NotNull(serviceProviderIsService);
            Assert.True(serviceProviderIsService.IsService(typeof(IFakeOpenGenericService<IFakeService>)));
            Assert.False(serviceProviderIsService.IsService(typeof(IFakeOpenGenericService<>)));
        }

        [Fact]
        public void ClosedGenericsWithIsService()
        {
            if (!SupportsIServiceProviderIsService)
            {
                return;
            }

            // Arrange
            var collection = new TestServiceCollection();
            collection.AddTransient(typeof(IFakeOpenGenericService<IFakeService>), typeof(FakeOpenGenericService<IFakeService>));
            var provider = CreateServiceProvider(collection);

            // Act
            var serviceProviderIsService = provider.GetService<IServiceProviderIsService>();

            // Assert
            Assert.NotNull(serviceProviderIsService);
            Assert.True(serviceProviderIsService.IsService(typeof(IFakeOpenGenericService<IFakeService>)));
        }

        [Fact]
        public void IEnumerableWithIsServiceAlwaysReturnsTrue()
        {
            if (!SupportsIServiceProviderIsService)
            {
                return;
            }

            // Arrange
            var collection = new TestServiceCollection();
            collection.AddTransient(typeof(IFakeService), typeof(FakeService));
            var provider = CreateServiceProvider(collection);

            // Act
            var serviceProviderIsService = provider.GetService<IServiceProviderIsService>();

            // Assert
            Assert.NotNull(serviceProviderIsService);
            Assert.True(serviceProviderIsService.IsService(typeof(IEnumerable<IFakeService>)));
            Assert.True(serviceProviderIsService.IsService(typeof(IEnumerable<FakeService>)));
            Assert.False(serviceProviderIsService.IsService(typeof(IEnumerable<>)));
        }

        [Fact]
        public void BuiltInServicesWithIsServiceReturnsTrue()
        {
            if (!SupportsIServiceProviderIsService)
            {
                return;
            }

            // Arrange
            var collection = new TestServiceCollection();
            collection.AddTransient(typeof(IFakeService), typeof(FakeService));
            var provider = CreateServiceProvider(collection);

            // Act
            var serviceProviderIsService = provider.GetService<IServiceProviderIsService>();

            // Assert
            Assert.NotNull(serviceProviderIsService);
            Assert.True(serviceProviderIsService.IsService(typeof(IServiceProvider)));
            Assert.True(serviceProviderIsService.IsService(typeof(IServiceScopeFactory)));
            Assert.True(serviceProviderIsService.IsService(typeof(IServiceProviderIsService)));
        }
    }
}
