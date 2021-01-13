// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Http
{
    public class ScopeTest
    {
        private static readonly TimeSpan HandlerLifetime = TimeSpan.FromSeconds(5);

        private const string NamedClientName = "test";
        private const string TypedClientName = nameof(TypedClient);

        [Fact]
        public void MessageHandler_ManualScope()
        {
            int scopedServiceInstanceCount = 0;

            var services = PrepareContainer(
                preserveExistingScope: false,
                registerTypedClient: false,
                isExpiryTest: false,
                () => scopedServiceInstanceCount++
            );

            var name = NamedClientName;

            // ---

            HttpMessageHandler topHandler, sameScopeTopHandler, otherScopeTopHandler;
            LifetimeTrackingHttpMessageHandler handlerFromFactory;
            ScopedService scopedServiceFromContainer, scopedServiceFromContainerOtherScope;

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                var factory = scopeServices.GetRequiredService<IHttpMessageHandlerFactory>();

                topHandler = factory.CreateHandler(name);
                handlerFromFactory = GetHandlerFromFactory(factory, name);
                sameScopeTopHandler = factory.CreateHandler(name);
                Assert.Equal(2, scopedServiceInstanceCount); // 1 for container scope + 1 for handler lifetime scope
            }

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainerOtherScope = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(3, scopedServiceInstanceCount); // 2 for 2 container scopes + 1 for handler lifetime scope

                var factory = scopeServices.GetRequiredService<IHttpMessageHandlerFactory>();
                otherScopeTopHandler = factory.CreateHandler(name);
                Assert.Equal(3, scopedServiceInstanceCount); // 2 for 2 container scopes + 1 for handler lifetime scope
            }

            var scopedServiceFromHandler = ExtractScopedService(topHandler);
            var scopedServiceFromOtherScopeHandler = ExtractScopedService(otherScopeTopHandler);

            // ---

            Assert.Same(topHandler, handlerFromFactory); // full handler chain is cached in factory
            Assert.Same(topHandler, sameScopeTopHandler); // within lifetime handlers are cached
            Assert.Same(sameScopeTopHandler, otherScopeTopHandler); // within lifetime handlers are cached

            Assert.NotSame(scopedServiceFromContainer, scopedServiceFromContainerOtherScope); // DI expected behavior
            Assert.NotSame(scopedServiceFromContainer, scopedServiceFromHandler); // handler has custom scope unrelated to outer scope
            Assert.NotSame(scopedServiceFromContainerOtherScope, scopedServiceFromOtherScopeHandler); // handler has custom scope unrelated to outer scope
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task MessageHandler_ManualScope_Expiry()
        {
            int scopedServiceInstanceCount = 0;

            var services = PrepareContainer(
                preserveExistingScope: false,
                registerTypedClient: false,
                isExpiryTest: true,
                () => scopedServiceInstanceCount++
            );

            var name = NamedClientName;

            // ---

            HttpMessageHandler topHandler, newLifetimeTopHandler;
            ScopedService scopedServiceFromContainer;

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                var factory = scopeServices.GetRequiredService<IHttpMessageHandlerFactory>();

                topHandler = factory.CreateHandler(name);
                Assert.Equal(2, scopedServiceInstanceCount); // 1 for container scope + 1 for handler lifetime scope

                await WaitForExpiry(factory, name);

                newLifetimeTopHandler = factory.CreateHandler(name);
                Assert.Equal(3, scopedServiceInstanceCount); // 1 for container scope + 2 for 2 handler lifetime scopes
            }

            var primaryHandler = GetPrimaryHandler(topHandler);
            var newLifetimePrimaryHandler = GetPrimaryHandler(newLifetimeTopHandler);

            var scopedServiceFromHandler = ExtractScopedService(topHandler);
            var scopedServiceFromNewLifetimeHandler = ExtractScopedService(newLifetimeTopHandler);

            // ---

            Assert.NotSame(topHandler, newLifetimeTopHandler); // lifetime expired, so new handler is created

            Assert.NotSame(primaryHandler, newLifetimePrimaryHandler); // in new lifetime, whole chain is created incl. primary handler

            Assert.NotSame(scopedServiceFromHandler, scopedServiceFromNewLifetimeHandler); // custom scope is bound to lifetime
        }

        [Fact]
        public void MessageHandler_PreserveExistingScope()
        {
            int scopedServiceInstanceCount = 0;

            var services = PrepareContainer(
                preserveExistingScope: true,
                registerTypedClient: false,
                isExpiryTest: false,
                () => scopedServiceInstanceCount++
            );

            var name = NamedClientName;

            // ---

            HttpMessageHandler topHandler, sameScopeTopHandler, otherScopeTopHandler;
            LifetimeTrackingHttpMessageHandler handlerFromFactory;
            ScopedService scopedServiceFromContainer, scopedServiceFromContainerOtherScope;

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                var factory = scopeServices.GetRequiredService<IScopedHttpMessageHandlerFactory>();

                topHandler = factory.CreateHandler(name);
                handlerFromFactory = GetHandlerFromFactory(factory, name);
                sameScopeTopHandler = factory.CreateHandler(name);
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope
            }

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainerOtherScope = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(2, scopedServiceInstanceCount); // 2 for 2 container scopes

                var factory = scopeServices.GetRequiredService<IScopedHttpMessageHandlerFactory>();
                otherScopeTopHandler = factory.CreateHandler(name);
                Assert.Equal(2, scopedServiceInstanceCount); // 2 for 2 container scopes
            }

            var primaryHandler = GetPrimaryHandler(topHandler);
            var scopedServiceFromHandler = ExtractScopedService(topHandler);

            var otherScopePrimaryHandler = GetPrimaryHandler(otherScopeTopHandler);
            var scopedServiceFromOtherScopeHandler = ExtractScopedService(otherScopeTopHandler);

            // ---

            Assert.Same(topHandler, sameScopeTopHandler); // within the scope handlers are cached
            Assert.NotSame(topHandler, otherScopeTopHandler); // in another scope different top handler will be created

            Assert.Same(primaryHandler, handlerFromFactory.InnerHandler); // only primary handler is cached in factory
            Assert.Same(primaryHandler, otherScopePrimaryHandler); // lifetime not expired between scopes, so old primary handler is reused

            Assert.NotSame(scopedServiceFromContainer, scopedServiceFromContainerOtherScope); // DI expected behavior
            Assert.Same(scopedServiceFromContainer, scopedServiceFromHandler); // outer scope is preserved in handler
            Assert.Same(scopedServiceFromContainerOtherScope, scopedServiceFromOtherScopeHandler); // outer scope is preserved in handler
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task MessageHandler_PreserveExistingScope_Expiry()
        {
            int scopedServiceInstanceCount = 0;

            var services = PrepareContainer(
                preserveExistingScope: true,
                registerTypedClient: false,
                isExpiryTest: true,
                () => scopedServiceInstanceCount++
            );

            var name = NamedClientName;

            // ---

            HttpMessageHandler topHandler, newLifetimeSameScopeTopHandler, otherScopeTopHandler;
            HttpMessageHandler handlerFromFactory, newLifetimeSameScopeHandlerFromFactory, otherScopeHandlerFromFactory;

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                var scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                var factory = scopeServices.GetRequiredService<IScopedHttpMessageHandlerFactory>();

                topHandler = factory.CreateHandler(name);
                handlerFromFactory = GetHandlerFromFactory(factory, name);
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                await WaitForExpiry(factory, name);

                newLifetimeSameScopeTopHandler = factory.CreateHandler(name);
                newLifetimeSameScopeHandlerFromFactory = GetHandlerFromFactory(factory, name);
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope
            }

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                var factory = scopeServices.GetRequiredService<IScopedHttpMessageHandlerFactory>();

                otherScopeTopHandler = factory.CreateHandler(name);
                otherScopeHandlerFromFactory = GetHandlerFromFactory(factory, name);
                Assert.Equal(2, scopedServiceInstanceCount); // 2 for 2 container scopes
            }

            // ---

            Assert.Same(topHandler, newLifetimeSameScopeTopHandler); // within the scope handlers are cached

            Assert.NotNull(handlerFromFactory); // in scope 1 handler is successfully created and cached (both in singleton and scope cache)
            Assert.Null(newLifetimeSameScopeHandlerFromFactory); // after expiry singleton cache is empty
            Assert.NotNull(otherScopeHandlerFromFactory); // in scope 2 new handler is successfully created and cached
            Assert.NotSame(handlerFromFactory, otherScopeHandlerFromFactory);
        }

        [Fact]
        public void NamedClient_ManualScope()
        {
            int scopedServiceInstanceCount = 0;

            var services = PrepareContainer(
                preserveExistingScope: false,
                registerTypedClient: false,
                isExpiryTest: false,
                () => scopedServiceInstanceCount++
            );

            var name = NamedClientName;

            // ---

            HttpClient client, sameScopeClient, otherScopeClient;
            HttpMessageHandler topHandler, sameScopeTopHandler, otherScopeTopHandler;

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                var scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                var factory = scopeServices.GetRequiredService<IHttpClientFactory>();

                client = factory.CreateClient(name);
                topHandler = GetHandlerFromFactory(factory, name);
                sameScopeClient = factory.CreateClient(name);
                sameScopeTopHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(2, scopedServiceInstanceCount); // 1 for container scope + 1 for handler lifetime scope
            }

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                var scopedServiceFromContainerOtherScope = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(3, scopedServiceInstanceCount); // 2 for 2 container scopes + 1 for handler lifetime scope

                var factory = scopeServices.GetRequiredService<IHttpClientFactory>();
                otherScopeClient = factory.CreateClient(name);
                otherScopeTopHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(3, scopedServiceInstanceCount); // 2 for 2 container scopes + 1 for handler lifetime scope
            }

            // ---

            Assert.NotSame(client, sameScopeClient); // re-created each time
            Assert.NotSame(sameScopeClient, otherScopeClient); // re-created each time

            Assert.Same(topHandler, sameScopeTopHandler); // within lifetime handlers are cached
            Assert.Same(topHandler, otherScopeTopHandler); // within lifetime handlers are cached
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task NamedClient_ManualScope_Expiry()
        {
            int scopedServiceInstanceCount = 0;

            var services = PrepareContainer(
                preserveExistingScope: false,
                registerTypedClient: false,
                isExpiryTest: true,
                () => scopedServiceInstanceCount++
            );

            var name = NamedClientName;

            // ---

            HttpMessageHandler topHandler, newLifetimeSameScopeTopHandler;

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                var scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                var factory = scopeServices.GetRequiredService<IHttpClientFactory>();

                var client = factory.CreateClient(name);
                topHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(2, scopedServiceInstanceCount); // 1 for container scope + 1 for handler lifetime scope

                await WaitForExpiry(factory, name);

                var newLifetimeSameScopeClient = factory.CreateClient(name);
                newLifetimeSameScopeTopHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(3, scopedServiceInstanceCount); // 1 for container scope + 2 for 2 handler lifetime scopes
            }

            // ---

            Assert.NotSame(topHandler, newLifetimeSameScopeTopHandler); // lifetime expired, so new handler is created
        }

        [Fact]
        public void NamedClient_PreserveExistingScope()
        {
            int scopedServiceInstanceCount = 0;

            var services = PrepareContainer(
                preserveExistingScope: true,
                registerTypedClient: false,
                isExpiryTest: false,
                () => scopedServiceInstanceCount++
            );

            var name = NamedClientName;

            // ---

            HttpClient client, sameScopeClient, otherScopeClient;
            HttpMessageHandler handler, sameScopeHandler, otherScopeHandler;
            ScopedService scopedServiceFromContainer, scopedServiceFromContainerOtherScope;

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                var factory = scopeServices.GetRequiredService<IScopedHttpClientFactory>();

                client = factory.CreateClient(name);
                handler = GetHandlerFromFactory(factory, name);
                sameScopeClient = factory.CreateClient(name);
                sameScopeHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope
            }

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainerOtherScope = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(2, scopedServiceInstanceCount); // 2 for 2 container scopes

                var factory = scopeServices.GetRequiredService<IScopedHttpClientFactory>();
                otherScopeClient = factory.CreateClient(name);
                otherScopeHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(2, scopedServiceInstanceCount); // 2 for 2 container scopes
            }

            // ---

            Assert.NotSame(client, sameScopeClient); // re-created each time
            Assert.NotSame(sameScopeClient, otherScopeClient); // re-created each time

            Assert.Same(handler, sameScopeHandler); // within lifetime primary handlers are cached
            Assert.Same(handler, otherScopeHandler); // lifetime not expired between scopes, so old primary handler is reused
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task NamedClient_PreserveExistingScope_Expiry()
        {
            int scopedServiceInstanceCount = 0;

            var services = PrepareContainer(
                preserveExistingScope: true,
                registerTypedClient: false,
                isExpiryTest: true,
                () => scopedServiceInstanceCount++
            );

            var name = NamedClientName;

            // ---

            HttpClient client, newLifetimeSameScopeClient;
            HttpMessageHandler handler, newLifetimeSameScopeHandler;

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                var scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                var factory = scopeServices.GetRequiredService<IScopedHttpClientFactory>();

                client = factory.CreateClient(name);
                handler = GetHandlerFromFactory(factory, name);
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                await WaitForExpiry(factory, name);

                newLifetimeSameScopeClient = factory.CreateClient(name);
                newLifetimeSameScopeHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope
            }

            // ---

            Assert.NotNull(newLifetimeSameScopeClient); // client is successfully created using scope cached handler
            Assert.Null(newLifetimeSameScopeHandler); // lifetime expired, so singleton cache is empty. full chain is cached on scope level
        }

        [Fact]
        public void TypedClient_ManualScope()
        {
            int scopedServiceInstanceCount = 0;

            var services = PrepareContainer(
                preserveExistingScope: false,
                registerTypedClient: true,
                isExpiryTest: false,
                () => scopedServiceInstanceCount++
            );

            var name = TypedClientName;

            // ---

            TypedClient typedClient, sameScopeTypedClient, otherScopeTypedClient;
            HttpMessageHandler topHandler, sameScopeTopHandler, otherScopeTopHandler;
            ScopedService scopedServiceFromContainer, scopedServiceFromContainerOtherScope;

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                var factory = scopeServices.GetRequiredService<IHttpClientFactory>();

                typedClient = scopeServices.GetRequiredService<TypedClient>();
                topHandler = GetHandlerFromFactory(factory, name);
                sameScopeTypedClient = scopeServices.GetRequiredService<TypedClient>();
                sameScopeTopHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(2, scopedServiceInstanceCount); // 1 for container scope + 1 for handler lifetime scope
            }

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainerOtherScope = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(3, scopedServiceInstanceCount); // 2 for 2 container scopes + 1 for handler lifetime scope

                var factory = scopeServices.GetRequiredService<IHttpClientFactory>();
                otherScopeTypedClient = scopeServices.GetRequiredService<TypedClient>();
                otherScopeTopHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(3, scopedServiceInstanceCount); // 2 for 2 container scopes + 1 for handler lifetime scope
            }

            // ---

            Assert.NotSame(typedClient, sameScopeTypedClient); // transient
            Assert.NotSame(sameScopeTypedClient, otherScopeTypedClient); // transient

            Assert.NotSame(typedClient.HttpClient, sameScopeTypedClient.HttpClient); // re-created each time
            Assert.NotSame(sameScopeTypedClient.HttpClient, otherScopeTypedClient.HttpClient); // re-created each time

            Assert.Same(scopedServiceFromContainer, typedClient.ScopedService); // typed client instances are created in outer scope (both in scope 1)
            Assert.Same(typedClient.ScopedService, sameScopeTypedClient.ScopedService); // typed client instances are created in outer scope (both in scope 1)
            Assert.NotSame(typedClient.ScopedService, otherScopeTypedClient.ScopedService); // typed client instances are created in outer scope (scope 1 vs scope 2)
            Assert.Same(scopedServiceFromContainerOtherScope, otherScopeTypedClient.ScopedService); // typed client instances are created in outer scope (both in scope 2)

            Assert.Same(topHandler, sameScopeTopHandler); // within lifetime handlers are cached
            Assert.Same(sameScopeTopHandler, otherScopeTopHandler); // within lifetime handlers are cached
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task TypedClient_ManualScope_Expiry()
        {
            int scopedServiceInstanceCount = 0;

            var services = PrepareContainer(
                preserveExistingScope: false,
                registerTypedClient: true,
                isExpiryTest: true,
                () => scopedServiceInstanceCount++
            );

            var name = TypedClientName;

            // ---

            TypedClient typedClient, newLifetimeSameScopeTypedClient;
            HttpMessageHandler topHandler, newLifetimeSameScopeTopHandler;

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                var scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                var factory = scopeServices.GetRequiredService<IHttpClientFactory>();

                typedClient = scopeServices.GetRequiredService<TypedClient>();
                topHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(2, scopedServiceInstanceCount); // 1 for container scope + 1 for handler lifetime scope

                await WaitForExpiry(factory, name);

                newLifetimeSameScopeTypedClient = scopeServices.GetRequiredService<TypedClient>();
                newLifetimeSameScopeTopHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(3, scopedServiceInstanceCount); // 1 for container scope + 2 for 2 handler lifetime scopes
            }

            // ---

            Assert.NotSame(typedClient, newLifetimeSameScopeTypedClient); // transient
            Assert.NotSame(typedClient.HttpClient, newLifetimeSameScopeTypedClient.HttpClient); // re-created each time

            Assert.Same(typedClient.ScopedService, newLifetimeSameScopeTypedClient.ScopedService); // typed client instances are created in outer scope

            Assert.NotSame(topHandler, newLifetimeSameScopeTopHandler); // lifetime expired, so new handler is created
        }

        [Fact]
        public void TypedClient_PreserveExistingScope()
        {
            int scopedServiceInstanceCount = 0;

            var services = PrepareContainer(
                preserveExistingScope: true,
                registerTypedClient: true,
                isExpiryTest: false,
                () => scopedServiceInstanceCount++
            );

            var name = TypedClientName;

            // ---

            TypedClient typedClient, sameScopeTypedClient, otherScopeTypedClient;
            HttpMessageHandler handler, sameScopeHandler, otherScopeHandler;
            ScopedService scopedServiceFromContainer, scopedServiceFromContainerOtherScope;

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                var factory = scopeServices.GetRequiredService<IScopedHttpClientFactory>();

                typedClient = scopeServices.GetRequiredService<TypedClient>();
                handler = GetHandlerFromFactory(factory, name);
                sameScopeTypedClient = scopeServices.GetRequiredService<TypedClient>();
                sameScopeHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope
            }

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainerOtherScope = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(2, scopedServiceInstanceCount); // 2 for 2 container scopes

                var factory = scopeServices.GetRequiredService<IScopedHttpClientFactory>();
                otherScopeTypedClient = scopeServices.GetRequiredService<TypedClient>();
                otherScopeHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(2, scopedServiceInstanceCount); // 2 for 2 container scopes
            }

            // ---

            Assert.NotSame(typedClient, sameScopeTypedClient); // transient
            Assert.NotSame(sameScopeTypedClient, otherScopeTypedClient); // transient

            Assert.NotSame(typedClient.HttpClient, sameScopeTypedClient.HttpClient); // re-created each time
            Assert.NotSame(sameScopeTypedClient.HttpClient, otherScopeTypedClient.HttpClient); // re-created each time

            Assert.Same(scopedServiceFromContainer, typedClient.ScopedService); // typed client instances are created in outer scope (both in scope 1)
            Assert.Same(typedClient.ScopedService, sameScopeTypedClient.ScopedService); // typed client instances are created in outer scope (both in scope 1)
            Assert.NotSame(typedClient.ScopedService, otherScopeTypedClient.ScopedService); // typed client instances are created in outer scope (scope 1 vs scope 2)
            Assert.Same(scopedServiceFromContainerOtherScope, otherScopeTypedClient.ScopedService); // typed client instances are created in outer scope (both in scope 2)

            Assert.Same(handler, sameScopeHandler); // within lifetime primary handlers are cached

            Assert.Same(handler, otherScopeHandler); // lifetime not expired between scopes, so old primary handler is reused
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task TypedClient_PreserveExistingScope_Expiry()
        {
            int scopedServiceInstanceCount = 0;

            var services = PrepareContainer(
                preserveExistingScope: true,
                registerTypedClient: true,
                isExpiryTest: true,
                () => scopedServiceInstanceCount++
            );

            var name = TypedClientName;

            // ---

            TypedClient typedClient, newLifetimeSameScopeTypedClient;
            HttpMessageHandler handler, newLifetimeSameScopeHandler;

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                var scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                var factory = scopeServices.GetRequiredService<IScopedHttpClientFactory>();

                typedClient = scopeServices.GetRequiredService<TypedClient>();
                handler = GetHandlerFromFactory(factory, name);
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                await WaitForExpiry(factory, name);

                newLifetimeSameScopeTypedClient = scopeServices.GetRequiredService<TypedClient>();
                newLifetimeSameScopeHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope
            }

            // ---

            Assert.Same(typedClient.ScopedService, newLifetimeSameScopeTypedClient.ScopedService); // typed client instances are created in outer scope

            Assert.Null(newLifetimeSameScopeHandler); // lifetime expired, so singleton cache is empty. full handler chain is cached on scope level
        }

        private ServiceProvider PrepareContainer(bool preserveExistingScope, bool registerTypedClient, bool isExpiryTest, Action scopedServiceInstanceCountIncrement)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped(sc =>
            {
                scopedServiceInstanceCountIncrement.Invoke();
                return new ScopedService();
            });

            serviceCollection.AddScoped<MessageHandlerWithScopedService>();

            if (isExpiryTest)
            {
                serviceCollection.AddSingleton<DefaultHttpClientFactory, TestHttpClientFactory>(); // substitute default factory to enable await expiry
            }

            var builder = registerTypedClient
                ? serviceCollection.AddHttpClient<TypedClient>()
                : serviceCollection.AddHttpClient(NamedClientName);

            builder.SetPreserveExistingScope(preserveExistingScope)
                .AddHttpMessageHandler<MessageHandlerWithScopedService>();

            if (isExpiryTest)
            {
                builder.SetHandlerLifetime(HandlerLifetime);
            }

            return serviceCollection.BuildServiceProvider();
        }

        private HttpMessageHandler GetPrimaryHandler(HttpMessageHandler topHandler)
        {
            var primaryHandler = topHandler;
            while (primaryHandler is DelegatingHandler)
            {
                primaryHandler = ((DelegatingHandler)primaryHandler).InnerHandler;
            }

            return primaryHandler;
        }

        private ScopedService ExtractScopedService(HttpMessageHandler topHandler)
        {
            var lifetimeHandler = (LifetimeTrackingHttpMessageHandler)topHandler;
            var loggingHandler = (LoggingScopeHttpMessageHandler)lifetimeHandler.InnerHandler;
            var scopeHandler = (MessageHandlerWithScopedService)loggingHandler.InnerHandler;
            return scopeHandler.ScopedService;
        }

        private LifetimeTrackingHttpMessageHandler GetHandlerFromFactory(IHttpMessageHandlerFactory factory, string name) => GetHandlerFromFactory((DefaultHttpClientFactory)factory, name);
        private LifetimeTrackingHttpMessageHandler GetHandlerFromFactory(IHttpClientFactory factory, string name) => GetHandlerFromFactory((DefaultHttpClientFactory)factory, name);

        private LifetimeTrackingHttpMessageHandler GetHandlerFromFactory(DefaultHttpClientFactory factory, string name)
        {
            var entry = factory._activeHandlers[name];
            Assert.False(entry.IsPrimary);
            return entry.Handler;
        }

        private LifetimeTrackingHttpMessageHandler GetHandlerFromFactory(IScopedHttpMessageHandlerFactory factory, string name) => GetHandlerFromFactory((DefaultScopedHttpClientFactory)factory, name);
        private LifetimeTrackingHttpMessageHandler GetHandlerFromFactory(IScopedHttpClientFactory factory, string name) => GetHandlerFromFactory((DefaultScopedHttpClientFactory)factory, name);

        private LifetimeTrackingHttpMessageHandler GetHandlerFromFactory(DefaultScopedHttpClientFactory factory, string name)
        {
            var activeHandlers = factory._singletonFactory._activeHandlers;
            if (activeHandlers.TryGetValue(name, out var entry))
            {
                Assert.True(entry.IsPrimary);
                return entry.Handler;
            }
            return null;
        }

        private class ScopedService
        {
        }

        private class MessageHandlerWithScopedService : DelegatingHandler
        {
            public ScopedService ScopedService { get; private set; }

            public MessageHandlerWithScopedService(ScopedService scopedService)
            {
                ScopedService = scopedService;
            }
        }

        private class TypedClient
        {
            public ScopedService ScopedService { get; private set; }
            public HttpClient HttpClient { get; private set; }

            public TypedClient(ScopedService scopedService, HttpClient httpClient)
            {
                ScopedService = scopedService;
                HttpClient = httpClient;
            }
        }

        private static async Task WaitForExpiry(IHttpMessageHandlerFactory factory, string name) => await WaitForExpiry((TestHttpClientFactory)factory, name);
        private static async Task WaitForExpiry(IHttpClientFactory factory, string name) => await WaitForExpiry((TestHttpClientFactory)factory, name);
        private static async Task WaitForExpiry(IScopedHttpMessageHandlerFactory factory, string name)
            => await WaitForExpiry((TestHttpClientFactory)((DefaultScopedHttpClientFactory)factory)._singletonFactory, name);
        private static async Task WaitForExpiry(IScopedHttpClientFactory factory, string name)
            => await WaitForExpiry((TestHttpClientFactory)((DefaultScopedHttpClientFactory)factory)._singletonFactory, name);

        private static async Task WaitForExpiry(TestHttpClientFactory factory, string name) => await factory.WaitForExpiry(name);

        // allows awaiting on expiry timers
        private class TestHttpClientFactory : DefaultHttpClientFactory
        {
            public TestHttpClientFactory(
                IServiceProvider services,
                IServiceScopeFactory scopeFactory,
                ILoggerFactory loggerFactory,
                IOptionsMonitor<HttpClientFactoryOptions> optionsMonitor,
                IEnumerable<IHttpMessageHandlerBuilderFilter> filters)
                : base(services, scopeFactory, loggerFactory, optionsMonitor, filters)
            {
                ActiveEntryState = new Dictionary<ActiveHandlerTrackingEntry, TaskCompletionSource<bool>>();
            }

            public Dictionary<ActiveHandlerTrackingEntry, TaskCompletionSource<bool>> ActiveEntryState { get; }

            internal override void StartHandlerEntryTimer(ActiveHandlerTrackingEntry entry)
            {
                lock (ActiveEntryState)
                {
                    if (!ActiveEntryState.ContainsKey(entry))
                    {
                        ActiveEntryState.Add(entry, new TaskCompletionSource<bool>());
                    }

                    base.StartHandlerEntryTimer(entry);
                }
            }

            internal override void ExpiryTimer_Tick(object state)
            {
                lock (ActiveEntryState)
                {
                    base.ExpiryTimer_Tick(state);

                    var entry = (ActiveHandlerTrackingEntry)state;
                    TaskCompletionSource<bool> completionSource = ActiveEntryState[entry];
                    ActiveEntryState.Remove(entry);
                    completionSource.SetResult(true);
                }
            }

            internal async Task WaitForExpiry(string name)
            {
                Task t;

                lock (ActiveEntryState)
                {
                    if (!_activeHandlers.TryGetValue(name, out var entry))
                    {
                        t = Task.CompletedTask;
                    }
                    if (!ActiveEntryState.TryGetValue(entry, out var completionSource))
                    {
                        t = Task.CompletedTask;
                    }
                    t = completionSource.Task;
                }

                await t;
            }
        }
    }
}
