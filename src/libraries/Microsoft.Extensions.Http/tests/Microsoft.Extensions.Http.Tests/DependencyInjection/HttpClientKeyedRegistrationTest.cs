// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class HttpClientKeyedRegistrationTest
    {
        [Fact]
        public void HttpClient_ResolvedAsKeyedService_Success()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("test", c => c.BaseAddress = new Uri("http://example.com")).AsKeyed(ServiceLifetime.Transient);

            var services = serviceCollection.BuildServiceProvider();

            var client = services.GetRequiredKeyedService<HttpClient>("test");

            Assert.Equal(new Uri("http://example.com"), client.BaseAddress);
        }

        [Fact]
        public void HttpClient_ResolvedAsKeyedService_AbsentClient()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("test").AsKeyed(ServiceLifetime.Transient);

            var services = serviceCollection.BuildServiceProvider();

            var client = services.GetKeyedService<HttpClient>("absent");

            Assert.Null(client);
        }
        [Fact]
        public void HttpMessageHandler_ResolvedAsKeyedService_Success()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("test").AsKeyed(ServiceLifetime.Transient)
                .ConfigurePrimaryHttpMessageHandler(() => new TestMessageHandler());

            var services = serviceCollection.BuildServiceProvider();

            var handler = services.GetRequiredKeyedService<HttpMessageHandler>("test");
            while (handler is DelegatingHandler dh)
            {
                handler = dh.InnerHandler;
            }

            Assert.IsType<TestMessageHandler>(handler);
        }

        [Fact]
        public void HttpMessageHandler_ResolvedAsKeyedService_AbsentHandler()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("test").AsKeyed(ServiceLifetime.Transient);

            var services = serviceCollection.BuildServiceProvider();

            var handler = services.GetKeyedService<HttpMessageHandler>("absent");

            Assert.Null(handler);
        }

        [Fact]
        public void HttpClient_InjectedAsKeyedService_Success()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("test", c => c.BaseAddress = new Uri("http://example.com")).AsKeyed(ServiceLifetime.Transient);
            serviceCollection.AddSingleton<KeyedClientTestService>();

            var services = serviceCollection.BuildServiceProvider();

            var testService = services.GetRequiredService<KeyedClientTestService>();

            Assert.Equal(new Uri("http://example.com"), testService.HttpClient.BaseAddress);
        }

        [Fact]
        public void AdditionalHandler_InjectedAsKeyedService_Success()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddKeyedTransient<KeyedDelegatingHandler>(KeyedService.AnyKey);
            serviceCollection.AddHttpClient("test").AsKeyed(ServiceLifetime.Transient)
                .AddHttpMessageHandler<KeyedDelegatingHandler>();
            serviceCollection.AddHttpClient("test2").AsKeyed(ServiceLifetime.Transient)
                .AddHttpMessageHandler<KeyedDelegatingHandler>();

            var services = serviceCollection.BuildServiceProvider();

            ValidateHandler("test");
            ValidateHandler("test2");

            void ValidateHandler(string clientName)
            {
                var handler = services.GetRequiredKeyedService<HttpMessageHandler>(clientName);
                while (handler is DelegatingHandler dh)
                {
                    if (dh is KeyedDelegatingHandler)
                    {
                        break;
                    }
                    handler = dh.InnerHandler;
                }

                var keyedHandler = Assert.IsType<KeyedDelegatingHandler>(handler);
                Assert.Equal(clientName, keyedHandler.Key);
            }
        }

        [Fact]
        public void PrimaryHandler_InjectedAsKeyedService_Success()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddKeyedTransient<BaseKeyedHandler, KeyedPrimaryHandler>("test");
            serviceCollection.AddKeyedTransient<BaseKeyedHandler, OtherKeyedPrimaryHandler>("other-implementation");
            serviceCollection.AddTransient<BaseKeyedHandler>(_ => new KeyedPrimaryHandler("non-keyed-fallback"));

            serviceCollection.AddHttpClient("test").AsKeyed(ServiceLifetime.Transient)
                .ConfigurePrimaryHttpMessageHandler<BaseKeyedHandler>();
            serviceCollection.AddHttpClient("other-implementation").AsKeyed(ServiceLifetime.Transient)
                .ConfigurePrimaryHttpMessageHandler<BaseKeyedHandler>();
            serviceCollection.AddHttpClient("non-keyed").AsKeyed(ServiceLifetime.Transient)
                .ConfigurePrimaryHttpMessageHandler<BaseKeyedHandler>();

            var services = serviceCollection.BuildServiceProvider();

            var handler = services.GetRequiredKeyedService<HttpMessageHandler>("test");
            while (handler is DelegatingHandler dh)
            {
                handler = dh.InnerHandler;
            }
            var keyedHandler = Assert.IsType<KeyedPrimaryHandler>(handler);
            Assert.Equal("test", keyedHandler.Key);

            var otherHandler = services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler("other-implementation");
            while (otherHandler is DelegatingHandler odh)
            {
                otherHandler = odh.InnerHandler;
            }
            var otherKeyedHandler = Assert.IsType<OtherKeyedPrimaryHandler>(otherHandler);
            Assert.Equal("{ \"key\": \"other-implementation\" }", otherKeyedHandler.Key);

            var fallbackHandler = services.GetRequiredKeyedService<HttpMessageHandler>("non-keyed");
            while (fallbackHandler is DelegatingHandler fdh)
            {
                fallbackHandler = fdh.InnerHandler;
            }
            var nonKeyedHandler = Assert.IsType<KeyedPrimaryHandler>(fallbackHandler);
            Assert.Equal("non-keyed-fallback", nonKeyedHandler.Key);
        }

        [Fact]
        public async Task HttpClientLogger_InjectedAsKeyedService_Success()
        {
            var sink = new TestSink();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));
            serviceCollection.AddTransient<TestMessageHandler>();
            serviceCollection.AddKeyedSingleton<KeyedHttpClientLogger>(KeyedService.AnyKey);

            serviceCollection.AddHttpClient("FirstClient").AsKeyed(ServiceLifetime.Transient)
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .RemoveAllLoggers()
                .AddLogger<KeyedHttpClientLogger>();

            serviceCollection.AddHttpClient("SecondClient").AsKeyed(ServiceLifetime.Transient)
                .ConfigurePrimaryHttpMessageHandler<TestMessageHandler>()
                .RemoveAllLoggers()
                .AddLogger<KeyedHttpClientLogger>();

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            var client1 = factory.CreateClient("FirstClient");
            var client2 = factory.CreateClient("SecondClient");

            _ = await client1.GetAsync(new Uri("http://example.com"));
            Assert.Equal(2, sink.Writes.Count);
            Assert.Equal(2, sink.Writes.Count(w => w.LoggerName == typeof(KeyedHttpClientLogger).FullName + ".FirstClient"));

            _ = await client2.GetAsync(new Uri("http://example.com"));
            Assert.Equal(4, sink.Writes.Count);
            Assert.Equal(2, sink.Writes.Count(w => w.LoggerName == typeof(KeyedHttpClientLogger).FullName + ".SecondClient"));
        }

        internal class KeyedClientTestService
        {
            public KeyedClientTestService([FromKeyedServices("test")] HttpClient httpClient)
            {
                HttpClient = httpClient;
            }

            public HttpClient HttpClient { get; }
        }

        internal class KeyedDelegatingHandler : DelegatingHandler
        {
            public string Key { get; }

            public KeyedDelegatingHandler([ServiceKey] string key)
            {
                Key = key;
            }
        }

        internal abstract class BaseKeyedHandler : TestMessageHandler
        {
            public string Key { get; protected set; }
        }

        internal class KeyedPrimaryHandler : BaseKeyedHandler
        {
            public KeyedPrimaryHandler([ServiceKey] string key)
            {
                Key = key;
            }
        }

        internal class OtherKeyedPrimaryHandler : BaseKeyedHandler
        {
            public OtherKeyedPrimaryHandler([ServiceKey] string key)
            {
                Key = "{ \"key\": \"" + key + "\" }";
            }
        }

        internal class KeyedHttpClientLogger : IHttpClientLogger
        {
            private readonly ILogger _logger;
            public KeyedHttpClientLogger(ILoggerFactory loggerFactory, [ServiceKey] string key)
            {
                _logger = loggerFactory.CreateLogger(typeof(KeyedHttpClientLogger).FullName + "." + key);
            }

            public object? LogRequestStart(HttpRequestMessage request)
            {
                _logger.LogInformation("LogRequestStart");
                return null;
            }

            public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
                => _logger.LogInformation("LogRequestStop");

            public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception,TimeSpan elapsed)
                => _logger.LogInformation("LogRequestFailed");
        }
    }
}
