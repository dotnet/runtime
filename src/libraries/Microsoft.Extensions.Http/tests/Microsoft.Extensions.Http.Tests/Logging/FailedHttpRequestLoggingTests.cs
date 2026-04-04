// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Http.Tests.Logging
{
    public class FailedHttpRequestLoggingTests
    {
        private const string OuterLoggerName = "System.Net.Http.HttpClient.test.LogicalHandler";
        private const string InnerLoggerName = "System.Net.Http.HttpClient.test.ClientHandler";

        private static class EventIds
        {
            public static readonly EventId RequestFailed = new EventId(104, "RequestFailed");
            public static readonly EventId PipelineFailed = new EventId(104, "RequestPipelineFailed");
        }

        [Fact]
        public async Task FailedHttpRequestLogged()
        {
            // Arrange
            var sink = new TestSink();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));

            // Act
            serviceCollection
                .AddHttpClient("test")
                .ConfigurePrimaryHttpMessageHandler(() => new FailMessageHandler());

            // Assert
            var services = serviceCollection.BuildServiceProvider();

            var client = services.GetRequiredService<IHttpClientFactory>().CreateClient("test");

            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

            await Assert.ThrowsAsync<HttpRequestException>(async () =>
                await client.SendAsync(request));

            var messages = sink.Writes.ToArray();

            var requestFailedMessage = Assert.Single(messages, m =>
            {
                return
                    m.EventId == EventIds.RequestFailed &&
                    m.LoggerName == InnerLoggerName;
            });
            var pipelineFailedMessage = Assert.Single(messages, m =>
            {
                return
                    m.EventId == EventIds.PipelineFailed &&
                    m.LoggerName == OuterLoggerName;
            });
        }

        private const string ExceptionMessage = "Dummy error message";

        private class FailMessageHandler : HttpClientHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw new HttpRequestException(ExceptionMessage);
            }
        }
    }
}
