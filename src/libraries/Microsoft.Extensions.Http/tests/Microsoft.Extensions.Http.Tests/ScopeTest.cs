// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Logging;
using Xunit;

namespace Microsoft.Extensions.Http
{
    public class ScopeTest
    {
        private const int HandlerLifetime = 5;

        [Fact]
        public void MessageHandler_PreserveExistingScope_False()
        {
            var serviceCollection = new ServiceCollection();

            int scopedServiceInstanceCount = 0;
            serviceCollection.AddScoped(sc =>
            {
                scopedServiceInstanceCount++;
                return new ScopedService();
            });

            serviceCollection.AddScoped<MessageHandlerWithScopedService>();

            string name = "test";

            serviceCollection.AddHttpClient(name)
                .SetPreserveExistingScope(false)
                .AddHttpMessageHandler<MessageHandlerWithScopedService>()
                .SetHandlerLifetime(TimeSpan.FromSeconds(HandlerLifetime));

            var services = serviceCollection.BuildServiceProvider();

            // ---

            HttpMessageHandler topHandler, sameScopeTopHandler, newLifetimeSameScopeTopHandler, otherScopeTopHandler;
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

                Thread.Sleep(TimeSpan.FromSeconds(HandlerLifetime + 1));

                newLifetimeSameScopeTopHandler = factory.CreateHandler(name);
                Assert.Equal(3, scopedServiceInstanceCount); // 1 for container scope + 2 for 2 handler lifetime scopes
            }

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainerOtherScope = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(4, scopedServiceInstanceCount); // 2 for 2 container scopes + 2 for 2 handler lifetime scopes

                var factory = scopeServices.GetRequiredService<IHttpMessageHandlerFactory>();
                otherScopeTopHandler = factory.CreateHandler(name);
                Assert.Equal(4, scopedServiceInstanceCount); // 2 for 2 container scopes + 2 for 2 handler lifetime scopes
            }

            var primaryHandler = GetPrimaryHandler(topHandler);
            var scopedServiceFromHandler = ExtractScopedService(topHandler);

            var newLifetimeSameScopePrimaryHandler = GetPrimaryHandler(newLifetimeSameScopeTopHandler);
            var scopedServiceFromNewLifetimeSameScopeHandler = ExtractScopedService(newLifetimeSameScopeTopHandler);

            var scopedServiceFromOtherScopeHandler = ExtractScopedService(otherScopeTopHandler);

            // ---

            Assert.Same(topHandler, handlerFromFactory); // full handler chain is cached in factory
            Assert.Same(topHandler, sameScopeTopHandler); // within lifetime handlers are cached
            Assert.Same(newLifetimeSameScopeTopHandler, otherScopeTopHandler); // within lifetime handlers are cached
            Assert.NotSame(topHandler, newLifetimeSameScopeTopHandler); // lifetime expired, so new handler is created

            Assert.NotSame(primaryHandler, newLifetimeSameScopePrimaryHandler); // in new lifetime, whole chain is created incl. primary handler

            Assert.NotSame(scopedServiceFromContainer, scopedServiceFromContainerOtherScope); // DI expected behavior
            Assert.NotSame(scopedServiceFromContainer, scopedServiceFromHandler); // handler has custom scope unrelated to outer scope
            Assert.NotSame(scopedServiceFromContainerOtherScope, scopedServiceFromOtherScopeHandler); // handler has custom scope unrelated to outer scope
            Assert.NotSame(scopedServiceFromHandler, scopedServiceFromNewLifetimeSameScopeHandler); // custom scope is bound to lifetime
        }

        [Fact]
        public void MessageHandler_PreserveExistingScope_True()
        {
            var serviceCollection = new ServiceCollection();

            int scopedServiceInstanceCount = 0;
            serviceCollection.AddScoped(sc =>
            {
                scopedServiceInstanceCount++;
                return new ScopedService();
            });

            serviceCollection.AddScoped<MessageHandlerWithScopedService>();

            string name = "test";

            serviceCollection.AddHttpClient(name)
                .SetPreserveExistingScope(true)
                .AddHttpMessageHandler<MessageHandlerWithScopedService>()
                .SetHandlerLifetime(TimeSpan.FromSeconds(HandlerLifetime));

            var services = serviceCollection.BuildServiceProvider();

            // ---

            HttpMessageHandler topHandler, sameScopeTopHandler, newLifetimeSameScopeTopHandler, otherScopeTopHandler, thirdScopeTopHandler;
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

                Thread.Sleep(TimeSpan.FromSeconds(HandlerLifetime + 1));

                newLifetimeSameScopeTopHandler = factory.CreateHandler(name);
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

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                var factory = scopeServices.GetRequiredService<IScopedHttpMessageHandlerFactory>();
                thirdScopeTopHandler = factory.CreateHandler(name);
                Assert.Equal(3, scopedServiceInstanceCount); // 3 for 3 container scopes
            }

            var primaryHandler = GetPrimaryHandler(topHandler);
            var scopedServiceFromHandler = ExtractScopedService(topHandler);

            var otherScopePrimaryHandler = GetPrimaryHandler(otherScopeTopHandler);
            var scopedServiceFromOtherScopeHandler = ExtractScopedService(otherScopeTopHandler);

            var thirdScopePrimaryHandler = GetPrimaryHandler(thirdScopeTopHandler);

            // ---

            Assert.Same(topHandler, sameScopeTopHandler); // within the scope handlers are cached
            Assert.Same(topHandler, newLifetimeSameScopeTopHandler); // within the scope handlers are cached
            Assert.NotSame(topHandler, otherScopeTopHandler); // in another scope different top handler will be created
            Assert.NotSame(otherScopeTopHandler, thirdScopeTopHandler); // in another scope different top handler will be created

            Assert.Same(primaryHandler, handlerFromFactory.InnerHandler); // only primary handler is cached in factory
            Assert.NotSame(primaryHandler, otherScopePrimaryHandler); // lifetime expired between scopes, so new primary handler is created
            Assert.Same(otherScopePrimaryHandler, thirdScopePrimaryHandler); // lifetime not expired between scopes, so old primary handler is reused

            Assert.NotSame(scopedServiceFromContainer, scopedServiceFromContainerOtherScope); // DI expected behavior
            Assert.Same(scopedServiceFromContainer, scopedServiceFromHandler); // outer scope is preserved in handler
            Assert.Same(scopedServiceFromContainerOtherScope, scopedServiceFromOtherScopeHandler); // outer scope is preserved in handler
        }

        [Fact]
        public void NamedClient_PreserveExistingScope_False()
        {
            var serviceCollection = new ServiceCollection();

            int scopedServiceInstanceCount = 0;
            serviceCollection.AddScoped(sc =>
            {
                scopedServiceInstanceCount++;
                return new ScopedService();
            });

            serviceCollection.AddScoped<MessageHandlerWithScopedService>();

            string name = "test";

            serviceCollection.AddHttpClient(name)
                .SetPreserveExistingScope(false)
                .AddHttpMessageHandler<MessageHandlerWithScopedService>()
                .SetHandlerLifetime(TimeSpan.FromSeconds(HandlerLifetime));

            var services = serviceCollection.BuildServiceProvider();

            // ---

            HttpClient client, sameScopeClient, newLifetimeSameScopeClient, otherScopeClient;
            HttpMessageHandler topHandler, sameScopeTopHandler, newLifetimeSameScopeTopHandler, otherScopeTopHandler;
            ScopedService scopedServiceFromContainer, scopedServiceFromContainerOtherScope;

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(1, scopedServiceInstanceCount); // 1 for container scope

                var factory = scopeServices.GetRequiredService<IHttpClientFactory>();

                client = factory.CreateClient(name);
                topHandler = GetHandlerFromFactory(factory, name);
                sameScopeClient = factory.CreateClient(name);
                sameScopeTopHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(2, scopedServiceInstanceCount); // 1 for container scope + 1 for handler lifetime scope

                Thread.Sleep(TimeSpan.FromSeconds(HandlerLifetime + 1));

                newLifetimeSameScopeClient = factory.CreateClient(name);
                newLifetimeSameScopeTopHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(3, scopedServiceInstanceCount); // 1 for container scope + 2 for 2 handler lifetime scopes
            }

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainerOtherScope = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(4, scopedServiceInstanceCount); // 2 for 2 container scopes + 2 for 2 handler lifetime scopes

                var factory = scopeServices.GetRequiredService<IHttpClientFactory>();
                otherScopeClient = factory.CreateClient(name);
                otherScopeTopHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(4, scopedServiceInstanceCount); // 2 for 2 container scopes + 2 for 2 handler lifetime scopes
            }

            // ---

            Assert.Same(topHandler, sameScopeTopHandler); // within lifetime handlers are cached
            Assert.Same(newLifetimeSameScopeTopHandler, otherScopeTopHandler); // within lifetime handlers are cached
            Assert.NotSame(topHandler, newLifetimeSameScopeTopHandler); // lifetime expired, so new handler is created
        }

        [Fact]
        public void NamedClient_PreserveExistingScope_True()
        {
            var serviceCollection = new ServiceCollection();

            int scopedServiceInstanceCount = 0;
            serviceCollection.AddScoped(sc =>
            {
                scopedServiceInstanceCount++;
                return new ScopedService();
            });

            serviceCollection.AddTransient<MessageHandlerWithScopedService>();

            string name = "test";

            serviceCollection.AddHttpClient(name)
                .SetPreserveExistingScope(true)
                .AddHttpMessageHandler<MessageHandlerWithScopedService>()
                .SetHandlerLifetime(TimeSpan.FromSeconds(HandlerLifetime));

            var services = serviceCollection.BuildServiceProvider();

            // ---

            HttpClient client, sameScopeClient, newLifetimeSameScopeClient, otherScopeClient, thirdScopeClient;
            HttpMessageHandler handler, sameScopeHandler, newLifetimeSameScopeHandler, otherScopeHandler, thirdScopeHandler;
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

                Thread.Sleep(TimeSpan.FromSeconds(HandlerLifetime + 1));

                newLifetimeSameScopeClient = factory.CreateClient(name);
                newLifetimeSameScopeHandler = GetHandlerFromFactory(factory, name);
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

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                var factory = scopeServices.GetRequiredService<IScopedHttpClientFactory>();
                thirdScopeClient = factory.CreateClient(name);
                thirdScopeHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(3, scopedServiceInstanceCount); // 3 for 3 container scopes
            }

            // ---

            Assert.Same(handler, sameScopeHandler); // within the scope primary handlers are cached
            Assert.Null(newLifetimeSameScopeHandler); // lifetime expired, so singleton cache is empty. scope-wise cache is higher on scope level

            Assert.NotSame(handler, otherScopeHandler); // lifetime expired between scopes, so new primary handler is created
            Assert.Same(otherScopeHandler, thirdScopeHandler); // lifetime not expired between scopes, so old primary handler is reused
        }

        [Fact]
        public void TypedClient_PreserveExistingScope_False()
        {
            var serviceCollection = new ServiceCollection();

            int scopedServiceInstanceCount = 0;
            serviceCollection.AddScoped(sc =>
            {
                scopedServiceInstanceCount++;
                return new ScopedService();
            });

            serviceCollection.AddScoped<MessageHandlerWithScopedService>();

            serviceCollection.AddHttpClient<TypedClient>()
                .SetPreserveExistingScope(false)
                .AddHttpMessageHandler<MessageHandlerWithScopedService>()
                .SetHandlerLifetime(TimeSpan.FromSeconds(HandlerLifetime));

            string name = "TypedClient";

            var services = serviceCollection.BuildServiceProvider();

            // ---

            TypedClient typedClient, sameScopeTypedClient, newLifetimeSameScopeTypedClient, otherScopeTypedClient;
            HttpMessageHandler topHandler, sameScopeTopHandler, newLifetimeSameScopeTopHandler, otherScopeTopHandler;
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

                Thread.Sleep(TimeSpan.FromSeconds(HandlerLifetime + 1));

                newLifetimeSameScopeTypedClient = scopeServices.GetRequiredService<TypedClient>();
                newLifetimeSameScopeTopHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(3, scopedServiceInstanceCount); // 1 for container scope + 2 for 2 handler lifetime scopes
            }

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                scopedServiceFromContainerOtherScope = scopeServices.GetRequiredService<ScopedService>();
                Assert.Equal(4, scopedServiceInstanceCount); // 2 for 2 container scopes + 2 for 2 handler lifetime scopes

                var factory = scopeServices.GetRequiredService<IHttpClientFactory>();
                otherScopeTypedClient = scopeServices.GetRequiredService<TypedClient>();
                otherScopeTopHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(4, scopedServiceInstanceCount); // 2 for 2 container scopes + 2 for 2 handler lifetime scopes
            }

            // ---

            Assert.NotSame(typedClient, sameScopeTypedClient); // transient
            Assert.NotSame(sameScopeTypedClient, newLifetimeSameScopeTypedClient); // transient
            Assert.NotSame(newLifetimeSameScopeTypedClient, otherScopeTypedClient); // transient

            Assert.NotSame(typedClient.HttpClient, sameScopeTypedClient.HttpClient); // re-created each time
            Assert.NotSame(sameScopeTypedClient.HttpClient, newLifetimeSameScopeTypedClient.HttpClient); // re-created each time
            Assert.NotSame(newLifetimeSameScopeTypedClient.HttpClient, otherScopeTypedClient.HttpClient); // re-created each time

            Assert.Same(typedClient.ScopedService, sameScopeTypedClient.ScopedService); // typed client instances are created in outer scope
            Assert.Same(sameScopeTypedClient.ScopedService, newLifetimeSameScopeTypedClient.ScopedService); // typed client instances are created in outer scope
            Assert.NotSame(typedClient.ScopedService, otherScopeTypedClient.ScopedService); // typed client instances are created in outer scope

            Assert.Same(topHandler, sameScopeTopHandler); // within lifetime handlers are cached
            Assert.Same(newLifetimeSameScopeTopHandler, otherScopeTopHandler); // within lifetime handlers are cached
            Assert.NotSame(topHandler, newLifetimeSameScopeTopHandler); // lifetime expired, so new handler is created
        }

        [Fact]
        public void TypedClient_PreserveExistingScope_True()
        {
            var serviceCollection = new ServiceCollection();

            int scopedServiceInstanceCount = 0;
            serviceCollection.AddScoped(sc =>
            {
                scopedServiceInstanceCount++;
                return new ScopedService();
            });

            serviceCollection.AddScoped<MessageHandlerWithScopedService>();

            serviceCollection.AddHttpClient<TypedClient>()
                .SetPreserveExistingScope(true)
                .AddHttpMessageHandler<MessageHandlerWithScopedService>()
                .SetHandlerLifetime(TimeSpan.FromSeconds(HandlerLifetime));

            string name = "TypedClient";

            var services = serviceCollection.BuildServiceProvider();

            // ---

            TypedClient typedClient, sameScopeTypedClient, newLifetimeSameScopeTypedClient, otherScopeTypedClient, thirdScopeTypedClient;
            HttpMessageHandler handler, sameScopeHandler, newLifetimeSameScopeHandler, otherScopeHandler, thirdScopeHandler;
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

                Thread.Sleep(TimeSpan.FromSeconds(HandlerLifetime + 1));

                newLifetimeSameScopeTypedClient = scopeServices.GetRequiredService<TypedClient>();
                newLifetimeSameScopeHandler = GetHandlerFromFactory(factory, name);
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

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;
                var factory = scopeServices.GetRequiredService<IScopedHttpClientFactory>();
                thirdScopeTypedClient = scopeServices.GetRequiredService<TypedClient>();
                thirdScopeHandler = GetHandlerFromFactory(factory, name);
                Assert.Equal(3, scopedServiceInstanceCount); // 3 for 3 container scopes
            }

            // ---

            Assert.NotSame(typedClient, sameScopeTypedClient); // transient
            Assert.NotSame(sameScopeTypedClient, newLifetimeSameScopeTypedClient); // transient
            Assert.NotSame(newLifetimeSameScopeTypedClient, otherScopeTypedClient); // transient
            Assert.NotSame(otherScopeTypedClient, thirdScopeTypedClient); // transient

            Assert.NotSame(typedClient.HttpClient, sameScopeTypedClient.HttpClient); // re-created each time
            Assert.NotSame(sameScopeTypedClient.HttpClient, newLifetimeSameScopeTypedClient.HttpClient); // re-created each time
            Assert.NotSame(newLifetimeSameScopeTypedClient.HttpClient, otherScopeTypedClient.HttpClient); // re-created each time
            Assert.NotSame(otherScopeTypedClient.HttpClient, thirdScopeTypedClient.HttpClient); // re-created each time

            Assert.Same(typedClient.ScopedService, sameScopeTypedClient.ScopedService); // typed client instances are created in outer scope
            Assert.Same(sameScopeTypedClient.ScopedService, newLifetimeSameScopeTypedClient.ScopedService); // typed client instances are created in outer scope
            Assert.NotSame(typedClient.ScopedService, otherScopeTypedClient.ScopedService); // typed client instances are created in outer scope
            Assert.NotSame(otherScopeTypedClient.ScopedService, thirdScopeTypedClient.ScopedService); // typed client instances are created in outer scope

            Assert.Same(handler, sameScopeHandler); // within the scope primary handlers are cached
            Assert.Null(newLifetimeSameScopeHandler); // lifetime expired, so singleton cache is empty. scope-wise cache is higher on scope level

            Assert.NotSame(handler, otherScopeHandler); // lifetime expired between scopes, so new primary handler is created
            Assert.Same(otherScopeHandler, thirdScopeHandler); // lifetime not expired between scopes, so old primary handler is reused
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
        private LifetimeTrackingHttpMessageHandler GetHandlerFromFactory(DefaultHttpClientFactory factory, string name) => factory._activeHandlers[name].Handler;

        private LifetimeTrackingHttpMessageHandler GetHandlerFromFactory(IScopedHttpMessageHandlerFactory factory, string name) => GetHandlerFromFactory((DefaultScopedHttpClientFactory)factory, name);
        private LifetimeTrackingHttpMessageHandler GetHandlerFromFactory(IScopedHttpClientFactory factory, string name) => GetHandlerFromFactory((DefaultScopedHttpClientFactory)factory, name);

        private LifetimeTrackingHttpMessageHandler GetHandlerFromFactory(DefaultScopedHttpClientFactory factory, string name)
        {
            var activeHandlers = factory._singletonFactory._activeHandlers;
            if (activeHandlers.TryGetValue(name, out var entry))
            {
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
    }
}
