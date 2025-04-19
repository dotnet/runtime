// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public partial class HttpClientKeyedRegistrationTest
    {
        [Fact]
        public void HttpClient_RespectsSingletonLifetime()
        {
            HttpClient clientA, factoryClient;
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Singleton);

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);

            var clientFactory = (DefaultHttpClientFactory)rootServices.GetRequiredService<IHttpClientFactory>();

            // --- root

            Assert.Same(
                rootServices.GetRequiredKeyedService<HttpClient>(Test),
                rootServices.GetRequiredKeyedService<HttpClient>(Test));

            Assert.NotSame(
                clientFactory.CreateClient(Test),
                rootServices.GetRequiredKeyedService<HttpClient>(Test));

            // --- scope

            using (var scopeA = rootServices.CreateScope())
            {
                var servicesA = scopeA.ServiceProvider;

                Assert.Same(
                    servicesA.GetRequiredKeyedService<HttpClient>(Test),
                    servicesA.GetRequiredKeyedService<HttpClient>(Test));

                Assert.Same(
                    rootServices.GetRequiredKeyedService<HttpClient>(Test),
                    servicesA.GetRequiredKeyedService<HttpClient>(Test));

                Assert.NotSame(
                    clientFactory.CreateClient(Test),
                    servicesA.GetRequiredKeyedService<HttpClient>(Test));

                // --- other scope

                using (var scopeB = rootServices.CreateScope())
                {
                    var servicesB = scopeB.ServiceProvider;

                    Assert.Same(
                        servicesB.GetRequiredKeyedService<HttpClient>(Test),
                        servicesB.GetRequiredKeyedService<HttpClient>(Test));

                    Assert.Same(
                        servicesA.GetRequiredKeyedService<HttpClient>(Test),
                        servicesB.GetRequiredKeyedService<HttpClient>(Test));
                }

                clientA = servicesA.GetRequiredKeyedService<HttpClient>(Test);
                factoryClient = clientFactory.CreateClient(Test);
                AssertAlive(clientA);
                AssertAlive(factoryClient);

                scopeA.Dispose();
            }

            AssertAlive(clientA);
            AssertAlive(factoryClient);
        }

        [Fact]
        public void HttpClientOrHandler_Injected_RespectsSingletonLifetime()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Singleton);

            serviceCollection.AddTransient<ServiceWithTestClient>(); // [FromKeyedServices(Test)]
            serviceCollection.AddTransient<ServiceWithTestHandler>(); // [FromKeyedServices(Test)]

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);

            // --- root

            Assert.Same( // same singleton instance injected into a transient service
                rootServices.GetRequiredService<ServiceWithTestClient>().HttpClient,
                rootServices.GetRequiredKeyedService<HttpClient>(Test));

            Assert.Same(
                rootServices.GetRequiredService<ServiceWithTestHandler>().Handler,
                rootServices.GetRequiredKeyedService<HttpMessageHandler>(Test));

            // --- scope

            var scope = rootServices.CreateScope();
            var services = scope.ServiceProvider;

            Assert.Same( // same singleton instance injected into a transient service in a scope
                services.GetRequiredService<ServiceWithTestClient>().HttpClient,
                services.GetRequiredKeyedService<HttpClient>(Test));

            Assert.Same(
                services.GetRequiredService<ServiceWithTestHandler>().Handler,
                services.GetRequiredKeyedService<HttpMessageHandler>(Test));
        }

        [Theory]
        [InlineData(typeof(HttpClient))]
        [InlineData(typeof(HttpMessageHandler))]
        public void HttpClientOrHandler_RespectsTransientLifetime(Type clientOrHandler)
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Transient);

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);

            var clientFactory = (DefaultHttpClientFactory)rootServices.GetRequiredService<IHttpClientFactory>();
            object CreateWithFactory(Type type, string name) => type == typeof(HttpClient)
                ? clientFactory.CreateClient(name)
                : clientFactory.CreateHandler(name);

            // --- root

            Assert.NotSame( // different instance resolved each time from the root
                rootServices.GetRequiredKeyedService(clientOrHandler, Test),
                rootServices.GetRequiredKeyedService(clientOrHandler, Test));

            Assert.NotSame(
                CreateWithFactory(clientOrHandler, Test),
                rootServices.GetRequiredKeyedService(clientOrHandler, Test));

            // --- scope

            var scopeA = rootServices.CreateScope();
            var servicesA = scopeA.ServiceProvider;

            Assert.NotSame( // different instance resolved each time from the scope
                servicesA.GetRequiredKeyedService(clientOrHandler, Test),
                servicesA.GetRequiredKeyedService(clientOrHandler, Test));

            Assert.NotSame(
                rootServices.GetRequiredKeyedService(clientOrHandler, Test),
                servicesA.GetRequiredKeyedService(clientOrHandler, Test));

            Assert.NotSame(
                CreateWithFactory(clientOrHandler, Test),
                servicesA.GetRequiredKeyedService(clientOrHandler, Test));

            // --- other scope

            var scopeB = rootServices.CreateScope();
            var servicesB = scopeB.ServiceProvider;

            Assert.NotSame(
                servicesB.GetRequiredKeyedService(clientOrHandler, Test),
                servicesB.GetRequiredKeyedService(clientOrHandler, Test));

            Assert.NotSame(
                servicesA.GetRequiredKeyedService(clientOrHandler, Test),
                servicesB.GetRequiredKeyedService(clientOrHandler, Test));

            // --- scope disposal

            if (clientOrHandler == typeof(HttpClient)) // HttpMessageHandler instances are not disposed
            {
                var clientA = servicesA.GetRequiredKeyedService<HttpClient>(Test);
                var clientB = servicesB.GetRequiredKeyedService<HttpClient>(Test);
                var rootClient = rootServices.GetRequiredKeyedService<HttpClient>(Test);
                var factoryClient = clientFactory.CreateClient(Test);
                AssertAlive(clientA);
                AssertAlive(clientB);
                AssertAlive(rootClient);
                AssertAlive(factoryClient);

                scopeA.Dispose();

                AssertDisposed(clientA); // transient instance disposed with the respective scope
                AssertAlive(clientB); // transient instance from another scope is not disposed
                AssertAlive(rootClient);
                AssertAlive(factoryClient);
            }
        }

        [Fact]
        public void HttpClientOrHandler_Injected_RespectsTransientLifetime()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Transient);

            serviceCollection.AddTransient<ServiceWithTestClient>(); // [FromKeyedServices(Test)]
            serviceCollection.AddTransient<ServiceWithTestHandler>(); // [FromKeyedServices(Test)]

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);

            // --- root

            Assert.NotSame( // different instance injected each time from the root
                rootServices.GetRequiredService<ServiceWithTestClient>().HttpClient,
                rootServices.GetRequiredService<ServiceWithTestClient>().HttpClient);

            Assert.NotSame(
                rootServices.GetRequiredService<ServiceWithTestHandler>().Handler,
                rootServices.GetRequiredService<ServiceWithTestHandler>().Handler);

            // --- scope

            var scope = rootServices.CreateScope();
            var services = scope.ServiceProvider;

            Assert.NotSame( // different instance injected each time from the scope
                services.GetRequiredService<ServiceWithTestClient>().HttpClient,
                services.GetRequiredService<ServiceWithTestClient>().HttpClient);

            Assert.NotSame(
                services.GetRequiredService<ServiceWithTestHandler>().Handler,
                services.GetRequiredService<ServiceWithTestHandler>().Handler);
        }

        [Fact]
        public void HttpClient_ScopedLifetime_Success()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Scoped);

            var rootServices = serviceCollection.BuildServiceProvider(false);

            var clientFactory = (DefaultHttpClientFactory)rootServices.GetRequiredService<IHttpClientFactory>();

            Assert.Same(
                rootServices.GetRequiredKeyedService<HttpClient>(Test),
                rootServices.GetRequiredKeyedService<HttpClient>(Test));

            Assert.NotSame(
                clientFactory.CreateClient(Test),
                rootServices.GetRequiredKeyedService<HttpClient>(Test));

            var scopeA = rootServices.CreateScope();
            var servicesA = scopeA.ServiceProvider;

            Assert.Same(
            servicesA.GetRequiredKeyedService<HttpClient>(Test),
            servicesA.GetRequiredKeyedService<HttpClient>(Test));

            Assert.NotSame(
                clientFactory.CreateClient(Test),
                servicesA.GetRequiredKeyedService<HttpClient>(Test));

            Assert.NotSame(
                rootServices.GetRequiredKeyedService<HttpClient>(Test),
                servicesA.GetRequiredKeyedService<HttpClient>(Test));

            using (var scopeB = rootServices.CreateScope())
            {
                var servicesB = scopeB.ServiceProvider;

                Assert.Same(
                    servicesB.GetRequiredKeyedService<HttpClient>(Test),
                    servicesB.GetRequiredKeyedService<HttpClient>(Test));

                Assert.NotSame(
                    servicesA.GetRequiredKeyedService<HttpClient>(Test),
                    servicesB.GetRequiredKeyedService<HttpClient>(Test));

                var clientA = servicesA.GetRequiredKeyedService<HttpClient>(Test);
                var clientB = servicesB.GetRequiredKeyedService<HttpClient>(Test);
                var factoryClient = clientFactory.CreateClient(Test);

                AssertAlive(clientA);
                AssertAlive(clientB);
                AssertAlive(factoryClient);

                scopeA.Dispose();

                AssertDisposed(clientA);
                AssertAlive(clientB);
                AssertAlive(factoryClient);
            }
        }

        [Fact]
        public void HttpClient_ScopedLifetimeWithValidateScopes_Success()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Scoped);

            var rootServices = serviceCollection.BuildServiceProvider(true);

            var clientFactory = (DefaultHttpClientFactory)rootServices.GetRequiredService<IHttpClientFactory>();

            var scopeA = rootServices.CreateScope();
            var servicesA = scopeA.ServiceProvider;

            Assert.Same(
                servicesA.GetRequiredKeyedService<HttpClient>(Test),
                servicesA.GetRequiredKeyedService<HttpClient>(Test));

            Assert.NotSame(
                clientFactory.CreateHandler(Test),
                servicesA.GetRequiredKeyedService<HttpClient>(Test));

            using (var scopeB = rootServices.CreateScope())
            {
                var servicesB = scopeB.ServiceProvider;

                Assert.Same(
                    servicesB.GetRequiredKeyedService<HttpClient>(Test),
                    servicesB.GetRequiredKeyedService<HttpClient>(Test));

                Assert.NotSame(
                    servicesA.GetRequiredKeyedService<HttpClient>(Test),
                    servicesB.GetRequiredKeyedService<HttpClient>(Test));

                var clientA = servicesA.GetRequiredKeyedService<HttpClient>(Test);
                var clientB = servicesB.GetRequiredKeyedService<HttpClient>(Test);
                var factoryClient = clientFactory.CreateClient(Test);

                AssertAlive(clientA);
                AssertAlive(clientB);
                AssertAlive(factoryClient);

                scopeA.Dispose();

                AssertDisposed(clientA);
                AssertAlive(clientB);
                AssertAlive(factoryClient);
            }
        }

        [Fact]
        public void HttpClient_ScopedLifetimeWithValidateScopes_ThrowsInvalidOperationException()
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Scoped);

            var rootServices = serviceCollection.BuildServiceProvider(true);

            var clientFactory = (DefaultHttpClientFactory)rootServices.GetRequiredService<IHttpClientFactory>();

            Assert.Throws<InvalidOperationException>(
                () => rootServices.GetRequiredKeyedService<HttpClient>(Test));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HttpClientOrHandler_Injected_RespectsScopedLifetime(bool validateScopes)
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Scoped);

            serviceCollection.AddTransient<ServiceWithTestClient>(); // [FromKeyedServices(Test)]
            serviceCollection.AddTransient<ServiceWithTestHandler>(); // [FromKeyedServices(Test)]

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes);

            // --- root

            if (validateScopes)
            {
                Assert.Throws<InvalidOperationException>( // cannot inject scoped dependency from root
                    rootServices.GetRequiredService<ServiceWithTestClient>);
                Assert.Throws<InvalidOperationException>( // cannot inject scoped dependency from root
                    rootServices.GetRequiredService<ServiceWithTestHandler>);
            }
            else
            {
                Assert.Same( // same root-captured instance injected each time from the root
                    rootServices.GetRequiredService<ServiceWithTestClient>().HttpClient,
                    rootServices.GetRequiredService<ServiceWithTestClient>().HttpClient);

                Assert.Same(
                    rootServices.GetRequiredService<ServiceWithTestHandler>().Handler,
                    rootServices.GetRequiredService<ServiceWithTestHandler>().Handler);
            }

            // --- scope

            var scopeA = rootServices.CreateScope();
            var servicesA = scopeA.ServiceProvider;

            Assert.Same( // same scoped instance injected each time from the scope
                servicesA.GetRequiredService<ServiceWithTestClient>().HttpClient,
                servicesA.GetRequiredService<ServiceWithTestClient>().HttpClient);

            Assert.Same(
                servicesA.GetRequiredService<ServiceWithTestHandler>().Handler,
                servicesA.GetRequiredService<ServiceWithTestHandler>().Handler);

            if (!validateScopes)
            {
                Assert.NotSame( // scoped instance is different from the root-captured instance
                    rootServices.GetRequiredService<ServiceWithTestClient>().HttpClient,
                    servicesA.GetRequiredService<ServiceWithTestClient>().HttpClient);

                Assert.NotSame( // scoped instance is different from the root-captured instance
                    rootServices.GetRequiredService<ServiceWithTestHandler>().Handler,
                    servicesA.GetRequiredService<ServiceWithTestHandler>().Handler);
            }

            // --- other scope

            var scopeB = rootServices.CreateScope();
            var servicesB = scopeB.ServiceProvider;

            Assert.Same( // same scoped instance injected each time from the same scope
                servicesB.GetRequiredService<ServiceWithTestClient>().HttpClient,
                servicesB.GetRequiredService<ServiceWithTestClient>().HttpClient);

            Assert.NotSame( // different scoped instances injected from different scopes
                servicesB.GetRequiredService<ServiceWithTestHandler>().Handler,
                servicesA.GetRequiredService<ServiceWithTestHandler>().Handler);
        }

        private static void AssertAlive(HttpClient client)
            => Assert.Equal(Test, SendDummyRequest(client));

        private static void AssertDisposed(HttpClient client)
        {
            var exception = Assert.Throws<ObjectDisposedException>(() => SendDummyRequest(client));
            Assert.Contains(typeof(HttpClient).FullName, exception.Message);
        }

        private static string SendDummyRequest(HttpClient client)
            => client.GetStringAsync("/").GetAwaiter().GetResult(); // KeyedPrimaryHandler returns a string with the client name
    }
}
