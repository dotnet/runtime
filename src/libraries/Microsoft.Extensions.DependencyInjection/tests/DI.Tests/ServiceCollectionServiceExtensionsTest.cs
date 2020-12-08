// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ServiceCollectionServiceExtensionsTest
    {
        private static readonly FakeService _instance = new FakeService();

        public static TheoryData AddImplementationTypeData
        {
            get
            {
                var serviceType = typeof(IFakeService);
                var implementationType = typeof(FakeService);
                return new TheoryData<Action<IServiceCollection>, Type, Type, ServiceLifetime>
                {
                    { collection => collection.AddTransient(serviceType, implementationType), serviceType, implementationType, ServiceLifetime.Transient },
                    { collection => collection.AddTransient<IFakeService, FakeService>(), serviceType, implementationType, ServiceLifetime.Transient },
                    { collection => collection.AddTransient<IFakeService>(), serviceType, serviceType, ServiceLifetime.Transient },
                    { collection => collection.AddTransient(implementationType), implementationType, implementationType, ServiceLifetime.Transient },

                    { collection => collection.AddScoped(serviceType, implementationType), serviceType, implementationType, ServiceLifetime.Scoped },
                    { collection => collection.AddScoped<IFakeService, FakeService>(), serviceType, implementationType, ServiceLifetime.Scoped },
                    { collection => collection.AddScoped<IFakeService>(), serviceType, serviceType, ServiceLifetime.Scoped },
                    { collection => collection.AddScoped(implementationType), implementationType, implementationType, ServiceLifetime.Scoped },

                    { collection => collection.AddSingleton(serviceType, implementationType), serviceType, implementationType, ServiceLifetime.Singleton },
                    { collection => collection.AddSingleton<IFakeService, FakeService>(), serviceType, implementationType, ServiceLifetime.Singleton },
                    { collection => collection.AddSingleton<IFakeService>(), serviceType, serviceType, ServiceLifetime.Singleton },
                    { collection => collection.AddSingleton(implementationType), implementationType, implementationType, ServiceLifetime.Singleton },
                };
            }
        }

        [Theory]
        [MemberData(nameof(AddImplementationTypeData))]
        public void AddWithTypeAddsServiceWithRightLifecyle(Action<IServiceCollection> addTypeAction,
                                                            Type expectedServiceType,
                                                            Type expectedImplementationType,
                                                            ServiceLifetime lifeCycle)
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            addTypeAction(collection);

            // Assert
            var descriptor = Assert.Single(collection);
            Assert.Equal(expectedServiceType, descriptor.ServiceType);
            Assert.Equal(expectedImplementationType, descriptor.ImplementationType);
            Assert.Equal(lifeCycle, descriptor.Lifetime);
        }

        public static TheoryData AddImplementationFactoryData
        {
            get
            {
                var serviceType = typeof(IFakeService);
                var implementationType = typeof(FakeService);
                var objectType = typeof(object);

                return new TheoryData<Action<IServiceCollection>, Type, Type, ServiceLifetime>
                {
                    { collection => collection.AddTransient(serviceType, s => new FakeService()), serviceType, objectType, ServiceLifetime.Transient },
                    { collection => collection.AddTransient<IFakeService>(s => new FakeService()), serviceType, serviceType, ServiceLifetime.Transient },
                    { collection => collection.AddTransient<IFakeService, FakeService>(s => new FakeService()), serviceType, implementationType, ServiceLifetime.Transient },

                    { collection => collection.AddScoped(serviceType, s => new FakeService()), serviceType, objectType, ServiceLifetime.Scoped },
                    { collection => collection.AddScoped<IFakeService>(s => new FakeService()), serviceType, serviceType, ServiceLifetime.Scoped },
                    { collection => collection.AddScoped<IFakeService, FakeService>(s => new FakeService()), serviceType, implementationType, ServiceLifetime.Scoped },

                    { collection => collection.AddSingleton(serviceType, s => new FakeService()), serviceType, objectType, ServiceLifetime.Singleton },
                    { collection => collection.AddSingleton<IFakeService>(s => new FakeService()), serviceType, serviceType, ServiceLifetime.Singleton },
                    { collection => collection.AddSingleton<IFakeService, FakeService>(s => new FakeService()), serviceType, implementationType, ServiceLifetime.Singleton },
                };
            }
        }

        [Theory]
        [MemberData(nameof(AddImplementationFactoryData))]
        public void AddWithFactoryAddsServiceWithRightLifecyle(
            Action<IServiceCollection> addAction,
            Type serviceType,
            Type implementationType,
            ServiceLifetime lifeCycle)
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            addAction(collection);

            // Assert
            var descriptor = Assert.Single(collection);
            Assert.Equal(serviceType, descriptor.ServiceType);
            Assert.Equal(implementationType, descriptor.GetImplementationType());
            Assert.Equal(lifeCycle, descriptor.Lifetime);
        }

        public static TheoryData AddSingletonData
        {
            get
            {
                return new TheoryData<Action<IServiceCollection>>
                {
                    { collection => collection.AddSingleton<IFakeService>(_instance) },
                    { collection => collection.AddSingleton(typeof(IFakeService), _instance) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(AddSingletonData))]
        public void AddSingleton_AddsWithSingletonLifecycle(Action<IServiceCollection> addAction)
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            addAction(collection);

            // Assert
            var descriptor = Assert.Single(collection);
            Assert.Equal(typeof(IFakeService), descriptor.ServiceType);
            Assert.Same(_instance, descriptor.ImplementationInstance);
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }

        [Theory]
        [MemberData(nameof(AddSingletonData))]
        public void TryAddNoOpFailsIfExists(Action<IServiceCollection> addAction)
        {
            // Arrange
            var collection = new ServiceCollection();
            addAction(collection);
            var d = ServiceDescriptor.Transient<IFakeService, FakeService>();

            // Act
            collection.TryAdd(d);

            // Assert
            var descriptor = Assert.Single(collection);
            Assert.Equal(typeof(IFakeService), descriptor.ServiceType);
            Assert.Same(_instance, descriptor.ImplementationInstance);
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }

        public static TheoryData TryAddImplementationTypeData
        {
            get
            {
                var serviceType = typeof(IFakeService);
                var implementationType = typeof(FakeService);
                return new TheoryData<Action<IServiceCollection>, Type, Type, ServiceLifetime>
                {
                    { collection => collection.TryAddTransient(serviceType, implementationType), serviceType, implementationType, ServiceLifetime.Transient },
                    { collection => collection.TryAddTransient<IFakeService, FakeService>(), serviceType, implementationType, ServiceLifetime.Transient },
                    { collection => collection.TryAddTransient<IFakeService>(), serviceType, serviceType, ServiceLifetime.Transient },
                    { collection => collection.TryAddTransient(implementationType), implementationType, implementationType, ServiceLifetime.Transient },

                    { collection => collection.TryAddScoped(serviceType, implementationType), serviceType, implementationType, ServiceLifetime.Scoped },
                    { collection => collection.TryAddScoped<IFakeService, FakeService>(), serviceType, implementationType, ServiceLifetime.Scoped },
                    { collection => collection.TryAddScoped<IFakeService>(), serviceType, serviceType, ServiceLifetime.Scoped },
                    { collection => collection.TryAddScoped(implementationType), implementationType, implementationType, ServiceLifetime.Scoped },

                    { collection => collection.TryAddSingleton(serviceType, implementationType), serviceType, implementationType, ServiceLifetime.Singleton },
                    { collection => collection.TryAddSingleton<IFakeService, FakeService>(), serviceType, implementationType, ServiceLifetime.Singleton },
                    { collection => collection.TryAddSingleton<IFakeService>(), serviceType, serviceType, ServiceLifetime.Singleton },
                    { collection => collection.TryAddSingleton(implementationType), implementationType, implementationType, ServiceLifetime.Singleton },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TryAddImplementationTypeData))]
        public void TryAdd_WithType_AddsService(
            Action<IServiceCollection> addAction,
            Type expectedServiceType,
            Type expectedImplementationType,
            ServiceLifetime expectedLifetime)
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            addAction(collection);

            // Assert
            var descriptor = Assert.Single(collection);
            Assert.Equal(expectedServiceType, descriptor.ServiceType);
            Assert.Same(expectedImplementationType, descriptor.ImplementationType);
            Assert.Equal(expectedLifetime, descriptor.Lifetime);
        }

        [Theory]
        [MemberData(nameof(TryAddImplementationTypeData))]
        public void TryAdd_WithType_DoesNotAddDuplicate(
            Action<IServiceCollection> addAction,
            Type expectedServiceType,
            // Test verifies that descriptor is not added so we don't need to assert it's properties
#pragma warning disable xUnit1026
            Type expectedImplementationType,
            ServiceLifetime expectedLifetime
#pragma warning restore xUnit1026
            )
        {
            // Arrange
            var collection = new ServiceCollection
            {
                ServiceDescriptor.Transient(expectedServiceType, expectedServiceType)
            };

            // Act
            addAction(collection);

            // Assert
            var descriptor = Assert.Single(collection);
            Assert.Equal(expectedServiceType, descriptor.ServiceType);
            Assert.Same(expectedServiceType, descriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        }

        [Fact]
        public void TryAddIfMissingActuallyAdds()
        {
            // Arrange
            var collection = new ServiceCollection();
            var d = ServiceDescriptor.Transient<IFakeService, FakeService>();

            // Act
            collection.TryAdd(d);

            // Assert
            var descriptor = Assert.Single(collection);
            Assert.Equal(typeof(IFakeService), descriptor.ServiceType);
            Assert.Null(descriptor.ImplementationInstance);
            Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        }

        public static TheoryData TryAddEnumerableImplementationTypeData
        {
            get
            {
                var serviceType = typeof(IFakeService);
                var implementationType = typeof(FakeService);
                return new TheoryData<ServiceDescriptor, Type, Type, ServiceLifetime>
                {
                    { ServiceDescriptor.Transient<IFakeService, FakeService>(), serviceType, implementationType, ServiceLifetime.Transient },
                    { ServiceDescriptor.Transient<IFakeService, FakeService>(s => new FakeService()), serviceType, implementationType, ServiceLifetime.Transient },

                    { ServiceDescriptor.Scoped<IFakeService, FakeService>(), serviceType, implementationType, ServiceLifetime.Scoped },
                    { ServiceDescriptor.Scoped<IFakeService, FakeService>(s => new FakeService()), serviceType, implementationType, ServiceLifetime.Scoped },

                    { ServiceDescriptor.Singleton<IFakeService, FakeService>(), serviceType, implementationType, ServiceLifetime.Singleton },
                    { ServiceDescriptor.Singleton<IFakeService, FakeService >(s => new FakeService()), serviceType, implementationType, ServiceLifetime.Singleton },

                    { ServiceDescriptor.Singleton<IFakeService>(_instance), serviceType, implementationType, ServiceLifetime.Singleton },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TryAddEnumerableImplementationTypeData))]
        public void TryAddEnumerable_AddsService(
            ServiceDescriptor descriptor,
            Type expectedServiceType,
            Type expectedImplementationType,
            ServiceLifetime expectedLifetime)
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            collection.TryAddEnumerable(descriptor);

            // Assert
            var d = Assert.Single(collection);
            Assert.Equal(expectedServiceType, d.ServiceType);
            Assert.Equal(expectedImplementationType, d.GetImplementationType());
            Assert.Equal(expectedLifetime, d.Lifetime);
        }


        [Theory]
        [MemberData(nameof(TryAddEnumerableImplementationTypeData))]
        public void TryAddEnumerable_DoesNotAddDuplicate(
            ServiceDescriptor descriptor,
            Type expectedServiceType,
            Type expectedImplementationType,
            ServiceLifetime expectedLifetime)
        {
            // Arrange
            var collection = new ServiceCollection();
            collection.TryAddEnumerable(descriptor);

            // Act
            collection.TryAddEnumerable(descriptor);

            // Assert
            var d = Assert.Single(collection);
            Assert.Equal(expectedServiceType, d.ServiceType);
            Assert.Equal(expectedImplementationType, d.GetImplementationType());
            Assert.Equal(expectedLifetime, d.Lifetime);
        }

        public static TheoryData TryAddEnumerableInvalidImplementationTypeData
        {
            get
            {
                var serviceType = typeof(IFakeService);
                var implementationType = typeof(FakeService);
                var objectType = typeof(object);

                return new TheoryData<ServiceDescriptor, Type, Type>
                {
                    { ServiceDescriptor.Transient<IFakeService>(s => new FakeService()), serviceType, serviceType },
                    { ServiceDescriptor.Transient(serviceType, s => new FakeService()), serviceType, objectType },

                    { ServiceDescriptor.Scoped<IFakeService>(s => new FakeService()), serviceType, serviceType },
                    { ServiceDescriptor.Scoped(serviceType, s => new FakeService()), serviceType, objectType },

                    { ServiceDescriptor.Singleton<IFakeService>(s => new FakeService()), serviceType, serviceType },
                    { ServiceDescriptor.Singleton(serviceType, s => new FakeService()), serviceType, objectType },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TryAddEnumerableInvalidImplementationTypeData))]
        public void TryAddEnumerable_ThrowsWhenAddingIndistinguishableImplementationType(
            ServiceDescriptor descriptor,
            Type serviceType,
            Type implementationType)
        {
            // Arrange
            var collection = new ServiceCollection();

            AssertExtensions.ThrowsContains<ArgumentException>(() => collection.TryAddEnumerable(descriptor), 
                string.Format(@"Implementation type cannot be '{0}' because it is indistinguishable from other services registered for '{1}'.", implementationType, serviceType));
        }

        [Fact]
        public void AddSequence_AddsServicesToCollection()
        {
            // Arrange
            var collection = new ServiceCollection();
            var descriptor1 = new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient);
            var descriptor2 = new ServiceDescriptor(typeof(IFakeOuterService), typeof(FakeOuterService), ServiceLifetime.Transient);
            var descriptors = new[] { descriptor1, descriptor2 };

            // Act
            var result = collection.Add(descriptors);

            // Assert
            Assert.Equal(descriptors, collection);
        }

        [Fact]
        public void Replace_AddsServiceIfServiceTypeIsNotRegistered()
        {
            // Arrange
            var collection = new ServiceCollection();
            var descriptor1 = new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient);
            var descriptor2 = new ServiceDescriptor(typeof(IFakeOuterService), typeof(FakeOuterService), ServiceLifetime.Transient);
            collection.Add(descriptor1);

            // Act
            collection.Replace(descriptor2);

            // Assert
            Assert.Equal(new[] { descriptor1, descriptor2 }, collection);
        }

        [Fact]
        public void Replace_ReplacesFirstServiceWithMatchingServiceType()
        {
            // Arrange
            var collection = new ServiceCollection();
            var descriptor1 = new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient);
            var descriptor2 = new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient);
            collection.Add(descriptor1);
            collection.Add(descriptor2);
            var descriptor3 = new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Singleton);

            // Act
            collection.Replace(descriptor3);

            // Assert
            Assert.Equal(new[] { descriptor2, descriptor3 }, collection);
        }

        [Fact]
        public void RemoveAll_RemovesAllServicesWithMatchingServiceType()
        {
            // Arrange
            var descriptor = new ServiceDescriptor(typeof(IFakeServiceInstance), typeof(FakeService), ServiceLifetime.Transient);
            var collection = new ServiceCollection
            {
                descriptor,
                new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient)
            };

            // Act
            collection.RemoveAll<IFakeService>();

            // Assert
            Assert.Equal(new[] { descriptor }, collection);
        }
    }
}
