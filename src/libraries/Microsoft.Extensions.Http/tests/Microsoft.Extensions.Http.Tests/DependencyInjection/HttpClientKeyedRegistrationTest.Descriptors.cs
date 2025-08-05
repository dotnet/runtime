// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public partial class HttpClientKeyedRegistrationTest
    {
        [Fact]
        public void AddAsKeyed_ScopedLifetime()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddHttpClient(Test).AddAsKeyed();

            AssertSingleKeyedClientDescriptor(serviceCollection, ServiceLifetime.Scoped, Test);
        }

        [Fact]
        public void AddAsKeyed_Twice_RewritesDescriptor()
        {
            var serviceCollection = new ServiceCollection();

            var builder = serviceCollection.AddHttpClient(Test).AddAsKeyed(ServiceLifetime.Singleton);

            AssertSingleKeyedClientDescriptor(serviceCollection, ServiceLifetime.Singleton, Test);

            builder.AddAsKeyed(ServiceLifetime.Scoped);

            AssertSingleKeyedClientDescriptor(serviceCollection, ServiceLifetime.Scoped, Test);

            builder.AddAsKeyed(ServiceLifetime.Transient);

            AssertSingleKeyedClientDescriptor(serviceCollection, ServiceLifetime.Transient, Test);
        }

        [Fact]
        public void RemoveAsKeyed_DescriptorRemoved()
        {
            var serviceCollection = new ServiceCollection();

            var builder = serviceCollection.AddHttpClient(Test).RemoveAsKeyed();

            Assert.DoesNotContain(serviceCollection, IsKeyedClientDescriptor);
            Assert.DoesNotContain(serviceCollection, IsKeyedHandlerDescriptor);

            builder.AddAsKeyed();

            var descriptor = Assert.Single(serviceCollection, IsKeyedClientDescriptor);
            Assert.Equal(descriptor.ServiceKey, Test);

            builder.RemoveAsKeyed();

            Assert.DoesNotContain(serviceCollection, IsKeyedClientDescriptor);
            Assert.DoesNotContain(serviceCollection, IsKeyedHandlerDescriptor);
        }

        [Fact]
        public void AddAsKeyed_Defaults_AnyKeyRegistration()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.ConfigureHttpClientDefaults(b => b.AddAsKeyed());

            var descriptor = Assert.Single(serviceCollection, IsKeyedClientDescriptor);
            Assert.Equal(descriptor.ServiceKey, KeyedService.AnyKey);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);

            var handlerDescriptor = Assert.Single(serviceCollection, IsKeyedHandlerDescriptor);
            Assert.Equal(handlerDescriptor.ServiceKey, KeyedService.AnyKey);
            Assert.Equal(ServiceLifetime.Scoped, handlerDescriptor.Lifetime);
        }

        [Fact]
        public void RemoveAsKeyed_Defaults_AnyKeyDescriptorRemoved()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.ConfigureHttpClientDefaults(b => b.RemoveAsKeyed());

            Assert.DoesNotContain(serviceCollection, IsKeyedClientDescriptor);
            Assert.DoesNotContain(serviceCollection, IsKeyedHandlerDescriptor);

            serviceCollection.ConfigureHttpClientDefaults(b => b.AddAsKeyed());

            var descriptor = Assert.Single(serviceCollection, IsKeyedClientDescriptor);
            Assert.Equal(descriptor.ServiceKey, KeyedService.AnyKey);

            serviceCollection.ConfigureHttpClientDefaults(b => b.RemoveAsKeyed());

            Assert.DoesNotContain(serviceCollection, IsKeyedClientDescriptor);
            Assert.DoesNotContain(serviceCollection, IsKeyedHandlerDescriptor);
        }

        [Fact]
        public void RemoveAsKeyed_PerName_AnyKeyDescriptorRemains()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.ConfigureHttpClientDefaults(b => b.AddAsKeyed());

            var descriptorBefore = Assert.Single(serviceCollection, IsKeyedClientDescriptor);
            Assert.Equal(descriptorBefore.ServiceKey, KeyedService.AnyKey);

            serviceCollection.AddHttpClient(Test).RemoveAsKeyed();

            var descriptorAfter = Assert.Single(serviceCollection, IsKeyedClientDescriptor);
            Assert.Equal(descriptorAfter.ServiceKey, KeyedService.AnyKey);
            Assert.Same(descriptorBefore, descriptorAfter);
        }

        private static bool IsKeyedClientDescriptor(ServiceDescriptor descriptor)
            => descriptor.IsKeyedService && descriptor.ServiceType == typeof(HttpClient);

        private static bool IsKeyedHandlerDescriptor(ServiceDescriptor descriptor)
            => descriptor.IsKeyedService && descriptor.ServiceType == typeof(HttpMessageHandler);

        private static void AssertSingleKeyedClientDescriptor(IServiceCollection services, ServiceLifetime lifetime, object key)
        {
            ServiceDescriptor clientDescriptor = Assert.Single(services, IsKeyedClientDescriptor);
            Assert.Equal(clientDescriptor.ServiceKey, key);
            Assert.Equal(lifetime, clientDescriptor.Lifetime);

            ServiceDescriptor handlerDescriptor = Assert.Single(services, IsKeyedHandlerDescriptor);
            Assert.Equal(handlerDescriptor.ServiceKey, key);
            Assert.Equal(lifetime, handlerDescriptor.Lifetime);
        }
    }
}
