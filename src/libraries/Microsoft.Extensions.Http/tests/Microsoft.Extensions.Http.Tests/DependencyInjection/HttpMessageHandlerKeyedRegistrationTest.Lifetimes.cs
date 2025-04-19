// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class HttpMessageHandlerKeyedRegistrationTest
    {
        private const string Test = $"test-{nameof(HttpMessageHandlerKeyedRegistrationTest)}";

        [Fact]
        public void HttpMessageHandler_ScopedLifetime_Success()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Scoped);

            var rootServices = serviceCollection.BuildServiceProvider(false);

            var clientFactory = (DefaultHttpClientFactory)rootServices.GetRequiredService<IHttpClientFactory>();

            Assert.Same(
                rootServices.GetRequiredKeyedService<HttpMessageHandler>(Test),
                rootServices.GetRequiredKeyedService<HttpMessageHandler>(Test));

            Assert.NotSame(
                clientFactory.CreateHandler(Test),
                rootServices.GetRequiredKeyedService<HttpMessageHandler>(Test));

            using (var scopeA = rootServices.CreateScope())
            {
                var servicesA = scopeA.ServiceProvider;

                Assert.Same(
                servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test),
                servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test));

                Assert.NotSame(
                    clientFactory.CreateHandler(Test),
                    servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test));

                Assert.NotSame(
                    rootServices.GetRequiredKeyedService<HttpMessageHandler>(Test),
                    servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test));

                using (var scopeB = rootServices.CreateScope())
                {
                    var servicesB = scopeB.ServiceProvider;

                    Assert.Same(
                        servicesB.GetRequiredKeyedService<HttpMessageHandler>(Test),
                        servicesB.GetRequiredKeyedService<HttpMessageHandler>(Test));

                    Assert.NotSame(
                        servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test),
                        servicesB.GetRequiredKeyedService<HttpMessageHandler>(Test));
                }
            }
        }

        [Fact]
        public void HttpMessageHandler_ScopedLifetimeWithValidateScopes_Success()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Scoped);

            var rootServices = serviceCollection.BuildServiceProvider(true);

            var clientFactory = (DefaultHttpClientFactory)rootServices.GetRequiredService<IHttpClientFactory>();

            using (var scopeA = rootServices.CreateScope())
            {
                var servicesA = scopeA.ServiceProvider;

                Assert.Same(
                    servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test),
                    servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test));

                Assert.NotSame(
                    clientFactory.CreateHandler(Test),
                    servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test));

                using (var scopeB = rootServices.CreateScope())
                {
                    var servicesB = scopeB.ServiceProvider;

                    Assert.Same(
                        servicesB.GetRequiredKeyedService<HttpMessageHandler>(Test),
                        servicesB.GetRequiredKeyedService<HttpMessageHandler>(Test));

                    Assert.NotSame(
                        servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test),
                        servicesB.GetRequiredKeyedService<HttpMessageHandler>(Test));
                }
            }
        }

        [Fact]
        public void HttpMessageHandler_ScopedLifetimeWithValidateScopes_ThrowsInvalidOperationException()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Scoped);

            var rootServices = serviceCollection.BuildServiceProvider(true);

            var clientFactory = (DefaultHttpClientFactory)rootServices.GetRequiredService<IHttpClientFactory>();

            Assert.Throws<InvalidOperationException>(
                () => rootServices.GetRequiredKeyedService<HttpMessageHandler>(Test));
        }

        [Fact]
        public void HttpMessageHandler_SingletonLifetime_Success()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Singleton);

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);

            var clientFactory = (DefaultHttpClientFactory)rootServices.GetRequiredService<IHttpClientFactory>();

            Assert.Same(
                rootServices.GetRequiredKeyedService<HttpMessageHandler>(Test),
                rootServices.GetRequiredKeyedService<HttpMessageHandler>(Test));

            Assert.NotSame(
                clientFactory.CreateHandler(Test),
                rootServices.GetRequiredKeyedService<HttpMessageHandler>(Test));

            using (var scopeA = rootServices.CreateScope())
            {
                var servicesA = scopeA.ServiceProvider;

                Assert.Same(
                    servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test),
                    servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test));

                Assert.Same(
                    rootServices.GetRequiredKeyedService<HttpMessageHandler>(Test),
                    servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test));

                Assert.NotSame(
                    clientFactory.CreateHandler(Test),
                    servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test));

                using (var scopeB = rootServices.CreateScope())
                {
                    var servicesB = scopeB.ServiceProvider;

                    Assert.Same(
                        servicesB.GetRequiredKeyedService<HttpMessageHandler>(Test),
                        servicesB.GetRequiredKeyedService<HttpMessageHandler>(Test));

                    Assert.Same(
                        servicesA.GetRequiredKeyedService<HttpMessageHandler>(Test),
                        servicesB.GetRequiredKeyedService<HttpMessageHandler>(Test));
                }
            }
        }

        private static IHttpClientBuilder AddConfiguredNamedClient(ServiceCollection services, string name)
        {
            services.AddKeyedTransient(name, (_, _) => new KeyedPrimaryHandler(name));

            return services
                .AddHttpClient(name, c => c.BaseAddress = GetUri(name))
                .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredKeyedService<KeyedPrimaryHandler>(name));
        }

        private static Uri GetUri(string name) => new Uri($"http://{name}.example.com");
    }
}
