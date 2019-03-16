// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ServiceCollectionDescriptorExtensionsTest
    {
        [Fact]
        public void Add_AddsDescriptorToServiceDescriptors()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var descriptor = new ServiceDescriptor(typeof(IFakeService), new FakeService());

            // Act
            serviceCollection.Add(descriptor);

            // Assert
            var result = Assert.Single(serviceCollection);
            Assert.Same(result, descriptor);
        }

        [Fact]
        public void Add_AddsMultipleDescriptorToServiceDescriptors()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var descriptor1 = new ServiceDescriptor(typeof(IFakeService), new FakeService());
            var descriptor2 = new ServiceDescriptor(typeof(IFactoryService), typeof(TransientFactoryService), ServiceLifetime.Transient);

            // Act
            serviceCollection.Add(descriptor1);
            serviceCollection.Add(descriptor2);

            // Assert
            Assert.Equal(2, serviceCollection.Count);
            Assert.Equal(new[] { descriptor1, descriptor2 }, serviceCollection);
        }

        [Fact]
        public void ServiceDescriptors_AllowsRemovingPreviousRegisteredServices()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var descriptor1 = new ServiceDescriptor(typeof(IFakeService), new FakeService());
            var descriptor2 = new ServiceDescriptor(typeof(IFactoryService), typeof(TransientFactoryService), ServiceLifetime.Transient);

            // Act
            serviceCollection.Add(descriptor1);
            serviceCollection.Add(descriptor2);
            serviceCollection.Remove(descriptor1);

            // Assert
            var result = Assert.Single(serviceCollection);
            Assert.Same(result, descriptor2);
        }

        public static TheoryData<ServiceDescriptor, string> ServiceDescriptorToStringData = new TheoryData<ServiceDescriptor, string>()
        {
            {
                new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Scoped),
                "ServiceType: Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeService Lifetime: Scoped ImplementationType: Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeService"
            },
            {
                new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Singleton),
                "ServiceType: Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeService Lifetime: Singleton ImplementationType: Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeService"
            },
            {
                new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient),
                "ServiceType: Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeService Lifetime: Transient ImplementationType: Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeService"
            },
            {
                new ServiceDescriptor(typeof(IFakeService), new FakeServiceToString()),
                "ServiceType: Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeService Lifetime: Singleton ImplementationInstance: [FakeServiceToString]"
            },
            {
                new ServiceDescriptor(typeof(IFakeService), CreateFakeService, ServiceLifetime.Scoped),
                "ServiceType: Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeService Lifetime: Scoped ImplementationFactory: Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeService CreateFakeService(System.IServiceProvider)"
            },
            {
                new ServiceDescriptor(typeof(IFakeService), CreateFakeService, ServiceLifetime.Singleton),
                "ServiceType: Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeService Lifetime: Singleton ImplementationFactory: Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeService CreateFakeService(System.IServiceProvider)"
            },
            {
                new ServiceDescriptor(typeof(IFakeService), CreateFakeService, ServiceLifetime.Transient),
                "ServiceType: Microsoft.Extensions.DependencyInjection.Specification.Fakes.IFakeService Lifetime: Transient ImplementationFactory: Microsoft.Extensions.DependencyInjection.Specification.Fakes.FakeService CreateFakeService(System.IServiceProvider)"
            },
        };

        [Theory]
        [MemberData(nameof(ServiceDescriptorToStringData))]
        public void ServiceDescriptor_ToString(ServiceDescriptor descriptor, string expectedString)
        {
            Assert.Equal(expectedString, descriptor.ToString());
        }

        private static FakeService CreateFakeService(IServiceProvider provider)
        {
            return new FakeService();
        }

        private class FakeServiceToString : IFakeService
        {
            public override string ToString()
            {
                return "[FakeServiceToString]";
            }
        }
    }
}