// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Microsoft.Extensions.DependencyInjection.Tests;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ServiceCollectionKeyedServiceExtensionsTest
    {
        private static readonly FakeService _instance = new FakeService();

        public static TheoryData AddImplementationTypeData
        {
            get
            {
                var serviceType = typeof(IFakeService);
                var implementationType = typeof(FakeService);
                return new TheoryData<Action<IServiceCollection>, Type, object, Type, ServiceLifetime>
                {
                    { collection => collection.AddKeyedTransient(serviceType, "some-key-1", implementationType), serviceType, "some-key-1", implementationType, ServiceLifetime.Transient },
                    { collection => collection.AddKeyedTransient<IFakeService, FakeService>("some-key-2"), serviceType, "some-key-2", implementationType, ServiceLifetime.Transient },
                    { collection => collection.AddKeyedTransient<IFakeService>("some-key-3"), serviceType, "some-key-3", serviceType, ServiceLifetime.Transient },
                    { collection => collection.AddKeyedTransient(implementationType, "some-key-4"), implementationType, "some-key-4", implementationType, ServiceLifetime.Transient },

                    { collection => collection.AddKeyedScoped(serviceType, "some-key-5", implementationType), serviceType, "some-key-5", implementationType, ServiceLifetime.Scoped },
                    { collection => collection.AddKeyedScoped<IFakeService, FakeService>("some-key-6"), serviceType, "some-key-6", implementationType, ServiceLifetime.Scoped },
                    { collection => collection.AddKeyedScoped<IFakeService>("some-key-7"), serviceType, "some-key-7", serviceType, ServiceLifetime.Scoped },
                    { collection => collection.AddKeyedScoped(implementationType, "some-key-8"), implementationType, "some-key-8", implementationType, ServiceLifetime.Scoped },

                    { collection => collection.AddKeyedSingleton(serviceType, "some-key-9", implementationType), serviceType, "some-key-9", implementationType, ServiceLifetime.Singleton },
                    { collection => collection.AddKeyedSingleton<IFakeService, FakeService>("some-key-10"), serviceType, "some-key-10", implementationType, ServiceLifetime.Singleton },
                    { collection => collection.AddKeyedSingleton<IFakeService>("some-key-12"), serviceType, "some-key-12", serviceType, ServiceLifetime.Singleton },
                    { collection => collection.AddKeyedSingleton(serviceType: implementationType, "some-key-13"), implementationType, "some-key-13", implementationType, ServiceLifetime.Singleton },
                };
            }
        }

        [Theory]
        [MemberData(nameof(AddImplementationTypeData))]
        public void AddWithTypeAddsServiceWithRightLifecyle(Action<IServiceCollection> addTypeAction,
                                                            Type expectedServiceType,
                                                            object expectedKey,
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
            Assert.Equal(expectedKey, descriptor.ServiceKey);
            Assert.Equal(expectedImplementationType, descriptor.KeyedImplementationType);
            Assert.Equal(lifeCycle, descriptor.Lifetime);
        }

        public static TheoryData AddImplementationFactoryData
        {
            get
            {
                var serviceType = typeof(IFakeService);
                var implementationType = typeof(FakeService);
                var objectType = typeof(object);

                return new TheoryData<Action<IServiceCollection>, Type, object, Type, ServiceLifetime>
                {
                    { collection => collection.AddKeyedTransient(serviceType, "some-key-1", (s,k) => new FakeService()), serviceType, "some-key-1", objectType, ServiceLifetime.Transient },
                    { collection => collection.AddKeyedTransient<IFakeService>("some-key-2", (s,k) => new FakeService()), serviceType, "some-key-2", serviceType, ServiceLifetime.Transient },
                    { collection => collection.AddKeyedTransient<IFakeService, FakeService>("some-key-3", (s,k) => new FakeService()), serviceType, "some-key-3", implementationType, ServiceLifetime.Transient },

                    { collection => collection.AddKeyedScoped(serviceType, "some-key-4", (s,k) => new FakeService()), serviceType, "some-key-4", objectType, ServiceLifetime.Scoped },
                    { collection => collection.AddKeyedScoped<IFakeService>("some-key-5", (s,k) => new FakeService()), serviceType, "some-key-5", serviceType, ServiceLifetime.Scoped },
                    { collection => collection.AddKeyedScoped<IFakeService, FakeService>("some-key-6", (s,k) => new FakeService()), serviceType, "some-key-6", implementationType, ServiceLifetime.Scoped },

                    { collection => collection.AddKeyedSingleton(serviceType, "some-key-7", (s,k) => new FakeService()), serviceType, "some-key-7", objectType, ServiceLifetime.Singleton },
                    { collection => collection.AddKeyedSingleton<IFakeService>("some-key-8", (s,k) => new FakeService()), serviceType, "some-key-8", serviceType, ServiceLifetime.Singleton },
                    { collection => collection.AddKeyedSingleton<IFakeService, FakeService>("some-key-9", (s,k) => new FakeService()), serviceType, "some-key-9", implementationType, ServiceLifetime.Singleton },
                };
            }
        }

        [Theory]
        [MemberData(nameof(AddImplementationFactoryData))]
        public void AddWithFactoryAddsServiceWithRightLifecyle(
            Action<IServiceCollection> addAction,
            Type serviceType,
            object? serviceKey,
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
            Assert.Equal(serviceKey, descriptor.ServiceKey);
            Assert.Equal(implementationType, descriptor.GetImplementationType());
            Assert.Equal(lifeCycle, descriptor.Lifetime);
        }

        public static TheoryData AddSingletonData
        {
            get
            {
                return new TheoryData<Action<IServiceCollection>>
                {
                    { collection => collection.AddKeyedSingleton<IFakeService>("service", _instance) },
                    { collection => collection.AddKeyedSingleton(typeof(IFakeService), "service", _instance) },
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
            Assert.Equal("service", descriptor.ServiceKey.ToString());
            Assert.Same(_instance, descriptor.KeyedImplementationInstance);
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }

        [Theory]
        [MemberData(nameof(AddSingletonData))]
        public void TryAddNoOpFailsIfExists(Action<IServiceCollection> addAction)
        {
            // Arrange
            var collection = new ServiceCollection();
            addAction(collection);
            var d = ServiceDescriptor.KeyedTransient<IFakeService, FakeService>("service");

            // Act
            collection.TryAdd(d);

            // Assert
            var descriptor = Assert.Single(collection);
            Assert.Equal(typeof(IFakeService), descriptor.ServiceType);
            Assert.Same(_instance, descriptor.KeyedImplementationInstance);
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }

        public static TheoryData TryAddImplementationTypeData
        {
            get
            {
                var serviceType = typeof(IFakeService);
                var implementationType = typeof(FakeService);
                return new TheoryData<Action<IServiceCollection>, Type, object, Type, ServiceLifetime>
                {
                    { collection => collection.TryAddKeyedTransient(serviceType, "key-1", implementationType), serviceType, "key-1", implementationType, ServiceLifetime.Transient },
                    { collection => collection.TryAddKeyedTransient<IFakeService, FakeService>("key-2"), serviceType, "key-2", implementationType, ServiceLifetime.Transient },
                    { collection => collection.TryAddKeyedTransient<IFakeService>("key-3"), serviceType, "key-3", serviceType, ServiceLifetime.Transient },
                    { collection => collection.TryAddKeyedTransient(implementationType, "key-4"), implementationType, "key-4", implementationType, ServiceLifetime.Transient },
                    { collection => collection.TryAddKeyedTransient(implementationType, 9), implementationType, 9, implementationType, ServiceLifetime.Transient },

                    { collection => collection.TryAddKeyedScoped(serviceType, "key-1", implementationType), serviceType, "key-1", implementationType, ServiceLifetime.Scoped },
                    { collection => collection.TryAddKeyedScoped<IFakeService, FakeService>("key-2"), serviceType, "key-2", implementationType, ServiceLifetime.Scoped },
                    { collection => collection.TryAddKeyedScoped<IFakeService>("key-3"), serviceType, "key-3", serviceType, ServiceLifetime.Scoped },
                    { collection => collection.TryAddKeyedScoped(implementationType, "key-4"), implementationType, "key-4", implementationType, ServiceLifetime.Scoped },

                    { collection => collection.TryAddKeyedSingleton(serviceType, "key-5", implementationType), serviceType, "key-5", implementationType, ServiceLifetime.Singleton },
                    { collection => collection.TryAddKeyedSingleton<IFakeService, FakeService>("key-6"), serviceType, "key-6", implementationType, ServiceLifetime.Singleton },
                    { collection => collection.TryAddKeyedSingleton<IFakeService>("key-7"), serviceType, "key-7", serviceType, ServiceLifetime.Singleton },
                    { collection => collection.TryAddKeyedSingleton(service: implementationType, "key-8"), implementationType, "key-8", implementationType, ServiceLifetime.Singleton },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TryAddImplementationTypeData))]
        public void TryAdd_WithType_AddsService(
            Action<IServiceCollection> addAction,
            Type expectedServiceType,
            object expectedServiceKey,
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
            Assert.Equal(expectedServiceKey, descriptor.ServiceKey);
            Assert.Same(expectedImplementationType, descriptor.KeyedImplementationType);
            Assert.Equal(expectedLifetime, descriptor.Lifetime);
        }

        [Theory]
        [MemberData(nameof(TryAddImplementationTypeData))]
        public void TryAdd_WithType_DoesNotAddDuplicate(
            Action<IServiceCollection> addAction,
            Type expectedServiceType,
            object expectedServiceKey,
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
                ServiceDescriptor.KeyedTransient(expectedServiceType, expectedServiceKey, expectedServiceType)
            };

            // Act
            addAction(collection);

            // Assert
            var descriptor = Assert.Single(collection);
            Assert.Equal(expectedServiceType, descriptor.ServiceType);
            Assert.Same(expectedServiceType, descriptor.KeyedImplementationType);
            Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        }

        [Fact]
        public void TryAddIfMissingActuallyAdds()
        {
            // Arrange
            var collection = new ServiceCollection();
            var key = new object();
            var d = ServiceDescriptor.KeyedTransient<IFakeService, FakeService>(key);

            // Act
            collection.TryAdd(d);

            // Assert
            var descriptor = Assert.Single(collection);
            Assert.Equal(typeof(IFakeService), descriptor.ServiceType);
            Assert.Equal(key, descriptor.ServiceKey);
            Assert.Null(descriptor.KeyedImplementationInstance);
            Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        }

        public static TheoryData TryAddEnumerableImplementationTypeData
        {
            get
            {
                var serviceType = typeof(IFakeService);
                var implementationType = typeof(FakeService);
                return new TheoryData<ServiceDescriptor, Type, object, Type, ServiceLifetime>
                {
                    { ServiceDescriptor.KeyedTransient<IFakeService, FakeService>("service1"), serviceType, "service1", implementationType, ServiceLifetime.Transient },
                    { ServiceDescriptor.KeyedTransient<IFakeService, FakeService>("service2", (s,k) => new FakeService()), serviceType, "service2", implementationType, ServiceLifetime.Transient },

                    { ServiceDescriptor.KeyedScoped<IFakeService, FakeService>("service3"), serviceType, "service3", implementationType, ServiceLifetime.Scoped },
                    { ServiceDescriptor.KeyedScoped<IFakeService, FakeService>("service4", (s,k) => new FakeService()), serviceType, "service4", implementationType, ServiceLifetime.Scoped },

                    { ServiceDescriptor.KeyedSingleton<IFakeService, FakeService>("service5"), serviceType, "service5", implementationType, ServiceLifetime.Singleton },
                    { ServiceDescriptor.KeyedSingleton<IFakeService, FakeService>("service6", (s,k) => new FakeService()), serviceType, "service6", implementationType, ServiceLifetime.Singleton },

                    { ServiceDescriptor.KeyedSingleton<IFakeService>("service6", _instance), serviceType, "service6", implementationType, ServiceLifetime.Singleton },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TryAddEnumerableImplementationTypeData))]
        public void TryAddEnumerable_AddsService(
            ServiceDescriptor descriptor,
            Type expectedServiceType,
            object expectedKey,
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
            Assert.Equal(expectedKey, d.ServiceKey);
            Assert.Equal(expectedImplementationType, d.GetImplementationType());
            Assert.Equal(expectedLifetime, d.Lifetime);
        }


        [Theory]
        [MemberData(nameof(TryAddEnumerableImplementationTypeData))]
        public void TryAddEnumerable_DoesNotAddDuplicate(
            ServiceDescriptor descriptor,
            Type expectedServiceType,
            object expectedKey,
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
            Assert.Equal(expectedKey, d.ServiceKey);
            Assert.Equal(expectedImplementationType, d.GetImplementationType());
            Assert.Equal(expectedLifetime, d.Lifetime);
        }

        [Fact]
        public void TryAddEnumerable_DoesNotAddDuplicateWhenKeyIsInt()
        {
            // Arrange
            var collection = new ServiceCollection();
            var descriptor1 = ServiceDescriptor.KeyedTransient<IFakeService, FakeService>(1);
            collection.TryAddEnumerable(descriptor1);
            var descriptor2 = ServiceDescriptor.KeyedTransient<IFakeService, FakeService>(1);

            // Act
            collection.TryAddEnumerable(descriptor2);

            // Assert
            var d = Assert.Single(collection);
            Assert.Same(descriptor1, d);
        }

        [Fact]
        public void TryAddEnumerable_DoesNotAddDuplicateWhenKeyIsString()
        {
            // Arrange
            var collection = new ServiceCollection();
            var descriptor1 = ServiceDescriptor.KeyedTransient<IFakeService, FakeService>("service1");
            collection.TryAddEnumerable(descriptor1);
            var descriptor2 = ServiceDescriptor.KeyedTransient<IFakeService, FakeService>("service1");

            // Act
            collection.TryAddEnumerable(descriptor2);

            // Assert
            var d = Assert.Single(collection);
            Assert.Same(descriptor1, d);
        }

        public static TheoryData TryAddEnumerableInvalidImplementationTypeData
        {
            get
            {
                var serviceType = typeof(IFakeService);
                var key = new object();
                var implementationType = typeof(FakeService);
                var objectType = typeof(object);

                return new TheoryData<ServiceDescriptor, Type, Type>
                {
                    { ServiceDescriptor.KeyedTransient<IFakeService>(key, (s,k) => new FakeService()), serviceType, serviceType },
                    { ServiceDescriptor.KeyedTransient(serviceType, key, (s,k) => new FakeService()), serviceType, objectType },

                    { ServiceDescriptor.KeyedScoped<IFakeService>(key, (s,k) => new FakeService()), serviceType, serviceType },
                    { ServiceDescriptor.KeyedScoped(serviceType, key, (s,k) => new FakeService()), serviceType, objectType },

                    { ServiceDescriptor.KeyedSingleton<IFakeService>(key, (s,k) => new FakeService()), serviceType, serviceType },
                    { ServiceDescriptor.KeyedSingleton(serviceType, key, (s,k) => new FakeService()), serviceType, objectType },
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
            var descriptor1 = new ServiceDescriptor(typeof(IFakeService), "key1", typeof(FakeService), ServiceLifetime.Transient);
            var descriptor2 = new ServiceDescriptor(typeof(IFakeOuterService), "key1", typeof(FakeOuterService), ServiceLifetime.Transient);
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
            var descriptor1 = new ServiceDescriptor(typeof(IFakeService), "key1", typeof(FakeService), ServiceLifetime.Transient);
            var descriptor2 = new ServiceDescriptor(typeof(IFakeOuterService), "key1", typeof(FakeOuterService), ServiceLifetime.Transient);
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
            var descriptor1 = new ServiceDescriptor(typeof(IFakeService), "key1", typeof(FakeService), ServiceLifetime.Transient);
            var descriptor2 = new ServiceDescriptor(typeof(IFakeService), "key1", typeof(FakeService), ServiceLifetime.Transient);
            collection.Add(descriptor1);
            collection.Add(descriptor2);
            var descriptor3 = new ServiceDescriptor(typeof(IFakeService), "key1", typeof(FakeService), ServiceLifetime.Singleton);

            // Act
            collection.Replace(descriptor3);

            // Assert
            Assert.Equal(new[] { descriptor2, descriptor3 }, collection);
        }

        [Fact]
        public void Replace_ReplacesFirstServiceWithMatchingServiceTypeWhenKeyIsInt()
        {
            // Arrange
            var collection = new ServiceCollection();
            var descriptor1 = new ServiceDescriptor(typeof(IFakeService), 1, typeof(FakeService), ServiceLifetime.Transient);
            var descriptor2 = new ServiceDescriptor(typeof(IFakeService), 1, typeof(FakeService), ServiceLifetime.Transient);
            collection.Add(descriptor1);
            collection.Add(descriptor2);
            var descriptor3 = new ServiceDescriptor(typeof(IFakeService), 1, typeof(FakeService), ServiceLifetime.Singleton);

            // Act
            collection.Replace(descriptor3);

            // Assert
            Assert.Equal(new[] { descriptor2, descriptor3 }, collection);
        }

        [Fact]
        public void RemoveAll_RemovesAllServicesWithMatchingServiceType()
        {
            // Arrange
            var descriptor = new ServiceDescriptor(typeof(IFakeServiceInstance), "key1", typeof(FakeService), ServiceLifetime.Transient);
            var collection = new ServiceCollection
            {
                descriptor,
                new ServiceDescriptor(typeof(IFakeService), "key1", typeof(FakeService), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeService), "key1", typeof(FakeService), ServiceLifetime.Transient)
            };

            // Act
            collection.RemoveAllKeyed<IFakeService>("key1");

            // Assert
            Assert.Equal(new[] { descriptor }, collection);
        }

        private enum ServiceKeyEnum { First, Second }

        [Fact]
        public void RemoveAll_RemovesAllMatchingServicesWhenKeyIsEnum()
        {
            var descriptor = new ServiceDescriptor(typeof(IFakeService), ServiceKeyEnum.First, typeof(FakeService), ServiceLifetime.Transient);
            var collection = new ServiceCollection
            {
                descriptor,
                new ServiceDescriptor(typeof(IFakeService), ServiceKeyEnum.Second, typeof(FakeService), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeService), ServiceKeyEnum.Second, typeof(FakeService), ServiceLifetime.Transient),
            };

            // Act
            collection.RemoveAllKeyed<IFakeService>(ServiceKeyEnum.Second);

            // Assert
            Assert.Equal(new[] { descriptor }, collection);
        }

        [Fact]
        public void RemoveAll_RemovesAllMatchingServicesWhenKeyIsInt()
        {
            var descriptor = new ServiceDescriptor(typeof(IFakeService), 1, typeof(FakeService), ServiceLifetime.Transient);
            var collection = new ServiceCollection
            {
                descriptor,
                new ServiceDescriptor(typeof(IFakeService), 2, typeof(FakeService), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeService), 2, typeof(FakeService), ServiceLifetime.Transient),
            };

            // Act
            collection.RemoveAllKeyed<IFakeService>(2);

            // Assert
            Assert.Equal(new[] { descriptor }, collection);
        }

        public static TheoryData NullServiceKeyData
        {
            get
            {
                var serviceType = typeof(IFakeService);
                object key = null;
                var implementationType = typeof(FakeService);
                var objectType = typeof(object);

                return new TheoryData<ServiceDescriptor>
                {
                    { ServiceDescriptor.KeyedTransient<IFakeService, FakeService>(key) },
                    { ServiceDescriptor.KeyedTransient<IFakeService>(key, (sp, key) => new FakeService()) },
                    { ServiceDescriptor.KeyedScoped<IFakeService, FakeService>(key) },
                    { ServiceDescriptor.KeyedScoped<IFakeService>(key, (sp, key) => new FakeService()) },
                    { ServiceDescriptor.KeyedSingleton<IFakeService, FakeService>(key) },
                    { ServiceDescriptor.KeyedSingleton<IFakeService>(key, new FakeService()) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(NullServiceKeyData))]
        public void NullServiceKey_IsKeyedServiceFalse(ServiceDescriptor serviceDescriptor)
        {
            Assert.False(serviceDescriptor.IsKeyedService);
            Assert.Throws<InvalidOperationException>(() => serviceDescriptor.KeyedImplementationInstance);
            Assert.Throws<InvalidOperationException>(() => serviceDescriptor.KeyedImplementationType);
            Assert.Throws<InvalidOperationException>(() => serviceDescriptor.KeyedImplementationFactory);
        }

        public static TheoryData NotNullServiceKeyData
        {
            get
            {
                var serviceType = typeof(IFakeService);
                object key = new();
                var implementationType = typeof(FakeService);
                var objectType = typeof(object);

                return new TheoryData<ServiceDescriptor>
                {
                    { ServiceDescriptor.KeyedTransient<IFakeService, FakeService>(key) },
                    { ServiceDescriptor.KeyedTransient<IFakeService>(key, (sp, key) => new FakeService()) },
                    { ServiceDescriptor.KeyedScoped<IFakeService, FakeService>(key) },
                    { ServiceDescriptor.KeyedScoped<IFakeService>(key, (sp, key) => new FakeService()) },
                    { ServiceDescriptor.KeyedSingleton<IFakeService, FakeService>(key) },
                    { ServiceDescriptor.KeyedSingleton<IFakeService>(key, new FakeService()) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(NotNullServiceKeyData))]
        public void NotNullServiceKey_IsKeyedServiceTrue(ServiceDescriptor serviceDescriptor)
        {
            Assert.True(serviceDescriptor.IsKeyedService);
            Assert.Throws<InvalidOperationException>(() => serviceDescriptor.ImplementationInstance);
            Assert.Throws<InvalidOperationException>(() => serviceDescriptor.ImplementationType);
            Assert.Throws<InvalidOperationException>(() => serviceDescriptor.ImplementationFactory);
        }
    }
}
