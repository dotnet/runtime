// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public abstract class KeyedServiceProviderContainerTests
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

        interface IService { }

        class Service : IService
        {
            private readonly string _id;

            public Service() => _id = Guid.NewGuid().ToString();

            public Service([ServiceKey] string id) => _id = id;

            public override string? ToString() => _id;
        }

        class OtherService
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
    }

    public class KeyedServiceProviderDefaultContainerTests : KeyedServiceProviderContainerTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection collection) => collection.BuildServiceProvider(ServiceProviderMode.Default);
    }

    public class KeyedServiceProviderDynamicContainerTests : KeyedServiceProviderContainerTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection collection) => collection.BuildServiceProvider();
    }

    public class KeyedServiceProviderExpressionContainerTests : KeyedServiceProviderContainerTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection collection) => collection.BuildServiceProvider(ServiceProviderMode.Expressions);
    }

    public class KeyedServiceProviderILEmitContainerTests : KeyedServiceProviderContainerTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection collection) => collection.BuildServiceProvider(ServiceProviderMode.ILEmit);
    }
}
