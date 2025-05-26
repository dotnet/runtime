// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public abstract partial class KeyedDependencyInjectionSpecificationTests
    {
        protected abstract IServiceProvider CreateServiceProvider(IServiceCollection collection);

        [Fact]
        public void CombinationalRegistration()
        {
            Service service1 = new();
            Service service2 = new();
            Service keyedService1 = new();
            Service keyedService2 = new();
            Service anykeyService1 = new();
            Service anykeyService2 = new();
            Service nullkeyService1 = new();
            Service nullkeyService2 = new();

            ServiceCollection serviceCollection = new();
            serviceCollection.AddSingleton<IService>(service1);
            serviceCollection.AddSingleton<IService>(service2);
            serviceCollection.AddKeyedSingleton<IService>(null, nullkeyService1);
            serviceCollection.AddKeyedSingleton<IService>(null, nullkeyService2);
            serviceCollection.AddKeyedSingleton<IService>(KeyedService.AnyKey, anykeyService1);
            serviceCollection.AddKeyedSingleton<IService>(KeyedService.AnyKey, anykeyService2);
            serviceCollection.AddKeyedSingleton<IService>("keyedService", keyedService1);
            serviceCollection.AddKeyedSingleton<IService>("keyedService", keyedService2);

            IServiceProvider provider = CreateServiceProvider(serviceCollection);

            /*
             * Table for what results are included:
             *
             * Query                     | Keyed? | Unkeyed? | AnyKey? | null key?
             * -------------------------------------------------------------------
             * GetServices(Type)         | no     | yes      | no      | yes
             * GetService(Type)          | no     | yes      | no      | yes
             *
             * GetKeyedServices(null)    | no     | yes      | no      | yes
             * GetKeyedService(null)     | no     | yes      | no      | yes
             *
             * GetKeyedServices(AnyKey)  | yes    | no       | no      | no
             * GetKeyedService(AnyKey)   | throw  | throw    | throw   | throw
             *
             * GetKeyedServices(key)     | yes    | no       | no      | no
             * GetKeyedService(key)      | yes    | no       | yes     | no
             *
             * Summary:
             * - A null key is the same as unkeyed. This allows the KeyServices APIs to support both keyed and unkeyed.
             * - AnyKey is a special case of Keyed.
             * - AnyKey registrations are not returned with GetKeyedServices(AnyKey) and GetKeyedService(AnyKey) always throws.
             * - For IEnumerable, the ordering of the results are in registration order.
             * - For a singleton resolve, the last match wins.
             */

            // Unkeyed (which is really keyed by Type).
            Assert.Equal(
                new[] { service1, service2, nullkeyService1, nullkeyService2 },
                provider.GetServices<IService>());

            Assert.Equal(nullkeyService2, provider.GetService<IService>());

            // Null key.
            Assert.Equal(
                new[] { service1, service2, nullkeyService1, nullkeyService2 },
                provider.GetKeyedServices<IService>(null));

            Assert.Equal(nullkeyService2, provider.GetKeyedService<IService>(null));

            // AnyKey.
            Assert.Equal(
                new[] { keyedService1, keyedService2 },
                provider.GetKeyedServices<IService>(KeyedService.AnyKey));

            Assert.Throws<InvalidOperationException>(() => provider.GetKeyedService<IService>(KeyedService.AnyKey));

            // Keyed.
            Assert.Equal(
                new[] { keyedService1, keyedService2 },
                provider.GetKeyedServices<IService>("keyedService"));

            Assert.Equal(keyedService2, provider.GetKeyedService<IService>("keyedService"));
        }

        [Fact]
        public void ResolveKeyedService()
        {
            var service1 = new Service();
            var service2 = new Service();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService>("service1", service1);
            serviceCollection.AddKeyedSingleton<IService>("service2", service2);

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            Assert.Same(service1, provider.GetKeyedService<IService>("service1"));
            Assert.Same(service2, provider.GetKeyedService<IService>("service2"));

            Assert.Null(provider.GetService(typeof(IService)));
            Assert.Same(service1, provider.GetKeyedService(typeof(IService), "service1"));
            Assert.Same(service2, provider.GetKeyedService(typeof(IService), "service2"));
        }

        [Fact]
        public void ResolveNullKeyedService()
        {
            var service1 = new Service();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService>(null, service1);

            var provider = CreateServiceProvider(serviceCollection);

            var nonKeyed = provider.GetService<IService>();
            var nullKeyOfT = provider.GetKeyedService<IService>(null);
            var nullKeyOfType = provider.GetKeyedService(typeof(IService), null);

            Assert.Same(service1, nonKeyed);
            Assert.Same(service1, nullKeyOfT);
            Assert.Same(service1, nullKeyOfType);
        }

        [Fact]
        public void ResolveNonKeyedService()
        {
            var service1 = new Service();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IService>(service1);

            var provider = CreateServiceProvider(serviceCollection);

            var nonKeyed = provider.GetService<IService>();
            var nullKey = provider.GetKeyedService<IService>(null);

            Assert.Same(service1, nonKeyed);
            Assert.Same(service1, nullKey);
        }

        [Fact]
        public void ResolveKeyedOpenGenericService()
        {
            var collection = new ServiceCollection();
            collection.AddKeyedTransient(typeof(IFakeOpenGenericService<>), "my-service", typeof(FakeOpenGenericService<>));
            collection.AddSingleton<IFakeSingletonService, FakeService>();
            var provider = CreateServiceProvider(collection);

            // Act
            var genericService = provider.GetKeyedService<IFakeOpenGenericService<IFakeSingletonService>>("my-service");
            var singletonService = provider.GetService<IFakeSingletonService>();

            // Assert
            Assert.Same(singletonService, genericService.Value);
        }

        [Fact]
        public void ResolveKeyedServices()
        {
            var service1 = new Service();
            var service2 = new Service();
            var service3 = new Service();
            var service4 = new Service();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService>("first-service", service1);
            serviceCollection.AddKeyedSingleton<IService>("service", service2);
            serviceCollection.AddKeyedSingleton<IService>("service", service3);
            serviceCollection.AddKeyedSingleton<IService>("service", service4);

            var provider = CreateServiceProvider(serviceCollection);

            var firstSvc = provider.GetKeyedServices<IService>("first-service").ToList();
            Assert.Single(firstSvc);
            Assert.Same(service1, firstSvc[0]);

            var services = provider.GetKeyedServices<IService>("service").ToList();
            Assert.Equal(new[] { service2, service3, service4 }, services);
        }

        [Fact]
        public void ResolveKeyedServicesAnyKey()
        {
            var service1 = new Service();
            var service2 = new Service();
            var service3 = new Service();
            var service4 = new Service();
            var service5 = new Service();
            var service6 = new Service();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService>("first-service", service1);
            serviceCollection.AddKeyedSingleton<IService>("service", service2);
            serviceCollection.AddKeyedSingleton<IService>("service", service3);
            serviceCollection.AddKeyedSingleton<IService>("service", service4);
            serviceCollection.AddKeyedSingleton<IService>(null, service5);
            serviceCollection.AddSingleton<IService>(service6);

            var provider = CreateServiceProvider(serviceCollection);

            // Return all services registered with a non null key
            var allServices = provider.GetKeyedServices<IService>(KeyedService.AnyKey).ToList();
            Assert.Equal(4, allServices.Count);
            Assert.Equal(new[] { service1, service2, service3, service4 }, allServices);

            // Check again (caching)
            var allServices2 = provider.GetKeyedServices<IService>(KeyedService.AnyKey).ToList();
            Assert.Equal(allServices, allServices2);
        }

        [Fact]
        public void ResolveKeyedServicesAnyKeyWithAnyKeyRegistration()
        {
            var service1 = new Service();
            var service2 = new Service();
            var service3 = new Service();
            var service4 = new Service();
            var service5 = new Service();
            var service6 = new Service();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedTransient<IService>(KeyedService.AnyKey, (sp, key) => new Service());
            serviceCollection.AddKeyedSingleton<IService>("first-service", service1);
            serviceCollection.AddKeyedSingleton<IService>("service", service2);
            serviceCollection.AddKeyedSingleton<IService>("service", service3);
            serviceCollection.AddKeyedSingleton<IService>("service", service4);
            serviceCollection.AddKeyedSingleton<IService>(null, service5);
            serviceCollection.AddSingleton<IService>(service6);

            var provider = CreateServiceProvider(serviceCollection);

            _ = provider.GetKeyedService<IService>("something-else");
            _ = provider.GetKeyedService<IService>("something-else-again");

            // Return all services registered with a non null key, but not the one "created" with KeyedService.AnyKey,
            // nor the KeyedService.AnyKey registration
            var allServices = provider.GetKeyedServices<IService>(KeyedService.AnyKey).ToList();
            Assert.Equal(4, allServices.Count);
            Assert.Equal(new[] { service1, service2, service3, service4 }, allServices);

            var someKeyedServices = provider.GetKeyedServices<IService>("service").ToList();
            Assert.Equal(new[] { service2, service3, service4 }, someKeyedServices);

            var unkeyedServices = provider.GetServices<IService>().ToList();
            Assert.Equal(new[] { service5, service6 }, unkeyedServices);
        }

        [Fact]
        public void ResolveKeyedServicesAnyKeyConsistency()
        {
            var serviceCollection = new ServiceCollection();
            var service = new Service("first-service");
            serviceCollection.AddKeyedSingleton<IService>("first-service", service);

            var provider1 = CreateServiceProvider(serviceCollection);
            Assert.Throws<InvalidOperationException>(() => provider1.GetKeyedService<IService>(KeyedService.AnyKey));
            // We don't return KeyedService.AnyKey registration when listing services
            Assert.Equal(new[] { service }, provider1.GetKeyedServices<IService>(KeyedService.AnyKey));

            var provider2 = CreateServiceProvider(serviceCollection);
            Assert.Equal(new[] { service }, provider2.GetKeyedServices<IService>(KeyedService.AnyKey));
            Assert.Throws<InvalidOperationException>(() => provider2.GetKeyedService<IService>(KeyedService.AnyKey));
        }

        [Fact]
        public void ResolveKeyedServicesAnyKeyConsistencyWithAnyKeyRegistration()
        {
            var serviceCollection = new ServiceCollection();
            var service = new Service("first-service");
            var any = new Service("any");
            serviceCollection.AddKeyedSingleton<IService>("first-service", service);
            serviceCollection.AddKeyedSingleton<IService>(KeyedService.AnyKey, (sp, key) => any);

            var provider1 = CreateServiceProvider(serviceCollection);
            Assert.Equal(new[] { service }, provider1.GetKeyedServices<IService>(KeyedService.AnyKey));

            // Check twice in different order to check caching
            var provider2 = CreateServiceProvider(serviceCollection);
            Assert.Equal(new[] { service }, provider2.GetKeyedServices<IService>(KeyedService.AnyKey));
            Assert.Same(any, provider2.GetKeyedService<IService>(new object()));

            Assert.Throws<InvalidOperationException>(() => provider2.GetKeyedService<IService>(KeyedService.AnyKey));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        // Test ordering and slot assignments when DI calls the service's constructor
        // across keyed services with different service types and keys.
        public void ResolveWithAnyKeyQuery_Constructor(bool anyKeyQueryBeforeSingletonQueries)
        {
            var serviceCollection = new ServiceCollection();

            // Interweave these to check that the slot \ ordering logic is correct.
            // Each unique key + its service Type maintains their own slot in a AnyKey query.
            serviceCollection.AddKeyedSingleton<TestServiceA>("key1");
            serviceCollection.AddKeyedSingleton<TestServiceB>("key1");
            serviceCollection.AddKeyedSingleton<TestServiceA>("key2");
            serviceCollection.AddKeyedSingleton<TestServiceB>("key2");
            serviceCollection.AddKeyedSingleton<TestServiceA>("key3");
            serviceCollection.AddKeyedSingleton<TestServiceB>("key3");

            var provider = CreateServiceProvider(serviceCollection);

            TestServiceA[] allInstancesA = null;
            TestServiceB[] allInstancesB = null;

            if (anyKeyQueryBeforeSingletonQueries)
            {
                DoAnyKeyQuery();
            }

            var serviceA1 = provider.GetKeyedService<TestServiceA>("key1");
            var serviceB1 = provider.GetKeyedService<TestServiceB>("key1");
            var serviceA2 = provider.GetKeyedService<TestServiceA>("key2");
            var serviceB2 = provider.GetKeyedService<TestServiceB>("key2");
            var serviceA3 = provider.GetKeyedService<TestServiceA>("key3");
            var serviceB3 = provider.GetKeyedService<TestServiceB>("key3");

            if (!anyKeyQueryBeforeSingletonQueries)
            {
                DoAnyKeyQuery();
            }

            Assert.Equal(
                new[] { serviceA1, serviceA2, serviceA3 },
                allInstancesA);

            Assert.Equal(
                new[] { serviceB1, serviceB2, serviceB3 },
                allInstancesB);

            void DoAnyKeyQuery()
            {
                IEnumerable<TestServiceA> allA = provider.GetKeyedServices<TestServiceA>(KeyedService.AnyKey);
                IEnumerable<TestServiceB> allB = provider.GetKeyedServices<TestServiceB>(KeyedService.AnyKey);

                // Verify caching returns the same IEnumerable<> instance.
                Assert.Same(allA, provider.GetKeyedServices<TestServiceA>(KeyedService.AnyKey));
                Assert.Same(allB, provider.GetKeyedServices<TestServiceB>(KeyedService.AnyKey));

                allInstancesA = allA.ToArray();
                allInstancesB = allB.ToArray();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        // Test ordering and slot assignments when DI calls the service's constructor
        // across keyed services with different service types with duplicate keys.
        public void ResolveWithAnyKeyQuery_Constructor_Duplicates(bool anyKeyQueryBeforeSingletonQueries)
        {
            var serviceCollection = new ServiceCollection();

            // Interweave these to check that the slot \ ordering logic is correct.
            // Each unique key + its service Type maintains their own slot in a AnyKey query.
            serviceCollection.AddKeyedSingleton<TestServiceA>("key");
            serviceCollection.AddKeyedSingleton<TestServiceB>("key");
            serviceCollection.AddKeyedSingleton<TestServiceA>("key");
            serviceCollection.AddKeyedSingleton<TestServiceB>("key");
            serviceCollection.AddKeyedSingleton<TestServiceA>("key");
            serviceCollection.AddKeyedSingleton<TestServiceB>("key");

            var provider = CreateServiceProvider(serviceCollection);

            TestServiceA[] allInstancesA = null;
            TestServiceB[] allInstancesB = null;

            if (anyKeyQueryBeforeSingletonQueries)
            {
                DoAnyKeyQuery();
            }

            var serviceA = provider.GetKeyedService<TestServiceA>("key");
            Assert.Same(serviceA, provider.GetKeyedService<TestServiceA>("key"));

            var serviceB = provider.GetKeyedService<TestServiceB>("key");
            Assert.Same(serviceB, provider.GetKeyedService<TestServiceB>("key"));

            if (!anyKeyQueryBeforeSingletonQueries)
            {
                DoAnyKeyQuery();
            }

            // An AnyKey query we get back the last registered service for duplicates.
            // The first and second services are effectively hidden unless we query all.
            Assert.Equal(3, allInstancesA.Length);
            Assert.Same(serviceA, allInstancesA[2]);
            Assert.NotSame(serviceA, allInstancesA[1]);
            Assert.NotSame(serviceA, allInstancesA[0]);
            Assert.NotSame(allInstancesA[0], allInstancesA[1]);

            Assert.Equal(3, allInstancesB.Length);
            Assert.Same(serviceB, allInstancesB[2]);
            Assert.NotSame(serviceB, allInstancesB[1]);
            Assert.NotSame(serviceB, allInstancesB[0]);
            Assert.NotSame(allInstancesB[0], allInstancesB[1]);

            void DoAnyKeyQuery()
            {
                IEnumerable<TestServiceA> allA = provider.GetKeyedServices<TestServiceA>(KeyedService.AnyKey);
                IEnumerable<TestServiceB> allB = provider.GetKeyedServices<TestServiceB>(KeyedService.AnyKey);

                // Verify caching returns the same IEnumerable<> instances.
                Assert.Same(allA, provider.GetKeyedServices<TestServiceA>(KeyedService.AnyKey));
                Assert.Same(allB, provider.GetKeyedServices<TestServiceB>(KeyedService.AnyKey));

                allInstancesA = allA.ToArray();
                allInstancesB = allB.ToArray();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        // Test ordering and slot assignments when service is provided
        // across keyed services with different service types and keys.
        public void ResolveWithAnyKeyQuery_InstanceProvided(bool anyKeyQueryBeforeSingletonQueries)
        {
            var serviceCollection = new ServiceCollection();

            TestServiceA serviceA1 = new();
            TestServiceA serviceA2 = new();
            TestServiceA serviceA3 = new();
            TestServiceB serviceB1 = new();
            TestServiceB serviceB2 = new();
            TestServiceB serviceB3 = new();

            // Interweave these to check that the slot \ ordering logic is correct.
            // Each unique key + its service Type maintains their own slot in a AnyKey query.
            serviceCollection.AddKeyedSingleton<TestServiceA>("key1", serviceA1);
            serviceCollection.AddKeyedSingleton<TestServiceB>("key1", serviceB1);
            serviceCollection.AddKeyedSingleton<TestServiceA>("key2", serviceA2);
            serviceCollection.AddKeyedSingleton<TestServiceB>("key2", serviceB2);
            serviceCollection.AddKeyedSingleton<TestServiceA>("key3", serviceA3);
            serviceCollection.AddKeyedSingleton<TestServiceB>("key3", serviceB3);

            var provider = CreateServiceProvider(serviceCollection);

            TestServiceA[] allInstancesA = null;
            TestServiceB[] allInstancesB = null;

            if (anyKeyQueryBeforeSingletonQueries)
            {
                DoAnyKeyQuery();
            }

            var fromServiceA1 = provider.GetKeyedService<TestServiceA>("key1");
            var fromServiceA2 = provider.GetKeyedService<TestServiceA>("key2");
            var fromServiceA3 = provider.GetKeyedService<TestServiceA>("key3");
            Assert.Same(serviceA1, fromServiceA1);
            Assert.Same(serviceA2, fromServiceA2);
            Assert.Same(serviceA3, fromServiceA3);

            var fromServiceB1 = provider.GetKeyedService<TestServiceB>("key1");
            var fromServiceB2 = provider.GetKeyedService<TestServiceB>("key2");
            var fromServiceB3 = provider.GetKeyedService<TestServiceB>("key3");
            Assert.Same(serviceB1, fromServiceB1);
            Assert.Same(serviceB2, fromServiceB2);
            Assert.Same(serviceB3, fromServiceB3);

            if (!anyKeyQueryBeforeSingletonQueries)
            {
                DoAnyKeyQuery();
            }

            Assert.Equal(
                new[] { serviceA1, serviceA2, serviceA3 },
                allInstancesA);

            Assert.Equal(
                new[] { serviceB1, serviceB2, serviceB3 },
                allInstancesB);

            void DoAnyKeyQuery()
            {
                IEnumerable<TestServiceA> allA = provider.GetKeyedServices<TestServiceA>(KeyedService.AnyKey);
                IEnumerable<TestServiceB> allB = provider.GetKeyedServices<TestServiceB>(KeyedService.AnyKey);

                // Verify caching returns the same items.
                Assert.Equal(allA, provider.GetKeyedServices<TestServiceA>(KeyedService.AnyKey));
                Assert.Equal(allB, provider.GetKeyedServices<TestServiceB>(KeyedService.AnyKey));

                allInstancesA = allA.ToArray();
                allInstancesB = allB.ToArray();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        // Test ordering and slot assignments when service is provided
        // across keyed services with different service types with duplicate keys.
        public void ResolveWithAnyKeyQuery_InstanceProvided_Duplicates(bool anyKeyQueryBeforeSingletonQueries)
        {
            var serviceCollection = new ServiceCollection();

            TestServiceA serviceA1 = new();
            TestServiceA serviceA2 = new();
            TestServiceA serviceA3 = new();
            TestServiceB serviceB1 = new();
            TestServiceB serviceB2 = new();
            TestServiceB serviceB3 = new();

            // Interweave these to check that the slot \ ordering logic is correct.
            // Each unique key + its service Type maintains their own slot in a AnyKey query.
            serviceCollection.AddKeyedSingleton<TestServiceA>("key", serviceA1);
            serviceCollection.AddKeyedSingleton<TestServiceB>("key", serviceB1);
            serviceCollection.AddKeyedSingleton<TestServiceA>("key", serviceA2);
            serviceCollection.AddKeyedSingleton<TestServiceB>("key", serviceB2);
            serviceCollection.AddKeyedSingleton<TestServiceA>("key", serviceA3);
            serviceCollection.AddKeyedSingleton<TestServiceB>("key", serviceB3);

            var provider = CreateServiceProvider(serviceCollection);

            TestServiceA[] allInstancesA = null;
            TestServiceB[] allInstancesB = null;

            if (anyKeyQueryBeforeSingletonQueries)
            {
                DoAnyKeyQuery();
            }

            // We get back the last registered service for duplicates.
            Assert.Same(serviceA3, provider.GetKeyedService<TestServiceA>("key"));
            Assert.Same(serviceB3, provider.GetKeyedService<TestServiceB>("key"));

            if (!anyKeyQueryBeforeSingletonQueries)
            {
                DoAnyKeyQuery();
            }

            Assert.Equal(
                new[] { serviceA1, serviceA2, serviceA3 },
                allInstancesA);

            Assert.Equal(
                new[] { serviceB1, serviceB2, serviceB3 },
                allInstancesB);

            void DoAnyKeyQuery()
            {
                IEnumerable<TestServiceA> allA = provider.GetKeyedServices<TestServiceA>(KeyedService.AnyKey);
                IEnumerable<TestServiceB> allB = provider.GetKeyedServices<TestServiceB>(KeyedService.AnyKey);

                // Verify caching returns the same items.
                Assert.Equal(allA, provider.GetKeyedServices<TestServiceA>(KeyedService.AnyKey));
                Assert.Equal(allB, provider.GetKeyedServices<TestServiceB>(KeyedService.AnyKey));

                allInstancesA = allA.ToArray();
                allInstancesB = allB.ToArray();
            }
        }

        private class TestServiceA { }
        private class TestServiceB { }

        [Fact]
        public void ResolveKeyedServicesAnyKeyOrdering()
        {
            var serviceCollection = new ServiceCollection();
            var service1 = new Service();
            var service2 = new Service();
            var service3 = new Service();

            serviceCollection.AddKeyedSingleton<IService>("A-service", service1);
            serviceCollection.AddKeyedSingleton<IService>("B-service", service2);
            serviceCollection.AddKeyedSingleton<IService>("A-service", service3);

            var provider = CreateServiceProvider(serviceCollection);

            // The order should be in registration order, and not grouped by key for example.
            // Although this isn't necessarily a requirement, it is the current behavior.
            Assert.Equal(
                new[] { service1, service2, service3 },
                provider.GetKeyedServices<IService>(KeyedService.AnyKey));
        }

        [Fact]
        public void ResolveKeyedGenericServices()
        {
            var service1 = new FakeService();
            var service2 = new FakeService();
            var service3 = new FakeService();
            var service4 = new FakeService();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IFakeOpenGenericService<PocoClass>>("first-service", service1);
            serviceCollection.AddKeyedSingleton<IFakeOpenGenericService<PocoClass>>("service", service2);
            serviceCollection.AddKeyedSingleton<IFakeOpenGenericService<PocoClass>>("service", service3);
            serviceCollection.AddKeyedSingleton<IFakeOpenGenericService<PocoClass>>("service", service4);

            var provider = CreateServiceProvider(serviceCollection);

            var firstSvc = provider.GetKeyedServices<IFakeOpenGenericService<PocoClass>>("first-service").ToList();
            Assert.Single(firstSvc);
            Assert.Same(service1, firstSvc[0]);

            var services = provider.GetKeyedServices<IFakeOpenGenericService<PocoClass>>("service").ToList();
            Assert.Equal(new[] { service2, service3, service4 }, services);
        }

        [Fact]
        public void ResolveKeyedServiceSingletonInstance()
        {
            var service = new Service();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService>("service1", service);

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            Assert.Same(service, provider.GetKeyedService<IService>("service1"));
            Assert.Same(service, provider.GetKeyedService(typeof(IService), "service1"));
        }

        [Fact]
        public void ResolveKeyedServiceSingletonInstanceWithKeyInjection()
        {
            var serviceKey = "this-is-my-service";
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService, Service>(serviceKey);

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            var svc = provider.GetKeyedService<IService>(serviceKey);
            Assert.NotNull(svc);
            Assert.Equal(serviceKey, svc.ToString());
        }

        [Fact]
        public void ResolveKeyedServiceSingletonInstanceWithAnyKey()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService, Service>(KeyedService.AnyKey);

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());

            var serviceKey1 = "some-key";
            var svc1 = provider.GetKeyedService<IService>(serviceKey1);
            Assert.NotNull(svc1);
            Assert.Equal(serviceKey1, svc1.ToString());

            var serviceKey2 = "some-other-key";
            var svc2 = provider.GetKeyedService<IService>(serviceKey2);
            Assert.NotNull(svc2);
            Assert.Equal(serviceKey2, svc2.ToString());
        }

        [Fact]
        public void ResolveKeyedServicesSingletonInstanceWithAnyKey()
        {
            var service1 = new FakeService();
            var service2 = new FakeService();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IFakeOpenGenericService<PocoClass>>(KeyedService.AnyKey, service1);
            serviceCollection.AddKeyedSingleton<IFakeOpenGenericService<PocoClass>>("some-key", service2);

            var provider = CreateServiceProvider(serviceCollection);

            var services = provider.GetKeyedServices<IFakeOpenGenericService<PocoClass>>("some-key").ToList();
            Assert.Equal(new[] { service2 }, services);
        }

        [Fact]
        public void ResolveKeyedServiceSingletonInstanceWithKeyedParameter()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService, Service>("service1");
            serviceCollection.AddKeyedSingleton<IService, Service>("service2");
            serviceCollection.AddSingleton<OtherService>();

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            var svc = provider.GetService<OtherService>();
            Assert.NotNull(svc);
            Assert.Equal("service1", svc.Service1.ToString());
            Assert.Equal("service2", svc.Service2.ToString());
        }

        [Fact]
        public void ResolveKeyedServiceWithKeyedParameter_MissingRegistration_SecondParameter()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddKeyedSingleton<IService, Service>("service1");
            // We are missing the registration for "service2" here and OtherService requires it.

            serviceCollection.AddSingleton<OtherService>();

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            Assert.Throws<InvalidOperationException>(() => provider.GetService<OtherService>());
        }

        [Fact]
        public void ResolveKeyedServiceWithKeyedParameter_MissingRegistration_FirstParameter()
        {
            var serviceCollection = new ServiceCollection();

            // We are not registering "service1" and "service1" keyed IService services and OtherService requires them.

            serviceCollection.AddSingleton<OtherService>();

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            Assert.Throws<InvalidOperationException>(() => provider.GetService<OtherService>());
        }

        [Fact]
        public void ResolveKeyedServiceWithKeyedParameter_MissingRegistrationButWithDefaults()
        {
            var serviceCollection = new ServiceCollection();

            // We are not registering "service1" and "service1" keyed IService services and OtherServiceWithDefaultCtorArgs
            // specifies them but has argument defaults if missing.

            serviceCollection.AddSingleton<OtherServiceWithDefaultCtorArgs>();

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            Assert.NotNull(provider.GetService<OtherServiceWithDefaultCtorArgs>());
        }

        [Fact]
        public void ResolveKeyedServiceWithKeyedParameter_MissingRegistrationButWithUnkeyedService()
        {
            var serviceCollection = new ServiceCollection();

            // We are not registering "service1" and "service1" keyed IService services and OtherService requires them,
            // but we are registering an unkeyed IService service which should not be injected into OtherService.
            serviceCollection.AddSingleton<IService, Service>();

            serviceCollection.AddSingleton<OtherService>();

            var provider = CreateServiceProvider(serviceCollection);

            Assert.NotNull(provider.GetService<IService>());
            Assert.Throws<InvalidOperationException>(() => provider.GetService<OtherService>());
        }

        [Fact]
        public void CreateServiceWithKeyedParameter()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IService, Service>();
            serviceCollection.AddKeyedSingleton<IService, Service>("service1");
            serviceCollection.AddKeyedSingleton<IService, Service>("service2");

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<OtherService>());
            var svc = ActivatorUtilities.CreateInstance<OtherService>(provider);
            Assert.NotNull(svc);
            Assert.Equal("service1", svc.Service1.ToString());
            Assert.Equal("service2", svc.Service2.ToString());
        }

        [Fact]
        public void ResolveKeyedServiceSingletonFactory()
        {
            var service = new Service();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService>("service1", (sp, key) => service);

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            Assert.Same(service, provider.GetKeyedService<IService>("service1"));
            Assert.Same(service, provider.GetKeyedService(typeof(IService), "service1"));
        }

        [Fact]
        public void ResolveKeyedServiceSingletonFactoryWithAnyKey()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService>(KeyedService.AnyKey, (sp, key) => new Service((string)key));

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());

            for (int i = 0; i < 3; i++)
            {
                var key = "service" + i;
                var s1 = provider.GetKeyedService<IService>(key);
                var s2 = provider.GetKeyedService<IService>(key);
                Assert.Same(s1, s2);
                Assert.Equal(key, s1.ToString());
            }
        }

        [Fact]
        public void ResolveKeyedServiceSingletonFactoryWithAnyKeyIgnoreWrongType()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedTransient<IService, ServiceWithIntKey>(KeyedService.AnyKey);

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            Assert.NotNull(provider.GetKeyedService<IService>(87));
            Assert.ThrowsAny<InvalidOperationException>(() => provider.GetKeyedService<IService>(new object()));
            Assert.ThrowsAny<InvalidOperationException>(() => provider.GetKeyedService(typeof(IService), new object()));
        }

        [Fact]
        public void ResolveKeyedServiceSingletonType()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService, Service>("service1");

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            Assert.Equal(typeof(Service), provider.GetKeyedService<IService>("service1")!.GetType());
        }

        [Fact]
        public void ResolveKeyedServiceTransientFactory()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedTransient<IService>("service1", (sp, key) => new Service(key as string));

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            var first = provider.GetKeyedService<IService>("service1");
            var second = provider.GetKeyedService<IService>("service1");
            Assert.NotSame(first, second);
            Assert.Equal("service1", first.ToString());
            Assert.Equal("service1", second.ToString());
        }

        [Fact]
        public void ResolveKeyedServiceTransientType()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedTransient<IService, Service>("service1");

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            var first = provider.GetKeyedService<IService>("service1");
            var second = provider.GetKeyedService<IService>("service1");
            Assert.NotSame(first, second);
        }

        [Fact]
        public void ResolveKeyedServiceTransientTypeWithAnyKey()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedTransient<IService, Service>(KeyedService.AnyKey);

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());
            var first = provider.GetKeyedService<IService>("service1");
            var second = provider.GetKeyedService<IService>("service1");
            Assert.NotSame(first, second);
        }

        [Fact]
        public void ResolveKeyedSingletonFromInjectedServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService, Service>("key");
            serviceCollection.AddSingleton<ServiceProviderAccessor>();

            var provider = CreateServiceProvider(serviceCollection);
            var accessor = provider.GetRequiredService<ServiceProviderAccessor>();

            Assert.Null(accessor.ServiceProvider.GetService<IService>());

            var service1 = accessor.ServiceProvider.GetKeyedService<IService>("key");
            var service2 = accessor.ServiceProvider.GetKeyedService<IService>("key");

            Assert.Same(service1, service2);
        }

        [Fact]
        public void ResolveKeyedTransientFromInjectedServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedTransient<IService, Service>("key");
            serviceCollection.AddSingleton<ServiceProviderAccessor>();

            var provider = CreateServiceProvider(serviceCollection);
            var accessor = provider.GetRequiredService<ServiceProviderAccessor>();

            Assert.Null(accessor.ServiceProvider.GetService<IService>());

            var service1 = accessor.ServiceProvider.GetKeyedService<IService>("key");
            var service2 = accessor.ServiceProvider.GetKeyedService<IService>("key");

            Assert.NotSame(service1, service2);
        }

        [Fact]
        public void ResolveKeyedSingletonFromScopeServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService, Service>("key");

            var provider = CreateServiceProvider(serviceCollection);
            var scopeA = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var scopeB = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();

            Assert.Null(scopeA.ServiceProvider.GetService<IService>());
            Assert.Null(scopeB.ServiceProvider.GetService<IService>());

            Assert.Throws<InvalidOperationException>(() => scopeA.ServiceProvider.GetKeyedService<IService>(KeyedService.AnyKey));
            Assert.Throws<InvalidOperationException>(() => scopeB.ServiceProvider.GetKeyedService<IService>(KeyedService.AnyKey));

            var serviceA1 = scopeA.ServiceProvider.GetKeyedService<IService>("key");
            var serviceA2 = scopeA.ServiceProvider.GetKeyedService<IService>("key");

            var serviceB1 = scopeB.ServiceProvider.GetKeyedService<IService>("key");
            var serviceB2 = scopeB.ServiceProvider.GetKeyedService<IService>("key");

            Assert.Same(serviceA1, serviceA2);
            Assert.Same(serviceB1, serviceB2);
            Assert.Same(serviceA1, serviceB1);
        }

        [Fact]
        public void ResolveKeyedScopedFromScopeServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedScoped<IService, Service>("key");

            var provider = CreateServiceProvider(serviceCollection);
            var scopeA = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var scopeB = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();

            Assert.Null(scopeA.ServiceProvider.GetService<IService>());
            Assert.Null(scopeB.ServiceProvider.GetService<IService>());

            Assert.Throws<InvalidOperationException>(() => scopeA.ServiceProvider.GetKeyedService<IService>(KeyedService.AnyKey));
            Assert.Throws<InvalidOperationException>(() => scopeB.ServiceProvider.GetKeyedService<IService>(KeyedService.AnyKey));

            var serviceA1 = scopeA.ServiceProvider.GetKeyedService<IService>("key");
            var serviceA2 = scopeA.ServiceProvider.GetKeyedService<IService>("key");

            var serviceB1 = scopeB.ServiceProvider.GetKeyedService<IService>("key");
            var serviceB2 = scopeB.ServiceProvider.GetKeyedService<IService>("key");

            Assert.Same(serviceA1, serviceA2);
            Assert.Same(serviceB1, serviceB2);
            Assert.NotSame(serviceA1, serviceB1);
        }

        [Fact]
        public void ResolveKeyedTransientFromScopeServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedTransient<IService, Service>("key");

            var provider = CreateServiceProvider(serviceCollection);
            var scopeA = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var scopeB = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();

            Assert.Null(scopeA.ServiceProvider.GetService<IService>());
            Assert.Null(scopeB.ServiceProvider.GetService<IService>());

            var serviceA1 = scopeA.ServiceProvider.GetKeyedService<IService>("key");
            var serviceA2 = scopeA.ServiceProvider.GetKeyedService<IService>("key");

            var serviceB1 = scopeB.ServiceProvider.GetKeyedService<IService>("key");
            var serviceB2 = scopeB.ServiceProvider.GetKeyedService<IService>("key");

            Assert.NotSame(serviceA1, serviceA2);
            Assert.NotSame(serviceB1, serviceB2);
            Assert.NotSame(serviceA1, serviceB1);
        }

        [Fact]
        public void ResolveRequiredKeyedServiceThrowsIfNotFound()
        {
            var serviceCollection = new ServiceCollection();
            var provider = CreateServiceProvider(serviceCollection);
            var serviceKey = new object();

            InvalidOperationException e;

            e = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredKeyedService<IService>(serviceKey));
            VerifyException();

            e = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredKeyedService(typeof(IService), serviceKey));
            VerifyException();

            void VerifyException()
            {
                Assert.Contains(nameof(IService), e.Message);
                Assert.Contains(serviceKey.GetType().FullName, e.Message);
            }
        }

        [Fact]
        public void ResolveKeyedServiceThrowsIfNotSupported()
        {
            var provider = new NonKeyedServiceProvider();
            var serviceKey = new object();

            Assert.Throws<InvalidOperationException>(() => provider.GetKeyedService<IService>(serviceKey));
            Assert.Throws<InvalidOperationException>(() => provider.GetKeyedService(typeof(IService), serviceKey));
            Assert.Throws<InvalidOperationException>(() => provider.GetKeyedServices<IService>(serviceKey));
            Assert.Throws<InvalidOperationException>(() => provider.GetKeyedServices(typeof(IService), serviceKey));
            Assert.Throws<InvalidOperationException>(() => provider.GetRequiredKeyedService<IService>(serviceKey));
            Assert.Throws<InvalidOperationException>(() => provider.GetRequiredKeyedService(typeof(IService), serviceKey));
        }

        public interface IService { }

        public class Service : IService
        {
            private readonly string _id;

            public Service() => _id = Guid.NewGuid().ToString();

            public Service([ServiceKey] string id) => _id = id;

            public override string? ToString() => _id;
        }

        public class OtherService
        {
            public OtherService(
                [FromKeyedServices("service1")] IService service1,
                [FromKeyedServices("service2")] IService service2)
            {
                Service1 = service1;
                Service2 = service2;
            }

            public IService Service1 { get; }

            public IService Service2 { get; }
        }

        internal class OtherServiceWithDefaultCtorArgs
        {
            public OtherServiceWithDefaultCtorArgs(
                [FromKeyedServices("service1")] IService service1 = null,
                [FromKeyedServices("service2")] IService service2 = null)
            {
                Service1 = service1;
                Service2 = service2;
            }

            public IService Service1 { get; }

            public IService Service2 { get; }
        }

        internal class ServiceWithOtherService
        {
            public ServiceWithOtherService(
                [FromKeyedServices("service1")] IService service1,
                [FromKeyedServices("service2")] IService service2)
            {
                Service1 = service1;
                Service2 = service2;
            }

            public IService Service1 { get; }

            public IService Service2 { get; }
        }

        internal class ServiceWithIntKey : IService
        {
            private readonly int _id;

            public ServiceWithIntKey([ServiceKey] int id) => _id = id;
        }

        internal class ServiceProviderAccessor
        {
            public ServiceProviderAccessor(IServiceProvider serviceProvider)
            {
                ServiceProvider = serviceProvider;
            }

            public IServiceProvider ServiceProvider { get; }
        }

        [Fact]
        public void SimpleServiceKeyedResolution()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddKeyedTransient<ISimpleService, SimpleService>("simple");
            services.AddKeyedTransient<ISimpleService, AnotherSimpleService>("another");
            services.AddTransient<SimpleParentWithDynamicKeyedService>();
            var provider = CreateServiceProvider(services);
            var sut = provider.GetService<SimpleParentWithDynamicKeyedService>();

            // Act
            var result = sut!.GetService("simple");

            // Assert
            Assert.True(result.GetType() == typeof(SimpleService));
        }

        public class SimpleParentWithDynamicKeyedService
        {
            private readonly IServiceProvider _serviceProvider;

            public SimpleParentWithDynamicKeyedService(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public ISimpleService GetService(string name) => _serviceProvider.GetKeyedService<ISimpleService>(name)!;
        }

        public interface ISimpleService { }

        public class SimpleService : ISimpleService { }

        public class AnotherSimpleService : ISimpleService { }

        public class NonKeyedServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType) => throw new NotImplementedException();
        }

#if NET10_0_OR_GREATER
        [Fact]
        public void ResolveKeyedServiceWithFromServiceKeyAttribute()
        {
            ServiceCollection services = new();
            services.AddKeyedSingleton<ServiceUsingFromServiceKeyAttribute>("key");
            services.AddKeyedSingleton<ServiceCreatedWithServiceKeyAttribute>("key");

            IServiceProvider provider = CreateServiceProvider(services);

            ServiceUsingFromServiceKeyAttribute service = provider.GetRequiredKeyedService<ServiceUsingFromServiceKeyAttribute>("key");
            Assert.Equal("key", service.OtherService.MyKey);
        }

        [Fact]
        public void ResolveKeyedServiceWithFromServiceKeyAttribute_NotFound()
        {
            ServiceCollection services = new();
            services.AddKeyedSingleton<ServiceUsingFromServiceKeyAttribute>("key1");
            services.AddKeyedSingleton<ServiceCreatedWithServiceKeyAttribute>("key2");

            IServiceProvider provider = CreateServiceProvider(services);

            Assert.Throws<InvalidOperationException>(() => provider.GetKeyedService<ServiceUsingFromServiceKeyAttribute>("key1"));
        }

        [Fact]
        public void ResolveKeyedServiceWithFromServiceKeyAttribute_NotFound_WithUnkeyed()
        {
            ServiceCollection services = new();
            services.AddKeyedSingleton<ServiceUsingFromServiceKeyAttribute>("key1");
            services.AddSingleton<ServiceCreatedWithServiceKeyAttribute>();

            IServiceProvider provider = CreateServiceProvider(services);

            Assert.Throws<InvalidOperationException>(() => provider.GetKeyedService<ServiceUsingFromServiceKeyAttribute>("key1"));
        }

        private class ServiceUsingFromServiceKeyAttribute : IService
        {
            public ServiceCreatedWithServiceKeyAttribute OtherService { get; }

            public ServiceUsingFromServiceKeyAttribute([FromKeyedServices] ServiceCreatedWithServiceKeyAttribute otherService)
            {
                OtherService = otherService;
            }
        }

        private class ServiceCreatedWithServiceKeyAttribute : IService
        {
            public string MyKey { get; }

            public ServiceCreatedWithServiceKeyAttribute([ServiceKey] string myKey)
            {
                MyKey = myKey;
            }
        }

        [Fact]
        public void ResolveUnkeyedServiceWithFromServiceKeyAttributeWithNullKey()
        {
            ServiceCollection services = new();
            services.AddSingleton<UnkeyedServiceWithFromServiceKeyAttributeWithNullKey>();
            services.AddSingleton<Service>();

            IServiceProvider provider = CreateServiceProvider(services);

            UnkeyedServiceWithFromServiceKeyAttributeWithNullKey service =
                provider.GetRequiredService<UnkeyedServiceWithFromServiceKeyAttributeWithNullKey>();

            Assert.NotNull(service.OtherService);
        }

        private class UnkeyedServiceWithFromServiceKeyAttributeWithNullKey : IService
        {
            public Service OtherService { get; }

            public UnkeyedServiceWithFromServiceKeyAttributeWithNullKey([FromKeyedServices(null)] Service otherService)
            {
                OtherService = otherService;
            }
        }
#endif
    }
}
