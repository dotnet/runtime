// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Http
{
#if NET8_0_OR_GREATER
    public class MeterFactoryIntegrationTest
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public void SocketsHttpHandler_Configured()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("Test").UseSocketsHttpHandler();

            var services = serviceCollection.BuildServiceProvider();
            var messageHandlerFactory = services.GetRequiredService<IHttpMessageHandlerFactory>();
            var meterFactory = services.GetRequiredService<IMeterFactory>();

            var configuredHandlerChain = messageHandlerFactory.CreateHandler("Test");
            var configuredHandler = (SocketsHttpHandler)GetPrimaryHandler(configuredHandlerChain);

            Assert.Same(meterFactory, configuredHandler.MeterFactory);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public void SocketsHttpHandler_HasExisting_Unchanged()
        {
            var testMeterFactory = new TestMeterFactory();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("Test")
                .UseSocketsHttpHandler(b => b.Configure((handler, provider) =>
                {
                    handler.MeterFactory = testMeterFactory;
                }));

            var services = serviceCollection.BuildServiceProvider();
            var messageHandlerFactory = services.GetRequiredService<IHttpMessageHandlerFactory>();

            var configuredHandlerChain = messageHandlerFactory.CreateHandler("Test");
            var configuredHandler = (SocketsHttpHandler)GetPrimaryHandler(configuredHandlerChain);

            Assert.Same(testMeterFactory, configuredHandler.MeterFactory);
        }

        [Fact]
        public void HttpClientHandler_Configured()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("Test")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());

            var services = serviceCollection.BuildServiceProvider();
            var messageHandlerFactory = services.GetRequiredService<IHttpMessageHandlerFactory>();
            var meterFactory = services.GetRequiredService<IMeterFactory>();

            var configuredHandlerChain = messageHandlerFactory.CreateHandler("Test");
            var configuredHandler = (HttpClientHandler)GetPrimaryHandler(configuredHandlerChain);

            Assert.Same(meterFactory, configuredHandler.MeterFactory);
        }

        [Fact]
        public void HttpClientHandler_HasExisting_Unchanged()
        {
            var testMeterFactory = new TestMeterFactory();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("Test")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { MeterFactory = testMeterFactory });

            var services = serviceCollection.BuildServiceProvider();
            var messageHandlerFactory = services.GetRequiredService<IHttpMessageHandlerFactory>();

            var configuredHandlerChain = messageHandlerFactory.CreateHandler("Test");
            var configuredHandler = (HttpClientHandler)GetPrimaryHandler(configuredHandlerChain);

            Assert.Same(testMeterFactory, configuredHandler.MeterFactory);
        }

        private static HttpMessageHandler GetPrimaryHandler(HttpMessageHandler handlerChain)
        {
            var handler = handlerChain;
            while (handler is DelegatingHandler delegatingHandler)
            {
                handler = delegatingHandler.InnerHandler;
            }
            return handler;
        }

        private sealed class TestMeterFactory : IMeterFactory
        {
            public Meter Create(MeterOptions options) => throw new System.NotImplementedException();
            public void Dispose() => throw new System.NotImplementedException();
        }
    }
#endif
}
