// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http
{
    public class ScopeTest
    {
        [Fact]
        public void TestNamed()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<ScopedService>();
            serviceCollection.AddTransient<MessageHandlerWithScopedService>();

            string name = "test";

            serviceCollection.AddHttpClient(name)
                .PreserveExistingScope(false)
                .AddHttpMessageHandler<MessageHandlerWithScopedService>()
                .SetHandlerLifetime(TimeSpan.FromSeconds(10));

            // ---

            var services = serviceCollection.BuildServiceProvider();

            var factory = (DefaultHttpClientFactory)services.GetRequiredService<IHttpClientFactory>();

            // ---

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;

                Assert.Same(scopeServices, scopeServices.GetRequiredService<IServiceProvider>());

                var scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();

                var client = factory.CreateClient(name);

                var (topHandler, primaryHandler) = GetTopAndPrimaryHandlers(name, client, factory);
                var scopedServiceFromHandler = ((MessageHandlerWithScopedService)topHandler).ScopedService;

                var client2 = factory.CreateClient(name);

                var (topHandler2, primaryHandler2) = GetTopAndPrimaryHandlers(name, client2, factory);
                var scopedServiceFromHandler2 = ((MessageHandlerWithScopedService)topHandler2).ScopedService;

                Thread.Sleep(20 * 1000);

                var client3 = factory.CreateClient(name);

                var (topHandler3, primaryHandler3) = GetTopAndPrimaryHandlers(name, client3, factory);
                var scopedServiceFromHandler3 = ((MessageHandlerWithScopedService)topHandler3).ScopedService;

                Assert.NotSame(scopedServiceFromContainer, scopedServiceFromHandler);
                Assert.NotSame(scopedServiceFromContainer, scopedServiceFromHandler3);
                Assert.Same(scopedServiceFromHandler, scopedServiceFromHandler2);
                Assert.NotSame(scopedServiceFromHandler, scopedServiceFromHandler3);

                Assert.Same(topHandler, topHandler2);
                Assert.NotSame(topHandler2, topHandler3);

                Assert.Same(primaryHandler, primaryHandler2);
                Assert.NotSame(primaryHandler, primaryHandler3);
            }
        }

        [Fact]
        public void TestNamedScoped()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<ScopedService>();
            serviceCollection.AddTransient<MessageHandlerWithScopedService>();

            string name = "test";

            serviceCollection.AddHttpClient(name)
                .PreserveExistingScope(true)
                .AddHttpMessageHandler<MessageHandlerWithScopedService>()
                .SetHandlerLifetime(TimeSpan.FromSeconds(10));

            // ---

            var services = serviceCollection.BuildServiceProvider();

            var factory = (DefaultHttpClientFactory)services.GetRequiredService<IHttpClientFactory>();

            // ---

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;

                Assert.Same(scopeServices, scopeServices.GetRequiredService<IServiceProvider>());

                var scopedFactory = scopeServices.GetRequiredService<IScopedHttpClientFactory>();

                var scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();

                var client = scopedFactory.CreateClient(name);

                var (topHandler, primaryHandler) = GetTopAndPrimaryHandlers(name, client, factory);
                var scopedServiceFromHandler = ((MessageHandlerWithScopedService)topHandler).ScopedService;

                var client2 = scopedFactory.CreateClient(name);

                var (topHandler2, primaryHandler2) = GetTopAndPrimaryHandlers(name, client2, factory);
                var scopedServiceFromHandler2 = ((MessageHandlerWithScopedService)topHandler2).ScopedService;

                Thread.Sleep(20 * 1000);

                var client3 = scopedFactory.CreateClient(name);

                var (topHandler3, primaryHandler3) = GetTopAndPrimaryHandlers(name, client3, factory);
                var scopedServiceFromHandler3 = ((MessageHandlerWithScopedService)topHandler3).ScopedService;

                Assert.Same(scopedServiceFromContainer, scopedServiceFromHandler);
                Assert.Same(scopedServiceFromContainer, scopedServiceFromHandler3);
                Assert.Same(scopedServiceFromHandler, scopedServiceFromHandler2);
                Assert.Same(scopedServiceFromHandler, scopedServiceFromHandler3);

                Assert.NotSame(topHandler, topHandler2);
                Assert.NotSame(topHandler2, topHandler3);

                Assert.Same(primaryHandler, primaryHandler2);
                Assert.NotSame(primaryHandler, primaryHandler3);
            }
        }

        [Fact]
        public void TestTyped()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<ScopedService>();
            serviceCollection.AddTransient<MessageHandlerWithScopedService>();

            serviceCollection.AddHttpClient<TypedClient>()
                .PreserveExistingScope(false)
                .AddHttpMessageHandler<MessageHandlerWithScopedService>()
                .SetHandlerLifetime(TimeSpan.FromSeconds(10));

            string name = "TypedClient";

            // ---

            var services = serviceCollection.BuildServiceProvider();

            var factory = (DefaultHttpClientFactory)services.GetRequiredService<IHttpClientFactory>();

            // ---

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;

                Assert.Same(scopeServices, scopeServices.GetRequiredService<IServiceProvider>());

                var scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();

                var typedClient = scopeServices.GetRequiredService<TypedClient>();
                var scopedServiceFromTypedClient = typedClient.ScopedService;

                var (topHandler, primaryHandler) = GetTopAndPrimaryHandlers(name, typedClient.HttpClient, factory);
                var scopedServiceFromHandler = ((MessageHandlerWithScopedService)topHandler).ScopedService;

                var typedClient2 = scopeServices.GetRequiredService<TypedClient>();
                var scopedServiceFromTypedClient2 = typedClient2.ScopedService;

                var (topHandler2, primaryHandler2) = GetTopAndPrimaryHandlers(name, typedClient2.HttpClient, factory);
                var scopedServiceFromHandler2 = ((MessageHandlerWithScopedService)topHandler2).ScopedService;

                Thread.Sleep(20 * 1000);

                var typedClient3 = scopeServices.GetRequiredService<TypedClient>();
                var scopedServiceFromTypedClient3 = typedClient3.ScopedService;

                var (topHandler3, primaryHandler3) = GetTopAndPrimaryHandlers(name, typedClient3.HttpClient, factory);
                var scopedServiceFromHandler3 = ((MessageHandlerWithScopedService)topHandler3).ScopedService;

                Assert.NotSame(scopedServiceFromContainer, scopedServiceFromHandler);
                Assert.NotSame(scopedServiceFromContainer, scopedServiceFromHandler3);
                Assert.Same(scopedServiceFromHandler, scopedServiceFromHandler2);
                Assert.NotSame(scopedServiceFromHandler, scopedServiceFromHandler3);

                Assert.NotSame(typedClient, typedClient2);
                Assert.NotSame(typedClient2, typedClient3);
                Assert.Same(scopedServiceFromContainer, scopedServiceFromTypedClient);
                Assert.Same(scopedServiceFromTypedClient, scopedServiceFromTypedClient2);
                Assert.Same(scopedServiceFromTypedClient2, scopedServiceFromTypedClient3);
                Assert.NotSame(scopedServiceFromTypedClient, scopedServiceFromHandler);
                Assert.NotSame(scopedServiceFromTypedClient2, scopedServiceFromHandler2);
                Assert.NotSame(scopedServiceFromTypedClient3, scopedServiceFromHandler3);

                Assert.Same(topHandler, topHandler2);
                Assert.NotSame(topHandler2, topHandler3);

                Assert.Same(primaryHandler, primaryHandler2);
                Assert.NotSame(primaryHandler, primaryHandler3);
            }
        }

        [Fact]
        public void TestTypedScoped()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<ScopedService>();
            serviceCollection.AddTransient<MessageHandlerWithScopedService>();

            serviceCollection.AddHttpClient<TypedClient>()
                .PreserveExistingScope(true)
                .AddHttpMessageHandler<MessageHandlerWithScopedService>()
                .SetHandlerLifetime(TimeSpan.FromSeconds(10));

            string name = "TypedClient";

            // ---

            var services = serviceCollection.BuildServiceProvider();

            var factory = (DefaultHttpClientFactory)services.GetRequiredService<IHttpClientFactory>();

            // ---

            using (var scope = services.CreateScope())
            {
                var scopeServices = scope.ServiceProvider;

                Assert.Same(scopeServices, scopeServices.GetRequiredService<IServiceProvider>());

                var scopedServiceFromContainer = scopeServices.GetRequiredService<ScopedService>();

                var typedClient = scopeServices.GetRequiredService<TypedClient>();
                var scopedServiceFromTypedClient = typedClient.ScopedService;

                var (topHandler, primaryHandler) = GetTopAndPrimaryHandlers(name, typedClient.HttpClient, factory);
                var scopedServiceFromHandler = ((MessageHandlerWithScopedService)topHandler).ScopedService;

                var typedClient2 = scopeServices.GetRequiredService<TypedClient>();
                var scopedServiceFromTypedClient2 = typedClient2.ScopedService;

                var (topHandler2, primaryHandler2) = GetTopAndPrimaryHandlers(name, typedClient2.HttpClient, factory);
                var scopedServiceFromHandler2 = ((MessageHandlerWithScopedService)topHandler2).ScopedService;

                Thread.Sleep(20 * 1000);

                var typedClient3 = scopeServices.GetRequiredService<TypedClient>();
                var scopedServiceFromTypedClient3 = typedClient3.ScopedService;

                var (topHandler3, primaryHandler3) = GetTopAndPrimaryHandlers(name, typedClient3.HttpClient, factory);
                var scopedServiceFromHandler3 = ((MessageHandlerWithScopedService)topHandler3).ScopedService;

                Assert.Same(scopedServiceFromContainer, scopedServiceFromHandler);
                Assert.Same(scopedServiceFromContainer, scopedServiceFromHandler3);
                Assert.Same(scopedServiceFromHandler, scopedServiceFromHandler2);
                Assert.Same(scopedServiceFromHandler, scopedServiceFromHandler3);

                Assert.NotSame(typedClient, typedClient2);
                Assert.NotSame(typedClient2, typedClient3);
                Assert.Same(scopedServiceFromContainer, scopedServiceFromTypedClient);
                Assert.Same(scopedServiceFromTypedClient, scopedServiceFromTypedClient2);
                Assert.Same(scopedServiceFromTypedClient2, scopedServiceFromTypedClient3);
                Assert.Same(scopedServiceFromTypedClient, scopedServiceFromHandler);
                Assert.Same(scopedServiceFromTypedClient2, scopedServiceFromHandler2);
                Assert.Same(scopedServiceFromTypedClient3, scopedServiceFromHandler3);

                Assert.NotSame(topHandler, topHandler2);
                Assert.NotSame(topHandler2, topHandler3);

                Assert.Same(primaryHandler, primaryHandler2);
                Assert.NotSame(primaryHandler, primaryHandler3);
            }
        }


        private static readonly FieldInfo HttpClientHandlerField =
            typeof(HttpMessageInvoker).GetField("_handler", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        private (HttpMessageHandler TopHandler, HttpMessageHandler PrimaryHandler) GetTopAndPrimaryHandlers(string name, HttpClient client, DefaultHttpClientFactory factory)
        {
            var entry = factory._activeHandlers[name].Value;

            HttpMessageHandler topHandler;
            HttpMessageHandler primaryHandler;

            if (entry.IsPrimary)
            {
                primaryHandler = entry.Handler.InnerHandler;
                var loggingHandler = ((LifetimeTrackingHttpMessageHandler)HttpClientHandlerField.GetValue(client)).InnerHandler;
                topHandler = ((DelegatingHandler)loggingHandler).InnerHandler;
            }
            else
            {
                var loggingHandler = entry.Handler.InnerHandler;
                topHandler = ((DelegatingHandler)loggingHandler).InnerHandler;
                primaryHandler = topHandler;
                while (primaryHandler is DelegatingHandler)
                {
                    primaryHandler = ((DelegatingHandler)primaryHandler).InnerHandler;
                }
            }

            return (topHandler, primaryHandler);
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
