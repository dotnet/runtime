// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
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
            serviceCollection.AddScoped<MessageHandlerWithScopedService>();

            serviceCollection.AddHttpClient("test")
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

                var client = factory.CreateClient("test");

                MessageHandlerWithScopedService handlerWithScopedService = (MessageHandlerWithScopedService)(
                    ((DelegatingHandler)factory._activeHandlers["test"].Value.Handler.InnerHandler).InnerHandler
                );

                var scopedServiceFromHandler = handlerWithScopedService.ScopedService;
                var primaryHandler = (HttpMessageHandler)(
                    ((DelegatingHandler)handlerWithScopedService.InnerHandler).InnerHandler
                );

                var client2 = factory.CreateClient("test");

                MessageHandlerWithScopedService handlerWithScopedService2 = (MessageHandlerWithScopedService)(
                    ((DelegatingHandler)factory._activeHandlers["test"].Value.Handler.InnerHandler).InnerHandler
                );

                var scopedServiceFromHandler2 = handlerWithScopedService2.ScopedService;
                var primaryHandler2 = (HttpMessageHandler)(
                    ((DelegatingHandler)handlerWithScopedService2.InnerHandler).InnerHandler
                );

                Thread.Sleep(20 * 1000);

                var client3 = factory.CreateClient("test");

                MessageHandlerWithScopedService handlerWithScopedService3 = (MessageHandlerWithScopedService)(
                    ((DelegatingHandler)factory._activeHandlers["test"].Value.Handler.InnerHandler).InnerHandler
                );

                var scopedServiceFromHandler3 = handlerWithScopedService3.ScopedService;
                var primaryHandler3 = (HttpMessageHandler)(
                    ((DelegatingHandler)handlerWithScopedService3.InnerHandler).InnerHandler
                );

                Assert.NotSame(scopedServiceFromContainer, scopedServiceFromHandler);
                Assert.NotSame(scopedServiceFromContainer, scopedServiceFromHandler3);
                Assert.Same(scopedServiceFromHandler, scopedServiceFromHandler2);
                Assert.NotSame(scopedServiceFromHandler, scopedServiceFromHandler3);

                Assert.Same(handlerWithScopedService, handlerWithScopedService2);
                Assert.NotSame(handlerWithScopedService, handlerWithScopedService3);

                Assert.Same(primaryHandler, primaryHandler2);
                Assert.NotSame(primaryHandler, primaryHandler3);
            }
        }

        [Fact]
        public void TestTyped()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<ScopedService>();
            serviceCollection.AddScoped<MessageHandlerWithScopedService>();

            serviceCollection.AddHttpClient<TypedClient>()
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

                MessageHandlerWithScopedService handlerWithScopedService = (MessageHandlerWithScopedService)(
                    ((DelegatingHandler)factory._activeHandlers[name].Value.Handler.InnerHandler).InnerHandler
                );

                var scopedServiceFromHandler = handlerWithScopedService.ScopedService;
                var primaryHandler = (HttpMessageHandler)(
                    ((DelegatingHandler)handlerWithScopedService.InnerHandler).InnerHandler
                );

                var typedClient2 = scopeServices.GetRequiredService<TypedClient>();
                var scopedServiceFromTypedClient2 = typedClient2.ScopedService;

                MessageHandlerWithScopedService handlerWithScopedService2 = (MessageHandlerWithScopedService)(
                    ((DelegatingHandler)factory._activeHandlers[name].Value.Handler.InnerHandler).InnerHandler
                );

                var scopedServiceFromHandler2 = handlerWithScopedService2.ScopedService;
                var primaryHandler2 = (HttpMessageHandler)(
                    ((DelegatingHandler)handlerWithScopedService2.InnerHandler).InnerHandler
                );

                Thread.Sleep(20 * 1000);

                var typedClient3 = scopeServices.GetRequiredService<TypedClient>();
                var scopedServiceFromTypedClient3 = typedClient3.ScopedService;

                MessageHandlerWithScopedService handlerWithScopedService3 = (MessageHandlerWithScopedService)(
                    ((DelegatingHandler)factory._activeHandlers[name].Value.Handler.InnerHandler).InnerHandler
                );

                var scopedServiceFromHandler3 = handlerWithScopedService3.ScopedService;
                var primaryHandler3 = (HttpMessageHandler)(
                    ((DelegatingHandler)handlerWithScopedService3.InnerHandler).InnerHandler
                );

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

                Assert.Same(handlerWithScopedService, handlerWithScopedService2);
                Assert.NotSame(handlerWithScopedService, handlerWithScopedService3);

                Assert.Same(primaryHandler, primaryHandler2);
                Assert.NotSame(primaryHandler, primaryHandler3);
            }
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
