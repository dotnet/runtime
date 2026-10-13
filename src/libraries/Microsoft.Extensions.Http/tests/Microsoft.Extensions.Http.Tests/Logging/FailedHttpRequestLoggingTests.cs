// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        private const string ExceptionMessage = "Dummy error message";

        private static class EventIds
        {
            public static readonly EventId RequestStart = new EventId(100, "RequestStart");
            public static readonly EventId PipelineStart = new EventId(100, "RequestPipelineStart");
            public static readonly EventId RequestEnd = new EventId(101, "RequestEnd");
            public static readonly EventId PipelineEnd = new EventId(101, "RequestPipelineEnd");
            public static readonly EventId RequestFailed = new EventId(104, "RequestFailed");
            public static readonly EventId PipelineFailed = new EventId(104, "RequestPipelineFailed");
        }

        public static IEnumerable<object[]> FailureData()
        {
            foreach (Type exceptionType in new[]
            {
                typeof(HttpRequestException),
                typeof(InvalidOperationException),
                typeof(NotSupportedException),
                typeof(OperationCanceledException),
                typeof(TaskCanceledException),
            })
            {
                foreach (bool rejectBeforePrimaryHandler in new[] { false, true })
                {
                    yield return new object[] { exceptionType, true, rejectBeforePrimaryHandler };
#if NET
                    yield return new object[] { exceptionType, false, rejectBeforePrimaryHandler };
#endif
                }
            }
        }

        [Theory]
        [MemberData(nameof(FailureData))]
        public async Task FailedHttpRequestLogged(Type exceptionType, bool useAsync, bool rejectBeforePrimaryHandler)
        {
            Exception exception = Assert.IsAssignableFrom<Exception>(Activator.CreateInstance(exceptionType, ExceptionMessage));
            var primaryHandler = new FailMessageHandler(exception);
            var sink = new TestSink();

            using ServiceProvider services = CreateServices(
                sink,
                primaryHandler,
                rejectBeforePrimaryHandler ? new FailMessageHandler(exception) : null);
            using HttpClient client = services.GetRequiredService<IHttpClientFactory>().CreateClient("test");
            using var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

            Exception actualException = await Assert.ThrowsAnyAsync<Exception>(() => SendRequestAsync(client, request, useAsync));
            Assert.Same(exception, actualException);
            Assert.Equal(!rejectBeforePrimaryHandler, primaryHandler.Invoked);

            WriteContext[] messages = sink.Writes.ToArray();
            Assert.Single(messages, m => m.EventId == EventIds.PipelineStart && m.LoggerName == OuterLoggerName);
            Assert.Equal(rejectBeforePrimaryHandler ? 0 : 1,
                messages.Count(m => m.EventId == EventIds.RequestStart && m.LoggerName == InnerLoggerName));
            Assert.DoesNotContain(messages, m => m.EventId == EventIds.RequestEnd);

            WriteContext[] failures = messages.Where(m => m.EventId.Id == EventIds.RequestFailed.Id).ToArray();
            if (exception is OperationCanceledException)
            {
                Assert.Empty(failures);
                return;
            }

            Assert.Equal(rejectBeforePrimaryHandler ? 1 : 2, failures.Length);
            AssertFailureLog(
                Assert.Single(failures, m => m.LoggerName == OuterLoggerName),
                EventIds.PipelineFailed,
                exception);

            if (!rejectBeforePrimaryHandler)
            {
                AssertFailureLog(
                    Assert.Single(failures, m => m.LoggerName == InnerLoggerName),
                    EventIds.RequestFailed,
                    exception);
            }
        }

        [Theory]
        [InlineData(true, HttpStatusCode.OK)]
        [InlineData(true, HttpStatusCode.InternalServerError)]
#if NET
        [InlineData(false, HttpStatusCode.OK)]
        [InlineData(false, HttpStatusCode.InternalServerError)]
#endif
        public async Task HttpResponse_DoesNotLogFailure(bool useAsync, HttpStatusCode statusCode)
        {
            var sink = new TestSink();
            using ServiceProvider services = CreateServices(sink, new TestMessageHandler(_ => new HttpResponseMessage(statusCode)));
            using HttpClient client = services.GetRequiredService<IHttpClientFactory>().CreateClient("test");
            using var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

            using HttpResponseMessage response = await SendRequestAsync(client, request, useAsync);

            Assert.Equal(statusCode, response.StatusCode);
            AssertResponseLogs(sink);
        }

        [Theory]
        [InlineData(true)]
#if NET
        [InlineData(false)]
#endif
        public async Task ResponseContentFailure_DoesNotLogRequestFailure(bool useAsync)
        {
            var exception = new InvalidOperationException(ExceptionMessage);
            var sink = new TestSink();
            using ServiceProvider services = CreateServices(sink, new TestMessageHandler(_ => new HttpResponseMessage
            {
                Content = new FailContent(exception),
            }));
            using HttpClient client = services.GetRequiredService<IHttpClientFactory>().CreateClient("test");
            using var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

            using HttpResponseMessage response = await SendRequestAsync(client, request, useAsync);
            InvalidOperationException actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => response.Content.ReadAsStringAsync());

            Assert.Same(exception, actualException);
            AssertResponseLogs(sink);
        }

        private static ServiceProvider CreateServices(TestSink sink, HttpMessageHandler primaryHandler, DelegatingHandler? additionalHandler = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILoggerFactory>(_ => new TestLoggerFactory(sink, enabled: true));

            IHttpClientBuilder builder = serviceCollection.AddHttpClient("test")
                .ConfigurePrimaryHttpMessageHandler(() => primaryHandler);
            if (additionalHandler is not null)
            {
                builder.AddHttpMessageHandler(() => additionalHandler);
            }

            return serviceCollection.BuildServiceProvider();
        }

        private static async Task<HttpResponseMessage> SendRequestAsync(HttpClient client, HttpRequestMessage request, bool useAsync)
        {
#if NET
            if (!useAsync)
            {
                return client.Send(request, HttpCompletionOption.ResponseHeadersRead);
            }
#endif
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        }

        private static void AssertFailureLog(WriteContext message, EventId expectedEventId, Exception exception)
        {
            Assert.Equal(expectedEventId.Id, message.EventId.Id);
            Assert.Equal(expectedEventId.Name, message.EventId.Name);
            Assert.Equal(LogLevel.Information, message.LogLevel);
            Assert.Same(exception, message.Exception);

            var state = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(message.State);
            Assert.Collection(state,
                elapsed =>
                {
                    Assert.Equal("ElapsedMilliseconds", elapsed.Key);
                    Assert.True(Assert.IsType<double>(elapsed.Value) >= 0);
                },
                format =>
                {
                    Assert.Equal("{OriginalFormat}", format.Key);
                    Assert.Equal("HTTP request failed after {ElapsedMilliseconds}ms", format.Value);
                });
        }

        private static void AssertResponseLogs(TestSink sink)
        {
            WriteContext[] messages = sink.Writes.ToArray();
            Assert.DoesNotContain(messages, m => m.EventId.Id == EventIds.RequestFailed.Id);
            Assert.Single(messages, m => m.EventId == EventIds.RequestEnd && m.LoggerName == InnerLoggerName);
            Assert.Single(messages, m => m.EventId == EventIds.PipelineEnd && m.LoggerName == OuterLoggerName);
        }

        private sealed class FailMessageHandler : DelegatingHandler
        {
            private readonly Exception _exception;

            public FailMessageHandler(Exception exception) => _exception = exception;

            public bool Invoked { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Invoked = true;
                await Task.Yield();
                throw _exception;
            }

#if NET
            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Invoked = true;
                throw _exception;
            }
#endif
        }

        private sealed class FailContent : HttpContent
        {
            private readonly Exception _exception;

            public FailContent(Exception exception) => _exception = exception;

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => Task.FromException(_exception);

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }
        }
    }
}
