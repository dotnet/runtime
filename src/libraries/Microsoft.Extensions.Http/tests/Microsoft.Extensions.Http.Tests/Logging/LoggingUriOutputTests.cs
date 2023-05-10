// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Http.Tests.Logging
{
    public class LoggingUriOutputTests
    {
        [Fact]
        public async Task LoggingHttpMessageHandler_LogsAbsoluteUri()
        {
            // Arrange
            var sink = new TestSink();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));

            serviceCollection
            .AddHttpClient("test")
            .ConfigurePrimaryHttpMessageHandler(() => new TestMessageHandler());

            var services = serviceCollection.BuildServiceProvider();

            var client = services.GetRequiredService<IHttpClientFactory>().CreateClient("test");


            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "http://api.example.com/search?term=Western%20Australia");

            await client.SendAsync(request);

            // Assert
            var messages = sink.Writes.ToArray();

            var message = Assert.Single(messages.Where(m =>
            {
                return
                    m.EventId == LoggingHttpMessageHandler.Log.EventIds.RequestStart &&
                    m.LoggerName == "System.Net.Http.HttpClient.test.ClientHandler";
            }));

            Assert.Equal("Sending HTTP request GET http://api.example.com/search?term=Western%20Australia", message.Message);
        }

        [Fact]
        public async Task LoggingScopeHttpMessageHandler_LogsAbsoluteUri()
        {
            // Arrange
            var sink = new TestSink();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));

            serviceCollection
            .AddHttpClient("test")
            .ConfigurePrimaryHttpMessageHandler(() => new TestMessageHandler());

            var services = serviceCollection.BuildServiceProvider();

            var client = services.GetRequiredService<IHttpClientFactory>().CreateClient("test");


            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "http://api.example.com/search?term=Western%20Australia");

            await client.SendAsync(request);

            // Assert
            var messages = sink.Writes.ToArray();

            var message = Assert.Single(messages.Where(m =>
            {
                return
                    m.EventId == LoggingScopeHttpMessageHandler.Log.EventIds.PipelineStart &&
                    m.LoggerName == "System.Net.Http.HttpClient.test.LogicalHandler";
            }));

            Assert.Equal("Start processing HTTP request GET http://api.example.com/search?term=Western%20Australia", message.Message);
            Assert.Equal("HTTP GET http://api.example.com/search?term=Western%20Australia", message.Scope.ToString());
        }

#if NET5_0_OR_GREATER
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public void LoggingHttpMessageHandler_LogsAbsoluteUri_Sync()
        {
            // Arrange
            var sink = new TestSink();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));

            serviceCollection
                .AddHttpClient("test")
                .ConfigurePrimaryHttpMessageHandler(() => new TestMessageHandler());

            var services = serviceCollection.BuildServiceProvider();

            var client = services.GetRequiredService<IHttpClientFactory>().CreateClient("test");


            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "http://api.example.com/search?term=Western%20Australia");

            client.Send(request);

            // Assert
            var messages = sink.Writes.ToArray();

            var message = Assert.Single(messages.Where(m =>
            {
                return
                    m.EventId == LoggingHttpMessageHandler.Log.EventIds.RequestStart &&
                    m.LoggerName == "System.Net.Http.HttpClient.test.ClientHandler";
            }));

            Assert.Equal("Sending HTTP request GET http://api.example.com/search?term=Western%20Australia", message.Message);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public void LoggingScopeHttpMessageHandler_LogsAbsoluteUri_Sync()
        {
            // Arrange
            var sink = new TestSink();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));

            serviceCollection
                .AddHttpClient("test")
                .ConfigurePrimaryHttpMessageHandler(() => new TestMessageHandler());

            var services = serviceCollection.BuildServiceProvider();

            var client = services.GetRequiredService<IHttpClientFactory>().CreateClient("test");


            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "http://api.example.com/search?term=Western%20Australia");

            client.Send(request);

            // Assert
            var messages = sink.Writes.ToArray();

            var message = Assert.Single(messages.Where(m =>
            {
                return
                    m.EventId == LoggingScopeHttpMessageHandler.Log.EventIds.PipelineStart &&
                    m.LoggerName == "System.Net.Http.HttpClient.test.LogicalHandler";
            }));

            Assert.Equal("Start processing HTTP request GET http://api.example.com/search?term=Western%20Australia", message.Message);
            Assert.Equal("HTTP GET http://api.example.com/search?term=Western%20Australia", message.Scope.ToString());
        }
#endif

        private class TestMessageHandler : HttpClientHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage();

                return Task.FromResult(response);
            }

#if NET5_0_OR_GREATER
            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => new();
#endif
        }
    }
}
