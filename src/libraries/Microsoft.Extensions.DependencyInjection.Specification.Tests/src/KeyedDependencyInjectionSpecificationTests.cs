// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public abstract partial class KeyedDependencyInjectionSpecificationTests
    {
        protected abstract  IServiceProvider CreateServiceProvider(IServiceCollection collection);

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
        }

        [Fact]
        public void ResolveNullKeyedService()
        {
            var service1 = new Service();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService>(null, service1);

            var provider = CreateServiceProvider(serviceCollection);

            var nonKeyed = provider.GetService<IService>();
            var nullKey = provider.GetKeyedService<IService>(null);

            Assert.Same(service1, nonKeyed);
            Assert.Same(service1, nullKey);
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

            // Return all services registered with a non null key, but not the one "created" with KeyedService.AnyKey
            var allServices = provider.GetKeyedServices<IService>(KeyedService.AnyKey).ToList();
            Assert.Equal(5, allServices.Count);
            Assert.Equal(new[] { service1, service2, service3, service4 }, allServices.Skip(1));
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
            Assert.Equal(new[] { service1, service2 }, services);
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
        }

        [Fact]
        public void ResolveKeyedServiceSingletonFactoryWithAnyKey()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedSingleton<IService>(KeyedService.AnyKey, (sp, key) => new Service((string)key));

            var provider = CreateServiceProvider(serviceCollection);

            Assert.Null(provider.GetService<IService>());

            for (int i=0; i<3; i++)
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

        internal interface IService { }

        internal class Service : IService
        {
            private readonly string _id;

            public Service() => _id = Guid.NewGuid().ToString();

            public Service([ServiceKey] string id) => _id = id;

            public override string? ToString() => _id;
        }

        internal class OtherService
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
    }
}
