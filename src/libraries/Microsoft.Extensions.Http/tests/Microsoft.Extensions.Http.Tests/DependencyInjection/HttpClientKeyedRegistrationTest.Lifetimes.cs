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
        [Theory]
        [InlineData(typeof(HttpClient))]
        [InlineData(typeof(HttpMessageHandler))]
        public void HttpClientOrHandler_RespectsSingletonLifetime(Type clientOrHandler)
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Singleton);

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes: true);

            var clientFactory = (DefaultHttpClientFactory)rootServices.GetRequiredService<IHttpClientFactory>();
            object CreateWithFactory(Type type, string name) => type == typeof(HttpClient)
                ? clientFactory.CreateClient(name)
                : clientFactory.CreateHandler(name);

            // --- root

            Assert.Same( // same singleton instance resolved twice from the root
                rootServices.GetRequiredKeyedService(clientOrHandler, Test),
                rootServices.GetRequiredKeyedService(clientOrHandler, Test));

            Assert.NotSame( // factory creates a different instance each time
                CreateWithFactory(clientOrHandler, Test),
                rootServices.GetRequiredKeyedService(clientOrHandler, Test));

            // --- scope

            var scopeA = rootServices.CreateScope();
            var servicesA = scopeA.ServiceProvider;

            Assert.Same( // same singleton instance resolved twice from a scope
                servicesA.GetRequiredKeyedService(clientOrHandler, Test),
                servicesA.GetRequiredKeyedService(clientOrHandler, Test));

            Assert.Same( // same singleton instance resolved from root and from scope
                rootServices.GetRequiredKeyedService(clientOrHandler, Test),
                servicesA.GetRequiredKeyedService(clientOrHandler, Test));

            Assert.NotSame( // factory creates a different instance each time
                CreateWithFactory(clientOrHandler, Test),
                servicesA.GetRequiredKeyedService(clientOrHandler, Test));

            // --- other scope

            var scopeB = rootServices.CreateScope();
            var servicesB = scopeB.ServiceProvider;

            Assert.Same( // same singleton instance resolved twice from a different scope
                servicesB.GetRequiredKeyedService(clientOrHandler, Test),
                servicesB.GetRequiredKeyedService(clientOrHandler, Test));

            Assert.Same( // same singleton instance resolved from two different scopes
                servicesA.GetRequiredKeyedService(clientOrHandler, Test),
                servicesB.GetRequiredKeyedService(clientOrHandler, Test));

            // --- scope disposal

            if (clientOrHandler == typeof(HttpClient)) // HttpMessageHandler instances are not disposed
            {
                var clientA = servicesA.GetRequiredKeyedService<HttpClient>(Test);
                var factoryClient = clientFactory.CreateClient(Test);
                AssertAlive(clientA);
                AssertAlive(factoryClient);

                scopeA.Dispose();

                AssertAlive(clientA); // singleton instances are not disposed with the scope
                AssertAlive(factoryClient); // factory instances are not disposed
            }
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

        [Theory]
        [InlineData(typeof(HttpClient), true)]
        [InlineData(typeof(HttpClient), false)]
        [InlineData(typeof(HttpMessageHandler), true)]
        [InlineData(typeof(HttpMessageHandler), false)]
        public void HttpClientOrHandler_RespectsScopedLifetime(Type clientOrHandler, bool validateScopes)
        {
            var serviceCollection = new ServiceCollection();

            AddConfiguredNamedClient(serviceCollection, Test)
                .AddAsKeyed(ServiceLifetime.Scoped);

            var rootServices = serviceCollection.BuildServiceProvider(validateScopes);

            var clientFactory = (DefaultHttpClientFactory)rootServices.GetRequiredService<IHttpClientFactory>();
            object CreateWithFactory(Type type, string name) => type == typeof(HttpClient)
                ? clientFactory.CreateClient(name)
                : clientFactory.CreateHandler(name);

            // --- root

            if (validateScopes)
            {
                Assert.Throws<InvalidOperationException>( // cannot resolve scoped instance from root
                    () => rootServices.GetRequiredKeyedService(clientOrHandler, Test));
            }
            else // the root scope behaves like a "normal" scope, and ends up capturing scoped instances
            {
                Assert.Same( // same root-captured instance resolved each time
                    rootServices.GetRequiredKeyedService(clientOrHandler, Test),
                    rootServices.GetRequiredKeyedService(clientOrHandler, Test));

                Assert.NotSame( // factory creates a different instance each time
                    CreateWithFactory(clientOrHandler, Test),
                    rootServices.GetRequiredKeyedService(clientOrHandler, Test));
            }

            // --- scope

            var scopeA = rootServices.CreateScope();
            var servicesA = scopeA.ServiceProvider;

            Assert.Same( // same scoped instance resolved each time
                servicesA.GetRequiredKeyedService(clientOrHandler, Test),
                servicesA.GetRequiredKeyedService(clientOrHandler, Test));

            Assert.NotSame(
                CreateWithFactory(clientOrHandler, Test),
                servicesA.GetRequiredKeyedService(clientOrHandler, Test));

            if (!validateScopes)
            {
                Assert.NotSame( // scoped instance is different from the root-captured instance
                    rootServices.GetRequiredKeyedService(clientOrHandler, Test),
                    servicesA.GetRequiredKeyedService(clientOrHandler, Test));
            }

            // --- other scope

            var scopeB = rootServices.CreateScope();
            var servicesB = scopeB.ServiceProvider;

            Assert.Same( // same scoped instance resolved each time from a different scope
                servicesB.GetRequiredKeyedService(clientOrHandler, Test),
                servicesB.GetRequiredKeyedService(clientOrHandler, Test));

            Assert.NotSame( // different scoped instances resolved from different scopes
                servicesA.GetRequiredKeyedService(clientOrHandler, Test),
                servicesB.GetRequiredKeyedService(clientOrHandler, Test));

            // --- scope disposal

            if (clientOrHandler == typeof(HttpClient)) // HttpMessageHandler instances are not disposed
            {
                var clientA = servicesA.GetRequiredKeyedService<HttpClient>(Test);
                var clientB = servicesB.GetRequiredKeyedService<HttpClient>(Test);
                var factoryClient = clientFactory.CreateClient(Test);
                var rootClient = validateScopes ? null : rootServices.GetRequiredKeyedService<HttpClient>(Test);

                AssertAlive(clientA);
                AssertAlive(clientB);
                AssertAlive(factoryClient);

                if (!validateScopes)
                {
                    AssertAlive(rootClient);
                }

                scopeA.Dispose();

                AssertDisposed(clientA); // scoped instance disposed with the respective scope
                AssertAlive(clientB);
                AssertAlive(factoryClient);

                if (!validateScopes)
                {
                    AssertAlive(rootClient);
                }
            }
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
